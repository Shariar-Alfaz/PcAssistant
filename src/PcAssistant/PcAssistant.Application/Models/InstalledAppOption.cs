namespace PcAssistant.Application.Models;

public sealed record InstalledAppOption(
    string DisplayName,
    string AppPath,
    string? IconSourcePath = null);
