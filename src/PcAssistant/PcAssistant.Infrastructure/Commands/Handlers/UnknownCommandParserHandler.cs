using PcAssistant.Application.Abstractions;
using PcAssistant.Application.Models;
using PcAssistant.Infrastructure.Commands.Parsing;

namespace PcAssistant.Infrastructure.Commands.Handlers;

public sealed class UnknownCommandParserHandler : ICommandParserHandler
{
    public int Order => int.MaxValue;

    public ParsedCommand? TryParse(CommandParseContext context)
    {
        return ParsedCommandFactory.Unknown(context.Text, "I could not understand this as a PC command.");
    }
}
