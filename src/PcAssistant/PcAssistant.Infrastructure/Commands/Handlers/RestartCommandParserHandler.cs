using PcAssistant.Application.Abstractions;
using PcAssistant.Application.Models;

namespace PcAssistant.Infrastructure.Commands.Handlers;

public sealed class RestartCommandParserHandler : ICommandParserHandler
{
    public int Order => 200;

    public ParsedCommand? TryParse(CommandParseContext context)
    {
        if (context.CommandLabel != "system.restart")
        {
            return null;
        }

        return new ParsedCommand
        {
            CommandLabel = context.CommandLabel,
            OriginalText = context.Text,
            RequiresConfirmation = true,
            Preview = "Restart this PC after a short delay.",
        };
    }
}
