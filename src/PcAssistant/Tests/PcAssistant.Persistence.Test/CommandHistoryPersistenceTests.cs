using Autofac;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Application.Models;
using PcAssistant.Domain.Enums;
using PcAssistant.Persistence.Database;
using PcAssistant.Persistence.DependencyInjection;
using Shouldly;

namespace PcAssistant.Persistence.Test;

[TestFixture]
public sealed class CommandHistoryPersistenceTests
{
    private string _databasePath = string.Empty;
    private IContainer _container = null!;

    [SetUp]
    public void SetUp()
    {
        _databasePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"{Guid.NewGuid():N}.db");
        var builder = new ContainerBuilder();
        builder.RegisterModule(new PcAssistantPersistenceModule(_databasePath));
        builder.RegisterType<FakeWebAutomationService>().As<IWebAutomationService>();
        _container = builder.Build();
        _container.Resolve<PcAssistantDatabaseInitializer>().Initialize();
    }

    [TearDown]
    public void TearDown()
    {
        _container.Dispose();
        SqliteConnection.ClearAllPools();
        foreach (var path in Directory.EnumerateFiles(
            Path.GetDirectoryName(_databasePath)!,
            $"{Path.GetFileName(_databasePath)}*"))
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task ListRecentAsync_orders_command_history_with_sqlite_timestamp_conversion()
    {
        var history = _container.Resolve<ICommandHistoryService>();
        var chat = await history.CreateChatAsync();
        var first = await history.RecordPredictionAsync(
            chat.Id,
            "open settings",
            CreateResponse("unknown", 0.89),
            CreateParsedCommand("open settings", "unknown"),
            "unknown",
            "I could not understand this as a PC command. Please rephrase it.",
            lowConfidenceRequiresConfirmation: false);
        await Task.Delay(10);
        var second = await history.RecordPredictionAsync(
            chat.Id,
            "restart pc",
            CreateResponse("system.restart", 0.95),
            CreateParsedCommand("restart pc", "system.restart"),
            "system.restart",
            "Please confirm before running. Restart this PC after a short delay.",
            lowConfidenceRequiresConfirmation: false);

        var recent = await history.ListRecentAsync(50);

        recent.Select(item => item.Id).ShouldBe(new[] { first.Id, second.Id });
    }

    [Test]
    public async Task Chat_sessions_can_be_loaded_and_deleted_with_their_messages()
    {
        var history = _container.Resolve<ICommandHistoryService>();
        var firstChat = await history.CreateChatAsync();
        var secondChat = await history.CreateChatAsync();

        await history.RecordPredictionAsync(
            firstChat.Id,
            "create reports folder",
            CreateResponse("filesystem.create_folder", 0.93),
            CreateParsedCommand("create reports folder", "filesystem.create_folder"),
            "filesystem.create_folder",
            "Create folder Reports.",
            lowConfidenceRequiresConfirmation: false);

        var loaded = await history.GetChatAsync(firstChat.Id);
        loaded.Summary.Title.ShouldBe("create reports folder");
        loaded.Messages.Count.ShouldBe(1);
        loaded.Messages[0].ChatSessionId.ShouldBe(firstChat.Id);

        var chats = await history.ListChatsAsync();
        chats.Select(chat => chat.Id).ShouldContain(firstChat.Id);
        chats.Select(chat => chat.Id).ShouldContain(secondChat.Id);

        await history.DeleteChatAsync(firstChat.Id);

        var remainingChats = await history.ListChatsAsync();
        remainingChats.Select(chat => chat.Id).ShouldNotContain(firstChat.Id);
        remainingChats.Select(chat => chat.Id).ShouldContain(secondChat.Id);
    }

    [Test]
    public async Task GetChatAsync_paginates_messages_from_newest_to_older_pages()
    {
        var history = _container.Resolve<ICommandHistoryService>();
        var chat = await history.CreateChatAsync();

        var messages = new List<CommandHistoryItem>();
        for (var index = 1; index <= 5; index++)
        {
            messages.Add(await history.RecordPredictionAsync(
                chat.Id,
                $"message {index}",
                CreateResponse("unknown", 0.89),
                CreateParsedCommand($"message {index}", "unknown"),
                "unknown",
                "I could not understand this as a PC command. Please rephrase it.",
                lowConfidenceRequiresConfirmation: false));
            await Task.Delay(5);
        }

        var newestPage = await history.GetChatAsync(chat.Id, messageCount: 2);

        newestPage.Messages.Select(message => message.UserText).ShouldBe(["message 4", "message 5"]);
        newestPage.HasOlderMessages.ShouldBeTrue();
        newestPage.OlderThanUtc?.ToUnixTimeMilliseconds().ShouldBe(messages[3].CreatedAtUtc.ToUnixTimeMilliseconds());

        var olderPage = await history.GetChatAsync(
            chat.Id,
            messageCount: 2,
            beforeCreatedAtUtc: newestPage.OlderThanUtc);

        olderPage.Messages.Select(message => message.UserText).ShouldBe(["message 2", "message 3"]);
        olderPage.HasOlderMessages.ShouldBeTrue();
        olderPage.OlderThanUtc?.ToUnixTimeMilliseconds().ShouldBe(messages[1].CreatedAtUtc.ToUnixTimeMilliseconds());
    }

    [Test]
    public async Task Web_automation_project_delete_removes_project_and_children_from_database()
    {
        var projects = _container.Resolve<IWebAutomationProjectService>();
        var created = await projects.CreateProjectAsync(new CreateWebAutomationProjectRequest(
            "Delete me",
            "Temporary project",
            "https://delete.example.com"));
        created.Succeeded.ShouldBeTrue();
        created.Value.ShouldNotBeNull();
        var projectId = created.Value.Id;
        var flowId = created.Value.Flows.Single().Id;

        var deleted = await projects.DeleteProjectAsync(projectId);

        deleted.Succeeded.ShouldBeTrue();
        using var scope = _container.BeginLifetimeScope();
        var dbContext = scope.Resolve<PcAssistantDbContext>();
        (await dbContext.WebAutomationProjects.AnyAsync(project => project.Id == projectId)).ShouldBeFalse();
        (await dbContext.WebAutomationFlows.AnyAsync(flow => flow.ProjectId == projectId)).ShouldBeFalse();
        (await dbContext.WebAutomationSteps.AnyAsync(step => step.FlowId == flowId)).ShouldBeFalse();
    }

    [Test]
    public async Task Web_automation_project_update_persists_project_details()
    {
        var projects = _container.Resolve<IWebAutomationProjectService>();
        var created = await projects.CreateProjectAsync(new CreateWebAutomationProjectRequest(
            "Original project",
            "Original description",
            "https://original.example.com"));
        created.Succeeded.ShouldBeTrue();
        created.Value.ShouldNotBeNull();

        var updated = await projects.UpdateProjectAsync(new UpdateWebAutomationProjectRequest(
            created.Value.Id,
            "Renamed project",
            "Updated description",
            "https://updated.example.com"));

        updated.Succeeded.ShouldBeTrue();
        updated.Value.ShouldNotBeNull();
        updated.Value.Name.ShouldBe("Renamed project");
        updated.Value.Description.ShouldBe("Updated description");
        updated.Value.StartUrl.ShouldBe("https://updated.example.com");
        var reloaded = await projects.GetProjectDetailsAsync(created.Value.Id);
        reloaded.ShouldNotBeNull();
        reloaded.Name.ShouldBe("Renamed project");
        reloaded.Description.ShouldBe("Updated description");
        reloaded.StartUrl.ShouldBe("https://updated.example.com");
    }

    [Test]
    public async Task Web_automation_steps_can_be_reordered_and_reloaded_from_database()
    {
        var projects = _container.Resolve<IWebAutomationProjectService>();
        var created = await projects.CreateProjectAsync(new CreateWebAutomationProjectRequest(
            "Reorder me",
            "Project with reorderable steps",
            "https://reorder.example.com"));
        created.Succeeded.ShouldBeTrue();
        created.Value.ShouldNotBeNull();
        var flow = created.Value.Flows.Single();
        var reversedIds = flow.Steps.OrderByDescending(step => step.OrderIndex).Select(step => step.Id).ToArray();

        var reordered = await projects.ReorderStepsAsync(new ReorderWebAutomationStepsRequest(flow.Id, reversedIds));

        reordered.Succeeded.ShouldBeTrue();
        reordered.Value.ShouldNotBeNull();
        reordered.Value.Select(step => step.Id).ShouldBe(reversedIds);
        var reloaded = await projects.GetProjectDetailsAsync(created.Value.Id);
        reloaded.ShouldNotBeNull();
        reloaded.Flows.Single().Steps.Select(step => step.Id).ShouldBe(reversedIds);
        reloaded.Flows.Single().Steps.Select(step => step.OrderIndex).ShouldBe([1, 2, 3, 4]);
    }

    [Test]
    public async Task Web_automation_step_delete_renumbers_remaining_steps()
    {
        var projects = _container.Resolve<IWebAutomationProjectService>();
        var created = await projects.CreateProjectAsync(new CreateWebAutomationProjectRequest(
            "Delete step",
            "Project with removable steps",
            "https://delete-step.example.com"));
        created.Succeeded.ShouldBeTrue();
        created.Value.ShouldNotBeNull();
        var flow = created.Value.Flows.Single();
        var removedStepId = flow.Steps.Single(step => step.OrderIndex == 2).Id;

        var deleted = await projects.DeleteStepAsync(removedStepId);

        deleted.Succeeded.ShouldBeTrue();
        var reloaded = await projects.GetProjectDetailsAsync(created.Value.Id);
        reloaded.ShouldNotBeNull();
        var steps = reloaded.Flows.Single().Steps;
        steps.Select(step => step.Id).ShouldNotContain(removedStepId);
        steps.Select(step => step.OrderIndex).ShouldBe([1, 2, 3]);
    }

    [Test]
    public async Task Web_automation_upload_file_step_can_be_created_without_default_file()
    {
        var projects = _container.Resolve<IWebAutomationProjectService>();
        var created = await projects.CreateProjectAsync(new CreateWebAutomationProjectRequest(
            "Upload file",
            "Project with upload step",
            "https://upload.example.com"));
        created.Succeeded.ShouldBeTrue();
        created.Value.ShouldNotBeNull();
        var flow = created.Value.Flows.Single();

        var added = await projects.AddStepAsync(new CreateWebAutomationStepRequest(
            flow.Id,
            WebAutomationStepType.UploadFile,
            WebSelectorType.Css,
            "input[type=\"file\"]",
            null,
            null,
            "Upload file",
            30000,
            250,
            false,
            false,
            0));

        added.Succeeded.ShouldBeTrue();
        added.Value.ShouldNotBeNull();
        added.Value.Value.ShouldBeNull();
    }

    private static CommandResponse CreateResponse(string label, double confidence)
    {
        return new CommandResponse
        {
            Input = label,
            CommandLabel = label,
            RawPredictedLabel = label,
            Source = "test",
            Confidence = confidence,
            RequiresConfirmation = label == "system.restart",
            Probabilities = new Dictionary<string, double> { [label] = confidence },
        };
    }

    private static ParsedCommand CreateParsedCommand(string text, string label)
    {
        return new ParsedCommand
        {
            CommandLabel = label,
            OriginalText = text,
            RequiresConfirmation = label == "system.restart",
            Preview = label == "system.restart"
                ? "Restart this PC after a short delay."
                : "I could not understand this as a PC command.",
        };
    }

    private sealed class FakeWebAutomationService : IWebAutomationService
    {
        public Task<WebAutomationRunDto> RunFlowAsync(
            WebAutomationFlowDto flow,
            bool runHeaded,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<WebAutomationRunStepLogDto> RunSingleStepAsync(
            WebAutomationFlowDto flow,
            WebAutomationStepDto step,
            bool runHeaded,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
