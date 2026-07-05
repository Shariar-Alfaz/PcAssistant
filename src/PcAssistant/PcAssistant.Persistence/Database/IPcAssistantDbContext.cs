using Microsoft.EntityFrameworkCore;
using PcAssistant.Domain.Entity;

namespace PcAssistant.Persistence.Database;

public interface IPcAssistantDbContext : IAsyncDisposable
{
    DbSet<CommandLogEntry> CommandLogs { get; }

    DbSet<ChatSession> ChatSessions { get; }

    DbSet<ScheduledTask> ScheduledTasks { get; }

    DbSet<WebAutomationProject> WebAutomationProjects { get; }

    DbSet<WebAutomationFlow> WebAutomationFlows { get; }

    DbSet<WebAutomationStep> WebAutomationSteps { get; }

    DbSet<WebSelectorSnapshot> WebSelectorSnapshots { get; }

    DbSet<WebAutomationVariable> WebAutomationVariables { get; }

    DbSet<BrowserProfile> BrowserProfiles { get; }

    DbSet<WebAutomationRun> WebAutomationRuns { get; }

    DbSet<WebAutomationRunStepLog> WebAutomationRunStepLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    void ClearTrackedChanges();
}
