using PcAssistant.Application.Models;

namespace PcAssistant.Infrastructure.Commands.Parsing;

internal static class ParsedCommandFactory
{
    public static ParsedCommand Unknown(string text, string message)
    {
        return new ParsedCommand
        {
            CommandLabel = "unknown",
            OriginalText = text,
            ErrorMessage = message,
            Preview = message,
        };
    }

    public static ParsedCommand Folder(
        string commandLabel,
        string text,
        string? folderName,
        string? locationAlias,
        string locationType,
        string pathText,
        SafetyValidationResult validation,
        bool requiresConfirmation)
    {
        var action = commandLabel == "filesystem.delete_folder" ? "Delete folder" : "Create folder";
        return new ParsedCommand
        {
            CommandLabel = commandLabel,
            OriginalText = text,
            FolderName = folderName,
            LocationAlias = locationAlias,
            LocationType = locationType,
            PathText = pathText,
            ResolvedPath = validation.ResolvedPath,
            RequiresConfirmation = requiresConfirmation || commandLabel == "filesystem.delete_folder",
            IsDangerous = !validation.IsAllowed,
            ErrorMessage = validation.Message,
            Preview = validation.IsAllowed
                ? $"{action}: {validation.ResolvedPath}"
                : validation.Message ?? "This folder action was blocked.",
        };
    }
}
