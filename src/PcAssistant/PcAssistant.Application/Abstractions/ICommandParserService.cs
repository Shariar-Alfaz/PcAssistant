using PcAssistant.Application.Models;

namespace PcAssistant.Application.Abstractions;

public interface ICommandParserService
{
    ParsedCommand Parse(string commandLabel, string text, bool requiresConfirmation);
}
