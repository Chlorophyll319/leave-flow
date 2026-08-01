using System.ComponentModel.DataAnnotations;

namespace LeaveFlow.Models;

public enum LeaveType
{
    Annual = 0,
    Sick = 1,
    Personal = 2,
    Other = 3
}

public enum LeaveStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3
}

public class LeaveRequest
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public Employee? Employee { get; set; }

    public LeaveType LeaveType { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public bool IsHourly { get; set; }

    public TimeOnly? StartTime { get; set; }

    public TimeOnly? EndTime { get; set; }

    [Required]
    [MaxLength(200)]
    public string Reason { get; set; } = string.Empty;

    public LeaveStatus Status { get; set; } = LeaveStatus.Pending;

    public DateTime CreatedAt { get; set; }

    [MaxLength(200)]
    public string? DecisionNote { get; set; }

    public DateTime? DecidedAt { get; set; }

    // 曆天數含頭尾；DateOnly 沒有減法運算子，須用 DayNumber 相減
    public int Days => EndDate.DayNumber - StartDate.DayNumber + 1;

    // 全站共用上班時段，不因人、不因日期而異
    private static readonly TimeOnly WorkDayStart = new(9, 0);
    private static readonly TimeOnly WorkDayEnd = new(18, 0);
    private static readonly TimeOnly LunchBreakStart = new(12, 0);
    private static readonly TimeOnly LunchBreakEnd = new(13, 0);

    // 以小時計時長；四捨五入至 0.5 小時
    // 同日與跨日皆套用固定上班時段與午休扣除；跨日另加中間完整天數（每天固定 8 小時），
    // 首尾（或同日單段）時段夾在上班時段內並扣除與午休重疊的時間
    public double Hours
    {
        get
        {
            double rawHours;
            if (StartDate == EndDate)
            {
                rawHours = NetWorkHours(StartTime ?? TimeOnly.MinValue, EndTime ?? TimeOnly.MinValue);
            }
            else
            {
                var firstDay = NetWorkHours(StartTime ?? TimeOnly.MinValue, WorkDayEnd);
                var middleDays = (Days - 2) * NetWorkHours(WorkDayStart, WorkDayEnd);
                var lastDay = NetWorkHours(WorkDayStart, EndTime ?? TimeOnly.MinValue);
                rawHours = firstDay + middleDays + lastDay;
            }

            return Math.Round(rawHours * 2, MidpointRounding.AwayFromZero) / 2;
        }
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
