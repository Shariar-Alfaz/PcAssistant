using Microsoft.EntityFrameworkCore;
using PcAssistant.Domain;

namespace PcAssistant.Persistence;

public interface IPcAssistantDbContext : IAsyncDisposable
{
    DbSet<CommandLogEntry> CommandLogs { get; }

    DbSet<ChatSession> ChatSessions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    void ClearTrackedChanges();
}
