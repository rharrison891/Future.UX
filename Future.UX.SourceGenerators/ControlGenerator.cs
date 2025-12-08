using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Future.UX.SourceGenerators
{
    [Generator]
    public class ControlGenerator : IIncrementalGenerator
    {

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var allFiles = context.AdditionalTextsProvider;
            var templateFiles = context.AdditionalTextsProvider
                .Where(at =>
                    at.Path.EndsWith("_cg.cs", StringComparison.OrdinalIgnoreCase) ||
                    at.Path.EndsWith("_cg.xaml", StringComparison.OrdinalIgnoreCase));
            context.RegisterSourceOutput(templateFiles.Collect(), (spc, files) =>
            {
                foreach (var f in files)
                {
                    ReportInfo(spc, $"Template file: {f.Path}");
                }
                var SourceDict = BuildTemplateDictionary(files, spc);
                ReportInfo(spc, $"Processing {SourceDict.Count()} templates");

                foreach (var k in SourceDict.Keys)
                {

                    ReportInfo(spc, $"Processing template: {k}");
                    var csProperties = ProcessCs(spc, SourceDict[k].cs);
                    var xamlProperties = ProcessXaml(spc, SourceDict[k].xaml);


                    var dpList = new HashSet<string>(); // track already created DPs

                    foreach (var (propertyName, propertyType) in xamlProperties.Properties)
                    {
                        // Skip if already generated
                        if (dpList.Contains(propertyName))
                            continue;

                        // Check if a field exists in CS for default value
                        csProperties.TryGetValue(propertyName, out var fieldInfo); // fieldInfo = (TypeName, Initializer)

                        string? defaultValueLiteral = fieldInfo.Initializer;
                        string typeToUse = fieldInfo.PropertyType ?? propertyType;

                        // Create the DependencyProperty
                        GeneratorStrings.AddDependencyProperty(propertyName, typeToUse, hasCallback: true, defaultValueLiteral);
                        GeneratorStrings.AddOnChangeHandler(propertyName);

                        dpList.Add(propertyName);
                        ReportInfo(spc, $"Generated DP {propertyName} ({typeToUse}) with default {defaultValueLiteral}");
                    }

                    // Optional: generate DPs for CS fields not referenced in XAML
                    foreach (var kvp in csProperties)
                    {
                        if (!dpList.Contains(kvp.Key))
                        {
                            GeneratorStrings.AddDependencyProperty(kvp.Key, kvp.Value.PropertyType, hasCallback: true, kvp.Value.Initializer);
                            GeneratorStrings.AddOnChangeHandler(kvp.Key);
                            dpList.Add(kvp.Key);
                            ReportInfo(spc, $"Generated DP {kvp.Key} ({kvp.Value.PropertyType}) from CS field only");
                        }
                    }
                    GeneratorStrings.AddDependencyProperty("ClickCommand", "ICommand", hasCallback: false, null);
                    GeneratorStrings.AddResourceDictionaryMerge(k);
                    GeneratorStrings.AddApplyTemplateEventWires();
                    var classText = GeneratorStrings.BuildFile();
                    spc.AddSource($"{k}.g.cs", classText);
                }
            });
        }

        private Dictionary<string, (string PropertyType, string? Initializer)> ProcessCs(SourceProductionContext spc, AdditionalText csFile)
        {
            var code = csFile.GetText()?.ToString() ?? "";
            var nsMatch = Regex.Match(code, @"namespace\s+([\w\.]+)");
            if (nsMatch.Success)
            {
                var ownerMatch = Regex.Match(code, @"class\s+(\w+)");
                if (ownerMatch.Success)
                {
                    var ownerName = ownerMatch.Groups[1].Value.Trim();
                    var namespaceName = nsMatch.Groups[1].Value.Trim();
                    GeneratorStrings.Init(ownerName, namespaceName);
                }
            }
            var fieldDict = new Dictionary<string, (string, string?)>();

            // Regex supports generic types and optional initializers
            var fieldMatches = Regex.Matches(code, @"private\s+readonly\s+([^\s]+(?:<[^>]+>)?)\s+__(\w+)\s*(?:=\s*(.+?))?\s*;");
            foreach (Match m in fieldMatches)
            {
                var typeName = m.Groups[1].Value;
                var fieldName = m.Groups[2].Value;
                var initializer = m.Groups[3].Success ? m.Groups[3].Value.Trim() : null;
                fieldDict[fieldName] = (typeName, initializer);
                ReportInfo(spc, $"Found field __{fieldName} of type {typeName} with initializer: {initializer}");
            }

            return fieldDict;
        }

        private (List<(string PropertyName, string PropertyType)> Properties, List<(string element, string eventName)> Events)
        ProcessXaml(SourceProductionContext spc, AdditionalText xamlFile)
        {
            var xmlDoc = XDocument.Load(xamlFile.Path);
            var dependencies = new List<(string PropertyName, string PropertyType)>();
            var events = new List<(string element, string eventName)>();

            foreach (var elem in xmlDoc.Descendants())
            {
                var elemName = elem.Name.LocalName;

                // Only process known WPF elements
                if (!WpfPropertyTypesLoader.Types.TryGetValue(elemName, out var propDict))
                    continue;
                // Process attributes for bindings and events
                foreach (var attr in elem.Attributes())
                {
                    // Get named parts
                    if (attr.Name.LocalName == "Name" || attr.Name.LocalName == "x:Name")
                    {
                        GeneratorStrings.AddTemplatePart(attr.Value, elem.Name.LocalName);
                    }
                    // Only process TemplateBindings
                    var IsBinding = attr.Value.StartsWith("{TemplateBinding ", StringComparison.Ordinal) || attr.Value.StartsWith("{Binding ");
                    if (!IsBinding)
                        continue;
                    var isTemplateBinging = attr.Value.StartsWith("{TemplateBinding ", StringComparison.Ordinal);
                    var propertyName = "";
                    if (isTemplateBinging)
                    {
                        ReportInfo(spc, $"Found TemplateBinding on {elemName} property {attr.Name.ToString()}");
                        propertyName = attr.Value.Replace("{TemplateBinding ", "").TrimEnd("}").Trim().ToString();
                    }
                    else
                    {
                        ReportInfo(spc, $"Found Binding on {elemName} property {attr.Name.ToString()}");
                        //Get PropertyName from Binding
                        var pathMatch = Regex.Match(attr.Value, @"Path\s*=\s*([A-Za-z0-9_]+)");
                        if (pathMatch.Success)
                            propertyName = pathMatch.Groups[1].Value;
                        else
                        {
                            // Try simple {Binding Foo, ...}
                            var simpleMatch = Regex.Match(attr.Value, @"\{Binding\s+([A-Za-z0-9_]+)");
                            if (simpleMatch.Success)
                                propertyName = simpleMatch.Groups[1].Value;
                        }
                    }
                    if (string.IsNullOrEmpty(propertyName))
                        continue;
                    // Only create DP if the property actually exists for this element type
                    if (!propDict.TryGetValue(attr.Name.ToString(), out var propertyType))
                    {
                        ReportInfo(spc, $"Skipping unknown property {propertyName} on {elemName}");
                        continue;
                    }
                    dependencies.Add((propertyName, propertyType));
                    if (!isTemplateBinging) 
                    { 
                        //V2 idea: Add ICommand for 2way bindings
                    }
                    ReportInfo(spc, $"Binding found {attr.Name.ToString()} on {elemName} value will be bound to {propertyName}");
                }
            }
            return (dependencies, events);
        }

        private static void ReportInfo(SourceProductionContext context, string message)
        {
            var descriptor = new DiagnosticDescriptor(
                "CTG001",
                "Control Generator Info",
                message,
                "SourceGenerator",
                DiagnosticSeverity.Warning,
                true);

            context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None));
        }

        private Dictionary<string, (AdditionalText cs, AdditionalText xaml)> BuildTemplateDictionary(
        ImmutableArray<AdditionalText> files, SourceProductionContext spc)
        {
            var result = new Dictionary<string, (AdditionalText cs, AdditionalText xaml)>(StringComparer.OrdinalIgnoreCase);
            ReportInfo(spc, $"Building Source Dictionary...");
            // Normalize and prepare inputs
            var inputs = files.Select(f =>
            {
                var fileName = Path.GetFileName(f.Path);
                var ext = Path.GetExtension(fileName).ToLowerInvariant();

                // Remove the "_cg" suffix to get the base template name
                var baseName = Path.GetFileNameWithoutExtension(fileName)
                    .Replace("_cg", "");

                return new
                {
                    File = f,
                    FileName = fileName,
                    BaseName = baseName,
                    Extension = ext
                };
            }).ToList();
            ReportInfo(spc, $"Found {inputs.Count()} file(s)");
            // Group by base name
            var groups = inputs.GroupBy(f => f.BaseName, StringComparer.OrdinalIgnoreCase);
            ReportInfo(spc, $"Grouping by control name. {groups.Count()} controls...");
            foreach (var group in groups)
            {
                // Try to find both .cs and .xaml for this template
                var csFile = group.FirstOrDefault(f => f.Extension == ".cs")?.File;
                var xamlFile = group.FirstOrDefault(f => f.Extension == ".xaml")?.File;
                ReportInfo(spc, $"Joining files for {group.Key}");
                if (csFile != null && xamlFile != null)
                {
                    result[group.Key] = (csFile, xamlFile);
                }
                else
                {
                    // Optional: report missing pair
                    if (csFile == null)
                        ReportInfo(spc, $"Warning: Missing CS file for template {group.Key}");
                    if (xamlFile == null)
                        ReportInfo(spc, $"Warning: Missing XAML file for template {group.Key}");
                }
            }
            ReportInfo(spc, $"Dictionary complete. Found {result.Count} templates");
            return result;
        }
    }
}
