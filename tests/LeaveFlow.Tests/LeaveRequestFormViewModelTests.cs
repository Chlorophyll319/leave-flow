using System.ComponentModel.DataAnnotations;
using LeaveFlow.Models;
using LeaveFlow.Models.ViewModels;

namespace LeaveFlow.Tests;

// 這個 ViewModel 由 LeaveRequestsController 的 Create 與 Edit action 共用，
// 因此這裡的驗證測試同時涵蓋兩者的表單規則。
public class LeaveRequestFormViewModelTests
{
    private static IList<ValidationResult> Validate(LeaveRequestFormViewModel vm)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(vm, new ValidationContext(vm), results, validateAllProperties: true);
        return results;
    }

    private static LeaveRequestFormViewModel ValidFullDayRequest() => new()
    {
        EmployeeId = 1,
        LeaveType = LeaveType.Annual,
        StartDate = new DateOnly(2026, 8, 1),
        EndDate = new DateOnly(2026, 8, 1),
        Reason = "測試"
    };

    [Fact]
    public void Validate_FullDayValidRequest_HasNoErrors()
    {
        var results = Validate(ValidFullDayRequest());

        Assert.Empty(results);
    }

    [Theory]
    [InlineData(nameof(LeaveRequestFormViewModel.EmployeeId))]
    [InlineData(nameof(LeaveRequestFormViewModel.LeaveType))]
    [InlineData(nameof(LeaveRequestFormViewModel.StartDate))]
    [InlineData(nameof(LeaveRequestFormViewModel.EndDate))]
    [InlineData(nameof(LeaveRequestFormViewModel.Reason))]
    public void Validate_MissingRequiredField_ReportsErrorForThatField(string propertyName)
    {
        var vm = ValidFullDayRequest();
        typeof(LeaveRequestFormViewModel).GetProperty(propertyName)!.SetValue(vm, null);

        var results = Validate(vm);

        Assert.Contains(results, r => r.MemberNames.Contains(propertyName));
    }

    [Fact]
    public void Validate_EndDateBeforeStartDate_ReportsErrorOnEndDate()
    {
        var vm = ValidFullDayRequest();
        vm.StartDate = new DateOnly(2026, 8, 10);
        vm.EndDate = new DateOnly(2026, 8, 9);

        var results = Validate(vm);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(vm.EndDate)));
    }

    [Fact]
    public void Validate_HourlyMissingStartTime_ReportsError()
    {
        var vm = ValidFullDayRequest();
        vm.IsHourly = true;
        vm.EndTime = new TimeOnly(10, 0);

        var results = Validate(vm);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(vm.StartTime)));
    }

    [Fact]
    public void Validate_HourlyMissingEndTime_ReportsError()
    {
        var vm = ValidFullDayRequest();
        vm.IsHourly = true;
        vm.StartTime = new TimeOnly(9, 0);

        var results = Validate(vm);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(vm.EndTime)));
    }

    [Fact]
    public void Validate_HourlyEndNotAfterStart_ReportsErrorOnEndTime()
    {
        var vm = ValidFullDayRequest();
        vm.IsHourly = true;
        vm.StartTime = new TimeOnly(9, 0);
        vm.EndTime = new TimeOnly(9, 0);

        var results = Validate(vm);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(vm.EndTime)));
    }

    [Fact]
    public void Validate_HourlyValidRequest_HasNoErrors()
    {
        var vm = ValidFullDayRequest();
        vm.IsHourly = true;
        vm.StartTime = new TimeOnly(9, 0);
        vm.EndTime = new TimeOnly(10, 0);

        var results = Validate(vm);

        Assert.Empty(results);
    }
}
