using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

public static class GeneratorStrings
{
    private const string Tab = "    ";
    private const string OpenBrace = "{";
    private const string CloseBrace = "}";
    private static readonly HashSet<string> IgnoredProperties = new()
    {
        "Background",
        "BorderBrush",
        "BorderThickness",
        "Foreground",
        "FontSize",
        "FontFamily",
        "Padding",
        "Margin",
        "HorizontalAlignment",
        "VerticalAlignment",
        "Width",
        "Height",
        "Visibility",
        "IsEnabled","ClipToBounds", "Cursor", "SnapsToDevicePixels", "DataContext", "Style"
        // add any others you want to skip
    };
    // ------------------------
    //  STORAGE
    // ------------------------
    private static readonly HashSet<string> _usings = new(StringComparer.Ordinal);
    private static readonly List<string> _dependencyProperties = new();
    private static readonly List<string> _getters = new();
    private static readonly List<string> _onChangeHandlers = new();
    private static readonly HashSet<string> _partialMethods = new(StringComparer.Ordinal);
    private static readonly List<(string Name, string Type)> _templateParts = new();
    private static readonly List<string> _applyTemplateCode = new();
    private static readonly List<string> _templateContent = new();
    private static List<string> _readonlyProperties = new();

    // ------------------------
    //  TEMPLATE OWNER INFO
    // ------------------------
    public static string? Namespace { get; set; }
    public static string? OwnerName { get; set; }
    private static string? AssemblyName { get; set; }

    public static void Init(string owner, string namespc)
    {
        Namespace = namespc;
        OwnerName = owner;
        _usings.Clear();
        _dependencyProperties.Clear();
        _getters.Clear();
        _onChangeHandlers.Clear();
        _partialMethods.Clear();
        _templateParts.Clear();
        _applyTemplateCode.Clear();
        _templateContent.Clear();

        // Default usings
        _usings.Add("using System;");
        _usings.Add("using System.Windows;");
        _usings.Add("using System.Windows.Controls;");
        _usings.Add("using System.Collections.ObjectModel;");
        _usings.Add("using System.Windows.Input;");
        _usings.Add("using System.IO;");
        _usings.Add("using System.Windows.Markup;");
    }

    // ------------------------
    //  USINGS
    // ------------------------
    public static void AddUsing(string import)
    {
        if (!string.IsNullOrWhiteSpace(import) &&
            !import.StartsWith("System.Windows") && // skip redundant
            !_usings.Contains(import))
        {
            _usings.Add(import.Trim());
        }
    }

    public static string BuildUsings()
    {
        var sb = new StringBuilder();
        foreach (var u in _usings.OrderBy(x => x))
            sb.AppendLine(u);
        sb.AppendLine();
        return sb.ToString();
    }


    // ------------------------
    //  DEPENDENCY PROPERTIES
    // ------------------------
    public static void AddDependencyProperty(string propertyName, string typeName = "object", bool hasCallback = true, string? defaultValueLiteral = null)
    {
        if (string.IsNullOrEmpty(OwnerName))
            throw new InvalidOperationException("OwnerName must be set before adding a dependency property.");
        if (IgnoredProperties.Contains(propertyName))
            return;
        string dpKey = $"{propertyName}Property";
        if (_dependencyProperties.Any(x => x.Contains($" {dpKey} ="))) return;

        var sb = new StringBuilder();
        sb.AppendLine($"{Tab}public static readonly DependencyProperty {dpKey} =");
        sb.AppendLine($"{Tab}{Tab}DependencyProperty.Register(");
        sb.AppendLine($"{Tab}{Tab}{Tab}\"{propertyName}\",");
        sb.AppendLine($"{Tab}{Tab}{Tab}typeof({typeName}),");
        sb.AppendLine($"{Tab}{Tab}{Tab}typeof({OwnerName}),");
        sb.Append($"{Tab}{Tab}{Tab}new FrameworkPropertyMetadata({defaultValueLiteral ?? $"default({typeName})"}");

        if (hasCallback)
            sb.Append($", propertyChangedCallback: On{propertyName}Changed");

        sb.AppendLine("));");
        sb.AppendLine();

        _dependencyProperties.Add(sb.ToString());
        if(Helpers.UsingsMap.Any(kv=>typeName.Contains(kv.Key)))
        {
            AddUsing($"using {Helpers.UsingsMap.First(kv => typeName.Contains(kv.Key)).Value};");
        }

        AddGetter(propertyName, typeName);
    }



