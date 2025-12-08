using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

public static class WpfPropertyTypesLoader
{
    private static Dictionary<string, Dictionary<string, string>>? _types;

    public static Dictionary<string, Dictionary<string, string>> Types
    {
        get
        {
            if (_types == null) Load();
            return _types!;
        }
    }

    private static void Load()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Future.UX.SourceGenerators.WpfElementProperties.json";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new Exception($"Resource {resourceName} not found.");

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        _types = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json)
                 ?? new Dictionary<string, Dictionary<string, string>>();
    }
}