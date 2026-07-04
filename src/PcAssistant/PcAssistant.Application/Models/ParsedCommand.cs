namespace PcAssistant.Application.Models;

public sealed class ParsedCommand
{
    public string CommandLabel { get; init; } = "unknown";

    public string OriginalText { get; init; } = string.Empty;

    public string? FolderName { get; init; }

    public string? LocationAlias { get; init; }

    public string? LocationType { get; init; }

    public string? PathText { get; init; }

    public string? ResolvedPath { get; init; }

    public TimeSpan? RestartDelay { get; init; }

    public bool RequiresConfirmation { get; init; }

    public bool IsDangerous { get; init; }

    public string Preview { get; init; } = string.Empty;

    public string? ErrorMessage { get; init; }
}
