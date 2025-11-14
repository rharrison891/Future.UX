using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Future.UX.SourceGenerators
{
    [Generator(LanguageNames.CSharp)]
    public sealed class AutoNotifyGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Detect field declarations starting with __
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

                    // Map of generated properties -> type
                    var fieldMap = group.ToDictionary(
                        f => char.ToUpper(f.FieldName[2]) + f.FieldName.Substring(3),
                        f => f.TypeName,
                        StringComparer.Ordinal);

                    var generatedProperties = new HashSet<string>(StringComparer.Ordinal);

                    // --- Detect commands ---
                    var commandMethods = classSyntax.Members
                        .OfType<MethodDeclarationSyntax>()
                        .Where(m =>
                            m.Identifier.Text.StartsWith("__") &&
                            !m.Identifier.Text.EndsWith("_Can", StringComparison.OrdinalIgnoreCase) &&
                            m.Modifiers.Any(md => md.IsKind(SyntaxKind.PrivateKeyword)))
                        .ToList();

                    var canMethods = classSyntax.Members
                        .OfType<MethodDeclarationSyntax>()
                        .Where(m =>
                            m.Identifier.Text.StartsWith("__") &&
                            m.Identifier.Text.EndsWith("_Can", StringComparison.OrdinalIgnoreCase) &&
                            m.Modifiers.Any(md => md.IsKind(SyntaxKind.PrivateKeyword)) &&
                            (model != null
                                ? (model.GetDeclaredSymbol(m)?.ReturnType.SpecialType == SpecialType.System_Boolean)
                                : (string.Equals(m.ReturnType?.ToString(), "bool", StringComparison.OrdinalIgnoreCase)
                                   || m.ReturnType?.ToString()?.EndsWith(".Boolean", StringComparison.OrdinalIgnoreCase) == true)))
                        .ToList();

                    var generatedCommands = new List<(string BackingField, bool HasCan)>();

                    // --- Field-backed properties ---
                    foreach (var field in group)
                    {
                        string propName = char.ToUpper(field.FieldName[2]) + field.FieldName.Substring(3);
                        bool alreadyExists = typeSymbol?.GetMembers(propName).Any(m => m.Kind == SymbolKind.Property) ?? false;
                        if (alreadyExists) continue;

                        var dependents = computedDependentsMap.TryGetValue(propName, out var deps)
                            ? deps.OrderBy(x => x).ToList()
                            : new List<string>();

                        generatedProperties.Add(propName);

                        sb.AppendLine($@"
        public {field.TypeName} {propName}
        {{
            get => {field.FieldName};
            set
            {{
                var oldValue = {field.FieldName};
                var newValue = value;
                if (System.Collections.Generic.EqualityComparer<{field.TypeName}>.Default.Equals(oldValue, newValue))
                    return;

                bool cancel = false;
                On{propName}Changing(oldValue, newValue, ref cancel);
                if (cancel) return;

                {field.FieldName} = newValue;

                On{propName}Changed(oldValue, newValue);

                OnPropertyChanged(nameof({propName}));");

                        foreach (var dep in dependents)
                            sb.AppendLine($"                OnPropertyChanged(nameof({dep}));");

                        // Raise CanExecuteChanged for commands with CanExecute
                        foreach (var cmd in generatedCommands)
                            if (cmd.HasCan)
                                sb.AppendLine($"                {cmd.BackingField}?.RaiseCanExecuteChanged();");

                        sb.AppendLine("            }\n        }");
                    }

                    // --- Generate commands ---
                    foreach (var method in commandMethods)
                    {
                        string methodName = method.Identifier.Text;
                        string commandBase = methodName.Substring(2);
                        string commandName = commandBase + "Command";
                        string backingField = "_" + char.ToLower(commandBase[0]) + commandBase.Substring(1) + "Command";

                        bool alreadyExists = typeSymbol?.GetMembers(commandName).Any() ?? false;
                        if (alreadyExists) continue;

                        bool isAsync = method.Modifiers.Any(md => md.IsKind(SyntaxKind.AsyncKeyword));
                        if (!isAsync && model != null)
                        {
                            var returnTypeSymbol = model.GetTypeInfo(method.ReturnType).Type;
                            if (returnTypeSymbol != null &&
                                returnTypeSymbol.ToDisplayString().Contains("Task", StringComparison.Ordinal))
                                isAsync = true;
                        }

                        var parameters = method.ParameterList.Parameters;
                        string? paramTypeName = null;
                        if (parameters.Count == 1)
                        {
                            ITypeSymbol? paramTypeSymbol = null;
                            if (model != null)
                                paramTypeSymbol = model.GetTypeInfo(parameters[0].Type!).Type;

                            paramTypeName = paramTypeSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                                .Replace("global::", string.Empty)
                                ?? parameters[0].Type?.ToString() ?? "object";
                        }
                        else if (parameters.Count > 1) continue;

                        // Matching CanExecute
                        MethodDeclarationSyntax? canMethod = canMethods
                            .FirstOrDefault(m =>
                                string.Equals(m.Identifier.Text, $"__{commandBase}_Can", StringComparison.OrdinalIgnoreCase) &&
                                m.ParameterList.Parameters.Count == parameters.Count);

                        bool hasCan = canMethod != null;
                        generatedCommands.Add((backingField, hasCan));

                        // Command type
                        string commandType = parameters.Count == 0
                            ? (isAsync ? "AsyncRelayCommand" : "RelayCommand")
                            : (isAsync ? $"AsyncRelayCommand<{paramTypeName}>" : $"RelayCommand<{paramTypeName}>");

                        // Factory expression
                        string factoryExpr = parameters.Count == 0
                            ? (hasCan ? $"new {commandType}({methodName}, {canMethod!.Identifier.Text})"
                                      : $"new {commandType}({methodName})")
                            : (hasCan ? $"new {commandType}({methodName}, {canMethod!.Identifier.Text})"
                                      : $"new {commandType}({methodName})");

                        sb.AppendLine($@"
        private {commandType}? {backingField};
        public {commandType} {commandName} => {backingField} ??= {factoryExpr};");
                    }

                    // --- Partial methods ---
                    if (generatedProperties.Count > 0)
                    {
                        sb.AppendLine();
                        foreach (var gp in generatedProperties.OrderBy(x => x))
                        {
                            var typeName = fieldMap[gp];
                            sb.AppendLine($"        partial void On{gp}Changing({typeName} oldValue, {typeName} newValue, ref bool cancel);");
                            sb.AppendLine($"        partial void On{gp}Changed({typeName} oldValue, {typeName} newValue);");
                        }
                    }

                    if (sb.Length == 0) continue;

                    var nsOpen = !string.IsNullOrEmpty(namespaceName) ? $"namespace {namespaceName}\n{{" : string.Empty;
                    var nsClose = !string.IsNullOrEmpty(namespaceName) ? "}" : string.Empty;

                    var src = $@"// <auto-generated>
//  ⚙️ This file was generated by Future.UX.SourceGenerators
//  Do not modify manually.
#nullable enable

using System;
using System.ComponentModel;
using System.Windows.Input;
using Future.UX.MVVM;

{nsOpen}
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
                    foreach (var tk in tokens) deps.Add(tk.ValueText);
                }

                directDeps[cp.Identifier.Text] = deps;
            }

            var reverse = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (var kvp in directDeps)
            {
                var computedName = kvp.Key;
                foreach (var dep in kvp.Value)
                {
                    if (!reverse.TryGetValue(dep, out var set))
                        reverse[dep] = set = new HashSet<string>(StringComparer.Ordinal);
                    set.Add(computedName);
                }
            }

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
                        result[key] = new HashSet<string>(StringComparer.Ordinal);

                    result[key].Add(cur);

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