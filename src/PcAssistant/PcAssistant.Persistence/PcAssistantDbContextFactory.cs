using Microsoft.EntityFrameworkCore;

namespace PcAssistant.Persistence;

internal sealed class PcAssistantDbContextFactory(
    DbContextOptions<PcAssistantDbContext> options) : IDbContextFactory<PcAssistantDbContext>
{
    public PcAssistantDbContext CreateDbContext()
    {
        SQLitePCL.Batteries_V2.Init();
        return new PcAssistantDbContext(options);
    }
}
