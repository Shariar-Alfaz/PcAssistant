using Autofac;
using Autofac.Extras.Moq;
using Moq;
using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Application.Models;
using PcAssistant.Infrastructure.Commands;
using PcAssistant.Infrastructure.Commands.Handlers;
using PcAssistant.Infrastructure.Commands.Parsing;
using PcAssistant.Infrastructure.DependencyInjection;
using Shouldly;

namespace PcAssistant.Infrastructure.Test;

[TestFixture]
public sealed class CommandParserServiceTests
{
    private AutoMock _mock = null!;

    [SetUp]
    public void SetUp()
    {
        _mock = AutoMock.GetLoose(builder =>
        {
            builder.RegisterType<ExplicitPathExtractor>();
            builder.RegisterType<LocationAliasResolver>();
            builder.RegisterType<FolderNameExtractor>();
        });
    }

    [TearDown]
    public void TearDown()
    {
        _mock.Dispose();
    }

    [Test]
    public void Parse_uses_first_handler_that_returns_result()
    {
        var first = _mock.Mock<ICommandParserHandler>();
        var second = new Mock<ICommandParserHandler>();
        var expected = new ParsedCommand
        {
            CommandLabel = "system.restart",
            OriginalText = "restart computer",
            RequiresConfirmation = true,
            Preview = "Restart this PC after a short delay.",
        };

        first.Setup(handler => handler.TryParse(It.IsAny<CommandParseContext>()))
            .Returns((ParsedCommand?)null);
        first.SetupGet(handler => handler.Order).Returns(100);
        second.Setup(handler => handler.TryParse(It.IsAny<CommandParseContext>()))
            .Returns(expected);
        second.SetupGet(handler => handler.Order).Returns(200);

        var parser = _mock.Create<CommandParserService>(
            new TypedParameter(
                typeof(IEnumerable<ICommandParserHandler>),
                new[] { first.Object, second.Object }));

        var actual = parser.Parse("system.restart", "restart computer", requiresConfirmation: true);

        actual.ShouldBe(expected);
        first.Verify(handler => handler.TryParse(It.Is<CommandParseContext>(context =>
            context.CommandLabel == "system.restart"
            && context.Text == "restart computer"
            && context.RequiresConfirmation)), Times.Once);
        second.Verify(handler => handler.TryParse(It.IsAny<CommandParseContext>()), Times.Once);
    }

    [Test]
    public void Parse_orders_handlers_by_declared_order()
    {
        var later = new Mock<ICommandParserHandler>();
        var earlier = new Mock<ICommandParserHandler>();
        var expected = new ParsedCommand
        {
            CommandLabel = "filesystem.create_folder",
            OriginalText = "create folder Reports",
            Preview = "handled",
        };

        later.SetupGet(handler => handler.Order).Returns(200);
        later.Setup(handler => handler.TryParse(It.IsAny<CommandParseContext>()))
            .Returns(new ParsedCommand { CommandLabel = "wrong", OriginalText = "create folder Reports" });
        earlier.SetupGet(handler => handler.Order).Returns(100);
        earlier.Setup(handler => handler.TryParse(It.IsAny<CommandParseContext>()))
            .Returns(expected);

        var parser = _mock.Create<CommandParserService>(
            new TypedParameter(
                typeof(IEnumerable<ICommandParserHandler>),
                new[] { later.Object, earlier.Object }));

        var actual = parser.Parse("filesystem.create_folder", "create folder Reports", requiresConfirmation: false);

        actual.ShouldBe(expected);
        earlier.Verify(handler => handler.TryParse(It.IsAny<CommandParseContext>()), Times.Once);
        later.Verify(handler => handler.TryParse(It.IsAny<CommandParseContext>()), Times.Never);
    }

    [Test]
    public void Infrastructure_module_resolves_parser_with_real_handler_chain()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule(new PcAssistantInfrastructureModule());
        using var container = builder.Build();

        var parser = container.Resolve<ICommandParserService>();

        var parsed = parser.Parse("system.restart", "restart pc", requiresConfirmation: false);

