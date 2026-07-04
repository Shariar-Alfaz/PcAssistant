using Autofac;
using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Infrastructure.Api;
using PcAssistant.Infrastructure.Commands;
using PcAssistant.Infrastructure.Commands.Handlers;
using PcAssistant.Infrastructure.Commands.Parsing;
using PcAssistant.Infrastructure.Platform;

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
            .SingleInstance();

        builder.RegisterType<CommandAiClient>()
            .As<ICommandAiClient>()
            .SingleInstance();

        builder.RegisterType<CommandSafetyValidator>()
            .As<ICommandSafetyValidator>()
            .SingleInstance();

        builder.RegisterType<CommandParserService>()
            .As<ICommandParserService>()
            .SingleInstance();

        builder.RegisterType<WindowsCommandExecutor>()
            .As<IWindowsCommandExecutor>()
            .SingleInstance();

        builder.RegisterType<DirectorySuggestionService>()
            .As<IDirectorySuggestionService>()
            .SingleInstance();

        builder.RegisterType<LocationPickerService>()
            .As<ILocationPickerService>()
            .SingleInstance();

        builder.RegisterType<ExplicitPathExtractor>().SingleInstance();
        builder.RegisterType<LocationAliasResolver>().SingleInstance();
        builder.RegisterType<FolderNameExtractor>().SingleInstance();

        builder.RegisterType<EmptyCommandParserHandler>().As<ICommandParserHandler>().SingleInstance();
        builder.RegisterType<FolderCommandParserHandler>().As<ICommandParserHandler>().SingleInstance();
        builder.RegisterType<FileSystemPathCommandParserHandler>().As<ICommandParserHandler>().SingleInstance();
        builder.RegisterType<RestartCommandParserHandler>().As<ICommandParserHandler>().SingleInstance();
        builder.RegisterType<CancelRestartCommandParserHandler>().As<ICommandParserHandler>().SingleInstance();
        builder.RegisterType<UnknownCommandParserHandler>().As<ICommandParserHandler>().SingleInstance();
    }
}
