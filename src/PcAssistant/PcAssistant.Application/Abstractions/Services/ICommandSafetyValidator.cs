using PcAssistant.Application.Models;

namespace PcAssistant.Application.Abstractions.Services;

public interface ICommandSafetyValidator
{
    SafetyValidationResult ValidateFileSystemTarget(string? pathText);
}
