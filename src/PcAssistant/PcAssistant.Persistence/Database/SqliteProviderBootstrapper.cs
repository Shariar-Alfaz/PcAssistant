namespace PcAssistant.Persistence.Database;

public sealed class SqliteProviderBootstrapper
{
    private static int _isInitialized;

    public void Initialize()
    {
        if (Interlocked.Exchange(ref _isInitialized, 1) == 1)
        {
            return;
        }

        SQLitePCL.Batteries_V2.Init();
    }
}
