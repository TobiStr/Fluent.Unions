using Fluent.Unions.Validation;
using Is = NUnit.Framework.Is;

namespace Fluent.Unions.Scenarios.Tests.DocumentApproval;

public record Draft(string Body);

public record InReview(string Body, string Author);

public record Approved(string Body, string Author, string Approver);

public record Rejected(string Body, string Author, string Reason);

public record Published(string Body, DateTimeOffset At);

public union DocumentState(Draft, InReview, Approved, Rejected, Published);

public record Submit(string Author);

public record Approve(string Approver);

public record Reject(string Approver, string Reason);

public record Publish;

public record Revise(string Body);

public union DocumentCommand(Submit, Approve, Reject, Publish, Revise);

public record AuditEntry(DateTimeOffset At, string Command, string From, string Outcome);

public class IllegalTransitionException(string state, string command)
    : InvalidOperationException($"{command} is not allowed when the document is {state}.");

public class SelfApprovalException(string author) : UnauthorizedAccessException($"{author} cannot approve their own document.");

/// <summary>
/// Moves a document through review. Shows state machines as unions: states and commands are closed sets, an
/// illegal move is an error value, and every attempt, successful or not, lands in the audit trail.
/// </summary>
public sealed class ApprovalWorkflow(TimeProvider clock)
{
    private readonly List<AuditEntry> trail = [];

    public IReadOnlyList<AuditEntry> Trail => trail;

    public Result<DocumentState> Execute(DocumentState state, DocumentCommand command) =>
        Apply(state, command).Do(
            next => trail.Add(new AuditEntry(clock.GetUtcNow(), Name(command), Name(state), $"-> {Name(next)}")),
            error => trail.Add(new AuditEntry(clock.GetUtcNow(), Name(command), Name(state), error.Message)));

    public Result<DocumentState> Run(DocumentState start, params DocumentCommand[] commands) =>
        commands.Aggregate((Result<DocumentState>)start, (current, command) => current.Chain(state => Execute(state, command)));

    private Result<DocumentState> Apply(DocumentState state, DocumentCommand command) => (state, command) switch
    {
        (Draft draft, Submit submit) => new DocumentState(new InReview(draft.Body, submit.Author)),
        (Draft or Rejected, Revise revise) => new DocumentState(new Draft(revise.Body)),
        (InReview review, Approve approve) when approve.Approver == review.Author => new SelfApprovalException(review.Author),
        (InReview review, Approve approve) => new DocumentState(new Approved(review.Body, review.Author, approve.Approver)),
        (InReview review, Reject reject) => reject.Reason.EnsureNotWhiteSpace(name: "Reason")
            .Select(reason => new DocumentState(new Rejected(review.Body, review.Author, reason))),
        (Approved approved, Publish) => new DocumentState(new Published(approved.Body, clock.GetUtcNow())),
        _ => new IllegalTransitionException(Name(state), Name(command))
    };

    public static string Name(DocumentState state) => state switch
    {
        Draft => "Draft",
        InReview => "InReview",
        Approved => "Approved",
        Rejected => "Rejected",
        Published => "Published"
    };

    public static string Name(DocumentCommand command) => command switch
    {
        Submit => "Submit",
        Approve => "Approve",
        Reject => "Reject",
        Publish => "Publish",
        Revise => "Revise"
    };

    public static bool IsEditable(DocumentState state) => state.Is<Draft>() || state.Is<Rejected>();

    public static Option<DateTimeOffset> PublishedAt(DocumentState state) => state.As<Published>().Select(published => published.At);
}

public class DocumentApprovalScenarioTests
{
    private static readonly DocumentState NewDocument = new Draft("Release notes");

    private readonly ManualTimeProvider clock = new(new DateTimeOffset(2026, 11, 20, 9, 0, 0, TimeSpan.Zero));

    [Test]
    public void ADocumentIsPublishedAfterReviewAndEveryStepIsAudited()
    {
        var workflow = new ApprovalWorkflow(clock);

        var result = workflow.Run(NewDocument, new Submit("ada"), new Approve("grace"), new Publish());

        Assert.That(ApprovalWorkflow.PublishedAt(result.GetValue()).GetValue(), Is.EqualTo(clock.GetUtcNow()));
        Assert.That(workflow.Trail.Select(entry => entry.Outcome), Is.EqualTo(new[] { "-> InReview", "-> Approved", "-> Published" }));
    }

    [Test]
    public void AuthorsCannotApproveTheirOwnDocument()
    {
        var workflow = new ApprovalWorkflow(clock);

        var result = workflow.Run(NewDocument, new Submit("ada"), new Approve("ada"));

        Assert.That(result.GetError(), Is.TypeOf<SelfApprovalException>());
        Assert.That(workflow.Trail[^1].Outcome, Is.EqualTo("ada cannot approve their own document."));
    }

    [Test]
    public void ARejectionNeedsAReasonAndCanBeRevised()
    {
        var workflow = new ApprovalWorkflow(clock);
        var inReview = workflow.Run(NewDocument, new Submit("ada")).GetValue();

        var blank = workflow.Execute(inReview, new Reject("grace", " "));
        var rejected = workflow.Execute(inReview, new Reject("grace", "Missing sources")).GetValue();
        var revised = workflow.Execute(rejected, new Revise("Release notes v2")).GetValue();

        Assert.That(((ValidationError)blank.GetError()).MemberName, Is.EqualTo("Reason"));
        Assert.That(ApprovalWorkflow.IsEditable(rejected), Is.True);
        Assert.That(ApprovalWorkflow.IsEditable(inReview), Is.False);
        Assert.That(revised.As<Draft>().GetValue().Body, Is.EqualTo("Release notes v2"));
    }

    [Test]
    public void AnIllegalMoveIsAnErrorAndStopsTheRun()
    {
        var workflow = new ApprovalWorkflow(clock);

        var result = workflow.Run(NewDocument, new Publish(), new Submit("ada"));

        Assert.That(result.GetError().Message, Is.EqualTo("Publish is not allowed when the document is Draft."));
        Assert.That(workflow.Trail, Has.Count.EqualTo(1));
    }

    [Test]
    public void TheAuditTrailKeepsTheTimeOfEachAttempt()
    {
        var workflow = new ApprovalWorkflow(clock);
        var first = clock.GetUtcNow();

        var inReview = workflow.Execute(NewDocument, new Submit("ada")).GetValue();
        clock.Advance(TimeSpan.FromHours(2));
        workflow.Execute(inReview, new Approve("ada"));

        Assert.That(workflow.Trail.Select(entry => entry.At), Is.EqualTo(new[] { first, first.AddHours(2) }));
        Assert.That(workflow.Trail.Select(entry => entry.From), Is.EqualTo(new[] { "Draft", "InReview" }));
    }

    [Test]
    public void AnUnpublishedDocumentHasNoPublicationTime()
    {
        Assert.That(ApprovalWorkflow.PublishedAt(NewDocument).HasValue, Is.False);
    }
}
