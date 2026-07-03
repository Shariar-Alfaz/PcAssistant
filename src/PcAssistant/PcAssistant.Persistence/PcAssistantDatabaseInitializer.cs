using Microsoft.EntityFrameworkCore;

namespace PcAssistant.Persistence;

public sealed class PcAssistantDatabaseInitializer(IDbContextFactory<PcAssistantDbContext> dbContextFactory)
{
    public void Initialize()
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        dbContext.Database.Migrate();
    }
}
