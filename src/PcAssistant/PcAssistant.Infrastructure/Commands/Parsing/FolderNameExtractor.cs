using System.Text.RegularExpressions;

namespace PcAssistant.Infrastructure.Commands.Parsing;

public sealed class FolderNameExtractor
{
    private static readonly string[] Patterns =
    {
        @"\b(?:named|called)\s+(?<name>.+?)\s+\b(?:in|on|from|under|inside)\b",
        @"\b(?:folder|directory|dir)\s+(?:named|called)?\s*(?<name>.+?)\s+\b(?:in|on|from|under|inside)\b",
        @"\b(?:create|make|new|delete|remove|erase)\s+(?:a\s+|the\s+)?(?:folder|directory|dir)\s+(?:named|called)?\s*(?<name>.+)$",
    };

    public string? Extract(string text, string locationAlias)
    {
        foreach (var pattern in Patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return Clean(match.Groups["name"].Value, locationAlias);
            }
        }

        return null;
    }

    private static string Clean(string value, string locationAlias)
    {
        var cleaned = value.Trim();
        if (!string.IsNullOrWhiteSpace(locationAlias))
        {
            cleaned = Regex.Replace(
                cleaned,
                $@"\s+\b(?:in|on|from|under|inside)\s+{Regex.Escape(locationAlias)}\b.*$",
                string.Empty,
                RegexOptions.IgnoreCase);
        }

        return cleaned.Trim().Trim('"', '\'', '.', ',', ';', ':');
    }
}
