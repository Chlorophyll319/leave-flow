using LeaveFlow.Models;

namespace LeaveFlow.Tests;

// Hours 四捨五入至 0.5 小時；同日直接相減（短時段可能顯示 0 小時的問題另案處理，此處只驗證現況）
// 跨日採 09:00〜18:00 上班時段公式，扣除與 12:00〜13:00 午休重疊的時間
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
    public void Hours_CrossDay_TimesOutsideWorkWindow_ClampToZero()
    {
        // 23:45(8/1) → 00:15(8/2)：首尾時間都落在上班時段外，夾值後皆為 0
        var request = Hourly(new DateOnly(2026, 8, 1), new TimeOnly(23, 45), new DateOnly(2026, 8, 2), new TimeOnly(0, 15));

        Assert.Equal(0, request.Hours);
    }

    [Fact]
    public void Hours_CrossDay_TypicalTwoDay_SumsFirstAndLastDay()
    {
        // 首日 10:00~18:00 扣午休 = 7；尾日 09:00~15:00 扣午休 = 5
        var request = Hourly(new DateOnly(2026, 8, 1), new TimeOnly(10, 0), new DateOnly(2026, 8, 2), new TimeOnly(15, 0));

        Assert.Equal(12, request.Hours);
    }

    [Fact]
    public void Hours_CrossDay_ExactWorkBoundaries_CountsFullDayBothEnds()
    {
        var request = Hourly(new DateOnly(2026, 8, 1), new TimeOnly(9, 0), new DateOnly(2026, 8, 2), new TimeOnly(18, 0));

        Assert.Equal(16, request.Hours);
    }

    [Fact]
    public void Hours_CrossDay_TimesOutsideWorkWindow_ClampToBoundary()
    {
        // StartTime 早於 09:00、EndTime 晚於 18:00，夾值後等同 09:00~18:00 整天
        var request = Hourly(new DateOnly(2026, 8, 1), new TimeOnly(7, 0), new DateOnly(2026, 8, 2), new TimeOnly(20, 0));

        Assert.Equal(16, request.Hours);
    }

    [Fact]
    public void Hours_CrossDay_FirstDayStartsMidLunch_PartialOverlapDeducted()
    {
        // 首日 12:30~18:00：與午休重疊 0.5 小時（12:30~13:00）；尾日 09:00~09:00 = 0
        var request = Hourly(new DateOnly(2026, 8, 1), new TimeOnly(12, 30), new DateOnly(2026, 8, 2), new TimeOnly(9, 0));

        Assert.Equal(5, request.Hours);
    }

    [Fact]
    public void Hours_CrossDay_LastDayEndsMidLunch_PartialOverlapDeducted()
    {
        // 首日 18:00~18:00 = 0；尾日 09:00~12:30：與午休重疊 0.5 小時（12:00~12:30）
        var request = Hourly(new DateOnly(2026, 8, 1), new TimeOnly(18, 0), new DateOnly(2026, 8, 2), new TimeOnly(12, 30));

        Assert.Equal(3, request.Hours);
    }

    [Fact]
    public void Hours_CrossDay_FirstDayStartsAfterLunch_NoOverlapDeducted()
    {
        // 首日 14:00~18:00：已過午休時段，不扣除；尾日 09:00~09:00 = 0
        var request = Hourly(new DateOnly(2026, 8, 1), new TimeOnly(14, 0), new DateOnly(2026, 8, 2), new TimeOnly(9, 0));

        Assert.Equal(4, request.Hours);
    }

    [Fact]
    public void Hours_CrossDay_LastDayEndsBeforeLunch_NoOverlapDeducted()
    {
        // 首日 18:00~18:00 = 0；尾日 09:00~11:00：尚未到午休時段，不扣除
        var request = Hourly(new DateOnly(2026, 8, 1), new TimeOnly(18, 0), new DateOnly(2026, 8, 2), new TimeOnly(11, 0));

        Assert.Equal(2, request.Hours);
    }

    [Fact]
    public void Hours_CrossDay_MultipleMiddleDays_EachCountsEightHours()
    {
        // 8/1~8/4 共 4 天：首尾各 8 小時，中間 2 個完整天，每天固定 8 小時
        var request = Hourly(new DateOnly(2026, 8, 1), new TimeOnly(9, 0), new DateOnly(2026, 8, 4), new TimeOnly(18, 0));

        Assert.Equal(32, request.Hours);
    }

    [Fact]
    public void Hours_CrossDay_RoundsToNearestHalfHour()
    {
        // 首日 10:15~18:00 扣午休 = 6.75，四捨五入至 0.5 小時 → 7.0
        var request = Hourly(new DateOnly(2026, 8, 1), new TimeOnly(10, 15), new DateOnly(2026, 8, 2), new TimeOnly(9, 0));

        Assert.Equal(7, request.Hours);
    }
}
