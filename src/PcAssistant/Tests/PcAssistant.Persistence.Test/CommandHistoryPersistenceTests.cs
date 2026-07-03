using Autofac;
using Microsoft.Data.Sqlite;
using PcAssistant.Application.Abstractions;
using PcAssistant.Application.Models;
using PcAssistant.Persistence;
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
        var first = await history.RecordPredictionAsync(
            "open settings",
            CreateResponse("unknown", 0.89),
            CreateParsedCommand("open settings", "unknown"),
            "unknown",
            "I could not understand this as a PC command. Please rephrase it.",
            lowConfidenceRequiresConfirmation: false);
        await Task.Delay(10);
        var second = await history.RecordPredictionAsync(
            "restart pc",
            CreateResponse("system.restart", 0.95),
            CreateParsedCommand("restart pc", "system.restart"),
            "system.restart",
            "Please confirm before running. Restart this PC after a short delay.",
            lowConfidenceRequiresConfirmation: false);

        var recent = await history.ListRecentAsync(50);

        recent.Select(item => item.Id).ShouldBe(new[] { first.Id, second.Id });
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
}
