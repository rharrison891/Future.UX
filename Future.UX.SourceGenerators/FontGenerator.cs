using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

[Generator]
public class FontFamilyGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Watch for any additional files under Fonts folder
        var fontsProvider = context.AdditionalTextsProvider
            .Where(file => file.Path.Contains("Fonts", StringComparison.OrdinalIgnoreCase));

        context.RegisterSourceOutput(fontsProvider.Collect(), (spc, files) =>
        {
            var fontFiles = files
                .Where(f => f.Path.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (fontFiles.Length == 0) return;

            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Windows.Markup;");
            sb.AppendLine("using System.Windows.Media;");
            sb.AppendLine("namespace Future.UX.Fonts");
            sb.AppendLine("{");

            // --- Families enum ---
            var families = fontFiles
                .Select(f => GetFamilyName(f.Path))
                .Distinct()
                .OrderBy(f => f)
                .ToArray();

            sb.AppendLine("    public enum Families");
            sb.AppendLine("    {");
            foreach (var family in families)
                sb.AppendLine($"        {family},");
            sb.AppendLine("    }");
            sb.AppendLine();

            // --- Shared FontStyles enum ---
            var allStyles = fontFiles
                .Select(f => GetStyleName(f.Path))
                .Append("Regular")
                .Select(Sanitize)
                .Distinct()
                .OrderBy(s => s)
                .ToArray();

            sb.AppendLine("public enum FontStyles");
            sb.AppendLine("{");

            for (int i = 0; i < allStyles.Length; i++)
            {
                sb.AppendLine($"    {allStyles[i]} = {i},");
            }

            sb.AppendLine("}");
            sb.AppendLine();

            // --- Per-family static classes ---
            foreach (var family in families)
            {
                var filesInFamily = fontFiles.Where(f => GetFamilyName(f.Path) == family).ToArray();
                sb.AppendLine($"    public static class {family}");
                sb.AppendLine("    {");

                foreach (var file in filesInFamily)
                {
                    var style = GetStyleName(file.Path);
                    var relative = file.Path.Substring(file.Path.IndexOf("Fonts")).Replace("\\", "/");
                    var fallbackName = Path.GetFileNameWithoutExtension(file.Path);

                    // Use filename as fallback for internal font family in pack URI
                    sb.AppendLine($"        public static FontFamily {style} => new(\"pack://application:,,,/{relative}#{fallbackName}\");");
                }

                // Per-family FromStyle method
                sb.AppendLine();
                sb.AppendLine("        public static FontFamily FromStyle(FontStyles style)");
                sb.AppendLine("        {");
                sb.AppendLine("            switch(style)");
                sb.AppendLine("            {");

                var styleNames = filesInFamily
                    .Select(f => GetStyleName(f.Path))
                    .Distinct()       // remove duplicate names
                    .Where(s => s != "Regular") // exclude Regular for now
                    .ToArray();

                // Generate explicit cases for all styles except Regular
                foreach (var style in styleNames)
                {
                    sb.AppendLine($"                case FontStyles.{style}: return {style};");
                }

                // Handle Regular explicitly
                if (filesInFamily.Any(f => GetStyleName(f.Path) == "Regular"))
                    sb.AppendLine("                case FontStyles.Regular: return Regular;");
                else if (styleNames.Length > 0)
                    sb.AppendLine($"                case FontStyles.Regular: return {styleNames[0]};");

                // Default fallback
                sb.AppendLine("                default: return Regular;");
                sb.AppendLine("            }");
                sb.AppendLine("        }"); // end FromStyle
                sb.AppendLine("    }"); // end static class {family}
                sb.AppendLine();
            }

            // --- Markup Extension ---
            sb.AppendLine(@"    [MarkupExtensionReturnType(typeof(FontFamily))]
    public class FontFamilyExtension : MarkupExtension
    {
        public Families Family { get; set; }
        public FontStyles Style { get; set; } = FontStyles.Regular;

        public FontFamilyExtension() { }
        public FontFamilyExtension(Families family) => Family = family;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            switch(Family)
            {");

            foreach (var family in families)
            {
                sb.AppendLine($"                case Families.{family}:");
                sb.AppendLine($"                    return {family}.FromStyle(Style);");
            }

            sb.AppendLine(@"                default:
                    return null;
            }
        }
    }");

            sb.AppendLine("}"); // namespace

            spc.AddSource("Fonts.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
        });
    }

    private static string GetFamilyName(string file)
    {
        var parts = file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var fontsIndex = Array.FindIndex(parts, p => p.Equals("Fonts", StringComparison.OrdinalIgnoreCase));
        return fontsIndex >= 0 && parts.Length > fontsIndex + 1
            ? Sanitize(parts[fontsIndex + 1])
            : "Unknown";
    }

    private static string GetStyleName(string file)
    {
        var name = Path.GetFileNameWithoutExtension(file);
        var last = name.Split('-').Last();
        return Sanitize(last);
    }

    private static string Sanitize(string s)
    {
        var clean = new string(s.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
        if (string.IsNullOrEmpty(clean)) return "_";
        if (char.IsDigit(clean[0])) return "_" + clean;
        return clean;
    }
}