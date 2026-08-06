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

    // 以小時計時長；四捨五入至 0.5 小時，計算規則見 LeaveHoursCalculator
    public double Hours => LeaveHoursCalculator.CalculateHours(
        StartDate, StartTime ?? TimeOnly.MinValue, EndDate, EndTime ?? TimeOnly.MinValue);
}
