using PcAssistant.Domain.Entity;

namespace PcAssistant.Application.Abstractions.Repositories;

public interface ICommandLogRepository
{
    Task AddAsync(CommandLogEntry entry, CancellationToken cancellationToken = default);

    Task<CommandLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommandLogEntry>> ListRecentAsync(int count, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommandLogEntry>> ListByChatSessionAsync(Guid chatSessionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommandLogEntry>> ListByChatSessionPageAsync(
        Guid chatSessionId,
        DateTimeOffset? beforeCreatedAtUtc,
        int count,
        CancellationToken cancellationToken = default);

    Task<int> CountByChatSessionAsync(Guid chatSessionId, CancellationToken cancellationToken = default);

    Task<bool> ExistsInChatSessionAsync(Guid chatSessionId, CancellationToken cancellationToken = default);
}
