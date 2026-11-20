using System.Diagnostics;

namespace Fluent.Unions.Tests;

public class ActivityExtensionsTests
{
    [Test]
    public void OkResultSetsStatusOk()
    {
        using var activity = new Activity("test").Start();

        var returned = activity.RecordResult(Result.Ok);

        Assert.That(returned, Is.SameAs(activity));
        Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Ok));
        Assert.That(activity.Events, Is.Empty);
    }

    [Test]
    public void OkResultOfTSetsStatusOk()
    {
        using var activity = new Activity("test").Start();

        Result<int> result = 5;
        var returned = activity.RecordResult(result);

        Assert.That(returned, Is.SameAs(activity));
        Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Ok));
        Assert.That(activity.Events, Is.Empty);
    }

    [Test]
    public void ErrorRecordsStatusAndException()
    {
        using var activity = new Activity("test").Start();

        var result = Result.Error(new InvalidOperationException("Cart is locked"));
        activity.RecordResult(result);

        AssertRecordedException(activity, "Cart is locked", typeof(InvalidOperationException));
    }

    [Test]
    public void ErrorOfTRecordsStatusAndException()
    {
        using var activity = new Activity("test").Start();

        Result<int> result = new KeyNotFoundException("Order 7 not found");
        activity.RecordResult(result);

        AssertRecordedException(activity, "Order 7 not found", typeof(KeyNotFoundException));
    }

    [Test]
    public void NonExceptionErrorIsWrappedInResultErrorException()
    {
        using var activity = new Activity("test").Start();

        Result<string, CheckoutError> result = (CheckoutError)new CartEmpty();
        activity.RecordResult(result);

        AssertRecordedException(activity, new ResultErrorException(result.GetError()).Message, typeof(ResultErrorException));
    }

    [Test]
    public void ValueOfTErrorResultSetsStatusOk()
    {
        using var activity = new Activity("test").Start();

        Result<string, CheckoutError> result = "order-7";
        activity.RecordResult(result);

        Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Ok));
        Assert.That(activity.Events, Is.Empty);
    }

    [Test]
    public void DefaultResultsSetErrorAndRecordTheException()
    {
        using var first = new Activity("first").Start();
        using var second = new Activity("second").Start();
        using var third = new Activity("third").Start();

        first.RecordResult(default(Result));
        second.RecordResult(default(Result<int>));
        third.RecordResult(default(Result<int, CheckoutError>));

        foreach (var activity in new[] { first, second, third })
        {
            Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Error));
            Assert.That(activity.Events.Count(), Is.EqualTo(1));
        }

        Assert.That(first.StatusDescription, Does.Contain("holds neither a value nor an error"));
        Assert.That(second.StatusDescription, Does.Contain("holds neither a value nor an error"));
    }

    [Test]
    public void NullActivityDoesNothing()
    {
        Activity? activity = null;
        Result<int> result = 5;
        Result<int, CheckoutError> data = 5;

        Assert.That(activity.RecordResult(Result.Ok), Is.Null);
        Assert.That(activity.RecordResult(result), Is.Null);
        Assert.That(activity.RecordResult(data), Is.Null);
        Assert.That(activity.RecordResult(default(Result<int>)), Is.Null);
    }

    private static void AssertRecordedException(Activity activity, string message, Type exceptionType)
    {
        Assert.That(activity.Status, Is.EqualTo(ActivityStatusCode.Error));
        Assert.That(activity.StatusDescription, Is.EqualTo(message));

        var recorded = activity.Events.Single();
        var tags = recorded.Tags.ToDictionary(tag => tag.Key, tag => tag.Value);
        Assert.That(recorded.Name, Is.EqualTo("exception"));
        Assert.That(tags["exception.type"], Is.EqualTo(exceptionType.FullName));
        Assert.That(tags["exception.message"], Is.EqualTo(message));
    }
}
