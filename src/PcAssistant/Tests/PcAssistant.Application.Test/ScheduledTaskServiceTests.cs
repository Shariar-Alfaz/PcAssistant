using PcAssistant.Application.Abstractions.Repositories;
using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Application.Abstractions.UnitOfWorks;
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
        var service = new ScheduledTaskService(new FakeUnitOfWorkFactory(repository));

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
        var service = new ScheduledTaskService(new FakeUnitOfWorkFactory(repository));

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
        var service = new ScheduledTaskService(new FakeUnitOfWorkFactory(repository));

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
        var service = new ScheduledTaskService(new FakeUnitOfWorkFactory(repository));

        var cancelled = await service.CancelQueuedTaskAsync(selected.Id, "Task cancelled.");

        cancelled.ShouldNotBeNull();
        cancelled.QueueStatus.ShouldBe(ScheduledTask.CancelledStatus);
        selected.LastMessage.ShouldBe("Task cancelled.");
        var queued = await service.ListQueuedAsync();
        queued.Count.ShouldBe(1);
        queued[0].Id.ShouldBe(remaining.Id);
        remaining.QueuePosition.ShouldBe(1);
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
    }
}
