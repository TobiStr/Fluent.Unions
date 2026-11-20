using System.Linq.Expressions;
using FluentValidation;

namespace Fluent.Unions.FluentValidation;

/// <summary>
/// Rules for <see cref="Option{T}"/> properties.
/// </summary>
public static class OptionRuleExtensions
{
    /// <summary>
    /// Starts a rule for the value of an option property, like <c>RuleFor</c>. The rule runs only when the option
    /// has a value, so <c>None</c> is always valid. Failures are named after the property.
    /// </summary>
    public static IRuleBuilderInitial<T, TValue> RuleForSome<T, TValue>(
        this AbstractValidator<T> validator,
        Expression<Func<T, Option<TValue>>> expression)
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(expression);

        var name = PropertyName(expression.Body)
            ?? throw new ArgumentException("The expression must select a property, such as x => x.Nickname.", nameof(expression));
        var option = expression.Compile();

        return validator.RuleFor(instance => option(instance).GetValueOrDefault()!)
            .Configure(rule =>
            {
                rule.PropertyName = name;
                rule.ApplySharedCondition(context => option(context.InstanceToValidate).HasValue);
            });
    }

    private static string? PropertyName(Expression expression)
    {
        var names = new Stack<string>();

        while (expression is MemberExpression member)
        {
            names.Push(member.Member.Name);
            expression = member.Expression!;
        }

        return expression is ParameterExpression && names.Count > 0 ? string.Join(".", names) : null;
    }
}
