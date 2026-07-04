using Microsoft.EntityFrameworkCore;
using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Domain.Entity;
using PcAssistant.Persistence.Database;

namespace PcAssistant.Persistence.Repositories;

internal sealed class EfChatSessionRepository(IPcAssistantDbContext dbContext) : IChatSessionRepository
{
    public async Task AddAsync(ChatSession session, CancellationToken cancellationToken = default)
    {
        await dbContext.ChatSessions.AddAsync(session, cancellationToken);
    }

    public Task<ChatSession?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.ChatSessions
            .FirstOrDefaultAsync(session => session.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ChatSession>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ChatSessions
            .AsNoTracking()
            .OrderByDescending(session => session.UpdatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public void Remove(ChatSession session)
    {
        dbContext.ChatSessions.Remove(session);
    }
}
