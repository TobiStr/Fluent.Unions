using NUnit.Framework;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Fluent.Unions.Dapper.Tests;

[TestFixture]
public class OptionHandlerTests
{
    private SqliteConnection _connection = null!;

    private enum Level
    {
        Low = 1,
        High = 2,
    }

    private record Person
    {
        public int Id { get; init; }

        public Option<string> Nickname { get; init; }

        public Option<int> Age { get; init; }
    }

    private record PositionalPerson(int Id, Option<string> Nickname, Option<int> Age);

    private class PersonRow
    {
        public int Id { get; set; }

        public Option<string> Nickname { get; set; }

        public Option<int> Age { get; set; }
    }

    [OneTimeSetUp]
    public void RegisterHandlers()
    {
        OptionHandlers.RegisterCommon();
        OptionHandlers.Register<Level>();
    }

    [SetUp]
    public void OpenDatabase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _connection.Execute("CREATE TABLE person (id INTEGER NOT NULL, nickname TEXT NULL, age INTEGER NULL)");
        _connection.Execute("INSERT INTO person VALUES (1, 'Ann', 30), (2, NULL, NULL)");
    }

    [TearDown]
    public void CloseDatabase() => _connection.Dispose();

    [Test]
    public void Parameter_with_a_value_is_sent_as_the_value()
    {
        var stored = _connection.ExecuteScalar<object>("SELECT @value", new { value = Option.Some(42) });

        Assert.That(stored, Is.EqualTo(42L));
    }

    [Test]
    public void Parameter_without_a_value_is_sent_as_null()
    {
        var stored = _connection.ExecuteScalar<object>("SELECT @value", new { value = (Option<int>)Option.None });

        Assert.That(stored, Is.Null.Or.EqualTo(DBNull.Value));
    }

    [Test]
    public void Default_option_parameter_is_sent_as_null()
    {
        var stored = _connection.ExecuteScalar<object>("SELECT @value", new { value = default(Option<string>) });

        Assert.That(stored, Is.Null.Or.EqualTo(DBNull.Value));
    }

    [Test]
    public void Parameter_filters_with_the_null_semantics_of_the_database()
    {
        var withAge = _connection.Query<int>("SELECT id FROM person WHERE age = @age", new { age = Option.Some(30) });
        var withoutAge = _connection.Query<int>("SELECT id FROM person WHERE age IS @age", new { age = (Option<int>)Option.None });

        Assert.That(withAge, Is.EqualTo(new[] { 1 }));
        Assert.That(withoutAge, Is.EqualTo(new[] { 2 }));
    }

    [Test]
    public void Dynamic_parameters_accept_an_option()
    {
        var parameters = new DynamicParameters();
        parameters.Add("some", Option.Some("x"));
        parameters.Add("none", (Option<string>)Option.None);

        _connection.Execute("INSERT INTO person (id, nickname) VALUES (3, @some), (4, @none)", parameters);

        var rows = _connection.Query<(int Id, string? Nickname)>("SELECT id, nickname FROM person WHERE id > 2 ORDER BY id");
        Assert.That(rows, Is.EqualTo(new (int, string?)[] { (3, "x"), (4, null) }));
    }

    [Test]
    public void Nullable_column_of_a_value_type_maps_to_none_and_some()
    {
        var ages = _connection.Query<Option<int>>("SELECT age FROM person ORDER BY id").ToList();

        Assert.That(ages[0].GetValue(), Is.EqualTo(30));
        Assert.That(ages[1].HasValue, Is.False);
    }

    [Test]
    public void Nullable_column_of_a_reference_type_maps_to_none_and_some()
    {
        var names = _connection.Query<Option<string>>("SELECT nickname FROM person ORDER BY id").ToList();

        Assert.That(names[0].GetValue(), Is.EqualTo("Ann"));
        Assert.That(names[1].HasValue, Is.False);
    }

    [Test]
    public void Empty_string_is_a_value()
    {
        var name = _connection.QuerySingle<Option<string>>("SELECT ''");

        Assert.That(name.GetValue(), Is.EqualTo(""));
    }

    [Test]
    public void Properties_of_a_row_class_are_materialized()
    {
        var rows = _connection.Query<PersonRow>("SELECT id, nickname, age FROM person ORDER BY id").ToList();

        Assert.That(rows[0].Nickname.GetValue(), Is.EqualTo("Ann"));
        Assert.That(rows[0].Age.GetValue(), Is.EqualTo(30));
        Assert.That(rows[1].Nickname.HasValue, Is.False);
        Assert.That(rows[1].Age.HasValue, Is.False);
    }

    [Test]
    public void Init_properties_of_a_record_are_materialized()
    {
        var rows = _connection.Query<Person>("SELECT id, nickname, age FROM person ORDER BY id").ToList();

        Assert.That(rows[0].Nickname.GetValue(), Is.EqualTo("Ann"));
        Assert.That(rows[0].Age.GetValue(), Is.EqualTo(30));
        Assert.That(rows[1].Nickname.HasValue, Is.False);
        Assert.That(rows[1].Age.HasValue, Is.False);
    }

    [Test]
    public void Positional_record_constructor_is_not_supported_by_dapper()
    {
        Assert.That(
            () => _connection.Query<PositionalPerson>("SELECT id, nickname, age FROM person").ToList(),
            Throws.InvalidOperationException);
    }

    [Test]
    public void Tuple_elements_are_materialized()
    {
        var row = _connection.QuerySingle<(int Id, Option<int> Age)>("SELECT id, age FROM person WHERE id = 2");

        Assert.That(row.Age.HasValue, Is.False);
    }

    [Test]
    public void Written_option_is_read_back()
    {
        _connection.Execute(
            "INSERT INTO person VALUES (@id, @nickname, @age)",
            new { id = 5, nickname = Option.Some("Bo"), age = (Option<int>)Option.None });

        var row = _connection.QuerySingle<Person>("SELECT id, nickname, age FROM person WHERE id = 5");

        Assert.That(row.Nickname.GetValue(), Is.EqualTo("Bo"));
        Assert.That(row.Age.HasValue, Is.False);
    }

    [Test]
    public void Wide_database_value_is_converted_to_the_option_type()
    {
        var value = _connection.QuerySingle<Option<short>>("SELECT 7");

        Assert.That(value.GetValue(), Is.EqualTo((short)7));
    }

    [Test]
    public void Common_types_round_trip()
    {
        var guid = Guid.NewGuid();
        var moment = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Unspecified);
        var offset = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(2));
        var span = TimeSpan.FromMinutes(90);
        var bytes = new byte[] { 1, 2, 3 };

        Assert.Multiple(() =>
        {
            Assert.That(RoundTrip(Option.Some(true)).GetValue(), Is.True);
            Assert.That(RoundTrip(Option.Some((byte)200)).GetValue(), Is.EqualTo((byte)200));
            Assert.That(RoundTrip(Option.Some((short)-5)).GetValue(), Is.EqualTo((short)-5));
            Assert.That(RoundTrip(Option.Some(123456789012L)).GetValue(), Is.EqualTo(123456789012L));
            Assert.That(RoundTrip(Option.Some(1.5f)).GetValue(), Is.EqualTo(1.5f));
            Assert.That(RoundTrip(Option.Some(2.25d)).GetValue(), Is.EqualTo(2.25d));
            Assert.That(RoundTrip(Option.Some(12.34m)).GetValue(), Is.EqualTo(12.34m));
            Assert.That(RoundTrip(Option.Some("text")).GetValue(), Is.EqualTo("text"));
            Assert.That(RoundTrip(Option.Some(guid)).GetValue(), Is.EqualTo(guid));
            Assert.That(RoundTrip(Option.Some(moment)).GetValue(), Is.EqualTo(moment));
            Assert.That(RoundTrip(Option.Some(offset)).GetValue(), Is.EqualTo(offset));
            Assert.That(RoundTrip(Option.Some(span)).GetValue(), Is.EqualTo(span));
            Assert.That(RoundTrip(Option.Some(bytes)).GetValue(), Is.EqualTo(bytes));
        });
    }

    [Test]
    public void Common_types_round_trip_none()
    {
        Assert.Multiple(() =>
        {
            Assert.That(RoundTrip((Option<bool>)Option.None).HasValue, Is.False);
            Assert.That(RoundTrip((Option<decimal>)Option.None).HasValue, Is.False);
            Assert.That(RoundTrip((Option<Guid>)Option.None).HasValue, Is.False);
            Assert.That(RoundTrip((Option<DateTimeOffset>)Option.None).HasValue, Is.False);
            Assert.That(RoundTrip((Option<byte[]>)Option.None).HasValue, Is.False);
        });
    }

    [Test]
    public void Enum_round_trips_through_its_underlying_value()
    {
        var stored = _connection.ExecuteScalar<object>("SELECT @value", new { value = Option.Some(Level.High) });

        Assert.That(stored, Is.EqualTo(2L));
        Assert.That(RoundTrip(Option.Some(Level.Low)).GetValue(), Is.EqualTo(Level.Low));
    }

    [Test]
    public void Register_handles_a_single_type()
    {
        OptionHandlers.Register<sbyte>();

        var value = _connection.QuerySingle<Option<sbyte>>("SELECT @value", new { value = Option.Some((sbyte)-3) });

        Assert.That(value.GetValue(), Is.EqualTo((sbyte)-3));
    }

    [Test]
    public void Unregistered_option_type_throws_when_the_query_runs()
    {
        Assert.That(
            () => _connection.ExecuteScalar<object>("SELECT @value", new { value = Option.Some('x') }),
            Throws.Exception);
    }

    [Test]
    public void Value_that_cannot_be_converted_throws()
    {
        Assert.That(() => _connection.QuerySingle<Option<int>>("SELECT 'abc'"), Throws.Exception);
    }

    [Test]
    public void Handler_parse_maps_null_and_dbnull_to_none()
    {
        var handler = new OptionTypeHandler<int>();

        Assert.That(handler.Parse(null!).HasValue, Is.False);
        Assert.That(handler.Parse(DBNull.Value).HasValue, Is.False);
    }

    private Option<T> RoundTrip<T>(Option<T> option) =>
        _connection.QuerySingle<Option<T>>("SELECT @value", new { value = option });
}
