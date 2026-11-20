using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fluent.Unions.Analyzers.CodeFixes;

/// <summary>
/// Replaces <c>throw x;</c> with <c>return x;</c> in a function that returns a result.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ThrowToReturnCodeFix)), Shared]
public sealed class ThrowToReturnCodeFix : CodeFixProvider
{
    private const string Title = "Return the exception";

    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ThrowInResultMethodAnalyzer.DiagnosticId);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics.First();

        if (root?.FindNode(diagnostic.Location.SourceSpan) is ThrowStatementSyntax { Expression: not null } statement)
        {
            context.RegisterCodeFix(
                CodeAction.Create(Title, token => ReplaceAsync(context.Document, statement, token), Title),
                diagnostic);
        }
    }

    private static async Task<Document> ReplaceAsync(Document document, ThrowStatementSyntax statement, CancellationToken token)
    {
        var root = (await document.GetSyntaxRootAsync(token).ConfigureAwait(false))!;
        var returnKeyword = SyntaxFactory.Token(
            statement.ThrowKeyword.LeadingTrivia,
            SyntaxKind.ReturnKeyword,
            statement.ThrowKeyword.TrailingTrivia);
        var replacement = SyntaxFactory.ReturnStatement(returnKeyword, statement.Expression, statement.SemicolonToken);

        return document.WithSyntaxRoot(root.ReplaceNode(statement, replacement));
    }
}
