using Dapper;
using Fluent.Unions.Validation;
using Microsoft.Data.Sqlite;
using NUnit.Framework;
using Is = NUnit.Framework.Is;

namespace Fluent.Unions.Dapper.Tests.Scenarios;

public class Customer
{
    public int Id { get; set; }

    public string Email { get; set; } = "";

    public Option<string> Nickname { get; set; }

    public Option<int> Age { get; set; }
}

public sealed class DuplicateEmailException(string email) : Exception($"{email} is already registered");

public sealed class CustomerRepository(SqliteConnection connection)
{
    public Result<Customer> Add(string email, Option<string> nickname, Option<int> age) =>
        Result<string>.Ok(email)
            .Validate(e => e.EnsureMatchesEmail(), _ => age.Match(a => a >= 0 ? Result.Ok : new ValidationError("Age must not be negative.", "age"), () => Result.Ok))
            .Chain(e => Result.Try(() => connection.ExecuteScalar<int>(
                    "INSERT INTO customer (email, nickname, age) VALUES (@email, @nickname, @age) RETURNING id",
                    new { email = e, nickname, age }))
                .Catch<int, SqliteException>(error => error.SqliteErrorCode == 19 ? new DuplicateEmailException(e) : error))
            .Chain(id => Require(id));

    public Option<Customer> Find(int id) =>
        connection.QuerySingleOrDefault<Customer>("SELECT id, email, nickname, age FROM customer WHERE id = @id", new { id }).ToOption();

    public Result<Customer> Require(int id) => Find(id).ToResult(() => new KeyNotFoundException($"No customer {id}"));

    public IReadOnlyList<Customer> Search(Option<string> nickname) =>
        connection.Query<Customer>(
            "SELECT id, email, nickname, age FROM customer WHERE @nickname IS NULL OR nickname = @nickname ORDER BY id",
            new { nickname }).ToList();

    public Result Update(int id, Option<string> nickname, Option<int> age)
    {
        var rows = connection.Execute(
            "UPDATE customer SET nickname = COALESCE(@nickname, nickname), age = COALESCE(@age, age) WHERE id = @id",
            new { id, nickname, age });

        return rows == 0 ? new KeyNotFoundException($"No customer {id}") : Result.Ok;
    }
}

/// <summary>A repository where nullable columns are Option values, so a missing row, an absent column and a patch field are explicit.</summary>
public class CustomerRepositoryScenario
{
    private SqliteConnection connection = null!;
    private CustomerRepository repository = null!;

    [OneTimeSetUp]
    public void RegisterHandlers() => OptionHandlers.RegisterCommon();

    [SetUp]
    public void OpenDatabase()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        connection.Execute("CREATE TABLE customer (id INTEGER PRIMARY KEY AUTOINCREMENT, email TEXT NOT NULL UNIQUE, nickname TEXT NULL, age INTEGER NULL)");
        repository = new CustomerRepository(connection);
    }

    [TearDown]
    public void CloseDatabase() => connection.Dispose();

    [Test]
    public void Optional_columns_round_trip_as_options()
    {
        var ann = repository.Add("ann@example.com", "annie", Option.None).GetValue();
        var bob = repository.Add("bob@example.com", Option.None, 41).GetValue();

        Assert.That(ann.Nickname.GetValue(), Is.EqualTo("annie"));
        Assert.That(ann.Age.HasValue, Is.False);
        Assert.That(bob.Nickname.HasValue, Is.False);
        Assert.That(bob.Age.GetValue(), Is.EqualTo(41));
    }

    [Test]
    public void An_update_only_touches_the_provided_fields()
    {
        var id = repository.Add("ann@example.com", "annie", 30).GetValue().Id;

        repository.Update(id, Option.None, 31).Check();
        var afterAge = repository.Require(id).GetValue();
        repository.Update(id, Option.None, Option.None).Check();
        var untouched = repository.Require(id).GetValue();

        Assert.That(afterAge.Nickname.GetValue(), Is.EqualTo("annie"));
        Assert.That(afterAge.Age.GetValue(), Is.EqualTo(31));
        Assert.That(untouched.Age.GetValue(), Is.EqualTo(31));
    }

    [Test]
    public void A_missing_row_is_none_for_a_lookup_and_an_error_for_an_update()
    {
        Assert.That(repository.Find(99).HasValue, Is.False);
        Assert.That(repository.Update(99, "x", Option.None).GetError(), Is.TypeOf<KeyNotFoundException>());
        Assert.That(repository.Require(99).GetError().Message, Is.EqualTo("No customer 99"));
    }

    [Test]
    public void A_duplicate_email_is_a_domain_error_not_a_database_exception()
    {
        repository.Add("ann@example.com", Option.None, Option.None).Check();

        var duplicate = repository.Add("ann@example.com", Option.None, Option.None);

        Assert.That(duplicate.GetError(), Is.TypeOf<DuplicateEmailException>());
        Assert.That(repository.Search(Option.None), Has.Count.EqualTo(1));
    }

    [Test]
    public void Invalid_input_is_rejected_before_any_sql_runs()
    {
        var result = repository.Add("not-an-email", Option.None, -1);

        var errors = (ValidationErrors)result.GetError();
        Assert.That(errors.Errors, Has.Count.EqualTo(2));
        Assert.That(repository.Search(Option.None), Is.Empty);
    }

    [Test]
    public void A_none_filter_matches_every_row_and_a_some_filter_narrows_it()
    {
        repository.Add("ann@example.com", "annie", Option.None).Check();
        repository.Add("bob@example.com", Option.None, Option.None).Check();

        Assert.That(repository.Search(Option.None), Has.Count.EqualTo(2));
        Assert.That(repository.Search("annie").Select(c => c.Email), Is.EqualTo(new[] { "ann@example.com" }));
    }
}
