namespace PcAssistant.Application.Abstractions;

public interface ICommandHistoryUnitOfWork : IAsyncDisposable
{
    IChatSessionRepository ChatSessions { get; }

    ICommandLogRepository CommandLogs { get; }

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
