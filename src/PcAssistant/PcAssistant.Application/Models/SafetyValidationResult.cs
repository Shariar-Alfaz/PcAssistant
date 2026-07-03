namespace PcAssistant.Application.Models;

public sealed class SafetyValidationResult
{
    private SafetyValidationResult(bool isAllowed, string? resolvedPath, string? message)
    {
        IsAllowed = isAllowed;
        ResolvedPath = resolvedPath;
        Message = message;
    }

    public bool IsAllowed { get; }

    public string? ResolvedPath { get; }

    public string? Message { get; }

    public static SafetyValidationResult Allowed(string resolvedPath) => new(true, resolvedPath, null);

    public static SafetyValidationResult Blocked(string message) => new(false, null, message);
}
