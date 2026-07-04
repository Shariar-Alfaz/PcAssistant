using PcAssistant.Application.Models;

namespace PcAssistant.Application.Abstractions;

public interface IDirectorySuggestionService
{
    Task<IReadOnlyList<DirectorySuggestion>> SuggestAsync(
        string? query,
        int count,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DirectorySuggestion>> SuggestChildrenAsync(
        string directoryPath,
        string? query,
        int count,
        CancellationToken cancellationToken = default);
}
