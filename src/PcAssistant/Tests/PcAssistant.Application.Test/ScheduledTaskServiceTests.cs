using PcAssistant.Application.Abstractions.Repositories;
using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Application.Abstractions.UnitOfWorks;
using PcAssistant.Application.Models;
using PcAssistant.Application.UseCases;
using PcAssistant.Domain.Entity;
using Shouldly;

namespace PcAssistant.Application.Test;

[TestFixture]
public sealed class ScheduledTaskServiceTests
{
    [Test]
    public async Task QueueRestartAsync_orders_queue_by_due_time()
    {
        var repository = new FakeScheduledTaskRepository();
        var existing = ScheduledTask.QueueRestart(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddMinutes(5),
            DateTimeOffset.UtcNow,
            "Later restart.");
        existing.SetQueuePosition(1);
        repository.Tasks.Add(existing);
        var service = new ScheduledTaskService(new FakeUnitOfWorkFactory(repository), new FakeMessageAutomationService());

        var queued = await service.QueueRestartAsync(
            Guid.NewGuid(),
            Guid.NewGuid(),
            TimeSpan.FromSeconds(30),
            "Restart scheduled in 30 seconds.");

        queued.QueuePosition.ShouldBe(1);
        existing.QueuePosition.ShouldBe(2);
    }

    [Test]
    public async Task CompleteDueTasksAsync_marks_elapsed_tasks_completed()
    {
        var repository = new FakeScheduledTaskRepository();
        var due = ScheduledTask.QueueRestart(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddSeconds(-1),
            DateTimeOffset.UtcNow.AddMinutes(-1),
            "Restart scheduled.");
        due.SetQueuePosition(1);
        repository.Tasks.Add(due);
        var service = new ScheduledTaskService(new FakeUnitOfWorkFactory(repository), new FakeMessageAutomationService());

        var completed = await service.CompleteDueTasksAsync();

        completed.Count.ShouldBe(1);
        completed[0].QueueStatus.ShouldBe(ScheduledTask.CompletedStatus);
        due.CompletedAtUtc.ShouldNotBeNull();
        (await service.ListQueuedAsync()).ShouldBeEmpty();
    }

    [Test]
    public async Task CancelQueuedRestartTasksAsync_marks_restart_tasks_cancelled()
    {
        var repository = new FakeScheduledTaskRepository();
        var restart = ScheduledTask.QueueRestart(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddMinutes(5),
            DateTimeOffset.UtcNow,
            "Restart scheduled.");
        restart.SetQueuePosition(1);
        repository.Tasks.Add(restart);
        var service = new ScheduledTaskService(new FakeUnitOfWorkFactory(repository), new FakeMessageAutomationService());

        var cancelled = await service.CancelQueuedRestartTasksAsync("Pending restart cancelled.");

        cancelled.Count.ShouldBe(1);
        cancelled[0].QueueStatus.ShouldBe(ScheduledTask.CancelledStatus);
        restart.LastMessage.ShouldBe("Pending restart cancelled.");
        (await service.ListQueuedAsync()).ShouldBeEmpty();
    }

    [Test]
    public async Task CancelQueuedTaskAsync_marks_selected_task_cancelled()
    {
        var repository = new FakeScheduledTaskRepository();
        var selected = ScheduledTask.QueueRestart(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddMinutes(5),
            DateTimeOffset.UtcNow,
            "Restart scheduled.");
        selected.SetQueuePosition(1);
        var remaining = ScheduledTask.QueueRestart(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddMinutes(10),
            DateTimeOffset.UtcNow,
            "Later restart.");
        remaining.SetQueuePosition(2);
        repository.Tasks.Add(selected);
        repository.Tasks.Add(remaining);
        var service = new ScheduledTaskService(new FakeUnitOfWorkFactory(repository), new FakeMessageAutomationService());

        var cancelled = await service.CancelQueuedTaskAsync(selected.Id, "Task cancelled.");

        cancelled.ShouldNotBeNull();
        cancelled.QueueStatus.ShouldBe(ScheduledTask.CancelledStatus);
        selected.LastMessage.ShouldBe("Task cancelled.");
        var queued = await service.ListQueuedAsync();
        queued.Count.ShouldBe(1);
        queued[0].Id.ShouldBe(remaining.Id);
        remaining.QueuePosition.ShouldBe(1);
    }

