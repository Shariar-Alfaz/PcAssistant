using PcAssistant.Domain;

namespace PcAssistant.Application.Abstractions;

public interface IChatSessionRepository
{
    Task AddAsync(ChatSession session, CancellationToken cancellationToken = default);

    Task<ChatSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatSession>> ListAsync(CancellationToken cancellationToken = default);

    void Remove(ChatSession session);
}
