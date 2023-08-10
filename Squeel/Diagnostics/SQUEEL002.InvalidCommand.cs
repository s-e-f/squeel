using Microsoft.CodeAnalysis;

namespace Squeel.Diagnostics;

internal static partial class Descriptors
{
    public static readonly DiagnosticDescriptor QueryValidationFailed = new(
        id: "SQUEEL002", 
        title: "Query validation failed", 
        messageFormat: "{0}: {1}", 
        category: "Squeel", 
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "",
        helpLinkUri: "",
        customTags: Array.Empty<string>());
}

internal static partial class Errors
{
    public static Diagnostic QueryValidationFailed(Location location, string db, string message)
    {
        return Diagnostic.Create(Descriptors.QueryValidationFailed, location, db, message);
    }
}