    [Test]
    public async Task ScheduleMessageAutomationAsync_persists_message_payload()
    {
        var repository = new FakeScheduledTaskRepository();
        var service = new ScheduledTaskService(new FakeUnitOfWorkFactory(repository), new FakeMessageAutomationService());

        var scheduled = await service.ScheduleMessageAutomationAsync(
            new ScheduleMessageAutomationRequest(
                "Morning update",
                @"C:\Apps\Telegram.exe",
                "Telegram Desktop",
                ["Shariar", "Team"],
                "Good morning",
                DateTimeOffset.Now.AddMinutes(5),
                ScheduledTask.WeeklyRepeatMode,
                1 << (int)DateTimeOffset.Now.DayOfWeek));

        scheduled.TaskType.ShouldBe(ScheduledTask.MessageAutomationTaskType);
        scheduled.DisplayName.ShouldBe("Morning update");
        scheduled.AppPath.ShouldBe(@"C:\Apps\Telegram.exe");
        scheduled.AppDisplayName.ShouldBe("Telegram Desktop");
        scheduled.RecipientNames.ShouldBe(["Shariar", "Team"]);
        scheduled.MessageText.ShouldBe("Good morning");
        scheduled.RepeatMode.ShouldBe(ScheduledTask.WeeklyRepeatMode);
        repository.Tasks.Single().QueueStatus.ShouldBe(ScheduledTask.QueuedStatus);
    }

    [Test]
    public async Task ScheduleWebAutomationAsync_persists_flow_reference()
    {
        var repository = new FakeScheduledTaskRepository();
        var service = new ScheduledTaskService(new FakeUnitOfWorkFactory(repository), new FakeMessageAutomationService());
        var flowId = Guid.NewGuid();

        var scheduled = await service.ScheduleWebAutomationAsync(
            new ScheduleWebAutomationRequest(
                flowId,
                "Checkout flow",
                "https://example.com/checkout",
                DateTimeOffset.Now.AddMinutes(5),
                ScheduledTask.NoRepeatMode,
                RepeatDaysOfWeek: 0));

        scheduled.TaskType.ShouldBe(ScheduledTask.WebAutomationTaskType);
        scheduled.DisplayName.ShouldBe("Checkout flow");
        scheduled.AppPath.ShouldBe("https://example.com/checkout");
        scheduled.AppDisplayName.ShouldBe("Checkout flow");
        scheduled.MessageText.ShouldBe(flowId.ToString("D"));
        repository.Tasks.Single().QueueStatus.ShouldBe(ScheduledTask.QueuedStatus);
    }

    [Test]
    public async Task CancelQueuedTaskAsync_marks_selected_web_automation_cancelled()
    {
        var repository = new FakeScheduledTaskRepository();
        var flowId = Guid.NewGuid();
        var selected = ScheduledTask.QueueWebAutomation(
            flowId,
            "Checkout flow",
            "https://example.com/checkout",
            DateTimeOffset.UtcNow.AddMinutes(5),
            DateTimeOffset.UtcNow,
            ScheduledTask.NoRepeatMode,
            repeatDaysOfWeek: 0);
        selected.SetQueuePosition(1);
        repository.Tasks.Add(selected);
        var service = new ScheduledTaskService(new FakeUnitOfWorkFactory(repository), new FakeMessageAutomationService());

        var cancelled = await service.CancelQueuedTaskAsync(selected.Id, "Web automation schedule cancelled.");

        cancelled.ShouldNotBeNull();
        cancelled.TaskType.ShouldBe(ScheduledTask.WebAutomationTaskType);
        cancelled.QueueStatus.ShouldBe(ScheduledTask.CancelledStatus);
        selected.LastMessage.ShouldBe("Web automation schedule cancelled.");
        (await service.ListQueuedAsync()).ShouldBeEmpty();
    }

