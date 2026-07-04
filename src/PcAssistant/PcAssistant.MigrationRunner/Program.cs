using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PcAssistant.Persistence;

namespace PcAssistant.MigrationRunner;

public sealed class Program
{
    public static async Task Main(string[] args)
    {
        using var host = CreateHostBuilder(args).Build();
        using var scope = host.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PcAssistantDbContext>();

        await dbContext.Database.MigrateAsync();
        Console.WriteLine($"Applied PcAssistant migrations to {dbContext.Database.GetDbConnection().DataSource}.");
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                SQLitePCL.Batteries_V2.Init();
                var databasePath = ResolveDatabasePath(context.Configuration);
                services.AddDbContext<PcAssistantDbContext>(options =>
                    options.UseSqlite(
                        $"Data Source={databasePath}",
                        sqlite => sqlite.MigrationsAssembly(typeof(PcAssistantDbContext).Assembly.FullName)));
            });
    }

    private static string ResolveDatabasePath(IConfiguration configuration)
    {
        var configuredPath = configuration["PcAssistant:DatabasePath"];
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var databaseDirectory = Path.Combine(appData, "PcAssistant");
        Directory.CreateDirectory(databaseDirectory);
        return Path.Combine(databaseDirectory, "pcassistant.db");
    }
}
