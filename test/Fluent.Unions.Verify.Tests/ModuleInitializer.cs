using System.Runtime.CompilerServices;

namespace Fluent.Unions.Verify.Tests;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize() => VerifyFluentUnions.Initialize();
}
