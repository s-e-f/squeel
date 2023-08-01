using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace Squeel.Generators;

[Generator(LanguageNames.CSharp)]
public sealed class SqueelInterpolatedStringHandlerGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(context =>
        {
            context.AddSource("SqueelInterpolatedStringHandler.g.cs", SourceText.From($$"""
                {{GeneratedFileOptions.Header}}

                #nullable enable

                namespace {{GeneratedFileOptions.Namespace}};

                {{GeneratedFileOptions.Attribute}}
                internal readonly record struct ParameterDescriptor
                {
                    {{GeneratedFileOptions.Attribute}}
                    public required Type Type { get; init; }

                    {{GeneratedFileOptions.Attribute}}
                    public required object? Value { get; init; }

                    {{GeneratedFileOptions.Attribute}}
                    public required string Name { get; init; }
                }

                [global::System.Runtime.CompilerServices.InterpolatedStringHandler]
                {{GeneratedFileOptions.Attribute}}
                internal readonly struct SqueelInterpolatedStringHandler
                {
                    private readonly int _literalLength;
                    private readonly int _formattedCount;

                    private readonly List<ParameterDescriptor> _parameters = new();

                    {{GeneratedFileOptions.Attribute}}
                    public IEnumerable<ParameterDescriptor> Parameters => _parameters;

                    private readonly List<Func<char, string>> _appenders = new();

                    {{GeneratedFileOptions.Attribute}}
                    public SqueelInterpolatedStringHandler(int literalLength, int formattedCount, out bool shouldAppend)
                    {
                        shouldAppend = true;
                        _literalLength = literalLength;
                        _formattedCount = formattedCount;
                    }

                    {{GeneratedFileOptions.Attribute}}
                    public void AppendLiteral(string literal)
                        => _appenders.Add(p => literal);

                    {{GeneratedFileOptions.Attribute}}
                    public void AppendFormatted<T>(T value, [global::System.Runtime.CompilerServices.CallerArgumentExpression(nameof(value))] string expression = "")
                    {
                        var parameterName = $"{expression.Split('.').Last()}";
                        _parameters.Add(new ParameterDescriptor
                        {
                            Name = parameterName,
                            Value = value,
                            Type = typeof(T)
                        });
                        _appenders.Add(p => $"{p}{parameterName}");
                    }

                    {{GeneratedFileOptions.Attribute}}
                    public string ToString(char parameterPrefix)
                    {
                        var handler = new global::System.Runtime.CompilerServices.DefaultInterpolatedStringHandler(_literalLength, _formattedCount);

                        foreach (var appender in _appenders)
                            handler.AppendLiteral(appender(parameterPrefix));

                        return handler.ToString();
                    }
                }
                """, Encoding.UTF8));
        });
    }
}
