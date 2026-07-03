using PcAssistant.Application.Models;

namespace PcAssistant.Application.Abstractions;

public interface ICommandSafetyValidator
{
    SafetyValidationResult ValidateFileSystemTarget(string? pathText);
}
