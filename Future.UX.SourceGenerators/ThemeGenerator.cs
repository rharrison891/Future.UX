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
                predicate: static (node, _) =>
                    node is ClassDeclarationSyntax cds &&
                    cds.Identifier.Text == "Theme",
                transform: static (ctx, _) =>
                (
                    ClassDeclaration: (ClassDeclarationSyntax)ctx.Node,
                    SemanticModel: ctx.SemanticModel
                ))
            .Where(static t => t.ClassDeclaration != null);

        var comb = context.CompilationProvider.Combine(classDeclarations.Collect());

        context.RegisterSourceOutput(comb, (spc, src) =>
        {
            var (compilation, classes) = src;

            foreach (var cls in classes)
            {
                try
                {
                    GenerateTheme(spc, cls.ClassDeclaration, cls.SemanticModel);
                }
                catch (Exception ex)
                {
                    var d = new DiagnosticDescriptor(
                        "THEMEGEN001",
                        "Theme Generator Error",
                        "Theme generator failed: {0}",
                        "ThemeGenerator",
                        DiagnosticSeverity.Error,
                        true
                    );

                    spc.ReportDiagnostic(Diagnostic.Create(d, Location.None, ex.Message));
                }
            }
        });
    }

    private static void GenerateTheme(SourceProductionContext context, ClassDeclarationSyntax cls, SemanticModel sm)
    {
        var colors = ExtractColors(cls);
        var ns = GetNamespace(cls);

        context.AddSource("ThemeColor.g.cs", SourceText.From(GenerateEnum(colors, ns), Encoding.UTF8));
        context.AddSource("Theme.g.cs", SourceText.From(GenerateThemeClass(colors, ns), Encoding.UTF8));
        context.AddSource("BrushBaseExtension.g.cs", SourceText.From(GenerateBrushExtension(colors, ns), Encoding.UTF8));
    }

    // ────────────────────────────────────────────────────────────────
    // Syntax-only color extraction (supports ARGB + HEX)
    // ────────────────────────────────────────────────────────────────
    private static List<(string key, int a, int r, int g, int b)> ExtractColors(ClassDeclarationSyntax cls)
    {
        var baseColorField = cls.Members
            .OfType<FieldDeclarationSyntax>()
            .FirstOrDefault(f => f.Declaration.Variables.Any(v => v.Identifier.Text == "__baseColors"));

        var colors = new List<(string key, int a, int r, int g, int b)>();

        if (baseColorField != null)
        {
            var initializer = baseColorField.DescendantNodes()
                .OfType<InitializerExpressionSyntax>()
                .FirstOrDefault();

            if (initializer != null)
            {
                foreach (var expr in initializer.Expressions)
                {
                    if (expr is InitializerExpressionSyntax pair && pair.Expressions.Count == 2)
                    {
                        string key = pair.Expressions[0].ToString().Trim('"');
                        string valueExpr = pair.Expressions[1].ToString().Trim();

                        // ---- 1️⃣ HEX STRING VALUES ----
                        if (pair.Expressions[1] is LiteralExpressionSyntax stringLiteral)
                        {
                            string hex = stringLiteral.Token.ValueText;

                            if (TryParseHex(hex, out int a, out int r, out int g, out int b))
                            {
                                colors.Add((key, a, r, g, b));
                                continue;
                            }

                            throw new Exception($"Invalid HEX color value for '{key}': {hex}");
                        }

                        // ---- 2️⃣ Color.FromArgb(...) ----
                        if (valueExpr.StartsWith("Color.FromArgb("))
                        {
                            var inner = valueExpr.Replace("Color.FromArgb(", "").Replace(")", "");
                            var parts = inner.Split(',').Select(v => int.Parse(v.Trim())).ToArray();

                            if (parts.Length != 4)
                                throw new Exception($"Color.FromArgb must have exactly 4 components for '{key}'.");

                            colors.Add((key, parts[0], parts[1], parts[2], parts[3]));
                            continue;
                        }

                        // ---- 3️⃣ Anything else is invalid ----
                        throw new Exception(
                            $"Unsupported color format for '{key}'. Only Color.FromArgb(...) or HEX values are supported.");
                    }
                }
            }
        }

        if (!colors.Any())
        {
            // fallback default palette
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

        return colors;
    }

    private static bool TryParseArgbSyntax(string expr, out int a, out int r, out int g, out int b)
    {
        // Accept *any* namespace (System.Drawing.Color, System.Windows.Media.Color, MyColorStruct.Color)
        a = r = g = b = 0;

        if (!expr.Contains("FromArgb("))
            return false;

        var inside = expr.Substring(expr.IndexOf("FromArgb(") + 9)
                         .Replace(")", "")
                         .Split(',');

        if (inside.Length != 4)
            return false;

        try
        {
            a = int.Parse(inside[0].Trim());
            r = int.Parse(inside[1].Trim());
            g = int.Parse(inside[2].Trim());
            b = int.Parse(inside[3].Trim());
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryParseHex(string expr, out int a, out int r, out int g, out int b)
    {
        a = r = g = b = 0;

        if (expr.StartsWith("#")) expr = expr.Replace("#", "").Trim();
        if (expr.Length == 6)
        {
            a = 255;
            r = Convert.ToInt32(expr.Substring(0, 2), 16);
            g = Convert.ToInt32(expr.Substring(2, 2), 16);
            b = Convert.ToInt32(expr.Substring(4, 2), 16);
            return true;
        }

        if (expr.Length == 8)
        {
            a = Convert.ToInt32(expr.Substring(0, 2), 16);
            r = Convert.ToInt32(expr.Substring(2, 2), 16);
            g = Convert.ToInt32(expr.Substring(4, 2), 16);
            b = Convert.ToInt32(expr.Substring(6, 2), 16);
            return true;
        }

        return false;
    }

    private static void ReportBadColor(SourceProductionContext context, SyntaxNode node)
    {
        var d = new DiagnosticDescriptor(
            "THEMEGEN002",
            "Invalid Theme Color",
            "Only Color.FromArgb(...) or HEX values are supported.",
            "ThemeGenerator",
            DiagnosticSeverity.Error,
            true);

        context.ReportDiagnostic(Diagnostic.Create(d, node.GetLocation()));
    }

    private static string GetNamespace(ClassDeclarationSyntax cls)
    {
        var ns = cls.FirstAncestorOrSelf<NamespaceDeclarationSyntax>();
        return ns?.Name.ToString() ?? "GeneratedTheme";
    }

    // ────────────────────────────────────────────────────────────────
    // Generate enum
    // ────────────────────────────────────────────────────────────────
    private static string GenerateEnum(List<(string Key, int A, int R, int G, int B)> colors, string ns)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"namespace {ns}");
        sb.AppendLine("{");
        sb.AppendLine("    public enum ThemeColor");
        sb.AppendLine("    {");

        foreach (var c in colors)
            sb.AppendLine($"        {c.Key},");

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    // ────────────────────────────────────────────────────────────────
    // Generate Theme class (WPF code emitted)
    // ────────────────────────────────────────────────────────────────
    private static string GenerateThemeClass(List<(string Key, int A, int R, int G, int B)> colors, string ns)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Windows.Media;");

        sb.AppendLine($"namespace {ns}");
        sb.AppendLine("{");

        sb.AppendLine("    public static partial class Theme");
        sb.AppendLine("    {");

        // Base color table
        sb.AppendLine("        private static readonly Dictionary<ThemeColor, Color> BaseColors = new()");
        sb.AppendLine("        {");

        foreach (var c in colors)
        {
            sb.AppendLine($"            [ThemeColor.{c.Key}] = Color.FromArgb({c.A}, {c.R}, {c.G}, {c.B}),");
        }

        sb.AppendLine("        };");

        // Accessors
        sb.AppendLine("        public static Color GetColor(ThemeColor c) => BaseColors[c];");

        sb.AppendLine(@"
        private static readonly Dictionary<string, SolidColorBrush> __cache = new();

        public static SolidColorBrush GetBrush(ThemeColor color, double alpha = 1.0, double brightness = 0.0)
        {
            string key = $""{color}:{alpha}:{brightness}"";
            if (__cache.TryGetValue(key, out var b))
                return b;

            var c = GetColor(color);

            byte A = (byte)Math.Clamp(c.A * alpha, 0, 255);
            byte R = (byte)Math.Clamp(c.R + brightness, 0, 255);
            byte G = (byte)Math.Clamp(c.G + brightness, 0, 255);
            byte B = (byte)Math.Clamp(c.B + brightness, 0, 255);

            var brush = new SolidColorBrush(Color.FromArgb(A, R, G, B));
            brush.Freeze();
            __cache[key] = brush;
            return brush;
        }
");
        sb.AppendLine(@"
        public static void SetColor(ThemeColor key, Color newColor)
        {
            BaseColors[key] = newColor;

            // Purge all cached brushes for this color
            var prefix = key.ToString();
            var toRemove = __cache.Keys
                .Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
                .ToList();

            foreach (var r in toRemove)
                __cache.Remove(r);
        }

        // HEX overload
        public static void SetColor(ThemeColor key, string hex)
        {
            if (!TryParseHex(hex, out int a, out int r, out int g, out int b))
                throw new ArgumentException($""Invalid HEX color value: {hex}"", nameof(hex));

            SetColor(key, Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b));
        }");

        sb.AppendLine(@"
        private static bool TryParseHex(string expr, out int a, out int r, out int g, out int b)
        {
            a = r = g = b = 0;

            expr = expr.Trim('""');

            if (expr.StartsWith(""#""))
                expr = expr[1..];

            if (expr.Length == 6)
            {
                a = 255;
                r = Convert.ToInt32(expr[0..2], 16);
                g = Convert.ToInt32(expr[2..4], 16);
                b = Convert.ToInt32(expr[4..6], 16);
                return true;
            }

            if (expr.Length == 8)
            {
                a = Convert.ToInt32(expr[0..2], 16);
                r = Convert.ToInt32(expr[2..4], 16);
                g = Convert.ToInt32(expr[4..6], 16);
                b = Convert.ToInt32(expr[6..8], 16);
                return true;
            }

            return false;
        }");

        foreach (var c in colors)
        {
            sb.AppendLine($"        public static SolidColorBrush {c.Key}Brush => GetBrush(ThemeColor.{c.Key});");
            sb.AppendLine($"        public static Color {c.Key} => GetColor(ThemeColor.{c.Key});");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    // ────────────────────────────────────────────────────────────────
    // Brush markup extension
    // ────────────────────────────────────────────────────────────────
    private static string GenerateBrushExtension(List<(string Key, int A, int R, int G, int B)> colors, string ns)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using System;");
        sb.AppendLine("using System.Windows.Markup;");
        sb.AppendLine("using System.Windows.Media;");
        sb.AppendLine($"using {ns};");

        sb.AppendLine($"namespace {ns}");
        sb.AppendLine("{");
        sb.AppendLine("    [MarkupExtensionReturnType(typeof(Brush))]");
        sb.AppendLine("    public class BrushBaseExtension : MarkupExtension");
        sb.AppendLine("    {");
        sb.AppendLine("        public ThemeColor Base { get; set; }");
        sb.AppendLine("        public double Alpha { get; set; } = 1.0;");
        sb.AppendLine("        public double Brightness { get; set; } = 0.0;");
        sb.AppendLine("        public bool AsColor { get; set; } = false;");
        sb.AppendLine("");
        sb.AppendLine("        public override object ProvideValue(IServiceProvider sp) =>"); // Theme.GetBrush(Base, Alpha, Brightness);");
        sb.AppendLine("            AsColor ? (object)Theme.GetBrush(Base, Alpha, Brightness).Color");
        sb.AppendLine("            : Theme.GetBrush(Base, Alpha, Brightness);");
        sb.AppendLine("");
        sb.AppendLine("       public BrushBaseExtension() : this(ThemeColor.Primary, 0, 0, false) { }");
        sb.AppendLine("");
        sb.AppendLine("       public BrushBaseExtension(ThemeColor baseColor, double alpha = 1, double brightness = 0, bool asColor = false)");
        sb.AppendLine("       {");   
        sb.AppendLine("            Base = baseColor;");
        sb.AppendLine("            Alpha = alpha;");
        sb.AppendLine("            Brightness = brightness;");
        sb.AppendLine("            AsColor = asColor;");
        sb.AppendLine("       }");
        sb.AppendLine("");
        foreach (var c in colors)
        {
            sb.AppendLine($@"
        public static BrushBaseExtension {c.Key}(double alpha = 1.0, double brightness = 0.0, bool asColor = false)
            => new BrushBaseExtension {{ Base = ThemeColor.{c.Key}, Alpha = alpha, Brightness = brightness, AsColor = asColor }};");
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }
}