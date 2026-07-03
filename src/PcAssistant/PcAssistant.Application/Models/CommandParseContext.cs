namespace PcAssistant.Application.Models;

public sealed record CommandParseContext(
    string CommandLabel,
    string Text,
    bool RequiresConfirmation);
