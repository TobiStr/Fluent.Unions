using Fluent.Unions.Orleans;
using Fluent.Unions.Validation;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Orleans;
using Orleans.Serialization;
using Is = NUnit.Framework.Is;

namespace Fluent.Unions.Orleans.Tests.Scenarios;

[GenerateSerializer]
public sealed record ContactPatch(
    [property: Id(0)] Option<string> Email,
    [property: Id(1)] Option<string> Phone);

[GenerateSerializer]
public sealed record ContactState(
    [property: Id(0)] string Email,
    [property: Id(1)] Option<string> Phone);

[GenerateSerializer]
public sealed record PatchReply(
    [property: Id(0)] Option<ContactState> State,
    [property: Id(1)] Option<string> Problem);

internal sealed class ContactGrain(Serializer<ContactState> stateSerializer)
{
    private ContactState _state = new("ada@example.com", Option.None);
    private byte[] _persisted = stateSerializer.SerializeToArray(new ContactState("ada@example.com", Option.None));

    public ContactState Stored => stateSerializer.Deserialize(_persisted)!;

    public PatchReply Patch(ContactPatch patch) =>
        Apply(patch)
            .Do(Persist)
            .Match(state => new PatchReply(state, Option.None), error => new PatchReply(Option.None, error.Message));

    private Result<ContactState> Apply(ContactPatch patch) =>
        Result<ContactState>.Ok(new ContactState(patch.Email.GetValueOrDefault(_state.Email), patch.Phone.Or(() => _state.Phone))).Validate(
            state => state.Email.EnsureMatchesEmail(name: "Email"),
            state => state.Phone.Match<string, Result>(phone => phone.EnsureMatchesPhoneNumber(name: "Phone"), () => Result.Ok));

    private void Persist(ContactState state)
    {
        _persisted = stateSerializer.SerializeToArray(state);
        _state = state;
    }
}

/// <summary>
/// A patch with optional fields crosses the grain boundary as <see cref="Option{T}"/>, and a failed patch becomes a reply that never carries a result.
/// </summary>
[TestFixture]
public class ContactGrainScenario
{
    private ServiceProvider _provider = null!;
    private ContactGrain _grain = null!;

    [OneTimeSetUp]
    public void SetUpProvider() =>
        _provider = new ServiceCollection().AddSerializer(builder => builder.AddFluentUnions()).BuildServiceProvider();

    [OneTimeTearDown]
    public void TearDownProvider() => _provider.Dispose();

    [SetUp]
    public void SetUpGrain() => _grain = new ContactGrain(_provider.GetRequiredService<Serializer<ContactState>>());

    private T OverTheWire<T>(T value)
    {
        var serializer = _provider.GetRequiredService<Serializer<T>>();
        return serializer.Deserialize(serializer.SerializeToArray(value))!;
    }

    private PatchReply Call(ContactPatch patch) => OverTheWire(_grain.Patch(OverTheWire(patch)));

    [Test]
    public void PatchWithOnlyAPhone_KeepsTheEmailAndStoresThePhone()
    {
        var reply = Call(new ContactPatch(Option.None, "+49 30 1234567"));

        Assert.That(reply.Problem.HasValue, Is.False);
        Assert.That(reply.State.GetValue().Email, Is.EqualTo("ada@example.com"));
        Assert.That(_grain.Stored.Phone.GetValue(), Is.EqualTo("+49 30 1234567"));
    }

    [Test]
    public void EmptyPatch_ChangesNothing()
    {
        Call(new ContactPatch(Option.None, "+49 30 1234567"));

        var reply = Call(new ContactPatch(Option.None, Option.None));

        Assert.That(reply.State.GetValue().Phone.GetValue(), Is.EqualTo("+49 30 1234567"));
        Assert.That(_grain.Stored.Email, Is.EqualTo("ada@example.com"));
    }

    [Test]
    public void InvalidPatch_RepliesWithTheProblemAndKeepsTheStoredState()
    {
        var reply = Call(new ContactPatch("not-an-email", "+49 30 1234567"));

        Assert.That(reply.State.HasValue, Is.False);
        Assert.That(reply.Problem.GetValue(), Is.EqualTo("Email must be a valid email address."));
        Assert.That(_grain.Stored, Is.EqualTo(new ContactState("ada@example.com", Option.None)));
    }

    [Test]
    public void ReplyIsHandledWithoutNullChecks()
    {
        var reply = Call(new ContactPatch("grace@example.com", Option.None));

        var summary = reply.State.Match(
            state => $"stored {state.Email}",
            () => $"rejected: {reply.Problem.GetValueOrDefault("unknown")}");

        Assert.That(summary, Is.EqualTo("stored grace@example.com"));
    }

    [Test]
    public void ResultAsGrainState_IsRefusedAndNeverLeaksTheError()
    {
        Result<ContactState> failed = new InvalidOperationException("Secret");
        var serializer = _provider.GetRequiredService<Serializer<Result<ContactState>>>();

        var exception = Assert.Throws<NotSupportedException>(() => serializer.SerializeToArray(failed))!;

        Assert.That(exception.Message, Does.Not.Contain("Secret"));
    }
}
