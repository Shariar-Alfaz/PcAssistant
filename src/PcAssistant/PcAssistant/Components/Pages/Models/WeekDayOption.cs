using System;
using System.Collections.Generic;
using System.Text;

namespace PcAssistant.Components.Pages.Models
{
    public sealed class WeekDayOption(DayOfWeek day, string label)
    {
        public DayOfWeek Day { get; } = day;
        public string Label { get; } = label;
    }
}
