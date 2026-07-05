using PcAssistant.Application.Models;

namespace PcAssistant.Application.Abstractions.Services;

public interface IWindowsCommandExecutor
{
    Task<CommandExecutionResult> ExecuteAsync(
        ParsedCommand command,
        bool confirmed,
        CancellationToken cancellationToken = default);
}
