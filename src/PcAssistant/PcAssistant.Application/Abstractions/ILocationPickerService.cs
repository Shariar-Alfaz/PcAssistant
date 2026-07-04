using PcAssistant.Application.Models;

namespace PcAssistant.Application.Abstractions;

public interface ILocationPickerService
{
    Task<DirectorySuggestion?> PickDirectoryAsync(CancellationToken cancellationToken = default);
}