    [Test]
    public async Task CompleteDueTasksAsync_sends_due_one_time_message()
    {
        var repository = new FakeScheduledTaskRepository();
        var due = ScheduledTask.QueueMessageAutomation(
            "Reminder",
            @"C:\Apps\Telegram.exe",
            "Telegram Desktop",
            ["Shariar"],
            "Hello",
            DateTimeOffset.UtcNow.AddSeconds(-1),
            DateTimeOffset.UtcNow.AddMinutes(-1),
            ScheduledTask.NoRepeatMode,
            repeatDaysOfWeek: 0);
        due.SetQueuePosition(1);
        repository.Tasks.Add(due);
        var automation = new FakeMessageAutomationService();
        var service = new ScheduledTaskService(new FakeUnitOfWorkFactory(repository), automation);

        var completed = await service.CompleteDueTasksAsync();

        completed.Count.ShouldBe(1);
        completed[0].QueueStatus.ShouldBe(ScheduledTask.CompletedStatus);
        automation.SentRequests.Single().Message.ShouldBe("Hello");
        (await service.ListQueuedAsync()).ShouldBeEmpty();
    }

    private sealed class FakeUnitOfWorkFactory(FakeScheduledTaskRepository repository) : ICommandHistoryUnitOfWorkFactory
    {
        public ICommandHistoryUnitOfWork Create()
        {
            return new FakeUnitOfWork(repository);
        }
    }

    private sealed class FakeUnitOfWork(FakeScheduledTaskRepository repository) : ICommandHistoryUnitOfWork
    {
        public IChatSessionRepository ChatSessions => throw new NotSupportedException();

        public ICommandLogRepository CommandLogs => throw new NotSupportedException();

        public IScheduledTaskRepository ScheduledTasks => repository;

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeScheduledTaskRepository : IScheduledTaskRepository
    {
        public List<ScheduledTask> Tasks { get; } = new();

        public Task AddAsync(ScheduledTask task, CancellationToken cancellationToken = default)
        {
            Tasks.Add(task);
            return Task.CompletedTask;
        }

        public Task<ScheduledTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Tasks.FirstOrDefault(task => task.Id == id));
        }

        public Task<ScheduledTask?> GetQueuedByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Tasks.FirstOrDefault(
                task => task.Id == id && task.QueueStatus == ScheduledTask.QueuedStatus));
        }

        public Task<IReadOnlyList<ScheduledTask>> ListQueuedAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ScheduledTask>>(
                Tasks.Where(task => task.QueueStatus == ScheduledTask.QueuedStatus).ToArray());
        }

        public Task<IReadOnlyList<ScheduledTask>> ListByTaskTypeAsync(
            string taskType,
            string? searchText,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ScheduledTask>>(
                FilterByTaskTypeSearch(taskType, searchText)
                    .Skip(skip)
                    .Take(take)
                    .ToArray());
        }

        public Task<int> CountByTaskTypeAsync(
            string taskType,
            string? searchText,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(FilterByTaskTypeSearch(taskType, searchText).Count());
        }

        public void Delete(ScheduledTask task)
        {
            Tasks.Remove(task);
        }

        private IEnumerable<ScheduledTask> FilterByTaskTypeSearch(string taskType, string? searchText)
        {
            var query = Tasks.Where(task => task.TaskType == taskType);
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return query;
            }

            return query.Where(task =>
                task.DisplayName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                || task.QueueStatus.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                || (task.AppDisplayName?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true)
                || (task.RecipientNames?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true)
                || (task.MessageText?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true));
        }
    }

    private sealed class FakeMessageAutomationService : IMessageAutomationService
    {
        public List<SendMessageRequest> SentRequests { get; } = new();

        public Task SendMessageToSpecificPersonAsync(
            SendMessageRequest request,
            CancellationToken cancellationToken = default)
        {
            SentRequests.Add(request);
            return Task.CompletedTask;
        }
    }
}
