using System;
using System.Collections.Generic;
using System.Text;

public static class Parsers
{
    public static (string? Prefix, string TypeName) ParseType(this string targetTypeAttr)
    {
        string targetType = string.Empty;
        string? prefix = null;

        if (targetTypeAttr.StartsWith("{x:Type "))
        {
            targetType = targetTypeAttr.Substring(8, targetTypeAttr.Length - 9).Trim(); // remove "{x:Type " and "}"
            var parts = targetType.Split(':');

            if (parts.Length == 1)
            {
                targetType = parts[0].Trim();
            }
            else if (parts.Length == 2)
            {
                prefix = parts[0].Trim();
                targetType = parts[1].Trim();
            }
            else
            {
                targetType = targetTypeAttr.Trim();
            }
        }
        else
        {
            targetType = targetTypeAttr.Trim();
        }

        return (prefix, targetType);
    }
}