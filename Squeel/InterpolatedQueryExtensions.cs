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

    public static object ToRandomizedValue(this TypeInfo typeInfo)
    {
        return typeInfo.Type?.Name switch
        {
            "Int32" => new Random().Next(),
            "String" => Guid.NewGuid().ToString(),
            "Boolean" => new Random().Next() % 2 == 0,
            "DateTime" => DateTime.UtcNow,
            "Decimal" => new Random().Next(),
            "Double" => new Random().Next(),
            "Single" => new Random().Next(),
            "Int16" => new Random().Next(),
            "Int64" => new Random().Next(),
            "Byte" => new Random().Next(),
            "SByte" => new Random().Next(),
            "UInt16" => new Random().Next(),
            "UInt32" => new Random().Next(),
            "UInt64" => new Random().Next(),
            "Char" => Guid.NewGuid().ToString()[0],
            "Guid" => Guid.NewGuid(),
            "TimeSpan" => TimeSpan.FromTicks(new Random().Next()),
            "DateTimeOffset" => DateTimeOffset.UtcNow,
            //"Nullable`1" => ToRandomizedValue(),
            _ => throw new NotSupportedException($"Type {typeInfo.Type?.Name} is not supported"),
        };
    }
}