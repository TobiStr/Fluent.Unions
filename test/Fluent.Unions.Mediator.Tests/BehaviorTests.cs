using Mediator;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Fluent.Unions.Mediator.Tests;

public class BehaviorTests
{
    private ServiceProvider provider = null!;

    private IMediator Mediator => provider.GetRequiredService<IMediator>();

    [SetUp]
    public void SetUp()
    {
        Calls.Count = 0;

        var services = new ServiceCollection();
        services.AddMediator(options =>
        {
            options.PipelineBehaviors = [typeof(ExceptionToResultBehavior<,>), typeof(ValidationToResultBehavior<,>)];
        });
        provider = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown() => provider.Dispose();

    [Test]
    public async Task Exception_ThrownByTheHandler_IsReturnedAsAnError()
    {
        var result = await Mediator.Send(new GetNumber(-1));

        Assert.That(result.IsError, Is.True);
        Assert.That(result.GetError(), Is.TypeOf<InvalidOperationException>().With.Message.EqualTo("negative"));
    }

    [Test]
    public async Task Success_PassesThroughUnchanged()
    {
        var result = await Mediator.Send(new GetNumber(5));

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.GetValue(), Is.EqualTo(5));
    }

    [Test]
    public void Cancellation_IsNeverTurnedIntoAnError()
    {
        Assert.ThrowsAsync<OperationCanceledException>(async () => await Mediator.Send(new GetNumber(0)));
    }

    [Test]
    public async Task Exception_ThrownByAValidationAttribute_IsReturnedWhenTheExceptionBehaviorIsOuter()
    {
        var result = await Mediator.Send(new Broken("x"));

        Assert.That(result.GetError(), Is.TypeOf<InvalidOperationException>().With.Message.EqualTo("validator failed"));
    }

    [Test]
    public async Task Validation_InvalidRequest_ReturnsAValidationErrorPerMember()
    {
        var result = await Mediator.Send(new CreateUser(null!, 0));

        var errors = (ValidationErrors)result.GetError();
        Assert.That(errors.Errors.Select(error => error.MemberName), Is.EquivalentTo(new[] { "Name", "Age" }));
        Assert.That(errors.ToDictionary()["Age"], Has.Length.EqualTo(1));
    }

    [Test]
    public async Task Validation_InvalidRequest_SkipsTheHandler()
    {
        await Mediator.Send(new CreateUser("", 30));

        Assert.That(Calls.Count, Is.Zero);
    }

    [Test]
    public async Task Validation_ValidRequest_RunsTheHandler()
    {
        var result = await Mediator.Send(new CreateUser("Ada", 30));

        Assert.That(result.GetValue(), Is.EqualTo("Ada"));
        Assert.That(Calls.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task Validation_FailureWithoutMember_HasNoMemberName()
    {
        var result = await Mediator.Send(new Transfer("a", "a"));

        var error = ((ValidationErrors)result.GetError()).Errors.Single();
        Assert.That(error.MemberName, Is.Null);
        Assert.That(error.Message, Is.EqualTo("Source and target must differ."));
        Assert.That(Calls.Count, Is.Zero);
    }

    [Test]
    public async Task Validation_RequestWithoutRules_RunsTheHandler()
    {
        var result = await Mediator.Send(new GetNumber(3));

        Assert.That(result.GetValue(), Is.EqualTo(3));
    }

    [Test]
    public async Task Union_ResponseOutsideTheConstraint_IsHandledAndNotWrapped()
    {
        var shape = await Mediator.Send(new GetShape(2));

        Assert.That(shape.Value, Is.EqualTo(new Circle(2)));
        Assert.ThrowsAsync<InvalidOperationException>(async () => await Mediator.Send(new GetShape(-1)));
    }

    [Test]
    public async Task NonGenericResult_ExceptionBecomesTheError()
    {
        var failed = await Mediator.Send(new DoWork(-1));
        var succeeded = await Mediator.Send(new DoWork(1));

        Assert.That(failed.GetError(), Is.TypeOf<InvalidOperationException>().With.Message.EqualTo("negative"));
        Assert.That(succeeded.IsSuccess, Is.True);
    }

    [Test]
    public async Task Notification_IsPublishedWithTheBehaviorsRegistered()
    {
        await Mediator.Publish(new Announce("hi"));

        Assert.That(Calls.Count, Is.EqualTo(1));
    }
}
