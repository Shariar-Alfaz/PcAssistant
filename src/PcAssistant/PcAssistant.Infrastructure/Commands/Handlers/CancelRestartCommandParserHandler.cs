using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Application.Models;

namespace PcAssistant.Infrastructure.Commands.Handlers;

public sealed class CancelRestartCommandParserHandler : ICommandParserHandler
{
    public int Order => 300;

    public ParsedCommand? TryParse(CommandParseContext context)
    {
        if (context.CommandLabel != "system.cancel_restart")
        {
            return null;
        }

        return new ParsedCommand
        {
            CommandLabel = context.CommandLabel,
            OriginalText = context.Text,
            Preview = "Cancel a pending Windows restart or shutdown.",
        };
    }
}
