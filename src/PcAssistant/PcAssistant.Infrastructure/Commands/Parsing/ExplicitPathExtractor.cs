using System.Text.RegularExpressions;

namespace PcAssistant.Infrastructure.Commands.Parsing;

public sealed class ExplicitPathExtractor
{
    private static readonly Regex WindowsPathRegex = new(
        @"(?<path>[a-zA-Z]:\\[^\r\n]+)$",
        RegexOptions.Compiled);

    public string? TryExtract(string text)
    {
        var match = WindowsPathRegex.Match(text);
        return match.Success ? match.Groups["path"].Value.Trim().Trim('"') : null;
    }
}
