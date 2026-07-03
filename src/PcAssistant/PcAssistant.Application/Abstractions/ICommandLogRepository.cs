using PcAssistant.Domain;

namespace PcAssistant.Application.Abstractions;

public interface ICommandLogRepository
{
    Task AddAsync(CommandLogEntry entry, CancellationToken cancellationToken = default);

    Task<CommandLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommandLogEntry>> ListRecentAsync(int count, CancellationToken cancellationToken = default);
}