        parsed.CommandLabel.ShouldBe("system.restart");
        parsed.RequiresConfirmation.ShouldBeTrue();
        parsed.Preview.ShouldBe("Restart this PC after 30 seconds.");
    }

    [Test]
    public void Folder_handler_parses_create_folder_in_downloads()
    {
        var safety = _mock.Mock<ICommandSafetyValidator>();
        safety.Setup(validator => validator.ValidateFileSystemTarget(It.IsAny<string>()))
            .Returns((string? path) => SafetyValidationResult.Allowed(Path.GetFullPath(path!)));

        var handler = _mock.Create<FolderCommandParserHandler>();
        var context = new CommandParseContext(
            "filesystem.create_folder",
            "create a folder named Reports in downloads",
            RequiresConfirmation: false);

        var parsed = handler.TryParse(context);

        parsed.ShouldNotBeNull();
        parsed.CommandLabel.ShouldBe("filesystem.create_folder");
        parsed.FolderName.ShouldBe("Reports");
        parsed.LocationAlias.ShouldBe("downloads");
        parsed.LocationType.ShouldBe("known-folder");
        parsed.RequiresConfirmation.ShouldBeFalse();
        parsed.ResolvedPath.ShouldNotBeNull();
        parsed.ResolvedPath.ShouldContain("Downloads");
        parsed.ResolvedPath.ShouldContain("Reports");
        safety.Verify(validator => validator.ValidateFileSystemTarget(It.Is<string>(path =>
            path.Contains("Downloads", StringComparison.OrdinalIgnoreCase)
            && path.Contains("Reports", StringComparison.OrdinalIgnoreCase))), Times.Once);
    }

    [Test]
    public void Folder_handler_combines_named_folder_with_explicit_location_path()
    {
        var safety = _mock.Mock<ICommandSafetyValidator>();
        safety.Setup(validator => validator.ValidateFileSystemTarget(It.IsAny<string>()))
            .Returns((string? path) => SafetyValidationResult.Allowed(Path.GetFullPath(path!)));

        var handler = _mock.Create<FolderCommandParserHandler>();
        var downloadsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads");
        var context = new CommandParseContext(
            "filesystem.create_folder",
            $"create a folder named CT under {downloadsPath}",
            RequiresConfirmation: false);

        var parsed = handler.TryParse(context);

        parsed.ShouldNotBeNull();
        parsed.CommandLabel.ShouldBe("filesystem.create_folder");
        parsed.FolderName.ShouldBe("CT");
        parsed.ResolvedPath.ShouldBe(Path.Combine(downloadsPath, "CT"));
    }

    [Test]
    public void Folder_handler_combines_named_folder_with_drive_root_path()
    {
        var safety = _mock.Mock<ICommandSafetyValidator>();
        safety.Setup(validator => validator.ValidateFileSystemTarget(It.IsAny<string>()))
            .Returns((string? path) => SafetyValidationResult.Allowed(Path.GetFullPath(path!)));

        var handler = _mock.Create<FolderCommandParserHandler>();
        var context = new CommandParseContext(
            "filesystem.create_folder",
            @"create me a folder called Chat on D:\",
            RequiresConfirmation: false);

        var parsed = handler.TryParse(context);

        parsed.ShouldNotBeNull();
        parsed.CommandLabel.ShouldBe("filesystem.create_folder");
        parsed.FolderName.ShouldBe("Chat");
        parsed.LocationAlias.ShouldBe("explicit path");
        parsed.LocationType.ShouldBe("absolute");
        parsed.ResolvedPath.ShouldBe(@"D:\Chat");
    }

    [Test]
    public void Folder_handler_supports_singular_download_alias()
    {
        var safety = _mock.Mock<ICommandSafetyValidator>();
        safety.Setup(validator => validator.ValidateFileSystemTarget(It.IsAny<string>()))
            .Returns((string? path) => SafetyValidationResult.Allowed(Path.GetFullPath(path!)));

        var handler = _mock.Create<FolderCommandParserHandler>();
        var context = new CommandParseContext(
            "filesystem.create_folder",
            "create folder named CT under download",
            RequiresConfirmation: false);

        var parsed = handler.TryParse(context);

        parsed.ShouldNotBeNull();
        parsed.CommandLabel.ShouldBe("filesystem.create_folder");
        parsed.FolderName.ShouldBe("CT");
        parsed.ResolvedPath.ShouldNotBeNull();
        parsed.ResolvedPath!.ShouldContain("Downloads");
        parsed.ResolvedPath.ShouldEndWith($"{Path.DirectorySeparatorChar}CT");
    }

    [Test]
    public void Folder_handler_requires_confirmation_for_delete_folder()
    {
        var safety = _mock.Mock<ICommandSafetyValidator>();
        safety.Setup(validator => validator.ValidateFileSystemTarget(It.IsAny<string>()))
            .Returns((string? path) => SafetyValidationResult.Allowed(Path.GetFullPath(path!)));

        var handler = _mock.Create<FolderCommandParserHandler>();
        var context = new CommandParseContext(
            "filesystem.delete_folder",
            "delete the folder named Old Backup from documents",
            RequiresConfirmation: false);

        var parsed = handler.TryParse(context);

        parsed.ShouldNotBeNull();
        parsed.CommandLabel.ShouldBe("filesystem.delete_folder");
        parsed.FolderName.ShouldBe("Old Backup");
        parsed.LocationAlias.ShouldBe("documents");
        parsed.RequiresConfirmation.ShouldBeTrue();
    }

    [Test]
    public void Folder_handler_returns_blocked_result_when_safety_validator_blocks_path()
    {
        var safety = _mock.Mock<ICommandSafetyValidator>();
        safety.Setup(validator => validator.ValidateFileSystemTarget(It.IsAny<string>()))
            .Returns(SafetyValidationResult.Blocked("This path is protected."));

        var handler = _mock.Create<FolderCommandParserHandler>();
        var context = new CommandParseContext(
            "filesystem.create_folder",
            @"create folder C:\Windows\Test",
            RequiresConfirmation: false);

        var parsed = handler.TryParse(context);

        parsed.ShouldNotBeNull();
        parsed.IsDangerous.ShouldBeTrue();
        parsed.ErrorMessage.ShouldBe("This path is protected.");
        parsed.Preview.ShouldBe("This path is protected.");
    }

    [Test]
    public void File_system_path_handler_parses_open_folder_from_known_location()
    {
        var handler = _mock.Create<FileSystemPathCommandParserHandler>();
        var context = new CommandParseContext(
            "filesystem.open_folder",
            "open downloads folder",
            RequiresConfirmation: false);

        var parsed = handler.TryParse(context);

        parsed.ShouldNotBeNull();
        parsed.CommandLabel.ShouldBe("filesystem.open_folder");
        parsed.LocationAlias.ShouldBe("downloads");
        parsed.LocationType.ShouldBe("known-folder");
        parsed.RequiresConfirmation.ShouldBeFalse();
        parsed.ResolvedPath.ShouldNotBeNull();
        parsed.ResolvedPath!.ShouldContain("Downloads");
        parsed.Preview.ShouldStartWith("Open folder:");
    }

    [Test]
    public void File_system_path_handler_parses_file_details_in_known_location()
    {
        var handler = _mock.Create<FileSystemPathCommandParserHandler>();
        var context = new CommandParseContext(
            "filesystem.get_file_details",
            "give file details for report.pdf in downloads",
            RequiresConfirmation: false);

        var parsed = handler.TryParse(context);

        parsed.ShouldNotBeNull();
        parsed.CommandLabel.ShouldBe("filesystem.get_file_details");
        parsed.LocationAlias.ShouldBe("downloads");
        parsed.ResolvedPath.ShouldNotBeNull();
        parsed.ResolvedPath!.ShouldContain("Downloads");
        parsed.ResolvedPath.ShouldEndWith($"{Path.DirectorySeparatorChar}report.pdf");
        parsed.Preview.ShouldStartWith("Get file details:");
    }

    [Test]
    public void File_system_path_handler_parses_file_details_from_explicit_path()
    {
        var handler = _mock.Create<FileSystemPathCommandParserHandler>();
        var context = new CommandParseContext(
            "filesystem.get_file_details",
            @"show metadata for C:\Temp\Quarterly Report.xlsx",
            RequiresConfirmation: false);

        var parsed = handler.TryParse(context);

        parsed.ShouldNotBeNull();
        parsed.CommandLabel.ShouldBe("filesystem.get_file_details");
        parsed.LocationAlias.ShouldBe("explicit path");
        parsed.LocationType.ShouldBe("absolute");
        parsed.ResolvedPath.ShouldBe(@"C:\Temp\Quarterly Report.xlsx");
    }

    [Test]
    public void Restart_handler_requires_confirmation()
    {
        var handler = new RestartCommandParserHandler();
        var context = new CommandParseContext("system.restart", "restart pc", RequiresConfirmation: false);

        var parsed = handler.TryParse(context);

        parsed.ShouldNotBeNull();
        parsed.CommandLabel.ShouldBe("system.restart");
        parsed.RequiresConfirmation.ShouldBeTrue();
    }

    [Test]
    public void Restart_handler_parses_requested_delay()
    {
        var handler = new RestartCommandParserHandler();
        var context = new CommandParseContext("system.restart", "restart my pc after 1 minute", RequiresConfirmation: false);

        var parsed = handler.TryParse(context);

        parsed.ShouldNotBeNull();
        parsed.RestartDelay.ShouldBe(TimeSpan.FromMinutes(1));
        parsed.Preview.ShouldBe("Restart this PC after 1 minute.");
    }

    [Test]
    public void Unknown_handler_returns_friendly_unknown()
    {
        var handler = new UnknownCommandParserHandler();
        var context = new CommandParseContext("unknown", "sing a song", RequiresConfirmation: false);

        var parsed = handler.TryParse(context);

        parsed.ShouldNotBeNull();
        parsed.CommandLabel.ShouldBe("unknown");
        parsed.Preview.ShouldBe("I could not understand this as a PC command.");
    }
}
