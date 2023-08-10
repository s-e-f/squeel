using Microsoft.CodeAnalysis;

namespace Squeel.Diagnostics;


internal static partial class Descriptors
{
    public static readonly DiagnosticDescriptor MissingSqueelConnectionString = new(
        id: "SQUEEL001",
        title: "Missing SqueelConnectionString",
        messageFormat: "Missing SqueelConnectionString",
        category: "Squeel",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: "",
        customTags: WellKnownDiagnosticTags.CompilationEnd);
}

internal static partial class Errors
{
    public static Diagnostic MissingSqueelConnectionString()
    {
        return Diagnostic.Create(Descriptors.MissingSqueelConnectionString, Location.None);
    }
}