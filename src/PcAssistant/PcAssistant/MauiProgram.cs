using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Infrastructure.DependencyInjection;
using PcAssistant.Persistence.Database;
using PcAssistant.Persistence.DependencyInjection;
using PcAssistant.Services;

namespace PcAssistant
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            var databasePath = Path.Combine(FileSystem.AppDataDirectory, "pcassistant.db");
            builder.ConfigureContainer(
                new AutofacServiceProviderFactory(),
                containerBuilder => ConfigureAutofacContainer(containerBuilder, databasePath));

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();
            app.Services.GetRequiredService<PcAssistantDatabaseInitializer>().Initialize();
            var aiApiProcess = app.Services.GetRequiredService<IAiApiProcessService>();
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                aiApiProcess.StopAsync().GetAwaiter().GetResult();
            };

            app.Services.GetRequiredService<ScheduledTaskRunner>().Start();

            return app;
        }

        private static void ConfigureAutofacContainer(ContainerBuilder builder, string databasePath)
        {
            builder.RegisterModule(new PcAssistantInfrastructureModule());
            builder.RegisterModule(new PcAssistantPersistenceModule(databasePath));
            builder.RegisterType<ScheduledTaskRunner>()
                .InstancePerLifetimeScope();
        }
    }
}
