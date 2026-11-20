using NUnit.Framework;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Fluent.Unions.Abstractions;
using Fluent.Unions.Serilog;

namespace Fluent.Unions.Serilog.Tests;

public record NotFound(string Id) : IFromException<NotFound>
{
    public static NotFound FromException(Exception exception) => new(exception.Message);
}

public record Customer(string Name, Option<string> Nickname);

public class UnionDestructuringPolicyTests
{
    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }

    private static (LogEventPropertyValue Value, string Rendered, LogEvent Event) Log(object? value, bool register = true)
    {
        var sink = new CollectingSink();
        var configuration = new LoggerConfiguration();
        if (register)
        {
            configuration.Destructure.WithFluentUnions();
        }

        using var logger = configuration.WriteTo.Sink(sink).CreateLogger();
        logger.Information("Got {@Value}", value);

        var logEvent = sink.Events.Single();
        return (logEvent.Properties["Value"], logEvent.RenderMessage(), logEvent);
    }

    private static object? Scalar(LogEventPropertyValue value) => ((ScalarValue)value).Value;

    private static IReadOnlyDictionary<string, LogEventPropertyValue> Properties(LogEventPropertyValue value) =>
        ((StructureValue)value).Properties.ToDictionary(property => property.Name, property => property.Value);

    private static void AssertException(LogEventPropertyValue value, Type type, string message)
    {
        var error = Properties(Properties(value)["Error"]);

        Assert.That(error.Keys, Is.EqualTo(new[] { "Type", "Message" }));
        Assert.That(Scalar(error["Type"]), Is.EqualTo(type.FullName));
        Assert.That(Scalar(error["Message"]), Is.EqualTo(message));
    }

    [Test]
    public void Option_WithValue_LogsTheValue()
    {
        var (value, rendered, _) = Log(Option.Some(42));

        Assert.That(Scalar(value), Is.EqualTo(42));
        Assert.That(rendered, Is.EqualTo("Got 42"));
    }

    [Test]
    public void Option_None_LogsNull()
    {
        var (value, rendered, _) = Log((Option<int>)Option.None);

        Assert.That(Scalar(value), Is.Null);
        Assert.That(rendered, Is.EqualTo("Got null"));
    }

    [Test]
    public void Option_Default_LogsNull()
    {
        var (value, _, _) = Log(default(Option<int>));

        Assert.That(Scalar(value), Is.Null);
    }

    [Test]
    public void Option_HoldingAnObject_LogsTheDestructuredObject()
    {
        var (value, _, _) = Log(Option.Some(new Customer("Ada", Option.None)));

        var properties = Properties(value);
        Assert.That(Scalar(properties["Name"]), Is.EqualTo("Ada"));
    }

    [Test]
    public void Option_AsAPropertyOfADestructuredObject_LogsTheValueOrNull()
    {
        var (withNickname, _, _) = Log(new Customer("Ada", "Countess"));
        var (withoutNickname, _, _) = Log(new Customer("Ada", Option.None));

        Assert.That(Scalar(Properties(withNickname)["Nickname"]), Is.EqualTo("Countess"));
        Assert.That(Scalar(Properties(withoutNickname)["Nickname"]), Is.Null);
    }

    [Test]
    public void Option_InACollection_LogsEachElement()
    {
        Option<int>[] options = [1, Option.None, 3];

        var (value, _, _) = Log(options);

        var elements = ((SequenceValue)value).Elements;
        Assert.That(elements.Select(Scalar), Is.EqualTo(new object?[] { 1, null, 3 }));
    }

    [Test]
    public void Result_Ok_LogsOk()
    {
        var (value, _, _) = Log(Result.Ok);

        Assert.That(Scalar(value), Is.EqualTo("Ok"));
    }

    [Test]
    public void Result_Error_LogsTypeAndMessage()
    {
        var (value, _, _) = Log((Result)new InvalidOperationException("Boom"));

        AssertException(value, typeof(InvalidOperationException), "Boom");
    }

    [Test]
    public void Result_Default_LogsTheUninitializedError()
    {
        var (value, _, _) = Log(default(Result));

        Assert.That(Properties(value).Keys, Is.EqualTo(new[] { "Error" }));
        Assert.That(Properties(Properties(value)["Error"]).Keys, Is.EqualTo(new[] { "Type", "Message" }));
    }

    [Test]
    public void ResultOfT_Success_LogsTheValue()
    {
        var (value, rendered, _) = Log(Result<int>.Ok(7));

        Assert.That(Scalar(value), Is.EqualTo(7));
        Assert.That(rendered, Is.EqualTo("Got 7"));
    }

    [Test]
    public void ResultOfT_SuccessWithAnObject_LogsTheDestructuredObject()
    {
        var (value, _, _) = Log(Result<Customer>.Ok(new Customer("Ada", "Countess")));

        var properties = Properties(value);
        Assert.That(Scalar(properties["Name"]), Is.EqualTo("Ada"));
        Assert.That(Scalar(properties["Nickname"]), Is.EqualTo("Countess"));
    }

    [Test]
    public void ResultOfT_Error_LogsTypeAndMessage()
    {
        var (value, rendered, _) = Log((Result<int>)new ArgumentException("Bad"));

        AssertException(value, typeof(ArgumentException), "Bad");
        Assert.That(rendered, Does.Contain("Error").And.Contain("Bad"));
    }

    [Test]
    public void ResultOfT_Error_DoesNotLogTheStackTraceOrSetTheException()
    {
        Exception thrown;
        try
        {
            throw new InvalidOperationException("Secret");
        }
        catch (Exception exception)
        {
            thrown = exception;
        }

        var (value, rendered, logEvent) = Log((Result<int>)thrown);

        Assert.That(Properties(Properties(value)["Error"]).Keys, Is.EqualTo(new[] { "Type", "Message" }));
        Assert.That(rendered, Does.Not.Contain("StackTrace").And.Not.Contain(nameof(ResultOfT_Error_DoesNotLogTheStackTraceOrSetTheException)));
        Assert.That(logEvent.Exception, Is.Null);
    }

    [Test]
    public void ResultOfT_Default_LogsTheUninitializedError()
    {
        var (value, _, _) = Log(default(Result<int>));

        Assert.That(Properties(Properties(value)["Error"]).Keys, Is.EqualTo(new[] { "Type", "Message" }));
    }

    [Test]
    public void ResultOfT_ValidationErrors_LogTheCombinedMessage()
    {
        var errors = new ValidationErrors([new ValidationError("Name is required.", "Name"), new ValidationError("Age is invalid.", "Age")]);

        var (value, _, _) = Log((Result<int>)errors);

        AssertException(value, typeof(ValidationErrors), "Name is required. Age is invalid.");
    }

    [Test]
    public void ResultOfTAndTError_Success_LogsTheValue()
    {
        var (value, _, _) = Log((Result<int, NotFound>)7);

        Assert.That(Scalar(value), Is.EqualTo(7));
    }

    [Test]
    public void ResultOfTAndTError_Error_LogsTheDestructuredErrorData()
    {
        var (value, _, _) = Log((Result<int, NotFound>)new NotFound("42"));

        var error = Properties(Properties(value)["Error"]);
        Assert.That(Scalar(error["Id"]), Is.EqualTo("42"));
    }

    [Test]
    public void ResultOfTAndTError_Default_LogsTheErrorFromFromException()
    {
        var (value, _, _) = Log(default(Result<int, NotFound>));

        Assert.That(Properties(Properties(value)["Error"]).Keys, Is.EqualTo(new[] { "Id" }));
    }

    [Test]
    public void Loadable_NotLoadedAndDefault_LogNotLoaded()
    {
        var (notLoaded, _, _) = Log((Loadable<int>)Loadable.NotLoaded);
        var (defaulted, _, _) = Log(default(Loadable<int>));

        Assert.That(Scalar(notLoaded), Is.EqualTo("NotLoaded"));
        Assert.That(Scalar(defaulted), Is.EqualTo("NotLoaded"));
    }

    [Test]
    public void Loadable_Loading_LogsLoading()
    {
        var (value, _, _) = Log((Loadable<int>)Loadable.Loading);

        Assert.That(Scalar(value), Is.EqualTo("Loading"));
    }

    [Test]
    public void Loadable_Loaded_LogsTheValue()
    {
        var (value, _, _) = Log((Loadable<int>)5);

        Assert.That(Scalar(value), Is.EqualTo(5));
    }

    [Test]
    public void Loadable_Faulted_LogsTypeAndMessage()
    {
        var (value, _, _) = Log((Loadable<int>)new TimeoutException("Slow"));

        AssertException(value, typeof(TimeoutException), "Slow");
    }

    [Test]
    public void OtherValues_AreLeftToSerilog()
    {
        var (value, _, _) = Log(new Customer("Ada", Option.None));

        Assert.That(((StructureValue)value).TypeTag, Is.EqualTo(nameof(Customer)));
    }

    [Test]
    public void OtherUnions_AreLeftToSerilog()
    {
        var policy = new UnionDestructuringPolicy();

        Assert.That(policy.TryDestructure(new Shape(new Circle(2)), new Factory(), out var result), Is.False);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void WithoutTheExtension_AnOptionIsNotDestructured()
    {
        var (value, _, _) = Log(Option.Some(42), register: false);

        Assert.That(value is ScalarValue { Value: 42 }, Is.False);
    }

    [Test]
    public void WithFluentUnions_ReturnsTheLoggerConfiguration()
    {
        var configuration = new LoggerConfiguration();

        Assert.That(configuration.Destructure.WithFluentUnions(), Is.SameAs(configuration));
    }

    [Test]
    public void WithFluentUnions_NullConfiguration_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => LoggerDestructuringConfigurationExtensions.WithFluentUnions(null!));
    }

    public record Circle(double Radius);

    public record Square(double Side);

    public union Shape(Circle, Square);

    private sealed class Factory : ILogEventPropertyValueFactory
    {
        public LogEventPropertyValue CreatePropertyValue(object? value, bool destructureObjects = false) => new ScalarValue(value);
    }
}
