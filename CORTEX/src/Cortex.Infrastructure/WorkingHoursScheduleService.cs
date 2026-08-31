using Cortex.Core.Models;

namespace Cortex.Infrastructure;

/// <summary>
/// Turns a user-configured <see cref="IndexingScheduleSettings"/> ("only re-index between
/// 08:00 and 20:00 on weekdays, every 60 minutes") into a concrete answer: are we inside
/// the window right now, when does the next run happen, and how long is left until then?
/// The Settings page and Job Center both bind directly to <see cref="GetStatus"/>.
/// </summary>
public sealed class WorkingHoursScheduleService
{
    public ScheduleStatus GetStatus(IndexingScheduleSettings settings, DateTimeOffset nowLocal)
    {
        var windowLength = settings.WindowEnd - settings.WindowStart;
        if (!settings.Enabled || settings.ActiveDays.Count == 0)
        {
            return new ScheduleStatus(false, nowLocal, TimeSpan.Zero, windowLength);
        }

        for (var dayOffset = 0; dayOffset < 8; dayOffset++)
        {
            var candidateDate = nowLocal.Date.AddDays(dayOffset);
            if (!settings.ActiveDays.Contains(candidateDate.DayOfWeek)) continue;

            var windowStart = new DateTimeOffset(candidateDate, nowLocal.Offset) + settings.WindowStart;
            var windowEnd = new DateTimeOffset(candidateDate, nowLocal.Offset) + settings.WindowEnd;

            if (dayOffset == 0 && nowLocal >= windowStart && nowLocal < windowEnd)
            {
                // Inside today's window: the next run is the next interval boundary from windowStart.
                var elapsedMinutes = (nowLocal - windowStart).TotalMinutes;
                var intervalsElapsed = Math.Floor(elapsedMinutes / settings.IntervalMinutes) + 1;
                var nextRun = windowStart.AddMinutes(intervalsElapsed * settings.IntervalMinutes);
                if (nextRun > windowEnd) continue; // no more runs fit before the window closes today; fall through to next active day

                return new ScheduleStatus(true, nextRun, nextRun - nowLocal, windowLength);
            }

            if (windowStart > nowLocal)
            {
                return new ScheduleStatus(false, windowStart, windowStart - nowLocal, windowLength);
            }
        }

        // No active day found in the next week (shouldn't normally happen with ActiveDays non-empty).
        return new ScheduleStatus(false, nowLocal, TimeSpan.Zero, windowLength);
    }
}
