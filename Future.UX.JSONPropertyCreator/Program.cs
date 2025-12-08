using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

class Program
{
    static void Main()
    {
        // Dictionary to store all types and their properties
        var allTypes = new Dictionary<string, Dictionary<string, string>>();

        // WPF assembly
        var assembly = typeof(FrameworkElement).Assembly;

        var frameworkElements = assembly.GetTypes()
            .Where(t => typeof(FrameworkElement).IsAssignableFrom(t) && !t.IsAbstract)
            .OrderBy(t => t.Name);

        foreach (var type in frameworkElements)
        {
            var propDict = new Dictionary<string, string>();

            // ------------------------------
            // 1. Normal CLR properties
            // ------------------------------
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetIndexParameters().Length > 0) continue;

                var dpField = type.GetField(prop.Name + "Property",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                if (dpField == null)
                    continue;

                propDict[prop.Name] = prop.PropertyType.Name;
                Console.WriteLine($"CLR+DP property: {type.Name}.{prop.Name} ({prop.PropertyType.Name})");
            }

            // ------------------------------
            // 2. DP-only properties (no CLR wrapper)
            // ------------------------------
            var dpFields = type.GetFields(
                BindingFlags.Public |
                BindingFlags.Static |
                BindingFlags.FlattenHierarchy);

            foreach (var field in dpFields)
            {
                if (field.FieldType != typeof(DependencyProperty))
                    continue;

                var dpName = field.Name;
                if (!dpName.EndsWith("Property"))
                    continue;

                var cleanName = dpName.Replace("Property", "");

                // Skip if already captured via CLR property
                if (propDict.ContainsKey(cleanName))
                    continue;

                string valueType = "object";

                try
                {
                    var dp = (DependencyProperty?)field.GetValue(null);
                    if (dp != null)
                    {
                        valueType = dp.PropertyType.Name;
                    }
                }
                catch
                {
                    // some metadata can throw, just ignore
                }

                propDict[cleanName] = valueType;
                Console.WriteLine($"DP-only property: {type.Name}.{cleanName} ({valueType})");
            }

            if (propDict.Count > 0)
                allTypes[type.Name] = propDict;
        }

        // Serialize to JSON
        var json = JsonSerializer.Serialize(
            allTypes,
            new JsonSerializerOptions { WriteIndented = true });

        Console.WriteLine("Serialization complete.");

        // Save to file
        var filePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "WpfElementProperties.json");

        File.WriteAllText(filePath, json);

        Console.WriteLine($"WPF property JSON written to: {filePath}");
        Console.WriteLine("Operation complete. Press any key to continue...");
        Console.ReadKey();
    }
}