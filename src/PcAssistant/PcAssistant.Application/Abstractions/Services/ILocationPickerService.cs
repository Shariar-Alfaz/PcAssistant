using PcAssistant.Application.Models;

namespace PcAssistant.Application.Abstractions.Services;

public interface ILocationPickerService
{
    Task<DirectorySuggestion?> PickDirectoryAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> PickFilesAsync(bool allowMultiple = true, CancellationToken cancellationToken = default);
}
