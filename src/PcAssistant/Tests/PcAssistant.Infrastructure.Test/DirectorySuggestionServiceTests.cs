using PcAssistant.Infrastructure.Platform;
using Shouldly;

namespace PcAssistant.Infrastructure.Test;

[TestFixture]
public sealed class DirectorySuggestionServiceTests
{
    [Test]
    public async Task SuggestAsync_returns_drive_roots_first()
    {
        var service = new DirectorySuggestionService();

        var suggestions = await service.SuggestAsync(null, 16);

        suggestions.ShouldNotBeEmpty();
        suggestions[0].Kind.ShouldBe("drive");
        suggestions.ShouldContain(suggestion => suggestion.Kind == "drive");
    }
}
