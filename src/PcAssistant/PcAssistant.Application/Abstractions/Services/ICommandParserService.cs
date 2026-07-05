using PcAssistant.Application.Models;

namespace PcAssistant.Application.Abstractions.Services;

public interface ICommandParserService
{
    ParsedCommand Parse(string commandLabel, string text, bool requiresConfirmation);
}
