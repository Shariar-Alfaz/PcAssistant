using Autofac.Extras.Moq;
using PcAssistant.Application.Models;
using PcAssistant.Infrastructure.Platform;
using Shouldly;

namespace PcAssistant.Infrastructure.Test;

[TestFixture]
public sealed class WindowsCommandExecutorTests
{
    private AutoMock _mock = null!;
    private string _tempDirectory = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _mock = AutoMock.GetLoose();
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"pcassistant-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        _mock.Dispose();
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Test]
    public async Task ExecuteAsync_returns_file_details_for_existing_file()
    {
        var path = Path.Combine(_tempDirectory, "report.txt");
        await File.WriteAllTextAsync(path, "hello");
        var executor = _mock.Create<WindowsCommandExecutor>();
        var command = new ParsedCommand
        {
            CommandLabel = "filesystem.get_file_details",
            OriginalText = "give file details",
            PathText = path,
            ResolvedPath = path,
        };

        var result = await executor.ExecuteAsync(command, confirmed: false);

        result.Succeeded.ShouldBeTrue();
        result.Message.ShouldContain($"File details: {path}");
        result.Message.ShouldContain("Name: report.txt");
        result.Message.ShouldContain("Base name: report");
        result.Message.ShouldContain("Extension: .txt");
        result.Message.ShouldContain("Size: 5 B");
        result.Message.ShouldContain("Owner:");
        result.Message.ShouldContain("Created:");
        result.Message.ShouldContain("Modified:");
        result.Message.ShouldContain("Accessed:");
        result.Message.ShouldContain("Read-only:");
        result.Message.ShouldContain("Hidden:");
        result.Message.ShouldContain("System file:");
        result.Message.ShouldContain("Archive:");
        result.Message.ShouldContain("Attributes:");
    }

    [Test]
    public async Task ExecuteAsync_returns_failure_when_file_details_target_is_missing()
    {
        var path = Path.Combine(_tempDirectory, "missing.txt");
        var executor = _mock.Create<WindowsCommandExecutor>();
        var command = new ParsedCommand
        {
            CommandLabel = "filesystem.get_file_details",
            OriginalText = "give file details",
            PathText = path,
            ResolvedPath = path,
        };

        var result = await executor.ExecuteAsync(command, confirmed: false);

        result.Succeeded.ShouldBeFalse();
        result.Message.ShouldBe($"File not found: {path}");
    }
}
