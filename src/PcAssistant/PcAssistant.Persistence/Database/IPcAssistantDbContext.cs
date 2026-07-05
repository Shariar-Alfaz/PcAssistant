using Microsoft.EntityFrameworkCore;
using PcAssistant.Domain.Entity;

namespace PcAssistant.Persistence.Database;

public interface IPcAssistantDbContext : IAsyncDisposable
{
    DbSet<CommandLogEntry> CommandLogs { get; }

    DbSet<ChatSession> ChatSessions { get; }

    DbSet<ScheduledTask> ScheduledTasks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    void ClearTrackedChanges();
}
