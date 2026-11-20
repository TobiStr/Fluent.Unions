using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Fluent.Unions.Analyzers;

/// <summary>
/// Renames the called operator to the one the diagnostic names, keeping the type arguments.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RenameOperatorCodeFix))]
[Shared]
public sealed class RenameOperatorCodeFix : CodeFixProvider
{
    /// <inheritdoc />
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
        Descriptors.AsyncLambdaInSyncOperator.Id,
        Descriptors.AsyncVoidLambda.Id,
        Descriptors.NestedSelect.Id);

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var diagnostic = context.Diagnostics[0];

        if (!diagnostic.Properties.TryGetValue(Descriptors.ReplacementProperty, out var replacement) || replacement is null)
            return;

        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (root?.FindNode(diagnostic.Location.SourceSpan) is not SimpleNameSyntax name || !IsInvoked(name))
            return;

        var title = $"Use '{replacement}'";
        context.RegisterCodeFix(
            CodeAction.Create(
                title,
                _ => Task.FromResult(context.Document.WithSyntaxRoot(root.ReplaceNode(name, Rename(name, replacement)))),
                title),
            diagnostic);
    }

    private static SimpleNameSyntax Rename(SimpleNameSyntax name, string replacement) =>
        name.WithIdentifier(SyntaxFactory.Identifier(replacement).WithTriviaFrom(name.Identifier));

    private static bool IsInvoked(SimpleNameSyntax name) =>
        name.Parent switch
        {
            InvocationExpressionSyntax invocation => invocation.Expression == name,
            MemberAccessExpressionSyntax { Parent: InvocationExpressionSyntax invocation } access =>
                access.Name == name && invocation.Expression == access,
            MemberBindingExpressionSyntax { Parent: InvocationExpressionSyntax invocation } binding =>
                binding.Name == name && invocation.Expression == binding,
            _ => false
        };
}
