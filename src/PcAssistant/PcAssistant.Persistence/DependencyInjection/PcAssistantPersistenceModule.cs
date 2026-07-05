using Autofac;
using Microsoft.EntityFrameworkCore;
using PcAssistant.Application.Abstractions.Repositories;
using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Application.Abstractions.UnitOfWorks;
using PcAssistant.Application.UseCases;
using PcAssistant.Persistence.Database;
using PcAssistant.Persistence.Repositories;
using PcAssistant.Persistence.UnitOfWork;

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
            .InstancePerLifetimeScope();

        builder.RegisterType<SqliteProviderBootstrapper>()
            .InstancePerLifetimeScope();

        builder.RegisterType<PcAssistantDbContext>()
            .AsSelf()
            .As<IPcAssistantDbContext>()
            .InstancePerLifetimeScope();

        builder.RegisterType<EfChatSessionRepository>()
            .As<IChatSessionRepository>()
            .InstancePerLifetimeScope();

        builder.RegisterType<EfCommandLogRepository>()
            .As<ICommandLogRepository>()
            .InstancePerLifetimeScope();

        builder.RegisterType<EfScheduledTaskRepository>()
            .As<IScheduledTaskRepository>()
            .InstancePerLifetimeScope();

        builder.RegisterType<CommandHistoryUnitOfWork>()
            .AsSelf()
            .InstancePerLifetimeScope();

        builder.RegisterType<CommandHistoryUnitOfWorkFactory>()
            .As<ICommandHistoryUnitOfWorkFactory>()
            .InstancePerLifetimeScope();

        builder.RegisterType<CommandHistoryService>()
            .As<ICommandHistoryService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<ScheduledTaskService>()
            .As<IScheduledTaskService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<PcAssistantDatabaseInitializer>()
            .InstancePerLifetimeScope();
    }
}
