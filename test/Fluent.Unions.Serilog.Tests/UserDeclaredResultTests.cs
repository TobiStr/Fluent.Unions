using Fluent.Unions;
using Fluent.Unions.Serilog;
using NUnit.Framework;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace SerilogDomain.Tests;

public class UserDeclaredResultTests
{
    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }

    private static LogEventPropertyValue Log(object value)
    {
        var sink = new CollectingSink();

        using var logger = new LoggerConfiguration().Destructure.WithFluentUnions().WriteTo.Sink(sink).CreateLogger();
        logger.Information("Got {@Value}", value);

        return sink.Events.Single().Properties["Value"];
    }

    private static object? Scalar(LogEventPropertyValue value) => ((ScalarValue)value).Value;

    private static IReadOnlyDictionary<string, LogEventPropertyValue> Properties(LogEventPropertyValue value) =>
        ((StructureValue)value).Properties.ToDictionary(property => property.Name, property => property.Value);

    private static void AssertDomainError(LogEventPropertyValue value, string message)
    {
        var error = Properties(Properties(value)["Error"]);

        Assert.That(error.Keys, Is.EqualTo(new[] { "Type", "Message" }));
        Assert.That(Scalar(error["Type"]), Is.EqualTo(typeof(DomainException).FullName));
        Assert.That(Scalar(error["Message"]), Is.EqualTo(message));
    }

    [Test]
    public void SuccessLogsTheValue() =>
        Assert.That(Scalar(Log((Result<int>)7)), Is.EqualTo(7));

    [Test]
    public void SuccessWithAnObjectLogsTheDestructuredObject()
    {
        var properties = Properties(Log((Result<Customer>)new Customer("Ada")));

        Assert.That(Scalar(properties["Name"]), Is.EqualTo("Ada"));
    }

    [Test]
    public void ErrorLogsTypeAndMessage() =>
        AssertDomainError(Log((Result<int>)new DomainException("Bad")), "Bad");

    [Test]
    public void ValueLessSuccessLogsOk() =>
        Assert.That(Scalar(Log(Result.Ok)), Is.EqualTo("Ok"));

    [Test]
    public void ValueLessErrorLogsTypeAndMessage() =>
        AssertDomainError(Log((Result)new DomainException("Boom")), "Boom");

    [Test]
    public void DefaultLogsTheUninitializedError()
    {
        Assert.That(Properties(Log(default(Result<int>))).Keys, Is.EqualTo(new[] { "Error" }));
        Assert.That(Properties(Log(default(Result))).Keys, Is.EqualTo(new[] { "Error" }));
    }

    private sealed record Customer(string Name);
}
