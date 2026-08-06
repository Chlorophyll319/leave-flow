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

    [Fact]
    public void Validate_HourlySameDayFullyWithinLunchBreak_ReportsErrorOnEndTime()
    {
        var vm = ValidFullDayRequest();
        vm.IsHourly = true;
        vm.StartTime = new TimeOnly(12, 0);
        vm.EndTime = new TimeOnly(13, 0);

        var results = Validate(vm);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(vm.EndTime)));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(14)]
    public void Validate_HourlySameDayUnderFifteenMinutes_ReportsErrorOnEndTime(int minutes)
    {
        var vm = ValidFullDayRequest();
        vm.IsHourly = true;
        vm.StartTime = new TimeOnly(9, 0);
        vm.EndTime = new TimeOnly(9, 0).AddMinutes(minutes);

        var results = Validate(vm);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(vm.EndTime)));
    }

    [Fact]
    public void Validate_HourlySameDayExactlyFifteenMinutes_HasNoErrors()
    {
        var vm = ValidFullDayRequest();
        vm.IsHourly = true;
        vm.StartTime = new TimeOnly(9, 0);
        vm.EndTime = new TimeOnly(9, 15);

        var results = Validate(vm);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_HourlySameDayPartiallyWithinLunchBreak_NetsToZero_ReportsErrorOnEndTime()
    {
        // 11:50~12:20：經過時間 30 分鐘，但與午休重疊 20 分鐘，扣除後淨工時僅 10 分鐘，
        // 驗證判斷的是扣除午休後的淨工時，而非單純經過時間
        var vm = ValidFullDayRequest();
        vm.IsHourly = true;
        vm.StartTime = new TimeOnly(11, 50);
        vm.EndTime = new TimeOnly(12, 20);

        var results = Validate(vm);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(vm.EndTime)));
    }

    [Fact]
    public void Validate_HourlySameDayFullyOutsideWorkHours_ReportsErrorOnEndTime()
    {
        var vm = ValidFullDayRequest();
        vm.IsHourly = true;
        vm.StartTime = new TimeOnly(19, 0);
        vm.EndTime = new TimeOnly(20, 0);

        var results = Validate(vm);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(vm.EndTime)));
    }

    [Fact]
    public void Validate_HourlyCrossDayAdjacentTimesOutsideWorkWindow_ReportsErrorOnEndTime()
    {
        // 23:45(8/1) → 00:15(8/2)：首尾時間都落在上班時段外，換算後為 0
        var vm = ValidFullDayRequest();
        vm.IsHourly = true;
        vm.StartTime = new TimeOnly(23, 45);
        vm.EndDate = new DateOnly(2026, 8, 2);
        vm.EndTime = new TimeOnly(0, 15);

        var results = Validate(vm);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(vm.EndTime)));
    }

    [Fact]
    public void Validate_HourlyCrossDayWithMiddleFullWorkdays_HasNoErrors()
    {
        // 23:45(8/1) → 00:15(8/4)：首尾單獨換算皆為 0，但中間 8/2、8/3 為完整工作日（各 8 小時），
        // 驗證不會因首尾落在上班時段外就誤擋含中間完整工作日的跨日申請
        var vm = ValidFullDayRequest();
        vm.IsHourly = true;
        vm.StartTime = new TimeOnly(23, 45);
        vm.EndDate = new DateOnly(2026, 8, 4);
        vm.EndTime = new TimeOnly(0, 15);

        var results = Validate(vm);

        Assert.Empty(results);
    }
}
