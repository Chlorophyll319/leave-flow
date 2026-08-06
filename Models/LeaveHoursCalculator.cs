namespace LeaveFlow.Models;

// 全站共用的以小時計時數計算邏輯，供 LeaveRequest.Hours 與表單驗證共用，
// 避免上班時段／午休規則各自實作出現不一致
public static class LeaveHoursCalculator
{
    // 全站共用上班時段，不因人、不因日期而異
    private static readonly TimeOnly WorkDayStart = new(9, 0);
    private static readonly TimeOnly WorkDayEnd = new(18, 0);
    private static readonly TimeOnly LunchBreakStart = new(12, 0);
    private static readonly TimeOnly LunchBreakEnd = new(13, 0);

    // 以小時計時長；四捨五入至 0.5 小時
    // 同日與跨日皆套用固定上班時段與午休扣除；跨日另加中間完整天數（每天固定 8 小時），
    // 首尾（或同日單段）時段夾在上班時段內並扣除與午休重疊的時間
    public static double CalculateHours(DateOnly startDate, TimeOnly startTime, DateOnly endDate, TimeOnly endTime)
    {
        double rawHours;
        if (startDate == endDate)
        {
            rawHours = NetWorkHours(startTime, endTime);
        }
        else
        {
            var firstDay = NetWorkHours(startTime, WorkDayEnd);
            var days = endDate.DayNumber - startDate.DayNumber + 1;
            var middleDays = (days - 2) * NetWorkHours(WorkDayStart, WorkDayEnd);
            var lastDay = NetWorkHours(WorkDayStart, endTime);
            rawHours = firstDay + middleDays + lastDay;
        }

        return Math.Round(rawHours * 2, MidpointRounding.AwayFromZero) / 2;
    }

    // from/to 夾在上班時段內後的淨工時，精確扣除與午休重疊的時間；夾值後區間為空則回傳 0
    private static double NetWorkHours(TimeOnly from, TimeOnly to)
    {
        var clampedFrom = Clamp(from);
        var clampedTo = Clamp(to);
        if (clampedTo <= clampedFrom)
        {
            return 0;
        }

        var totalHours = (clampedTo - clampedFrom).TotalHours;
        var lunchOverlapStart = clampedFrom > LunchBreakStart ? clampedFrom : LunchBreakStart;
        var lunchOverlapEnd = clampedTo < LunchBreakEnd ? clampedTo : LunchBreakEnd;
        var lunchOverlap = lunchOverlapEnd > lunchOverlapStart ? (lunchOverlapEnd - lunchOverlapStart).TotalHours : 0;

        return totalHours - lunchOverlap;
    }

    private static TimeOnly Clamp(TimeOnly time)
    {
        if (time < WorkDayStart) return WorkDayStart;
        if (time > WorkDayEnd) return WorkDayEnd;
        return time;
    }
}
