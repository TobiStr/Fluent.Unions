using Fluent.Unions.Serilog;
using Fluent.Unions.Validation;
using NUnit.Framework;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Is = NUnit.Framework.Is;
using ILogger = Serilog.ILogger;

namespace Fluent.Unions.Serilog.Tests.Scenarios;

public record CheckoutRequest(string Customer, decimal Total, Option<string> Coupon);

public record Receipt(int PaymentId, decimal Total, Option<string> Coupon);

public sealed class CheckoutService(ILogger logger, Func<decimal, int> charge)
{
    public Result<Receipt> Checkout(CheckoutRequest request)
    {
        var outcome = Result<CheckoutRequest>.Ok(request)
            .Validate(r => r.Customer.EnsureNotEmpty(), r => r.Total.Ensure(t => t > 0, "Total must be positive."))
            .Chain(r => Result.Try(() => charge(r.Total)).Select(paymentId => new Receipt(paymentId, r.Total, r.Coupon)));

        outcome.Do(
            _ => logger.Information("Checkout finished {@Outcome}", outcome),
            error =>
            {
                if (error is ValidationErrors)
                {
                    logger.Warning("Checkout rejected {@Outcome}", outcome);
                }
                else
                {
                    logger.Error(error, "Checkout failed {@Outcome}", outcome);
                }
            });

        return outcome;
    }
}

/// <summary>Logging a checkout outcome structurally, so values, absent options and errors become queryable properties instead of text.</summary>
public class CheckoutLoggingScenario
{
    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }

    private CollectingSink sink = null!;
    private Logger logger = null!;

    [SetUp]
    public void SetUp()
    {
        sink = new CollectingSink();
        logger = new LoggerConfiguration().Destructure.WithFluentUnions().WriteTo.Sink(sink).CreateLogger();
    }

    [TearDown]
    public void TearDown() => logger.Dispose();

    private static IReadOnlyDictionary<string, LogEventPropertyValue> Outcome(LogEvent logEvent) =>
        ((StructureValue)logEvent.Properties["Outcome"]).Properties.ToDictionary(p => p.Name, p => p.Value);

    private static object? Scalar(LogEventPropertyValue value) => ((ScalarValue)value).Value;

    [Test]
    public void A_successful_checkout_logs_the_receipt_with_an_absent_coupon_as_null()
    {
        var service = new CheckoutService(logger, _ => 77);

        service.Checkout(new CheckoutRequest("Ada", 20m, Option.None)).Check();

        var logEvent = sink.Events.Single();
        var outcome = Outcome(logEvent);
        Assert.That(logEvent.Level, Is.EqualTo(LogEventLevel.Information));
        Assert.That(Scalar(outcome["PaymentId"]), Is.EqualTo(77));
        Assert.That(Scalar(outcome["Total"]), Is.EqualTo(20m));
        Assert.That(Scalar(outcome["Coupon"]), Is.Null);
    }

    [Test]
    public void A_present_coupon_is_logged_as_its_value()
    {
        var service = new CheckoutService(logger, _ => 78);

        service.Checkout(new CheckoutRequest("Ada", 20m, "SAVE5")).Check();

        Assert.That(Scalar(Outcome(sink.Events.Single())["Coupon"]), Is.EqualTo("SAVE5"));
    }

    [Test]
    public void A_rejected_request_is_a_warning_and_the_charge_never_runs()
    {
        var charges = 0;
        var service = new CheckoutService(logger, _ => ++charges);

        var result = service.Checkout(new CheckoutRequest("", 0m, Option.None));

        var logEvent = sink.Events.Single();
        var error = (StructureValue)Outcome(logEvent)["Error"];
        Assert.That(result.IsError, Is.True);
        Assert.That(logEvent.Level, Is.EqualTo(LogEventLevel.Warning));
        Assert.That(Scalar(error.Properties.Single(p => p.Name == "Type").Value), Is.EqualTo(typeof(ValidationErrors).FullName));
        Assert.That(charges, Is.EqualTo(0));
    }

    [Test]
    public void An_unexpected_failure_is_an_error_event_that_carries_the_exception()
    {
        var service = new CheckoutService(logger, _ => throw new TimeoutException("gateway timed out"));

        var result = service.Checkout(new CheckoutRequest("Ada", 20m, Option.None));

        var logEvent = sink.Events.Single();
        var error = (StructureValue)Outcome(logEvent)["Error"];
        Assert.That(result.GetError(), Is.TypeOf<TimeoutException>());
        Assert.That(logEvent.Level, Is.EqualTo(LogEventLevel.Error));
        Assert.That(logEvent.Exception, Is.SameAs(result.GetError()));
        Assert.That(Scalar(error.Properties.Single(p => p.Name == "Message").Value), Is.EqualTo("gateway timed out"));
    }
}
