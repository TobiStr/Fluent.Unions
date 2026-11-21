using Dapper;

namespace Fluent.Unions.Dapper;

/// <summary>
/// Registers the Dapper type handlers for <see cref="Option{T}"/>.
/// </summary>
public static class FluentUnionsDapper
{
    /// <summary>
    /// Registers a handler for <c>Option&lt;T&gt;</c>, for a <typeparamref name="T"/> beyond the common types. Calling it again replaces the handler with an equal one.
    /// </summary>
    public static void Register<T>() => SqlMapper.AddTypeHandler(new OptionTypeHandler<T>());

    /// <summary>
    /// Registers handlers for <c>Option&lt;T&gt;</c> of the common database types: bool, byte, short, int, long, float, double, decimal, string, Guid, DateTime, DateTimeOffset, TimeSpan and byte[]. Safe to call more than once.
    /// </summary>
    public static void Register()
    {
        Register<bool>();
        Register<byte>();
        Register<short>();
        Register<int>();
        Register<long>();
        Register<float>();
        Register<double>();
        Register<decimal>();
        Register<string>();
        Register<Guid>();
        Register<DateTime>();
        Register<DateTimeOffset>();
        Register<TimeSpan>();
        Register<byte[]>();
    }
}
