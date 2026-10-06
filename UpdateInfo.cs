using System;
using System.IO;
using System.Text.RegularExpressions;

namespace MediHiStat;

public static class UpdateInfo
{
    public static string Label { get; } = ReadLabel();

    private static string ReadLabel()
    {
        try
        {
            string label = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "UPDATE_VERSION.txt")).Trim();
            if (Regex.IsMatch(label, @"^update[0-9]+$")) return label;
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return "update4";
    }
}
