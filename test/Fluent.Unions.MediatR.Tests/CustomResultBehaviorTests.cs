using Fluent.Unions;
using Fluent.Unions.MediatR;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace MediatRDomain.Tests;

public class CustomResultBehaviorTests
{
    private ServiceProvider provider = null!;

    private IMediator Mediator => provider.GetRequiredService<IMediator>();

    [SetUp]
    public void SetUp()
    {
        RegisterHandler.Calls = 0;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblyContaining<CustomResultBehaviorTests>();
            configuration.AddOpenBehavior(typeof(ExceptionToResultBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationToResultBehavior<,>));
        });
        provider = services.BuildServiceProvider();
    }

    [TearDown]
    public void TearDown() => provider.Dispose();

    [Test]
    public async Task Exception_ThrownByTheHandler_IsWrappedByFromException()
    {
        var result = await Mediator.Send(new Find(-1));

        var error = result.GetError();
        Assert.That(error, Is.TypeOf<DomainException>().With.Message.EqualTo("negative"));
        Assert.That(error.InnerException, Is.TypeOf<InvalidOperationException>());
    }

    [Test]
    public async Task DomainException_ThrownByTheHandler_IsKept()
    {
        var result = await Mediator.Send(new Find(0));

        Assert.That(result.GetError(), Is.TypeOf<MissingException>());
    }

    [Test]
    public async Task Success_PassesThroughUnchanged()
    {
        var result = await Mediator.Send(new Find(5));

        Assert.That(result.GetValue(), Is.EqualTo(5));
    }

    [Test]
    public void Cancellation_IsNeverTurnedIntoAnError() =>
        Assert.ThrowsAsync<OperationCanceledException>(async () => await Mediator.Send(new Find(101)));

    [Test]
    public async Task ResultWithoutAValue_ExceptionBecomesTheError()
    {
        var failed = await Mediator.Send(new Run(-1));
        var succeeded = await Mediator.Send(new Run(1));

        Assert.That(failed.GetError(), Is.TypeOf<DomainException>().With.Message.EqualTo("negative"));
        Assert.That(succeeded.IsSuccess, Is.True);
    }

    [Test]
    public async Task Validation_InvalidRequest_ReturnsTheValidationErrorsWrappedByFromException()
    {
        var result = await Mediator.Send(new Register(null!));

        var error = result.GetError();
        Assert.That(error.InnerException, Is.TypeOf<ValidationErrors>());
        Assert.That(((ValidationErrors)error.InnerException!).Errors.Single().MemberName, Is.EqualTo("Name"));
        Assert.That(RegisterHandler.Calls, Is.Zero);
    }

    [Test]
    public async Task Validation_ValidRequest_RunsTheHandler()
    {
        var result = await Mediator.Send(new Register("Ada"));

        Assert.That(result.GetValue(), Is.EqualTo("Ada"));
        Assert.That(RegisterHandler.Calls, Is.EqualTo(1));
    }
}
