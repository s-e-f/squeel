using Microsoft.CodeAnalysis;

internal static class Diagnostics
{
    internal static readonly DiagnosticDescriptor MissingSqueelConnectionStringDescriptor = new(
        "SQUEEL001",
        "Missing SqueelConnectionString",
        "Missing SqueelConnectionString",
        "Squeel",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: WellKnownDiagnosticTags.CompilationEnd
    );
}