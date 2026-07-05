using PcAssistant.Application.Abstractions.Services;

namespace PcAssistant.Services;

public sealed class ScheduledTaskRunner(IServiceScopeFactory scopeFactory) : IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private readonly SemaphoreSlim _gate = new(1, 1);
    private Timer? _timer;

    public void Start()
    {
        _timer ??= new Timer(
            _ => _ = RunDueTasksAsync(),
            null,
            TimeSpan.Zero,
            PollInterval);
    }

    private async Task RunDueTasksAsync()
    {
        if (!await _gate.WaitAsync(0))
        {
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var scheduledTasks = scope.ServiceProvider.GetRequiredService<IScheduledTaskService>();
            await scheduledTasks.CompleteDueTasksAsync();
        }
        catch
        {
            // Scheduled task failures are stored on the task by the application service.
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _gate.Dispose();
    }
}
