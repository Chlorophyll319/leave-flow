using LeaveFlow.Models;

namespace LeaveFlow.Tests;

// Hours 四捨五入至 0.5 小時（現況行為，短時段可能顯示 0 小時的問題另案處理，此處只驗證現況）
public class LeaveRequestHoursTests
{
    private static LeaveRequest Hourly(DateOnly startDate, TimeOnly startTime, DateOnly endDate, TimeOnly endTime) => new()
    {
        StartDate = startDate,
        EndDate = endDate,
        IsHourly = true,
        StartTime = startTime,
        EndTime = endTime
    };

    [Fact]
    public void Hours_ZeroMinutes_IsZero()
    {
        var request = Hourly(new DateOnly(2026, 8, 1), new TimeOnly(9, 0), new DateOnly(2026, 8, 1), new TimeOnly(9, 0));

        Assert.Equal(0, request.Hours);
    }

    [Fact]
    public void Hours_FifteenMinutes_RoundsUpToHalfHour()
    {
        var request = Hourly(new DateOnly(2026, 8, 1), new TimeOnly(9, 0), new DateOnly(2026, 8, 1), new TimeOnly(9, 15));

        Assert.Equal(0.5, request.Hours);
    }

    [Fact]
    public void Hours_ThirtyMinutes_IsHalfHour()
    {
        var request = Hourly(new DateOnly(2026, 8, 1), new TimeOnly(9, 0), new DateOnly(2026, 8, 1), new TimeOnly(9, 30));

        Assert.Equal(0.5, request.Hours);
    }

    [Fact]
    public void Hours_CrossesMidnight_CountsFullElapsedTime()
    {
        var request = Hourly(new DateOnly(2026, 8, 1), new TimeOnly(23, 45), new DateOnly(2026, 8, 2), new TimeOnly(0, 15));

        Assert.Equal(0.5, request.Hours);
    }
}
