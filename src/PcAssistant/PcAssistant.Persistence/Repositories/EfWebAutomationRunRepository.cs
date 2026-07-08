using Microsoft.EntityFrameworkCore;
using PcAssistant.Application.Abstractions.Repositories;
using PcAssistant.Domain.Entity;
using PcAssistant.Persistence.Database;

namespace PcAssistant.Persistence.Repositories;

internal sealed class EfWebAutomationRunRepository(IPcAssistantDbContext dbContext) : IWebAutomationRunRepository
{
    public async Task AddRunAsync(WebAutomationRun run, CancellationToken cancellationToken = default)
    {
        await dbContext.WebAutomationRuns.AddAsync(run, cancellationToken);
    }

    public async Task AddStepLogAsync(WebAutomationRunStepLog stepLog, CancellationToken cancellationToken = default)
    {
        await dbContext.WebAutomationRunStepLogs.AddAsync(stepLog, cancellationToken);
    }

    public async Task<IReadOnlyList<WebAutomationRun>> ListRunsAsync(Guid flowId, CancellationToken cancellationToken = default)
    {
        return await dbContext.WebAutomationRuns
            .Include(run => run.StepLogs)
            .Where(run => run.FlowId == flowId)
            .OrderByDescending(run => run.StartedAtUtc)
            .Take(50)
            .ToArrayAsync(cancellationToken);
    }
}
