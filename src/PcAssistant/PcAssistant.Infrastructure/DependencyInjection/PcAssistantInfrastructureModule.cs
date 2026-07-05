using Autofac;
using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Infrastructure.Api;
using PcAssistant.Infrastructure.Commands;
using PcAssistant.Infrastructure.Commands.Handlers;
using PcAssistant.Infrastructure.Commands.Parsing;
using PcAssistant.Infrastructure.Platform;
using PcAssistant.Infrastructure.WebAutomation;

namespace PcAssistant.Infrastructure.DependencyInjection;

public sealed class PcAssistantInfrastructureModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.Register(_ => new HttpClient
        {
            BaseAddress = new Uri("http://127.0.0.1:8000/"),
            Timeout = TimeSpan.FromSeconds(20),
        })
            .InstancePerLifetimeScope();

        builder.RegisterType<CommandAiClient>()
            .As<ICommandAiClient>()
            .InstancePerLifetimeScope();

        builder.RegisterType<CommandSafetyValidator>()
            .As<ICommandSafetyValidator>()
            .InstancePerLifetimeScope();

        builder.RegisterType<CommandParserService>()
            .As<ICommandParserService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<WindowsCommandExecutor>()
            .As<IWindowsCommandExecutor>()
            .InstancePerLifetimeScope();

        builder.RegisterType<MessageAutomationService>()
            .As<IMessageAutomationService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<DirectorySuggestionService>()
            .As<IDirectorySuggestionService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<LocationPickerService>()
            .As<ILocationPickerService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<InstalledAppDiscoveryService>()
            .As<IInstalledAppDiscoveryService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<PythonAiApiProcessService>()
            .As<IAiApiProcessService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<PlaywrightWebAutomationService>()
            .As<IWebAutomationService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<PlaywrightWebPreviewService>()
            .As<IWebPreviewService>()
            .InstancePerLifetimeScope();

        builder.RegisterType<ExplicitPathExtractor>().InstancePerLifetimeScope();
        builder.RegisterType<LocationAliasResolver>().InstancePerLifetimeScope();
        builder.RegisterType<FolderNameExtractor>().InstancePerLifetimeScope();

        builder.RegisterType<EmptyCommandParserHandler>().As<ICommandParserHandler>().InstancePerLifetimeScope();
        builder.RegisterType<FolderCommandParserHandler>().As<ICommandParserHandler>().InstancePerLifetimeScope();
        builder.RegisterType<FileSystemPathCommandParserHandler>().As<ICommandParserHandler>().InstancePerLifetimeScope();
        builder.RegisterType<RestartCommandParserHandler>().As<ICommandParserHandler>().InstancePerLifetimeScope();
        builder.RegisterType<CancelRestartCommandParserHandler>().As<ICommandParserHandler>().InstancePerLifetimeScope();
        builder.RegisterType<UnknownCommandParserHandler>().As<ICommandParserHandler>().InstancePerLifetimeScope();
    }
}
