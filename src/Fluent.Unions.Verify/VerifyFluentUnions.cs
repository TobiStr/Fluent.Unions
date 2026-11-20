namespace Fluent.Unions.Verify;

/// <summary>
/// Registers the Fluent.Unions converters with Verify.
/// </summary>
public static class VerifyFluentUnions
{
    private static int initialized;

    /// <summary>
    /// Makes Verify write results, options and loadables as readable text, and leave stack traces out of exceptions. Call it once, for example from a module initializer. Further calls do nothing.
    /// </summary>
    public static void Initialize()
    {
        if (Interlocked.Exchange(ref initialized, 1) == 1)
            return;

        VerifierSettings.AddExtraSettings(settings => settings.Converters.Add(new UnionConverter()));
        VerifierSettings.IgnoreStackTrace();
    }
}
