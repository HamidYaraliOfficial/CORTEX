namespace Cortex.Core.Models;

/// <summary>
/// User-defined "working hours" for automated background re-indexing of a workspace.
/// CORTEX never re-indexes outside this window (to avoid competing with the developer's
/// own build/CPU usage during the day, for example), and always tells the user exactly
/// when the next run is and how long is left until it happens.
/// </summary>
public sealed class IndexingScheduleSettings
{
    /// <summary>Whether scheduled background indexing is enabled at all.</summary>
    public bool Enabled { get; set; }

    /// <summary>Local time-of-day the allowed window opens, e.g. 08:00.</summary>
    public TimeSpan WindowStart { get; set; } = new(8, 0, 0);

    /// <summary>Local time-of-day the allowed window closes, e.g. 20:00.</summary>
    public TimeSpan WindowEnd { get; set; } = new(20, 0, 0);

    /// <summary>Which days of the week the window applies to.</summary>
    public HashSet<DayOfWeek> ActiveDays { get; set; } = new()
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
    };

    /// <summary>How often, inside the window, CORTEX should re-index (minutes).</summary>
    public int IntervalMinutes { get; set; } = 60;
}

/// <summary>
/// The computed answer to "when does CORTEX run next, and how long until then?" —
/// produced by Cortex.Infrastructure.WorkingHoursScheduleService from an
/// <see cref="IndexingScheduleSettings"/> and the current local time.
/// </summary>
public sealed record ScheduleStatus(
    bool IsInsideWindowNow,
    DateTimeOffset NextRunAtLocal,
    TimeSpan TimeUntilNextRun,
    TimeSpan DailyWindowLength);
