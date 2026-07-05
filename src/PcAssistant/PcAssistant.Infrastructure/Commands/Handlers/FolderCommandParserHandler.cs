using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Application.Models;
using PcAssistant.Infrastructure.Commands.Parsing;

namespace PcAssistant.Infrastructure.Commands.Handlers;

public sealed class FolderCommandParserHandler(
    ICommandSafetyValidator safetyValidator,
    ExplicitPathExtractor pathExtractor,
    LocationAliasResolver locationResolver,
    FolderNameExtractor folderNameExtractor)
    : ICommandParserHandler
{
    public int Order => 100;

    public ParsedCommand? TryParse(CommandParseContext context)
    {
        if (context.CommandLabel is not ("filesystem.create_folder" or "filesystem.delete_folder"))
        {
            return null;
        }

        var explicitPath = pathExtractor.TryExtract(context.Text);
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            var explicitFolderName = folderNameExtractor.Extract(context.Text, explicitPath);
            var targetPath = IsExplicitPathUsedAsLocation(context.Text, explicitPath)
                && !string.IsNullOrWhiteSpace(explicitFolderName)
                    ? Path.Combine(explicitPath, NormalizeFolderName(explicitFolderName))
                    : explicitPath;
            var validation = safetyValidator.ValidateFileSystemTarget(targetPath);
            return ParsedCommandFactory.Folder(
                context.CommandLabel,
                context.Text,
                Path.GetFileName(validation.ResolvedPath ?? targetPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
                "explicit path",
                "absolute",
                targetPath,
                validation,
                context.RequiresConfirmation);
        }

        var location = locationResolver.Resolve(context.Text);
        var folderName = folderNameExtractor.Extract(context.Text, location.Alias);
        if (string.IsNullOrWhiteSpace(folderName))
        {
            return ParsedCommandFactory.Unknown(context.Text, "I could not find the folder name.");
        }

        var safeFolderName = NormalizeFolderName(folderName);

        if (safeFolderName.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
        {
            return ParsedCommandFactory.Unknown(context.Text, "The folder name contains invalid path characters.");
        }

        var pathText = Path.Combine(location.BasePath, safeFolderName);
        var validationResult = safetyValidator.ValidateFileSystemTarget(pathText);
        return ParsedCommandFactory.Folder(
            context.CommandLabel,
            context.Text,
            safeFolderName,
            location.Alias,
            location.Type,
            pathText,
            validationResult,
            context.RequiresConfirmation);
    }

    private static string NormalizeFolderName(string folderName)
    {
        return folderName
            .Trim()
            .Trim('"', '\'', '.', ',', ';', ':')
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
    }

    private static bool IsExplicitPathUsedAsLocation(string text, string explicitPath)
    {
        var pathIndex = text.LastIndexOf(explicitPath, StringComparison.OrdinalIgnoreCase);
        if (pathIndex <= 0)
        {
            return false;
        }

        var beforePath = text[..pathIndex].TrimEnd();
        return beforePath.EndsWith(" in", StringComparison.OrdinalIgnoreCase)
            || beforePath.EndsWith(" on", StringComparison.OrdinalIgnoreCase)
            || beforePath.EndsWith(" from", StringComparison.OrdinalIgnoreCase)
            || beforePath.EndsWith(" under", StringComparison.OrdinalIgnoreCase)
            || beforePath.EndsWith(" inside", StringComparison.OrdinalIgnoreCase);
    }
}
