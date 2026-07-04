using Autofac.Extras.Moq;
using Moq;
using PcAssistant.Application.Abstractions.Repositories;
using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Application.Abstractions.UnitOfWorks;
using PcAssistant.Application.Models;
using PcAssistant.Application.UseCases;
using PcAssistant.Domain.Entity;
using Shouldly;

namespace PcAssistant.Application.Test;

[TestFixture]
public sealed class CommandHistoryServiceTests
{
    private AutoMock _mock = null!;

    [SetUp]
    public void SetUp()
    {
        _mock = AutoMock.GetLoose();
    }

    [TearDown]
    public void TearDown()
    {
        _mock.Dispose();
    }

    [Test]
    public async Task RecordPredictionAsync_persists_command_response()
    {
        var chatSession = ChatSession.Create(ChatSession.DefaultTitle, DateTimeOffset.UtcNow);
        CommandLogEntry? savedEntry = null;
        _mock.Mock<ICommandHistoryUnitOfWorkFactory>()
            .Setup(factory => factory.Create())
            .Returns(_mock.Mock<ICommandHistoryUnitOfWork>().Object);
        _mock.Mock<ICommandHistoryUnitOfWork>()
            .SetupGet(unitOfWork => unitOfWork.ChatSessions)
            .Returns(_mock.Mock<IChatSessionRepository>().Object);
        _mock.Mock<ICommandHistoryUnitOfWork>()
            .SetupGet(unitOfWork => unitOfWork.CommandLogs)
            .Returns(_mock.Mock<ICommandLogRepository>().Object);
        _mock.Mock<IChatSessionRepository>()
            .Setup(repository => repository.GetByIdAsync(chatSession.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chatSession);
        _mock.Mock<ICommandLogRepository>()
            .Setup(repository => repository.AddAsync(It.IsAny<CommandLogEntry>(), It.IsAny<CancellationToken>()))
            .Callback<CommandLogEntry, CancellationToken>((entry, _) => savedEntry = entry)
            .Returns(Task.CompletedTask);
        var service = _mock.Create<CommandHistoryService>();
        var response = new CommandResponse
        {
            CommandLabel = "filesystem.create_folder",
            RawPredictedLabel = "filesystem.create_folder",
            Source = "model",
            Confidence = 0.92,
            RequiresConfirmation = false,
        };
        var parsed = new ParsedCommand
        {
            CommandLabel = "filesystem.create_folder",
            OriginalText = "create reports folder",
            Preview = "Create folder Reports.",
            ResolvedPath = @"C:\Users\User\Downloads\Reports",
        };

        var item = await service.RecordPredictionAsync(
            chatSession.Id,
            "create reports folder",
            response,
            parsed,
            "filesystem.create_folder",
            "Create folder Reports.",
            lowConfidenceRequiresConfirmation: false);

        item.UserText.ShouldBe("create reports folder");
        item.ChatSessionId.ShouldBe(chatSession.Id);
        item.CommandLabel.ShouldBe("filesystem.create_folder");
        item.ExecutionStatus.ShouldBe("pending");
        item.ResolvedPath.ShouldBe(@"C:\Users\User\Downloads\Reports");
        savedEntry.ShouldNotBeNull();
        savedEntry.Id.ShouldBe(item.Id);
        savedEntry.ChatSessionId.ShouldBe(chatSession.Id);
        chatSession.Title.ShouldBe("create reports folder");
        _mock.Mock<ICommandHistoryUnitOfWork>().Verify(unitOfWork => unitOfWork.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mock.Mock<ICommandHistoryUnitOfWork>().Verify(unitOfWork => unitOfWork.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RecordExecutionAsync_updates_existing_command()
    {
        var existing = CommandLogEntry.CreatePrediction(
            Guid.NewGuid(),
            "create reports folder",
            "filesystem.create_folder",
            "filesystem.create_folder",
            "model",
            0.92,
            requiresConfirmation: false,
            "Create folder Reports.",
            "Create folder Reports.",
            @"C:\Users\User\Downloads\Reports",
            isDangerous: false,
            DateTimeOffset.UtcNow);
        _mock.Mock<ICommandHistoryUnitOfWorkFactory>()
            .Setup(factory => factory.Create())
            .Returns(_mock.Mock<ICommandHistoryUnitOfWork>().Object);
        _mock.Mock<ICommandHistoryUnitOfWork>()
            .SetupGet(unitOfWork => unitOfWork.CommandLogs)
            .Returns(_mock.Mock<ICommandLogRepository>().Object);
        _mock.Mock<ICommandLogRepository>()
            .Setup(repository => repository.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var service = _mock.Create<CommandHistoryService>();

        var item = await service.RecordExecutionAsync(
            existing.Id,
            CommandExecutionResult.Success("Created folder."));

        item.ExecutionStatus.ShouldBe("succeeded");
        item.ExecutionMessage.ShouldBe("Created folder.");
        item.ExecutedAtUtc.ShouldNotBeNull();
        _mock.Mock<ICommandHistoryUnitOfWork>().Verify(unitOfWork => unitOfWork.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mock.Mock<ICommandHistoryUnitOfWork>().Verify(unitOfWork => unitOfWork.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UpdatePlannedTargetAsync_updates_path_before_execution()
    {
        var existing = CommandLogEntry.CreatePrediction(
            Guid.NewGuid(),
            "create reports folder",
            "filesystem.create_folder",
            "filesystem.create_folder",
            "model",
            0.92,
            requiresConfirmation: false,
            "Create folder Reports.",
            "Create folder Reports.",
            @"C:\Users\User\Downloads\Reports",
            isDangerous: false,
            DateTimeOffset.UtcNow);
        _mock.Mock<ICommandHistoryUnitOfWorkFactory>()
            .Setup(factory => factory.Create())
            .Returns(_mock.Mock<ICommandHistoryUnitOfWork>().Object);
        _mock.Mock<ICommandHistoryUnitOfWork>()
            .SetupGet(unitOfWork => unitOfWork.CommandLogs)
            .Returns(_mock.Mock<ICommandLogRepository>().Object);
        _mock.Mock<ICommandLogRepository>()
            .Setup(repository => repository.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var service = _mock.Create<CommandHistoryService>();
        var editedCommand = new ParsedCommand
        {
            CommandLabel = "filesystem.create_folder",
            Preview = @"Create folder: C:\Users\User\Desktop\Reports",
            ResolvedPath = @"C:\Users\User\Desktop\Reports",
        };

        var item = await service.UpdatePlannedTargetAsync(
            existing.Id,
            editedCommand,
            "Create folder at edited path.");

        item.ResolvedPath.ShouldBe(@"C:\Users\User\Desktop\Reports");
        item.Preview.ShouldBe(@"Create folder: C:\Users\User\Desktop\Reports");
        item.AssistantMessage.ShouldBe("Create folder at edited path.");
        _mock.Mock<ICommandHistoryUnitOfWork>().Verify(unitOfWork => unitOfWork.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mock.Mock<ICommandHistoryUnitOfWork>().Verify(unitOfWork => unitOfWork.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
