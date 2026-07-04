using Microsoft.EntityFrameworkCore;
using PcAssistant.Application.Abstractions;
using PcAssistant.Domain;

namespace PcAssistant.Persistence.Repositories;

internal sealed class EfCommandLogRepository(IPcAssistantDbContext dbContext) : ICommandLogRepository
{
    public async Task AddAsync(CommandLogEntry entry, CancellationToken cancellationToken = default)
    {
        await dbContext.CommandLogs.AddAsync(entry, cancellationToken);
    }

    public Task<CommandLogEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.CommandLogs.FirstOrDefaultAsync(entry => entry.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<CommandLogEntry>> ListRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(count, 1, 200);
        return await dbContext.CommandLogs
            .AsNoTracking()
            .OrderByDescending(entry => entry.CreatedAtUtc)
            .Take(take)
            .OrderBy(entry => entry.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CommandLogEntry>> ListByChatSessionAsync(
        Guid chatSessionId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.CommandLogs
            .AsNoTracking()
            .Where(entry => entry.ChatSessionId == chatSessionId)
            .OrderBy(entry => entry.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public Task<bool> ExistsInChatSessionAsync(Guid chatSessionId, CancellationToken cancellationToken = default)
    {
        return dbContext.CommandLogs.AnyAsync(entry => entry.ChatSessionId == chatSessionId, cancellationToken);
    }
}
