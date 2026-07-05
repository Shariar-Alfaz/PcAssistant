namespace PcAssistant.Application.Models;

public sealed record ScheduledTaskPage(
    IReadOnlyList<ScheduledTaskItem> Items,
    int PageNumber,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)Math.Max(1, PageSize)));
}
