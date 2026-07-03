using Autofac;
using Microsoft.EntityFrameworkCore;
using PcAssistant.Application.Abstractions;
using PcAssistant.Application.UseCases;

namespace PcAssistant.Persistence.DependencyInjection;

public sealed class PcAssistantPersistenceModule(string databasePath) : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        var connectionString = $"Data Source={databasePath}";

        builder.Register(_ =>
            new DbContextOptionsBuilder<PcAssistantDbContext>()
                .UseSqlite(connectionString)
                .Options)
            .SingleInstance();

        builder.RegisterType<PcAssistantDbContextFactory>()
            .As<IDbContextFactory<PcAssistantDbContext>>()
            .SingleInstance();

        builder.RegisterType<CommandHistoryUnitOfWorkFactory>()
            .As<ICommandHistoryUnitOfWorkFactory>()
            .SingleInstance();

        builder.RegisterType<CommandHistoryService>()
            .As<ICommandHistoryService>()
            .SingleInstance();

        builder.RegisterType<PcAssistantDatabaseInitializer>()
            .SingleInstance();
    }
}
