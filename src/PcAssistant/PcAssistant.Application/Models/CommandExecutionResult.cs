namespace PcAssistant.Application.Models;

public sealed class CommandExecutionResult
{
    private CommandExecutionResult(bool succeeded, string message)
    {
        Succeeded = succeeded;
        Message = message;
    }

    public bool Succeeded { get; }

    public string Message { get; }

    public static CommandExecutionResult Success(string message) => new(true, message);

    public static CommandExecutionResult Failure(string message) => new(false, message);
}
