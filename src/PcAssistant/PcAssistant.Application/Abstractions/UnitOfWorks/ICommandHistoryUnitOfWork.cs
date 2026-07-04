using PcAssistant.Application.Abstractions.Repositories;
using PcAssistant.Application.Abstractions.Services;

namespace PcAssistant.Application.Abstractions.UnitOfWorks;

public interface ICommandHistoryUnitOfWork : IAsyncDisposable
{
    IChatSessionRepository ChatSessions { get; }

    ICommandLogRepository CommandLogs { get; }

    IScheduledTaskRepository ScheduledTasks { get; }

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
