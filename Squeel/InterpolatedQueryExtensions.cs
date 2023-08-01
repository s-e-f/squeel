using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace Squeel;

public static class InterpolatedQueryExtensions
{
    public static string ToParameterizedString(this InterpolatedStringExpressionSyntax interpolated, out Dictionary<string, IdentifierNameSyntax> parameters)
    {
        var sql = new StringBuilder();
        parameters = new Dictionary<string, IdentifierNameSyntax>(interpolated.Contents.Count);
        foreach (var content in interpolated.Contents)
        {
            if (content is InterpolatedStringTextSyntax text)
            {
                sql.Append(text.TextToken.ValueText);
            }
            else if (content is InterpolationSyntax { Expression: IdentifierNameSyntax name })
            {
                sql.Append($"@{name.Identifier.ValueText}");
                parameters.Add(name.Identifier.ValueText, name);
            }
        }

        return sql.ToString();
    }

    public static object ToExampleValue(this TypeInfo typeInfo)
    {
        return typeInfo.Type?.Name switch
        {
            "Int32" => -32,
            "String" => "Hello World",
            "Boolean" => true,
            "DateTime" => new DateTime(2000, 1, 1),
            "Decimal" => 6.4m,
            "Double" => 3.2d,
            "Single" => 3.2f,
            "Int16" => -16,
            "Int64" => -64,
            "Byte" => -8,
            "SByte" => 8,
            "UInt16" => 16,
            "UInt32" => 32,
            "UInt64" => 64,
            "Char" => 'a',
            "Guid" => Guid.NewGuid(),
            "TimeSpan" => TimeSpan.FromTicks(100),
            "DateTimeOffset" => new DateTimeOffset(new DateTime(2000, 1, 1)),
            //"Nullable`1" => ToRandomizedValue(),
            _ => throw new NotSupportedException($"Type {typeInfo.Type?.Name} is not supported"),
        };
    }
}