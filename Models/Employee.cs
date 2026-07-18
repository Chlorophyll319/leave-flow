using System.ComponentModel.DataAnnotations;

namespace LeaveFlow.Models;

public class Employee
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Department { get; set; } = string.Empty;
}
