using System.ComponentModel.DataAnnotations;

namespace LeaveFlow.Models.ViewModels;

public class LeaveRequestFormViewModel : IValidatableObject
{
    [Display(Name = "員工")]
    [Required(ErrorMessage = "請選擇員工")]
    public int? EmployeeId { get; set; }

    [Display(Name = "假別")]
    [Required(ErrorMessage = "請選擇假別")]
    public LeaveType? LeaveType { get; set; }

    [Display(Name = "開始日期")]
    [Required(ErrorMessage = "請輸入開始日期")]
    [DataType(DataType.Date)]
    public DateOnly? StartDate { get; set; }

    [Display(Name = "結束日期")]
    [Required(ErrorMessage = "請輸入結束日期")]
    [DataType(DataType.Date)]
    public DateOnly? EndDate { get; set; }

    [Display(Name = "請假理由")]
    [Required(ErrorMessage = "請填寫請假理由")]
    [MaxLength(200, ErrorMessage = "理由不得超過 200 字")]
    public string? Reason { get; set; }

    [Display(Name = "請假時段")]
    public bool IsHourly { get; set; }

    [Display(Name = "開始時間")]
    [DataType(DataType.Time)]
    public TimeOnly? StartTime { get; set; }

    [Display(Name = "結束時間")]
    [DataType(DataType.Time)]
    public TimeOnly? EndTime { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate.HasValue && EndDate.HasValue && EndDate.Value < StartDate.Value)
        {
            yield return new ValidationResult("結束日期不得早於開始日期", new[] { nameof(EndDate) });
        }

        if (IsHourly)
        {
            if (!StartTime.HasValue)
            {
                yield return new ValidationResult("請輸入開始時間", new[] { nameof(StartTime) });
            }

            if (!EndTime.HasValue)
            {
                yield return new ValidationResult("請輸入結束時間", new[] { nameof(EndTime) });
            }

            if (StartDate.HasValue && StartTime.HasValue && EndDate.HasValue && EndTime.HasValue)
            {
                var start = StartDate.Value.ToDateTime(StartTime.Value);
                var end = EndDate.Value.ToDateTime(EndTime.Value);
                if (end <= start)
                {
                    yield return new ValidationResult("結束日期時間須晚於開始日期時間", new[] { nameof(EndTime) });
                }
                else if (LeaveHoursCalculator.CalculateHours(StartDate.Value, StartTime.Value, EndDate.Value, EndTime.Value) < 0.5)
                {
                    yield return new ValidationResult("請假時段換算工時不足 0.5 小時，請調整時間", new[] { nameof(EndTime) });
                }
            }
        }
    }
}
