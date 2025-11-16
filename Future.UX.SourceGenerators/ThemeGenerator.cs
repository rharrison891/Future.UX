using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[Generator]
public class ThemeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax cds && cds.Identifier.Text == "Theme",
                transform: static (ctx, _) => (ClassDeclaration: (ClassDeclarationSyntax)ctx.Node, SemanticModel: ctx.SemanticModel))
            .Where(static t => t != default);

        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());

        context.RegisterSourceOutput(compilationAndClasses, (spc, source) =>
        {
            var (compilation, classes) = source;
            foreach (var cls in classes)
            {
                try
                {
                    GenerateThemeSources(spc, cls.ClassDeclaration, cls.SemanticModel);
                }
                catch (Exception ex)
                {
                    // Report a diagnostic instead of letting the generator fail
                    var descriptor = new DiagnosticDescriptor(
                        id: "THEMEGEN001",
                        title: "Theme Generator Exception",
                        messageFormat: "Theme generator failed: {0}",
                        category: "ThemeGenerator",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true);

                    spc.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, ex.Message));
                }
            }
        });
    }

    private static void GenerateThemeSources(SourceProductionContext context, ClassDeclarationSyntax cls, SemanticModel semanticModel)
    {
        var baseColorField = cls.Members
            .OfType<FieldDeclarationSyntax>()
            .FirstOrDefault(f => f.Declaration.Variables.Any(v => v.Identifier.Text == "__baseColors"));

        if (baseColorField == null) return;

        var initializer = baseColorField.DescendantNodes()
            .OfType<InitializerExpressionSyntax>()
            .FirstOrDefault();

        var colors = new List<(string key, int a, int r, int g, int b)>();

        if (initializer != null)
        {
            foreach (var expr in initializer.Expressions)
            {
                if (expr is InitializerExpressionSyntax pair && pair.Expressions.Count == 2)
                {
                    var key = pair.Expressions[0].ToString().Trim('"');
                    var colorExpr = pair.Expressions[1].ToString();
                    var values = colorExpr.Replace("Color.FromArgb(", "").Replace(")", "")
                                          .Split(',')
                                          .Select(v => int.Parse(v.Trim()))
                                          .ToArray();
                    if (values.Length == 4)
                        colors.Add((key, values[0], values[1], values[2], values[3]));
                }
            }
        }

        if (!colors.Any())
        {
            colors = new List<(string, int, int, int, int)>
            {
                ("Background",255,30,30,30),
                ("Foreground",255,220,220,220),
                ("Primary",255,0,120,215),
                ("Secondary",255,32,32,32),
                ("Accent",255,0,153,204),
                ("Border",255,100,100,100),
                ("Error",255,232,17,35),
                ("Warning",255,255,185,0),
                ("Success",255,16,124,16)
            };
        }

        // Generate enum
        context.AddSource("ThemeColor.g.cs", SourceText.From(GenerateEnumSource(colors), Encoding.UTF8));

        // Generate Theme class with caching
        context.AddSource("Theme.g.cs", SourceText.From(GenerateThemeClassSource(colors), Encoding.UTF8));

        // Generate partial BrushBaseExtension with strongly-typed constructors
        context.AddSource("BrushBaseExtension.g.cs", SourceText.From(GenerateBrushBaseExtension(colors), Encoding.UTF8));
    }

    private static string GenerateEnumSource(List<(string key, int a, int r, int g, int b)> colors)
    {
        var sb = new StringBuilder();
        sb.AppendLine("namespace Future.UX.WPF.Theme");
        sb.AppendLine("{");
        sb.AppendLine("    public enum ThemeColor");
        sb.AppendLine("    {");
        foreach (var (key, _, _, _, _) in colors) sb.AppendLine($"        {key},");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string GenerateThemeClassSource(List<(string key, int a, int r, int g, int b)> colors)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Windows.Media;");
        sb.AppendLine("namespace Future.UX.WPF.Theme");
        sb.AppendLine("{");
        sb.AppendLine("    public static partial class Theme");
        sb.AppendLine("    {");

        // Brush cache
        sb.AppendLine("        private static readonly Dictionary<string, SolidColorBrush> __modifiedBrushCache = new();");

        // Generated BaseColors dictionary mapping enum to __baseColors
        sb.AppendLine("        private static readonly Dictionary<ThemeColor, Color> BaseColors = new()");
        sb.AppendLine("        {");
        foreach (var (key, _, _, _, _) in colors)
        {
            sb.AppendLine($"            [ThemeColor.{key}] = __baseColors[\"{key}\"],");
        }
        sb.AppendLine("        };");

        // GetColor
        sb.AppendLine("        public static Color GetColor(ThemeColor color) => BaseColors[color];");

        // GetBrush with caching, alpha, brightness
        sb.AppendLine(@"
        public static SolidColorBrush GetBrush(ThemeColor color, double alpha = 1.0, double brightness = 0.0)
        {
            string key = (alpha == 1.0 && brightness == 0.0) ? color.ToString() : $""{color}|{alpha}|{brightness}"";

            if (__modifiedBrushCache.TryGetValue(key, out var cached))
                return cached;

            var c = BaseColors[color];
            byte a = (byte)Math.Clamp(c.A * alpha, 0, 255);
            byte r = (byte)Math.Clamp(c.R + brightness, 0, 255);
            byte g = (byte)Math.Clamp(c.G + brightness, 0, 255);
            byte b = (byte)Math.Clamp(c.B + brightness, 0, 255);

            var brush = new SolidColorBrush(Color.FromArgb(a,r,g,b));
            brush.Freeze();
            __modifiedBrushCache[key] = brush;
            return brush;
        }");

        // Strongly-typed brushes and colors
        foreach (var (key, _, _, _, _) in colors)
        {
            sb.AppendLine($"        public static SolidColorBrush {key}Brush => GetBrush(ThemeColor.{key});");
            sb.AppendLine($"        public static Color {key} => BaseColors[ThemeColor.{key}];");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string GenerateBrushBaseExtension(List<(string key, int a, int r, int g, int b)> colors)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Windows.Markup;");
        sb.AppendLine("using System.Windows.Media;");
        sb.AppendLine("using Future.UX.WPF.Theme;");
        sb.AppendLine("namespace Future.UX.WPF.Markup");
        sb.AppendLine("{");
        sb.AppendLine("    [MarkupExtensionReturnType(typeof(Brush))]");
        sb.AppendLine("    public partial class BrushBaseExtension : MarkupExtension");
        sb.AppendLine("    {");
        sb.AppendLine("        public ThemeColor Base { get; set; }");
        sb.AppendLine("        public double Alpha { get; set; } = 1.0;");
        sb.AppendLine("        public double Brightness { get; set; } = 0.0;");
        sb.AppendLine("        public BrushBaseExtension() { }");
        sb.AppendLine("        public override object ProvideValue(IServiceProvider serviceProvider) => Theme.Theme.GetBrush(Base, Alpha, Brightness);");

        // Generate strongly-typed constructors
        foreach (var (key, _, _, _, _) in colors)
        {
            sb.AppendLine($@"
        public static BrushBaseExtension {key}(double alpha = 1.0, double brightness = 0.0) =>
            new BrushBaseExtension {{ Base = ThemeColor.{key}, Alpha = alpha, Brightness = brightness }};");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }
}