using PcAssistant.Application.Models;

namespace PcAssistant.Application.Abstractions;

public interface ICommandParserHandler
{
    int Order { get; }

    ParsedCommand? TryParse(CommandParseContext context);
}
