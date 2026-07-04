using PcAssistant.Application.Abstractions.Services;
using PcAssistant.Application.Models;
using System.Text.RegularExpressions;

namespace PcAssistant.Infrastructure.Commands.Handlers;

public sealed class RestartCommandParserHandler : ICommandParserHandler
{
    private static readonly TimeSpan DefaultDelay = TimeSpan.FromSeconds(30);
    private static readonly Regex DelayPattern = new(
        @"\b(?:after|in|within)?\s*(?<amount>\d{1,4})\s*(?<unit>seconds?|secs?|minutes?|mins?|hours?|hrs?)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public int Order => 200;

    public ParsedCommand? TryParse(CommandParseContext context)
    {
        if (context.CommandLabel != "system.restart")
        {
            return null;
        }

        var delay = ExtractDelay(context.Text);

        return new ParsedCommand
        {
            CommandLabel = context.CommandLabel,
            OriginalText = context.Text,
            RequiresConfirmation = true,
            RestartDelay = delay,
            Preview = $"Restart this PC {FormatDelayPreview(delay)}.",
        };
    }

    private static TimeSpan ExtractDelay(string text)
    {
        var match = DelayPattern.Match(text);
        if (!match.Success || !int.TryParse(match.Groups["amount"].Value, out var amount))
        {
            return DefaultDelay;
        }

        var unit = match.Groups["unit"].Value.ToLowerInvariant();
        return unit switch
        {
            var value when value.StartsWith("hour") || value.StartsWith("hr") => TimeSpan.FromHours(amount),
            var value when value.StartsWith("minute") || value.StartsWith("min") => TimeSpan.FromMinutes(amount),
            _ => TimeSpan.FromSeconds(amount),
        };
    }

    private static string FormatDelayPreview(TimeSpan delay)
    {
        if (delay.TotalHours >= 1 && delay.TotalHours % 1 == 0)
        {
            var hours = (int)delay.TotalHours;
            return $"after {hours} {(hours == 1 ? "hour" : "hours")}";
        }

        if (delay.TotalMinutes >= 1 && delay.TotalMinutes % 1 == 0)
        {
            var minutes = (int)delay.TotalMinutes;
            return $"after {minutes} {(minutes == 1 ? "minute" : "minutes")}";
        }

        var seconds = Math.Max(1, (int)Math.Round(delay.TotalSeconds));
        return $"after {seconds} {(seconds == 1 ? "second" : "seconds")}";
    }
}
