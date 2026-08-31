using Cortex.Core.Models;
using Cortex.Infrastructure;
using Xunit;

namespace Cortex.Tests;

public class WorkingHoursScheduleTests
{
    [Fact]
    public void InsideWindow_ReturnsNextIntervalBoundary()
    {
        var settings = new IndexingScheduleSettings
        {
            Enabled = true,
            WindowStart = new TimeSpan(8, 0, 0),
            WindowEnd = new TimeSpan(20, 0, 0),
            IntervalMinutes = 60,
            ActiveDays = { DayOfWeek.Monday }
        };
        var now = new DateTimeOffset(2026, 8, 31, 8, 15, 0, TimeSpan.Zero); // a Monday

        var status = new WorkingHoursScheduleService().GetStatus(settings, now);

        Assert.True(status.IsInsideWindowNow);
        Assert.Equal(new DateTimeOffset(2026, 8, 31, 9, 0, 0, TimeSpan.Zero), status.NextRunAtLocal);
        Assert.Equal(TimeSpan.FromMinutes(45), status.TimeUntilNextRun);
    }

    [Fact]
    public void OutsideWindow_ReturnsStartOfNextWindow()
    {
        var settings = new IndexingScheduleSettings
        {
            Enabled = true,
            WindowStart = new TimeSpan(8, 0, 0),
            WindowEnd = new TimeSpan(20, 0, 0),
            IntervalMinutes = 60,
            ActiveDays = { DayOfWeek.Monday }
        };
        var now = new DateTimeOffset(2026, 8, 31, 22, 0, 0, TimeSpan.Zero); // Monday night, after window closed

        var status = new WorkingHoursScheduleService().GetStatus(settings, now);

        Assert.False(status.IsInsideWindowNow);
        Assert.True(status.NextRunAtLocal > now);
    }

    [Fact]
    public void Disabled_ReturnsNotInsideWindow()
    {
        var settings = new IndexingScheduleSettings { Enabled = false };
        var status = new WorkingHoursScheduleService().GetStatus(settings, DateTimeOffset.Now);
        Assert.False(status.IsInsideWindowNow);
    }
}
