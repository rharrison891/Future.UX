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
    public sealed class CommandGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Find classes that contain methods starting with "__"
            var classProvider = context.SyntaxProvider
                .CreateSyntaxProvider(
                    static (node, _) =>
                        node is ClassDeclarationSyntax cd &&
                        cd.Members.OfType<MethodDeclarationSyntax>()
                            .Any(m => m.Identifier.Text.StartsWith("__")),
                    static (ctx, ct) => (ClassDeclarationSyntax)ctx.Node)
                .Where(c => c is not null);

            var compilationAndClasses =
                context.CompilationProvider.Combine(classProvider.Collect());

            context.RegisterSourceOutput(compilationAndClasses, static (spc, tuple) =>
            {
                var compilation = tuple.Left;
                var classes = tuple.Right;

                foreach (var classSyntax in classes)
                {
                    var semanticModel = compilation.GetSemanticModel(classSyntax.SyntaxTree);
                    if (semanticModel.GetDeclaredSymbol(classSyntax) is not INamedTypeSymbol typeSymbol)
                        continue;

                    var commands = GetCommandMethods(classSyntax, semanticModel);

                    if (commands.Count == 0)
                        continue;

                    var ns = GetNamespace(classSyntax);
                    var className = classSyntax.Identifier.Text;

                    var sb = new StringBuilder();

                    foreach (var cmd in commands)
                    {
                        string backingField = "_" + char.ToLower(cmd.BaseName[0]) + cmd.BaseName.Substring(1) + "Command";

                        // partial hooks
                        if (cmd.HasParameter)
                        {
                            sb.AppendLine($"        partial void Can{cmd.BaseName}Execute({cmd.ParameterType} parameter, ref bool canExecute);");
                            sb.AppendLine($"        partial void On{cmd.BaseName}Executing({cmd.ParameterType} parameter, ref bool cancel);");
                            sb.AppendLine($"        partial void On{cmd.BaseName}Executed({cmd.ParameterType} parameter);");
                        }
                        else
                        {
                            sb.AppendLine($"        partial void Can{cmd.BaseName}Execute(ref bool canExecute);");
                            sb.AppendLine($"        partial void On{cmd.BaseName}Executing(ref bool cancel);");
                            sb.AppendLine($"        partial void On{cmd.BaseName}Executed();");
                        }

                        sb.AppendLine();

                        // Determine command type
                        string commandType = cmd.HasParameter
                            ? (cmd.IsAsync ? $"AsyncRelayCommand<{cmd.ParameterType}>" : $"RelayCommand<{cmd.ParameterType}>")
                            : (cmd.IsAsync ? "AsyncRelayCommand" : "RelayCommand");

                        // Build execution lambda
                        string lambda;
                        if (cmd.HasParameter)
                        {
                            if (cmd.IsAsync)
                            {
                                lambda = $@"
    var canExecute = true;
    Can{cmd.BaseName}Execute(parameter, ref canExecute);
    if (!canExecute) return;

    var cancel = false;
    On{cmd.BaseName}Executing(parameter, ref cancel);
    if (cancel) return;

    await {cmd.RawMethodName}(parameter);

    On{cmd.BaseName}Executed(parameter);";
                            }
                            else
                            {
                                lambda = $@"
    var canExecute = true;
    Can{cmd.BaseName}Execute(parameter, ref canExecute);
    if (!canExecute) return;

    var cancel = false;
    On{cmd.BaseName}Executing(parameter, ref cancel);
    if (cancel) return;

    {cmd.RawMethodName}(parameter);

    On{cmd.BaseName}Executed(parameter);";
                            }
                        }
                        else
                        {
                            if (cmd.IsAsync)
                            {
                                lambda = $"await {cmd.RawMethodName}();";
                            }
                            else
                            {
                                lambda = $"{cmd.RawMethodName}();";
                            }
                        }

                        // Generate backing field + property
                        if (cmd.HasParameter)
                        {
                            if (cmd.IsAsync)
                            {
                                sb.AppendLine($@"
    private {commandType}? {backingField};
    public {commandType} {cmd.CommandName} => {backingField} ??= new {commandType}(
        async (parameter) => {{{lambda}
        }}
    );");
                            }
                            else
                            {
                                sb.AppendLine($@"
    private {commandType}? {backingField};
    public {commandType} {cmd.CommandName} => {backingField} ??= new {commandType}(
        parameter => {{{lambda}
        }}
    );");
                            }
                        }
                        else
                        {
                            if (cmd.IsAsync)
                            {
                                sb.AppendLine($@"
    private {commandType}? {backingField};
    public {commandType} {cmd.CommandName} => {backingField} ??= new {commandType}(
        async () => {{{lambda}
        }}
    );");
                            }
                            else
                            {
                                sb.AppendLine($@"
    private {commandType}? {backingField};
    public {commandType} {cmd.CommandName} => {backingField} ??= new {commandType}(
        _ => {{{lambda}
        }}
    );");
                            }
                        }

                        sb.AppendLine();
                    }

                    // emit file
                    var nsOpen = ns != null ? $"namespace {ns}\n{{" : string.Empty;
                    var nsClose = ns != null ? "}" : string.Empty;

                    var src = $@"// <auto-generated>
//  ⚙️ Future.UX CommandGenerator — commands only.
#nullable enable

using System;
using System.Windows.Input;
using Future.UX.MVVM;

{nsOpen}
    public partial class {className}
    {{
{sb}
    }}
{nsClose}";

                    spc.AddSource($"{className}.AutoCommands.g.cs", SourceText.From(src, Encoding.UTF8));
                }
            });
        }

        private static List<CommandInfo> GetCommandMethods(ClassDeclarationSyntax cls, SemanticModel model)
        {
            var methods = cls.Members.OfType<MethodDeclarationSyntax>().ToList();
            var commandMethods = new List<CommandInfo>();

            foreach (var method in methods)
            {
                string name = method.Identifier.Text;
                if (!name.StartsWith("__")) continue;
                if (!method.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword))) continue;

                // detect async
                bool isAsync = method.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword));
                if (!isAsync)
                {
                    var rt = model.GetTypeInfo(method.ReturnType).Type;
                    if (rt?.ToDisplayString().Contains("Task", StringComparison.Ordinal) == true)
                        isAsync = true;
                }

                string baseName = name.Substring(2);
                string commandName = baseName + "Command";

                var parameters = method.ParameterList.Parameters;
                bool hasParam = parameters.Count == 1;
                string paramType = "object";

                if (hasParam)
                {
                    var type = model.GetTypeInfo(parameters[0].Type!).Type;
                    paramType = type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                        .Replace("global::", string.Empty)
                        ?? parameters[0].Type?.ToString()
                        ?? "object";
                }
                else if (parameters.Count > 1)
                {
                    // skip multiparameter commands
                    continue;
                }

                commandMethods.Add(new CommandInfo(
                    rawMethodName: name,
                    baseName: baseName,
                    commandName: commandName,
                    isAsync: isAsync,
                    hasParameter: hasParam,
                    parameterType: paramType
                ));
            }

            return commandMethods;
        }

        private static string? GetNamespace(ClassDeclarationSyntax cls)
        {
            SyntaxNode? node = cls.Parent;
            while (node != null)
            {
                if (node is NamespaceDeclarationSyntax ns) return ns.Name.ToString();
                if (node is FileScopedNamespaceDeclarationSyntax fns) return fns.Name.ToString();
                node = node.Parent;
            }
            return null;
        }

        private sealed class CommandInfo
        {
            public string RawMethodName { get; }
            public string BaseName { get; }
            public string CommandName { get; }
            public bool IsAsync { get; }
            public bool HasParameter { get; }
            public string ParameterType { get; }

            public CommandInfo(string rawMethodName, string baseName, string commandName, bool isAsync, bool hasParameter, string parameterType)
            {
                RawMethodName = rawMethodName;
                BaseName = baseName;
                CommandName = commandName;
                IsAsync = isAsync;
                HasParameter = hasParameter;
                ParameterType = parameterType;
            }
        }
    }
}