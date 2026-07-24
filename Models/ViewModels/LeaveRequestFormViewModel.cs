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

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (StartDate.HasValue && EndDate.HasValue && EndDate.Value < StartDate.Value)
        {
            yield return new ValidationResult("結束日期不得早於開始日期", new[] { nameof(EndDate) });
        }
    }
}
