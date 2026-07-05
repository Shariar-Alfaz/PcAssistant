using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Application.Abstractions.UnitOfWorks;
using PcAssistant.Application.Models;
using PcAssistant.Domain.Entity;
using PcAssistant.Domain.Enums;

namespace PcAssistant.Application.UseCases;

public sealed class WebAutomationProjectService(
    IWebAutomationUnitOfWorkFactory unitOfWorkFactory,
    IWebAutomationService automationService) : IWebAutomationProjectService
{
    public async Task<WebAutomationOperationResult<WebAutomationProjectDto>> CreateProjectAsync(
        CreateWebAutomationProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateUrl(request.StartUrl, "Project URL");
        if (validation is not null)
        {
            return WebAutomationOperationResult<WebAutomationProjectDto>.Failure(validation);
        }

        await using var unitOfWork = unitOfWorkFactory.Create();
        try
        {
            var now = DateTime.UtcNow;
            var project = WebAutomationProject.Create(request.Name, request.Description, request.StartUrl, now);
            await unitOfWork.Automations.AddProjectAsync(project, cancellationToken);

            var flow = WebAutomationFlow.Create(project.Id, "Default flow", request.StartUrl, headless: false, "msedge", usePersistentSession: true, 30000, now);
            await unitOfWork.Automations.AddFlowAsync(flow, cancellationToken);
            await AddExampleStepsAsync(unitOfWork, flow.Id, now, cancellationToken);

            await unitOfWork.CommitAsync(cancellationToken);
            var saved = await unitOfWork.Automations.GetProjectAsync(project.Id, cancellationToken);
            return WebAutomationOperationResult<WebAutomationProjectDto>.Success(ToProjectDto(saved ?? project));
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            return WebAutomationOperationResult<WebAutomationProjectDto>.Failure(CleanMessage(ex));
        }
    }

    public async Task<WebAutomationOperationResult<WebAutomationProjectDto>> UpdateProjectAsync(
        UpdateWebAutomationProjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateUrl(request.StartUrl, "Project URL");
        if (validation is not null)
        {
            return WebAutomationOperationResult<WebAutomationProjectDto>.Failure(validation);
        }

        await using var unitOfWork = unitOfWorkFactory.Create();
        var project = await unitOfWork.Automations.GetProjectAsync(request.ProjectId, cancellationToken);
        if (project is null)
        {
            return WebAutomationOperationResult<WebAutomationProjectDto>.Failure("Web automation project was not found.");
        }

        try
        {
            project.Update(request.Name, request.Description, request.StartUrl, DateTime.UtcNow);
            await unitOfWork.CommitAsync(cancellationToken);
            return WebAutomationOperationResult<WebAutomationProjectDto>.Success(ToProjectDto(project));
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            return WebAutomationOperationResult<WebAutomationProjectDto>.Failure(CleanMessage(ex));
        }
    }

    public async Task<WebAutomationOperationResult<bool>> DeleteProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var project = await unitOfWork.Automations.GetProjectAsync(projectId, cancellationToken);
        if (project is null)
        {
            return WebAutomationOperationResult<bool>.Failure("Web automation project was not found.");
        }

        try
        {
            unitOfWork.Automations.DeleteProject(project);
            await unitOfWork.CommitAsync(cancellationToken);
            return WebAutomationOperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            return WebAutomationOperationResult<bool>.Failure(CleanMessage(ex));
        }
    }

    public async Task<IReadOnlyList<WebAutomationProjectDto>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var projects = await unitOfWork.Automations.ListProjectsAsync(cancellationToken);
        return projects.Select(ToProjectDto).ToArray();
    }

    public async Task<WebAutomationProjectDto?> GetProjectDetailsAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var project = await unitOfWork.Automations.GetProjectAsync(projectId, cancellationToken);
        return project is null ? null : ToProjectDto(project);
    }

    public async Task<WebAutomationOperationResult<WebAutomationFlowDto>> CreateFlowAsync(
        CreateWebAutomationFlowRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateUrl(request.StartUrl, "Flow URL") ?? ValidateTimeout(request.DefaultTimeoutMs);
        if (validation is not null)
        {
            return WebAutomationOperationResult<WebAutomationFlowDto>.Failure(validation);
        }

        await using var unitOfWork = unitOfWorkFactory.Create();
        try
        {
            var flow = WebAutomationFlow.Create(request.ProjectId, request.Name, request.StartUrl, request.Headless, request.BrowserChannel, request.UsePersistentSession, request.DefaultTimeoutMs, DateTime.UtcNow);
            await unitOfWork.Automations.AddFlowAsync(flow, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return WebAutomationOperationResult<WebAutomationFlowDto>.Success(ToFlowDto(flow));
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            return WebAutomationOperationResult<WebAutomationFlowDto>.Failure(CleanMessage(ex));
        }
    }

    public async Task<WebAutomationOperationResult<WebAutomationFlowDto>> UpdateFlowAsync(
        UpdateWebAutomationFlowRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateUrl(request.StartUrl, "Flow URL") ?? ValidateTimeout(request.DefaultTimeoutMs);
        if (validation is not null)
        {
            return WebAutomationOperationResult<WebAutomationFlowDto>.Failure(validation);
        }

        await using var unitOfWork = unitOfWorkFactory.Create();
        var flow = await unitOfWork.Automations.GetFlowAsync(request.FlowId, cancellationToken);
        if (flow is null)
        {
            return WebAutomationOperationResult<WebAutomationFlowDto>.Failure("Web automation flow was not found.");
        }

        try
        {
            flow.Update(request.Name, request.StartUrl, request.Headless, request.BrowserChannel, request.UsePersistentSession, request.DefaultTimeoutMs, DateTime.UtcNow);
            await unitOfWork.CommitAsync(cancellationToken);
            return WebAutomationOperationResult<WebAutomationFlowDto>.Success(ToFlowDto(flow));
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            return WebAutomationOperationResult<WebAutomationFlowDto>.Failure(CleanMessage(ex));
        }
    }

    public async Task<WebAutomationOperationResult<WebAutomationStepDto>> AddStepAsync(
        CreateWebAutomationStepRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateStep(request.StepType, request.Selector, request.Value, request.Url, request.TimeoutMs, request.RetryCount);
        if (validation is not null)
        {
            return WebAutomationOperationResult<WebAutomationStepDto>.Failure(validation);
        }

        await using var unitOfWork = unitOfWorkFactory.Create();
        try
        {
            var nextOrder = await unitOfWork.Automations.CountStepsAsync(request.FlowId, cancellationToken) + 1;
            var step = WebAutomationStep.Create(request.FlowId, nextOrder, request.StepType, request.SelectorType, request.Selector, request.Value, request.Url, request.Description, request.TimeoutMs, request.DelayAfterMs, request.IsOptional, request.TakeScreenshotAfterStep, request.RetryCount, DateTime.UtcNow);
            await unitOfWork.Automations.AddStepAsync(step, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return WebAutomationOperationResult<WebAutomationStepDto>.Success(ToStepDto(step));
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            return WebAutomationOperationResult<WebAutomationStepDto>.Failure(CleanMessage(ex));
        }
    }

    public async Task<WebAutomationOperationResult<WebAutomationStepDto>> UpdateStepAsync(
        UpdateWebAutomationStepRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateStep(request.StepType, request.Selector, request.Value, request.Url, request.TimeoutMs, request.RetryCount);
        if (validation is not null)
        {
            return WebAutomationOperationResult<WebAutomationStepDto>.Failure(validation);
        }

        await using var unitOfWork = unitOfWorkFactory.Create();
        var step = await unitOfWork.Automations.GetStepAsync(request.StepId, cancellationToken);
        if (step is null)
        {
            return WebAutomationOperationResult<WebAutomationStepDto>.Failure("Automation step was not found.");
        }

        try
        {
            step.Update(request.StepType, request.SelectorType, request.Selector, request.Value, request.Url, request.Description, request.TimeoutMs, request.DelayAfterMs, request.IsOptional, request.TakeScreenshotAfterStep, request.RetryCount, DateTime.UtcNow);
            await unitOfWork.CommitAsync(cancellationToken);
            return WebAutomationOperationResult<WebAutomationStepDto>.Success(ToStepDto(step));
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            return WebAutomationOperationResult<WebAutomationStepDto>.Failure(CleanMessage(ex));
        }
    }

    public async Task<WebAutomationOperationResult<bool>> DeleteStepAsync(Guid stepId, CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var step = await unitOfWork.Automations.GetStepAsync(stepId, cancellationToken);
        if (step is null)
        {
            return WebAutomationOperationResult<bool>.Failure("Automation step was not found.");
        }

        try
        {
            var flowId = step.FlowId;
            unitOfWork.Automations.DeleteStep(step);
            await unitOfWork.CommitAsync(cancellationToken);
            await NormalizeStepOrderAsync(flowId, cancellationToken);
            return WebAutomationOperationResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            return WebAutomationOperationResult<bool>.Failure(CleanMessage(ex));
        }
    }

    public async Task<WebAutomationOperationResult<WebAutomationStepDto>> DuplicateStepAsync(Guid stepId, CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var source = await unitOfWork.Automations.GetStepAsync(stepId, cancellationToken);
        if (source is null)
        {
            return WebAutomationOperationResult<WebAutomationStepDto>.Failure("Automation step was not found.");
        }

        try
        {
            var nextOrder = await unitOfWork.Automations.CountStepsAsync(source.FlowId, cancellationToken) + 1;
            var duplicate = WebAutomationStep.Create(source.FlowId, nextOrder, source.StepType, source.SelectorType, source.Selector, source.Value, source.Url, source.Description, source.TimeoutMs, source.DelayAfterMs, source.IsOptional, source.TakeScreenshotAfterStep, source.RetryCount, DateTime.UtcNow);
            await unitOfWork.Automations.AddStepAsync(duplicate, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return WebAutomationOperationResult<WebAutomationStepDto>.Success(ToStepDto(duplicate));
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            return WebAutomationOperationResult<WebAutomationStepDto>.Failure(CleanMessage(ex));
        }
    }

    public async Task<WebAutomationOperationResult<IReadOnlyList<WebAutomationStepDto>>> ReorderStepsAsync(
        ReorderWebAutomationStepsRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var flow = await unitOfWork.Automations.GetFlowAsync(request.FlowId, cancellationToken);
        if (flow is null)
        {
            return WebAutomationOperationResult<IReadOnlyList<WebAutomationStepDto>>.Failure("Web automation flow was not found.");
        }

        var orderedIds = request.StepIds.Distinct().ToArray();
        if (orderedIds.Length != flow.Steps.Count)
        {
            return WebAutomationOperationResult<IReadOnlyList<WebAutomationStepDto>>.Failure("Step order must include every step exactly once.");
        }

        var stepsById = flow.Steps.ToDictionary(step => step.Id);
        if (orderedIds.Any(id => !stepsById.ContainsKey(id)))
        {
            return WebAutomationOperationResult<IReadOnlyList<WebAutomationStepDto>>.Failure("Step order contains an unknown step.");
        }

        try
        {
            var now = DateTime.UtcNow;
            var temporaryOrderIndex = orderedIds.Length + 1;
            foreach (var step in flow.Steps.OrderBy(step => step.OrderIndex))
            {
                step.SetOrderIndex(temporaryOrderIndex++, now);
            }

            await unitOfWork.CommitAsync(cancellationToken);

            now = DateTime.UtcNow;
            for (var index = 0; index < orderedIds.Length; index++)
            {
                stepsById[orderedIds[index]].SetOrderIndex(index + 1, now);
            }

            await unitOfWork.CommitAsync(cancellationToken);
            return WebAutomationOperationResult<IReadOnlyList<WebAutomationStepDto>>.Success(flow.Steps.OrderBy(step => step.OrderIndex).Select(ToStepDto).ToArray());
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            return WebAutomationOperationResult<IReadOnlyList<WebAutomationStepDto>>.Failure(CleanMessage(ex));
        }
    }

    public async Task<WebAutomationOperationResult<WebAutomationRunDto>> RunFlowAsync(
        RunWebAutomationFlowRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var flow = await unitOfWork.Automations.GetFlowAsync(request.FlowId, cancellationToken);
        if (flow is null)
        {
            return WebAutomationOperationResult<WebAutomationRunDto>.Failure("Web automation flow was not found.");
        }

        if (RequiresConfirmation(flow) && !request.RequireConfirmation)
        {
            return WebAutomationOperationResult<WebAutomationRunDto>.Failure("This flow contains click or submit-like steps. Confirm before running it on an unknown website.");
        }

        try
        {
            var runResult = await automationService.RunFlowAsync(ToFlowDto(flow), request.RunHeaded, cancellationToken);
            await PersistRunAsync(unitOfWork, runResult, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);
            return WebAutomationOperationResult<WebAutomationRunDto>.Success(runResult);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(cancellationToken);
            return WebAutomationOperationResult<WebAutomationRunDto>.Failure(CleanMessage(ex));
        }
    }

    public async Task<WebAutomationOperationResult<WebAutomationRunStepLogDto>> RunSingleStepAsync(
        Guid flowId,
        Guid stepId,
        CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var flow = await unitOfWork.Automations.GetFlowAsync(flowId, cancellationToken);
        var step = flow?.Steps.FirstOrDefault(item => item.Id == stepId);
        if (flow is null || step is null)
        {
            return WebAutomationOperationResult<WebAutomationRunStepLogDto>.Failure("Automation step was not found.");
        }

        try
        {
            var log = await automationService.RunSingleStepAsync(ToFlowDto(flow), ToStepDto(step), runHeaded: true, cancellationToken);
            return WebAutomationOperationResult<WebAutomationRunStepLogDto>.Success(log);
        }
        catch (Exception ex)
        {
            return WebAutomationOperationResult<WebAutomationRunStepLogDto>.Failure(CleanMessage(ex));
        }
    }

    public async Task<IReadOnlyList<WebAutomationRunDto>> GetRunLogsAsync(Guid flowId, CancellationToken cancellationToken = default)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var runs = await unitOfWork.Runs.ListRunsAsync(flowId, cancellationToken);
        return runs.Select(ToRunDto).ToArray();
    }

    private static async Task AddExampleStepsAsync(
        IWebAutomationUnitOfWork unitOfWork,
        Guid flowId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var steps = new[]
        {
            WebAutomationStep.Create(flowId, 1, WebAutomationStepType.GotoUrl, WebSelectorType.Css, null, null, "https://example.com", "Open example.com", 30000, 250, false, false, 0, now),
            WebAutomationStep.Create(flowId, 2, WebAutomationStepType.WaitForSelector, WebSelectorType.Css, "h1", null, null, "Wait for heading", 30000, 250, false, false, 0, now),
            WebAutomationStep.Create(flowId, 3, WebAutomationStepType.ExtractText, WebSelectorType.Css, "h1", null, null, "Extract heading text", 30000, 250, false, false, 0, now),
            WebAutomationStep.Create(flowId, 4, WebAutomationStepType.Screenshot, WebSelectorType.Css, null, null, null, "Take screenshot", 30000, 0, false, true, 0, now),
        };

        foreach (var step in steps)
        {
            await unitOfWork.Automations.AddStepAsync(step, cancellationToken);
        }
    }

    private async Task NormalizeStepOrderAsync(Guid flowId, CancellationToken cancellationToken)
    {
        await using var unitOfWork = unitOfWorkFactory.Create();
        var flow = await unitOfWork.Automations.GetFlowAsync(flowId, cancellationToken);
        if (flow is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var index = 1;
        foreach (var step in flow.Steps.OrderBy(step => step.OrderIndex).ThenBy(step => step.CreatedAtUtc))
        {
            step.SetOrderIndex(index++, now);
        }

        await unitOfWork.CommitAsync(cancellationToken);
    }

    private static async Task PersistRunAsync(
        IWebAutomationUnitOfWork unitOfWork,
        WebAutomationRunDto runResult,
        CancellationToken cancellationToken)
    {
        var run = WebAutomationRun.Start(runResult.Id, runResult.FlowId, runResult.BrowserChannel, runResult.Headless, runResult.StartedAtUtc);
        run.Complete(runResult.Status, runResult.ErrorMessage, runResult.CompletedAtUtc ?? DateTime.UtcNow);
        await unitOfWork.Runs.AddRunAsync(run, cancellationToken);

        foreach (var log in runResult.StepLogs)
        {
            var persisted = WebAutomationRunStepLog.Create(run.Id, log.StepId, log.OrderIndex, log.Status, log.StartedAtUtc, log.CompletedAtUtc, log.Message, log.ErrorMessage, log.ScreenshotPath, log.ExtractedValue);
            await unitOfWork.Runs.AddStepLogAsync(persisted, cancellationToken);
        }
    }

    private static bool RequiresConfirmation(WebAutomationFlow flow)
    {
        return flow.Steps.Any(step => step.StepType is WebAutomationStepType.Click or WebAutomationStepType.Check or WebAutomationStepType.Uncheck);
    }

    private static string? ValidateStep(
        WebAutomationStepType stepType,
        string? selector,
        string? value,
        string? url,
        int timeoutMs,
        int retryCount)
    {
        if (ValidateTimeout(timeoutMs) is string timeoutError)
        {
            return timeoutError;
        }

        if (retryCount is < 0 or > 5)
        {
            return "Retry count must be between 0 and 5.";
        }

        if (stepType == WebAutomationStepType.GotoUrl)
        {
            return ValidateUrl(url, "Step URL");
        }

        if (RequiresSelector(stepType)
            && string.IsNullOrWhiteSpace(selector))
        {
            return $"{stepType} requires a selector.";
        }

        if (stepType is WebAutomationStepType.Fill or WebAutomationStepType.Type or WebAutomationStepType.Press or WebAutomationStepType.AssertText
            && string.IsNullOrWhiteSpace(value))
        {
            return $"{stepType} requires a value.";
        }

        return null;
    }

    private static bool RequiresSelector(WebAutomationStepType stepType)
    {
        return stepType is WebAutomationStepType.Click
            or WebAutomationStepType.Fill
            or WebAutomationStepType.Type
            or WebAutomationStepType.WaitForSelector
            or WebAutomationStepType.SelectOption
            or WebAutomationStepType.Check
            or WebAutomationStepType.Uncheck
            or WebAutomationStepType.Hover
            or WebAutomationStepType.ExtractText
            or WebAutomationStepType.AssertText
            or WebAutomationStepType.AssertVisible;
    }

    private static string? ValidateTimeout(int timeoutMs)
    {
        return timeoutMs is < 1000 or > 120000
            ? "Timeout must be between 1000 and 120000 milliseconds."
            : null;
    }

    private static string? ValidateUrl(string? url, string fieldName)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            return $"{fieldName} must be an absolute http:// or https:// URL.";
        }

        return null;
    }

    private static WebAutomationProjectDto ToProjectDto(WebAutomationProject project)
    {
        return new WebAutomationProjectDto(
            project.Id,
            project.Name,
            project.Description,
            project.StartUrl,
            project.CreatedAtUtc,
            project.UpdatedAtUtc,
            project.Flows.Where(flow => !flow.IsDeleted).OrderBy(flow => flow.CreatedAtUtc).Select(ToFlowDto).ToArray());
    }

    private static WebAutomationFlowDto ToFlowDto(WebAutomationFlow flow)
    {
        return new WebAutomationFlowDto(
            flow.Id,
            flow.ProjectId,
            flow.Name,
            flow.StartUrl,
            flow.Headless,
            flow.BrowserChannel,
            flow.UsePersistentSession,
            flow.DefaultTimeoutMs,
            flow.CreatedAtUtc,
            flow.UpdatedAtUtc,
            flow.Steps.OrderBy(step => step.OrderIndex).Select(ToStepDto).ToArray());
    }

    private static WebAutomationStepDto ToStepDto(WebAutomationStep step)
    {
        return new WebAutomationStepDto(
            step.Id,
            step.FlowId,
            step.OrderIndex,
            step.StepType,
            step.SelectorType,
            step.Selector,
            step.Value,
            step.Url,
            step.Description,
            step.TimeoutMs,
            step.DelayAfterMs,
            step.IsOptional,
            step.TakeScreenshotAfterStep,
            step.RetryCount,
            step.CreatedAtUtc,
            step.UpdatedAtUtc);
    }

    private static WebAutomationRunDto ToRunDto(WebAutomationRun run)
    {
        return new WebAutomationRunDto(
            run.Id,
            run.FlowId,
            run.Status,
            run.StartedAtUtc,
            run.CompletedAtUtc,
            run.ErrorMessage,
            run.BrowserChannel,
            run.Headless,
            run.StepLogs.OrderBy(log => log.OrderIndex).Select(ToStepLogDto).ToArray());
    }

    private static WebAutomationRunStepLogDto ToStepLogDto(WebAutomationRunStepLog log)
    {
        return new WebAutomationRunStepLogDto(
            log.Id,
            log.RunId,
            log.StepId,
            log.OrderIndex,
            log.Status,
            log.StartedAtUtc,
            log.CompletedAtUtc,
            log.Message,
            log.ErrorMessage,
            log.ScreenshotPath,
            log.ExtractedValue);
    }

    private static string CleanMessage(Exception exception)
    {
        return exception is ArgumentException or InvalidOperationException
            ? exception.Message
            : "Web automation failed. Check the run log for details.";
    }
}
