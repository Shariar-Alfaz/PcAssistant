using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Application.Models;

namespace PcAssistant.Infrastructure.Commands;

public sealed class CommandParserService(IEnumerable<ICommandParserHandler> handlers) : ICommandParserService
{
    private readonly IReadOnlyList<ICommandParserHandler> _handlers = handlers
        .OrderBy(handler => handler.Order)
        .ToArray();

    public ParsedCommand Parse(string commandLabel, string text, bool requiresConfirmation)
    {
        var context = new CommandParseContext(commandLabel, text, requiresConfirmation);
        foreach (var handler in _handlers)
        {
            var parsed = handler.TryParse(context);
            if (parsed is not null)
            {
                return parsed;
            }
        }

        return new ParsedCommand
        {
            CommandLabel = "unknown",
            OriginalText = text,
            ErrorMessage = "I could not understand this as a PC command.",
            Preview = "I could not understand this as a PC command.",
        };
    }
}