    private static string FormatDefault(object? val)
    {
        if (val == null) return "null";
        return val switch
        {
            string s => $"\"{s.Replace("\"", "\\\"")}\"",
            char c => $"'{c}'",
            bool b => b ? "true" : "false",
            Enum e => $"{e.GetType().FullName}.{e}",
            _ => val.ToString() ?? "null"
        };
    }

    public static string BuildDependencyProperties() => string.Concat(_dependencyProperties);

    // ------------------------
    //  GETTERS + SETTERS
    // ------------------------
    private static void AddGetter(string propertyName, string typeName)
    {
        if (_getters.Any(x => x.Contains($" {propertyName}\n"))) return;

        var sb = new StringBuilder();
        sb.AppendLine($"{Tab}public {typeName} {propertyName}");
        sb.AppendLine($"{Tab}{OpenBrace}");
        sb.AppendLine($"{Tab}{Tab}get => ({typeName})GetValue({propertyName}Property);");
        sb.AppendLine($"{Tab}{Tab}set => SetValue({propertyName}Property, value);");
        sb.AppendLine($"{Tab}{CloseBrace}");
        sb.AppendLine();

        _getters.Add(sb.ToString());
    }

    public static string BuildGetters() => string.Concat(_getters);

    // ------------------------
    //  ON-CHANGE HANDLERS
    // ------------------------
    public static void AddOnChangeHandler(string propertyName)
    {
        if (string.IsNullOrEmpty(OwnerName)) throw new InvalidOperationException("OwnerName must be set before adding an OnChange handler.");

        string signature = $"On{propertyName}Changed";
        if (_onChangeHandlers.Any(h => h.Contains(signature))) return;

        var sb = new StringBuilder();
        sb.AppendLine($"{Tab}private static void {signature}(DependencyObject d, DependencyPropertyChangedEventArgs e)");
        sb.AppendLine($"{Tab}{OpenBrace}");
        sb.AppendLine($"{Tab}{Tab}if (d is {OwnerName} owner)");
        sb.AppendLine($"{Tab}{Tab}{OpenBrace}");
        sb.AppendLine($"{Tab}{Tab}{Tab}owner.{propertyName}Changed(e);");
        sb.AppendLine($"{Tab}{Tab}{CloseBrace}");
        sb.AppendLine($"{Tab}{CloseBrace}");
        sb.AppendLine();

        _onChangeHandlers.Add(sb.ToString());
        _partialMethods.Add($"{Tab}partial void {propertyName}Changed(DependencyPropertyChangedEventArgs e);");
    }

    public static string BuildOnChangeHandlers() => string.Concat(_onChangeHandlers.OrderBy(x => x));

    // ------------------------
    //  TEMPLATE PARTS
    // ------------------------
    public static void AddTemplatePart(string partName, string elementType)
    {
        if (!_templateParts.Any(tp => tp.Name == partName))
        {
            _templateParts.Add((partName, elementType));
            AddReadonlyTemplateProperty(partName, elementType);
        }
    }

    private static void AddReadonlyTemplateProperty(string partName, string elementType)
    {
        if (_readonlyProperties.Any(r => r.Contains($" {partName} ")))
            return;

        _readonlyProperties.Add($@"{Tab}public {elementType} {partName} {{ get; private set; }}");
    }
    private static string BuildReadonlyProperties()
    {
        if (_readonlyProperties.Count == 0) return string.Empty;
        var sb = new StringBuilder();
        foreach (var rp in _readonlyProperties.OrderBy(x => x))
            sb.AppendLine(rp);
        sb.AppendLine();
        return sb.ToString();
    }
    // ------------------------
    //  TEMPLATE CONTENT
    // ------------------------
    public static void AddTemplateContent(string xamlContent)
    {
        if (!string.IsNullOrWhiteSpace(xamlContent))
        {
            _templateContent.Add(xamlContent);
        }
    }

    public static string BuildTemplateContent() => string.Concat(_templateContent);

    // ------------------------
    //  APPLY TEMPLATE
    // ------------------------
    public static void AddApplyTemplateWire(string wireCode)
    {
        _applyTemplateCode.Add(wireCode);
    }

    public static string BuildOnApplyTemplate()
    {
        if (_applyTemplateCode.Count == 0 && _templateParts.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine($"{Tab}public override void OnApplyTemplate()");
        sb.AppendLine($"{Tab}{OpenBrace}");
        sb.AppendLine($"{Tab}{Tab}base.OnApplyTemplate();");
        sb.AppendLine();

        foreach (var part in _templateParts)
            sb.AppendLine($"{Tab}{Tab}{part.Name} = GetTemplateChild(\"{part.Name}\") as {part.Type};");

        foreach (var line in _applyTemplateCode)
            sb.AppendLine($"{Tab}{Tab}{line}");

        sb.AppendLine($"{Tab}{Tab}OnTemplateApplied();");
        sb.AppendLine($"{Tab}{CloseBrace}");
        sb.AppendLine();
        return sb.ToString();
    }

    // ------------------------
    //  PARTIAL METHODS
    // ------------------------
    public static void AddPartialMethod(string methodName)
    {
        _partialMethods.Add($"partial void {methodName}(EventArgs e);");
    }
    public static string BuildPartialMethods()
    {
        var sb = new StringBuilder();
        foreach (var pm in _partialMethods.OrderBy(x => x))
            sb.AppendLine($"{Tab}{pm}");
        sb.AppendLine();
        return sb.ToString();
    }

    // ------------------------
    //  FINAL FILE OUTPUT
    // ------------------------
    public static string BuildFile()
    {
        if (string.IsNullOrEmpty(Namespace)) throw new InvalidOperationException("Namespace must be set.");
        if (string.IsNullOrEmpty(OwnerName)) throw new InvalidOperationException("OwnerName must be set.");

        var sb = new StringBuilder();
        sb.Append(BuildUsings());
        sb.AppendLine($"namespace {Namespace}");
        sb.AppendLine("{");
        sb.AppendLine($"{Tab}public partial class {OwnerName} : Control");
        sb.AppendLine($"{Tab}{OpenBrace}");
        sb.Append(BuildDependencyProperties());
        sb.Append(BuildGetters());
        sb.Append(BuildReadonlyProperties());
        sb.Append(BuildOnChangeHandlers());
        sb.Append(BuildOnApplyTemplate());
        sb.Append(BuildTemplateContent());
        sb.Append(BuildPartialMethods());
        sb.AppendLine($"{Tab}{CloseBrace}");
        sb.AppendLine("}");
        return sb.ToString();
    }

    public static void AddApplyTemplateEventWires()
    {
        // Example: Loaded event
        AddApplyTemplateWire("base.Loaded += (_, e) => OnLoaded();");
        _partialMethods.Add("partial void OnLoaded();");

        // Example: Click event
        AddApplyTemplateWire("base.MouseLeftButtonUp += (s, e) => Click(e);");
        _partialMethods.Add("partial void Click(MouseButtonEventArgs e);");

        // Optional: template applied hook
        _partialMethods.Add("partial void OnTemplateApplied();");
    }

    public static void AddResourceDictionaryMerge(string className)
    {
        var assemblyName = Namespace.Replace("." + Namespace.Split('.').Last(), "");
        _partialMethods.Add($@"
            private static bool _isMerged;

            static {className}()
            {{
                MergeGeneratedDictionary();
            }}

            private static void MergeGeneratedDictionary()
            {{
                if (_isMerged)
                    return;

                // Designer mode / VS XAML preview → App.Current may be null
                if (System.Windows.Application.Current == null)
                    return;

                try
            {{
                var uri = new Uri($""pack://application:,,,/{assemblyName};component/Controls/{className}_cg.xaml"");
                var rd = new ResourceDictionary {{ Source = uri }};
                Application.Current.Resources.MergedDictionaries.Add(rd);
            }}
            catch (Exception ex)
            {{
                // Optional: log or handle missing dictionary
            }}

                _isMerged = true;
            }}
        ");
    }


}