using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Future.UX.SourceGenerators
{
    [Generator(LanguageNames.CSharp)]
    public sealed class AutoNotifyGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var fieldDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    static (node, _) => node is FieldDeclarationSyntax f &&
                                        f.Declaration.Variables.Any(v => v.Identifier.Text.StartsWith("__")),
                    static (ctx, ct) =>
                    {
                        var fieldDecl = (FieldDeclarationSyntax)ctx.Node;
                        var variable = fieldDecl.Declaration.Variables.First(v => v.Identifier.Text.StartsWith("__"));
                        if (ctx.SemanticModel.GetDeclaredSymbol(variable, ct) is not IFieldSymbol fieldSymbol)
                            return null;

                        var typeName = fieldSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                            .Replace("global::", string.Empty);

                        if (fieldDecl.Parent is ClassDeclarationSyntax classSyntax)
                            return new FieldInfo(classSyntax, variable.Identifier.Text, typeName);

                        return null;
                    })
                .Where(f => f is not null);

            var grouped = fieldDeclarations.Collect();
            var compilationAndGrouped = context.CompilationProvider.Combine(grouped);

            context.RegisterSourceOutput(compilationAndGrouped, static (spc, compAndFields) =>
            {
                var compilation = compAndFields.Left;
                var fields = compAndFields.Right;

                var byClass = fields
                    .Where(f => f is not null && f.ContainingClass is not null)
                    .GroupBy(f => f!.ContainingClass!)
                    .ToList();

                foreach (var group in byClass)
                {
                    var classSyntax = group.Key;
                    var className = classSyntax.Identifier.Text;
                    var namespaceName = GetNamespace(classSyntax);

                    SemanticModel? model = null;
                    INamedTypeSymbol? typeSymbol = null;
                    try
                    {
                        model = compilation.GetSemanticModel(classSyntax.SyntaxTree);
                        typeSymbol = model.GetDeclaredSymbol(classSyntax) as INamedTypeSymbol;
                    }
                    catch { }

                    var propertyDecls = classSyntax.Members.OfType<PropertyDeclarationSyntax>().ToList();
                    var computedProps = propertyDecls
                        .Where(p =>
                            p.ExpressionBody != null ||
                            (p.AccessorList?.Accessors.Any(a =>
                                a.Kind() == SyntaxKind.GetAccessorDeclaration &&
                                (a.Body != null || a.ExpressionBody != null)) ?? false))
                        .ToList();

                    var computedDependentsMap = BuildComputedDependencyMap(model, typeSymbol, computedProps);
                    var sb = new StringBuilder();

                    // --- Field-backed properties ---
                    foreach (var field in group)
                    {
                        string propName = char.ToUpper(field.FieldName[2]) + field.FieldName.Substring(3);
                        bool alreadyExists = typeSymbol?.GetMembers(propName).Any(m => m.Kind == SymbolKind.Property) ?? false;
                        if (alreadyExists) continue;

                        var dependents = computedDependentsMap.TryGetValue(propName, out var deps)
                            ? deps.OrderBy(x => x).ToList()
                            : new List<string>();

                        sb.AppendLine($@"
        public {field.TypeName} {propName}
        {{
            get => {field.FieldName};
            set
            {{
                if ({field.FieldName} != value)
                {{
                    {field.FieldName} = value;
                    OnPropertyChanged(nameof({propName}));");
                        foreach (var dep in dependents)
                            sb.AppendLine($"                    OnPropertyChanged(nameof({dep}));");

                        sb.AppendLine($@"                }}
            }}
        }}");
                    }

                    // --- Commands from private __ methods ---
                    var methodDecls = classSyntax.Members
                        .OfType<MethodDeclarationSyntax>()
                        .Where(m => m.Identifier.Text.StartsWith("__") &&
                                    m.Modifiers.Any(md => md.IsKind(SyntaxKind.PrivateKeyword)))
                        .ToList();

                    foreach (var method in methodDecls)
                    {
                        string methodName = method.Identifier.Text;           // e.g. "__Play"
                        string commandBase = methodName.Substring(2);         // e.g. "Play"
                        string commandName = commandBase + "Command";         // e.g. "PlayCommand"
                        string backingField = "_" + char.ToLower(commandBase[0]) + commandBase.Substring(1) + "Command"; // _playCommand

                        bool alreadyExists = typeSymbol?.GetMembers(commandName).Any() ?? false;
                        if (alreadyExists) continue;

                        // Is async? use semantic info if available
                        bool isAsync = false;
                        if (method.Modifiers.Any(md => md.IsKind(SyntaxKind.AsyncKeyword)))
                            isAsync = true;
                        else if (model is not null)
                        {
                            // try to resolve return type symbol
                            var returnTypeSymbol = model.GetTypeInfo(method.ReturnType).Type;
                            if (returnTypeSymbol != null && returnTypeSymbol.ToDisplayString().Contains("Task", StringComparison.Ordinal))
                                isAsync = true;
                        }
                        else
                        {
                            // fallback textual check
                            isAsync = method.ReturnType.ToString().Contains("Task", StringComparison.Ordinal);
                        }

                        var parameters = method.ParameterList.Parameters;
                        string commandType;
                        string commandTypeWithGenerics = "";
                        string factoryExpr;

                        if (parameters.Count == 0)
                        {
                            commandType = isAsync ? "AsyncRelayCommand" : "RelayCommand";
                            factoryExpr = $"new {commandType}({methodName})";
                        }
                        else if (parameters.Count == 1)
                        {
                            // resolve parameter type nicely via semantic model if possible
                            ITypeSymbol? paramTypeSymbol = null;
                            if (model is not null)
                            {
                                paramTypeSymbol = model.GetTypeInfo(parameters[0].Type!).Type;
                            }

                            string paramTypeName = paramTypeSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                                .Replace("global::", string.Empty)
                                ?? parameters[0].Type?.ToString() ?? "object";

                            commandType = isAsync ? $"AsyncRelayCommand<{paramTypeName}>" : $"RelayCommand<{paramTypeName}>";
                            factoryExpr = $"new {commandType}({methodName})";
                            commandTypeWithGenerics = commandType;
                        }
                        else
                        {
                            // skip multi-parameter methods for now
                            continue;
                        }

                        // generate backing field + lazy property. Backing field must be a field of type ICommand?:
                        sb.AppendLine($@"
        private System.Windows.Input.ICommand? {backingField};
        public System.Windows.Input.ICommand {commandName} => {backingField} ??= {factoryExpr};");
                    }

                    if (sb.Length == 0) continue;

                    var hasNamespace = !string.IsNullOrEmpty(namespaceName);
                    var nsOpen = hasNamespace ? $"namespace {namespaceName}\n{{" : string.Empty;
                    var nsClose = hasNamespace ? "}" : string.Empty;

                    var src = $@"// <auto-generated>
//  ⚙️ This file was generated by Future.UX.SourceGenerators
//  Do not modify manually.
#nullable enable

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;
using Future.UX.MVVM;

{nsOpen}
    [DebuggerNonUserCode]
    public partial class {className} : INotifyPropertyChanged
    {{
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

{sb}
    }}
{nsClose}";

                    spc.AddSource($"{className}.AutoNotify.g.cs", SourceText.From(src, Encoding.UTF8));
                }
            });
        }

        // --- Computed dependency detection ---
        private static Dictionary<string, HashSet<string>> BuildComputedDependencyMap(
            SemanticModel? model,
            INamedTypeSymbol? typeSymbol,
            List<PropertyDeclarationSyntax> computedProps)
        {
            var directDeps = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

            foreach (var cp in computedProps)
            {
                var deps = new HashSet<string>(StringComparer.Ordinal);

                if (model != null && typeSymbol != null)
                {
                    var nodes = cp.DescendantNodes();
                    foreach (var id in nodes.OfType<IdentifierNameSyntax>())
                    {
                        var sym = model.GetSymbolInfo(id).Symbol;
                        if (sym is IPropertySymbol psym &&
                            SymbolEqualityComparer.Default.Equals(psym.ContainingType, typeSymbol))
                            deps.Add(psym.Name);
                    }

                    foreach (var ma in nodes.OfType<MemberAccessExpressionSyntax>())
                    {
                        if (ma.Name is IdentifierNameSyntax nameNode)
                        {
                            var sym = model.GetSymbolInfo(nameNode).Symbol;
                            if (sym is IPropertySymbol psym &&
                                SymbolEqualityComparer.Default.Equals(psym.ContainingType, typeSymbol))
                                deps.Add(psym.Name);
                        }
                    }
                }
                else
                {
                    var tokens = cp.DescendantTokens().Where(t => t.IsKind(SyntaxKind.IdentifierToken));
                    foreach (var tk in tokens)
                        deps.Add(tk.ValueText);
                }

                directDeps[cp.Identifier.Text] = deps;
            }

            // reverse graph: dependency -> computed props that directly depend on it
            var reverse = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (var kvp in directDeps)
            {
                var computedName = kvp.Key;
                foreach (var dep in kvp.Value)
                {
                    if (!reverse.TryGetValue(dep, out var set))
                    {
                        set = new HashSet<string>(StringComparer.Ordinal);
                        reverse[dep] = set;
                    }
                    set.Add(computedName);
                }
            }

            // compute transitive dependents
            var result = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var allKeys = new HashSet<string>(directDeps.Keys, StringComparer.Ordinal);
            allKeys.UnionWith(reverse.Keys);

            foreach (var key in allKeys)
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                var q = new Queue<string>();

                if (reverse.TryGetValue(key, out var starters))
                    foreach (var s in starters) q.Enqueue(s);

                while (q.Count > 0)
                {
                    var cur = q.Dequeue();
                    if (!seen.Add(cur)) continue;

                    if (!result.TryGetValue(key, out var depsForKey))
                    {
                        depsForKey = new HashSet<string>(StringComparer.Ordinal);
                        result[key] = depsForKey;
                    }

                    depsForKey.Add(cur);

                    if (reverse.TryGetValue(cur, out var next))
                        foreach (var n in next)
                            if (!seen.Contains(n))
                                q.Enqueue(n);
                }
            }

            return result;
        }

        private static string? GetNamespace(ClassDeclarationSyntax classSyntax)
        {
            SyntaxNode? node = classSyntax.Parent;
            while (node != null)
            {
                if (node is NamespaceDeclarationSyntax ns) return ns.Name.ToString();
                if (node is FileScopedNamespaceDeclarationSyntax fns) return fns.Name.ToString();
                node = node.Parent;
            }
            return null;
        }

        private sealed class FieldInfo
        {
            public ClassDeclarationSyntax? ContainingClass { get; }
            public string FieldName { get; }
            public string TypeName { get; }

            public FieldInfo(ClassDeclarationSyntax? parent, string name, string type)
            {
                ContainingClass = parent;
                FieldName = name;
                TypeName = type;
            }
        }
    }
}