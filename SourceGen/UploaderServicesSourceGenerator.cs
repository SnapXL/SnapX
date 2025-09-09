namespace SourceGen;

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[Generator]
public sealed class UploaderServicesSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext ctx)
    {
        var classDeclarations = ctx.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is ClassDeclarationSyntax,
            static (ctx, ct) =>
            {
                var cd = (ClassDeclarationSyntax)ctx.Node!;
                var sym = ctx.SemanticModel.GetDeclaredSymbol(cd, ct) as INamedTypeSymbol;
                if (sym == null) return null;
                var baseType = sym.BaseType;
                while (baseType != null)
                {
                    if (baseType.OriginalDefinition is INamedTypeSymbol nts &&
                        nts.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).StartsWith("global::UploaderService<", StringComparison.Ordinal))
                    {
                        return sym;
                    }
                    baseType = baseType.BaseType;
                }
                return null;
            });

        var distinctClasses = classDeclarations.Where(s => s != null).Collect();

        var factorySymbols = ctx.CompilationProvider.Select((comp, _) =>
        {
            foreach (var tree in comp.SyntaxTrees)
            {
                var root = tree.GetRoot();
                var candidates = root.DescendantNodes().OfType<ClassDeclarationSyntax>().Where(c => c.Identifier.Text == "UploaderFactory");
                foreach (var cd in candidates)
                {
                    var sym = comp.GetSemanticModel(tree).GetDeclaredSymbol(cd) as INamedTypeSymbol;
                    if (sym != null) return sym;
                }
            }
            var found = comp.GlobalNamespace.GetMembers().SelectMany(m => FindUploaderFactory(m)).FirstOrDefault();
            return found;

            static IEnumerable<INamedTypeSymbol> FindUploaderFactory(ISymbol s)
            {
                if (s is INamespaceSymbol ns)
                {
                    foreach (var m in ns.GetMembers())
                        foreach (var r in FindUploaderFactory(m))
                            yield return r;
                }
                else if (s is INamedTypeSymbol nts)
                {
                    if (nts.Name == "UploaderFactory") yield return nts;
                    foreach (var m in nts.GetTypeMembers())
                        foreach (var r in FindUploaderFactory(m))
                            yield return r;
                }
            }
        });

        var combined = distinctClasses.Combine(factorySymbols);

        ctx.RegisterSourceOutput(combined, (spc, tuple) =>
        {
            var classes = tuple.Left!.Where(x => x != null).Select(s => s!).ToArray();
            var factory = tuple.Right;
            var ns = factory?.ContainingNamespace.IsGlobalNamespace == false ? factory.ContainingNamespace.ToDisplayString() : null;
            var provider = spc;
            var writer = Generate(classes, ns);
            provider.AddSource("UploaderFactory.Generated.g.cs", SourceText.From(writer, System.Text.Encoding.UTF8));
        });
    }

    static string Generate(INamedTypeSymbol[] classes, string? factoryNamespace)
    {
        var groups = classes
            .Select(sym =>
            {
                var baseType = sym.BaseType!;
                while (baseType != null && !baseType.OriginalDefinition.ToDisplayString().StartsWith("global::UploaderService<", StringComparison.Ordinal))
                    baseType = baseType.BaseType;
                if (baseType == null) return null;
                var named = (INamedTypeSymbol)baseType;
                var arg = named.TypeArguments.First();
                return new
                {
                    ClassSymbol = sym,
                    ClassName = sym.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    KeyTypeName = arg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    KeyTypeShort = arg.Name
                };
            })
            .Where(x => x != null)
            .Select(x => x!)
            .GroupBy(x => x.KeyTypeName)
            .ToArray();

        var nsDecl = factoryNamespace != null ? $"namespace {factoryNamespace} {{" : string.Empty;
        var nsClose = factoryNamespace != null ? "}" : string.Empty;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine(nsDecl);
        sb.AppendLine("public static partial class UploaderFactory");
        sb.AppendLine("{");
        sb.AppendLine("    static UploaderFactory()");
        sb.AppendLine("    {");

        foreach (var g in groups)
        {
            foreach (var item in g)
            {
                var instVar = $"svc_{SanitizeName(item.ClassSymbol.Name)}";
                sb.AppendLine($"        try");
                sb.AppendLine("        {");
                sb.AppendLine($"            var {instVar} = new {item.ClassName}();");
                sb.AppendLine($"            AllServices.Add({instVar});");
                sb.AppendLine($"            AllGenericUploaderServices.Add({instVar});");
                var keyExpr = $"({item.KeyTypeName}){instVar}.EnumValue";
                var dictName = MapDictName(item.KeyTypeShort);
                sb.AppendLine($"            {dictName}[{keyExpr}] = {instVar};");
                sb.AppendLine("        }");
                sb.AppendLine("        catch (Exception) { }");
            }
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine(nsClose);

        return sb.ToString();

        static string SanitizeName(string s)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var c in s)
            {
                if (char.IsLetterOrDigit(c)) sb.Append(c);
                else sb.Append('_');
            }
            return sb.ToString();
        }

        static string MapDictName(string keyShort)
        {
            return keyShort switch
            {
                "ImageDestination" => "ImageUploaderServices",
                "TextDestination" => "TextUploaderServices",
                "FileDestination" => "FileUploaderServices",
                "UrlShortenerType" => "URLShortenerServices",
                "URLSharingServices" => "URLSharingServices",
                _ => $"UploaderServices_{keyShort}"
            };
        }
    }
}
