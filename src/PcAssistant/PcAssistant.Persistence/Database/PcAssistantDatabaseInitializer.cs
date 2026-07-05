using Autofac;
using Microsoft.EntityFrameworkCore;

namespace PcAssistant.Persistence.Database;

public sealed class PcAssistantDatabaseInitializer(
    SqliteProviderBootstrapper sqliteProviderBootstrapper,
    ILifetimeScope lifetimeScope)
{
    public void Initialize()
    {
        sqliteProviderBootstrapper.Initialize();
        using var scope = lifetimeScope.BeginLifetimeScope();
        var dbContext = scope.Resolve<PcAssistantDbContext>();
        dbContext.Database.Migrate();
    }
}
