using PcAssistant.Application.Abstractions.Repositories;
using PcAssistant.Application.Abstractions.UnitOfWorks;
using PcAssistant.Persistence.Database;

namespace PcAssistant.Persistence.UnitOfWork;

internal sealed class WebAutomationUnitOfWork(
    IPcAssistantDbContext dbContext,
    IWebAutomationRepository automations,
    IWebAutomationRunRepository runs) : IWebAutomationUnitOfWork
{
    public IWebAutomationRepository Automations { get; } = automations;

    public IWebAutomationRunRepository Runs { get; } = runs;

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        dbContext.ClearTrackedChanges();
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
