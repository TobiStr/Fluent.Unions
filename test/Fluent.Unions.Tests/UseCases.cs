namespace Fluent.Unions.Tests;

public class UseCases
{
    [Test]
    public void SampleUseCase()
    {
        var result = CopyPerson();

        if (!result.IsSuccess)
            Console.WriteLine($"{nameof(CopyPerson)} was not successfull: {result.GetError().Message}");
    }

    [Test]
    public void SampleFailingUseCase()
    {
        var result = CopyPersonThrowing();

        if (!result.IsSuccess)
            Console.WriteLine($"{nameof(CopyPerson)} was not successfull: {result.GetError().Message}");
    }

    [Test]
    public void SamplePatternMatchingUseCase()
    {
        var okDescription = DescribePerson(GetPersonFromDatabase());
        var errorDescription = DescribePerson(GetPersonFromDatabaseThrowing());

        Assert.That(okDescription, Is.EqualTo("Test (20)"));
        Assert.That(errorDescription, Is.EqualTo("Person not available!"));
    }

    [Test]
    public void SampleFluentUseCase()
    {
        var log = new List<string>();

        var copied = GetPersonFromDatabase()
            .Ensure(person => person.Age >= 18, "Underage")
            .Do(person => log.Add($"Copying {person.Name}"))
            .SelectMany(InsertPersonToOtherDatabase);

        var failed = GetPersonFromDatabaseThrowing()
            .Do(person => log.Add($"Copying {person.Name}"))
            .SelectMany(InsertPersonToOtherDatabase)
            .Catch((InvalidDataException error) => new ApplicationException("Copy failed", error));

        Assert.True(copied.IsSuccess);
        Assert.That(failed.GetError().Message, Is.EqualTo("Copy failed"));
        Assert.That(log, Is.EqualTo(new[] { "Copying Test" }));
    }

    private static string DescribePerson(Result<Person> personResult) => personResult switch
    {
        Person person => $"{person.Name} ({person.Age})",
        Exception error => error.Message
    };

    private Result CopyPerson()
    {
        try
        {
            var personResult = GetPersonFromDatabase()
                .Check();

            return InsertPersonToOtherDatabase(personResult.GetValue())
                .Check();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private Result CopyPersonThrowing()
    {
        try
        {
            var personResult = GetPersonFromDatabaseThrowing()
                .Check();

            return InsertPersonToOtherDatabase(personResult.GetValue())
                .Check();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private Result<Person> GetPersonFromDatabase()
    {
        try
        {
            return new Person("Test", 20);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private Result<Person> GetPersonFromDatabaseThrowing()
    {
        try
        {
            throw new InvalidDataException("Person not available!");
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private Result InsertPersonToOtherDatabase(Person person)
    {
        try
        {
            return Result.Ok;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}

internal record Person(string Name, int Age);
