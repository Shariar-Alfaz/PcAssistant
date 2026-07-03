using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PcAssistant.Persistence;

public sealed class DesignTimePcAssistantDbContextFactory : IDesignTimeDbContextFactory<PcAssistantDbContext>
{
    public PcAssistantDbContext CreateDbContext(string[] args)
    {
        SQLitePCL.Batteries_V2.Init();
        var options = new DbContextOptionsBuilder<PcAssistantDbContext>()
            .UseSqlite("Data Source=pcassistant.design.db")
            .Options;

        return new PcAssistantDbContext(options);
    }
}
