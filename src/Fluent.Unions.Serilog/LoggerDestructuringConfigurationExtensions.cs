using Serilog;
using Serilog.Configuration;

namespace Fluent.Unions.Serilog;

/// <summary>
/// Registers the Fluent.Unions destructuring policy with Serilog.
/// </summary>
public static class LoggerDestructuringConfigurationExtensions
{
    /// <summary>
    /// Logs results, options and loadables as structured values when they are destructured with <c>{@Name}</c>.
    /// </summary>
    public static LoggerConfiguration WithFluentUnions(this LoggerDestructuringConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return configuration.With(new UnionDestructuringPolicy());
    }
}
