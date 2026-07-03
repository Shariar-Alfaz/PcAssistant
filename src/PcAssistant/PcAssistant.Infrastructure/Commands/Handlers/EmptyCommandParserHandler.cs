using PcAssistant.Application.Abstractions;
using PcAssistant.Application.Models;
using PcAssistant.Infrastructure.Commands.Parsing;

namespace PcAssistant.Infrastructure.Commands.Handlers;

public sealed class EmptyCommandParserHandler : ICommandParserHandler
{
    public int Order => 0;

    public ParsedCommand? TryParse(CommandParseContext context)
    {
        return string.IsNullOrWhiteSpace(context.Text)
            ? ParsedCommandFactory.Unknown(context.Text, "Command text is empty.")
            : null;
    }
}
