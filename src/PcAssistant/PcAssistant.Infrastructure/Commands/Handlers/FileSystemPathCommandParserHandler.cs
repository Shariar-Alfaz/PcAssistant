using System.Text.RegularExpressions;
using PcAssistant.Application.Abstractions;
using PcAssistant.Application.Models;
using PcAssistant.Infrastructure.Commands.Parsing;

namespace PcAssistant.Infrastructure.Commands.Handlers;

public sealed class FileSystemPathCommandParserHandler(
    ExplicitPathExtractor pathExtractor,
    LocationAliasResolver locationResolver)
    : ICommandParserHandler
{
    private static readonly Regex LocationPrepositionRegex = new(
        @"\b(?:in|on|from|under|inside)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex FileNameRegex = new(
        @"(?<name>[A-Za-z0-9][A-Za-z0-9 .()'_-]*\.[A-Za-z0-9]{1,12})",
        RegexOptions.Compiled);

    public int Order => 110;

    public ParsedCommand? TryParse(CommandParseContext context)
    {
        return context.CommandLabel switch
        {
            "filesystem.open_folder" => ParseOpenFolder(context),
            "filesystem.get_file_details" => ParseFileDetails(context),
            _ => null,
        };
    }

    private ParsedCommand ParseOpenFolder(CommandParseContext context)
    {
        var explicitPath = pathExtractor.TryExtract(context.Text);
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return ParsedCommandFactory.FileSystemPath(
                context.CommandLabel,
                context.Text,
                explicitPath,
                "explicit path",
                "absolute",
                "Open folder",
                "The folder path is not valid.");
        }

        var location = locationResolver.Resolve(context.Text);
        return ParsedCommandFactory.FileSystemPath(
            context.CommandLabel,
            context.Text,
            location.BasePath,
            location.Alias,
            location.Type,
            "Open folder",
            "The folder path is not valid.");
    }

    private ParsedCommand ParseFileDetails(CommandParseContext context)
    {
        var explicitPath = pathExtractor.TryExtract(context.Text);
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return ParsedCommandFactory.FileSystemPath(
                context.CommandLabel,
                context.Text,
                explicitPath,
                "explicit path",
                "absolute",
                "Get file details",
                "The file path is not valid.");
        }

        var location = locationResolver.Resolve(context.Text);
        var fileName = ExtractFileName(context.Text);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return ParsedCommandFactory.Unknown(context.Text, "I could not find the file name.");
        }

        return ParsedCommandFactory.FileSystemPath(
            context.CommandLabel,
            context.Text,
            Path.Combine(location.BasePath, CleanFileName(fileName)),
            location.Alias,
            location.Type,
            "Get file details",
            "The file path is not valid.");
    }

    private static string? ExtractFileName(string text)
    {
        var beforeLocation = LocationPrepositionRegex.Split(text, 2)[0];
        var fileName = TryExtractFileNameCandidate(beforeLocation);
        return fileName ?? TryExtractFileNameCandidate(text);
    }

    private static string? TryExtractFileNameCandidate(string text)
    {
        var candidate = text.Trim();
        var markers = new[] { " for ", " of ", " about ", " named ", " called ", " file ", " document " };
        var markerIndex = -1;
        foreach (var marker in markers)
        {
            var index = candidate.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index > markerIndex)
            {
                markerIndex = index + marker.Length;
            }
        }

        if (markerIndex >= 0 && markerIndex < candidate.Length)
        {
            candidate = candidate[markerIndex..].Trim();
        }

        var match = FileNameRegex.Match(candidate);
        return match.Success ? match.Groups["name"].Value : null;
    }

    private static string CleanFileName(string fileName)
    {
        return fileName.Trim().Trim('"', '\'', '.', ',', ';', ':');
    }
}
