using PcAssistant.Application.Models;

namespace PcAssistant.Application.Abstractions.Services;

public interface ICommandParserHandler
{
    int Order { get; }

    ParsedCommand? TryParse(CommandParseContext context);
}
