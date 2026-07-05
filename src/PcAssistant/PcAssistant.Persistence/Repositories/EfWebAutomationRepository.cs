using Microsoft.EntityFrameworkCore;
using PcAssistant.Application.Abstractions.Repositories;
using PcAssistant.Domain.Entity;
using PcAssistant.Persistence.Database;

namespace PcAssistant.Persistence.Repositories;

internal sealed class EfWebAutomationRepository(IPcAssistantDbContext dbContext) : IWebAutomationRepository
{
    public async Task AddProjectAsync(WebAutomationProject project, CancellationToken cancellationToken = default)
    {
        await dbContext.WebAutomationProjects.AddAsync(project, cancellationToken);
    }

    public Task<WebAutomationProject?> GetProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return ProjectQuery()
            .FirstOrDefaultAsync(project => project.Id == projectId && !project.IsDeleted, cancellationToken);
    }

    public async Task<IReadOnlyList<WebAutomationProject>> ListProjectsAsync(CancellationToken cancellationToken = default)
    {
        return await ProjectQuery()
            .Where(project => !project.IsDeleted)
            .OrderByDescending(project => project.UpdatedAtUtc ?? project.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);
    }

    public Task<WebAutomationFlow?> GetFlowAsync(Guid flowId, CancellationToken cancellationToken = default)
    {
        return dbContext.WebAutomationFlows
            .Include(flow => flow.Steps)
            .Include(flow => flow.Runs)
                .ThenInclude(run => run.StepLogs)
            .FirstOrDefaultAsync(flow => flow.Id == flowId && !flow.IsDeleted, cancellationToken);
    }

    public async Task AddFlowAsync(WebAutomationFlow flow, CancellationToken cancellationToken = default)
    {
        await dbContext.WebAutomationFlows.AddAsync(flow, cancellationToken);
    }

    public async Task AddStepAsync(WebAutomationStep step, CancellationToken cancellationToken = default)
    {
        await dbContext.WebAutomationSteps.AddAsync(step, cancellationToken);
    }

    public Task<WebAutomationStep?> GetStepAsync(Guid stepId, CancellationToken cancellationToken = default)
    {
        return dbContext.WebAutomationSteps.FirstOrDefaultAsync(step => step.Id == stepId, cancellationToken);
    }

    public void DeleteStep(WebAutomationStep step)
    {
        dbContext.WebAutomationSteps.Remove(step);
    }

    public Task<int> CountStepsAsync(Guid flowId, CancellationToken cancellationToken = default)
    {
        return dbContext.WebAutomationSteps.CountAsync(step => step.FlowId == flowId, cancellationToken);
    }

    private IQueryable<WebAutomationProject> ProjectQuery()
    {
        return dbContext.WebAutomationProjects
            .Include(project => project.Flows)
                .ThenInclude(flow => flow.Steps)
            .Include(project => project.Flows)
                .ThenInclude(flow => flow.Runs)
                    .ThenInclude(run => run.StepLogs);
    }
}
