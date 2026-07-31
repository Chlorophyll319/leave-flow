using LeaveFlow.Controllers;
using LeaveFlow.Data;
using LeaveFlow.Models;
using LeaveFlow.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeaveFlow.Tests;

public class LeaveRequestsControllerTests
{
    private static LeaveRequestsController CreateController(AppDbContext context)
    {
        var controller = new LeaveRequestsController(context);
        ControllerTestHelper.AttachTempData(controller);
        return controller;
    }

    private static LeaveRequestFormViewModel ValidCreateVm(int employeeId = 1) => new()
    {
        EmployeeId = employeeId,
        LeaveType = LeaveType.Annual,
        StartDate = new DateOnly(2026, 8, 1),
        EndDate = new DateOnly(2026, 8, 1),
        Reason = "測試"
    };

    private static async Task<LeaveRequest> SeedLeaveRequestAsync(AppDbContext context, LeaveStatus status, string? decisionNote = null, DateTime? decidedAt = null)
    {
        var leaveRequest = new LeaveRequest
        {
            EmployeeId = 1,
            LeaveType = LeaveType.Annual,
            StartDate = new DateOnly(2026, 8, 1),
            EndDate = new DateOnly(2026, 8, 1),
            Reason = "original",
            Status = status,
            CreatedAt = DateTime.UtcNow,
            DecisionNote = decisionNote,
            DecidedAt = decidedAt
        };

        context.LeaveRequests.Add(leaveRequest);
        await context.SaveChangesAsync();
        return leaveRequest;
    }

    [Fact]
    public async Task Create_NonExistentEmployeeId_DoesNotPersist()
    {
        await using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);

        var result = await controller.Create(ValidCreateVm(employeeId: 999));

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Empty(await context.LeaveRequests.ToListAsync());
    }

    [Fact]
    public async Task Create_InvalidLeaveType_DoesNotPersist()
    {
        await using var context = TestDbContextFactory.Create();
        var controller = CreateController(context);
        var vm = ValidCreateVm();
        vm.LeaveType = (LeaveType)999;

        var result = await controller.Create(vm);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Empty(await context.LeaveRequests.ToListAsync());
    }

    [Fact]
    public async Task Edit_InvalidLeaveType_DoesNotPersistChange()
    {
        await using var context = TestDbContextFactory.Create();
        var original = await SeedLeaveRequestAsync(context, LeaveStatus.Pending);
        var controller = CreateController(context);

        var vm = ValidCreateVm();
        vm.LeaveType = (LeaveType)999;
        vm.Reason = "changed";

        var result = await controller.Edit(original.Id, vm);

        Assert.IsType<ViewResult>(result);
        var reloaded = await context.LeaveRequests.FindAsync(original.Id);
        Assert.Equal(LeaveType.Annual, reloaded!.LeaveType);
        Assert.Equal("original", reloaded.Reason);
    }

    [Fact]
    public async Task Edit_SubmittedEmployeeIdIsIgnored()
    {
        await using var context = TestDbContextFactory.Create();
        var original = await SeedLeaveRequestAsync(context, LeaveStatus.Pending);
        var controller = CreateController(context);

        var vm = ValidCreateVm(employeeId: 2);
        vm.Reason = "changed";

        var result = await controller.Edit(original.Id, vm);

        Assert.IsType<RedirectToActionResult>(result);
        var reloaded = await context.LeaveRequests.FindAsync(original.Id);
        Assert.Equal(1, reloaded!.EmployeeId);
        Assert.Equal("changed", reloaded.Reason);
    }

    [Theory]
    [InlineData(LeaveStatus.Approved)]
    [InlineData(LeaveStatus.Rejected)]
    [InlineData(LeaveStatus.Cancelled)]
    public async Task Edit_NonPendingRequest_RejectsAndLeavesDataUnchanged(LeaveStatus status)
    {
        await using var context = TestDbContextFactory.Create();
        var original = await SeedLeaveRequestAsync(context, status);
        var controller = CreateController(context);

        var vm = ValidCreateVm();
        vm.Reason = "changed";

        var result = await controller.Edit(original.Id, vm);

        Assert.IsType<RedirectToActionResult>(result);
        var reloaded = await context.LeaveRequests.FindAsync(original.Id);
        Assert.Equal(status, reloaded!.Status);
        Assert.Equal("original", reloaded.Reason);
    }

    [Theory]
    [InlineData(LeaveStatus.Approved)]
    [InlineData(LeaveStatus.Rejected)]
    [InlineData(LeaveStatus.Cancelled)]
    public async Task Cancel_NonPendingRequest_RejectsAndLeavesDataUnchanged(LeaveStatus status)
    {
        await using var context = TestDbContextFactory.Create();
        var original = await SeedLeaveRequestAsync(context, status, decisionNote: "既有備註", decidedAt: DateTime.UtcNow);
        var controller = CreateController(context);

        var result = await controller.Cancel(original.Id);

        Assert.IsType<RedirectToActionResult>(result);
        var reloaded = await context.LeaveRequests.FindAsync(original.Id);
        Assert.Equal(status, reloaded!.Status);
        Assert.Equal("既有備註", reloaded.DecisionNote);
        Assert.NotNull(reloaded.DecidedAt);
    }

    [Fact]
    public async Task Cancel_PendingRequest_ClearsDecisionNoteAndDecidedAt()
    {
        await using var context = TestDbContextFactory.Create();
        var original = await SeedLeaveRequestAsync(context, LeaveStatus.Pending, decisionNote: "殘留備註", decidedAt: DateTime.UtcNow);
        var controller = CreateController(context);

        var result = await controller.Cancel(original.Id);

        Assert.IsType<RedirectToActionResult>(result);
        var reloaded = await context.LeaveRequests.FindAsync(original.Id);
        Assert.Equal(LeaveStatus.Cancelled, reloaded!.Status);
        Assert.Null(reloaded.DecisionNote);
        Assert.Null(reloaded.DecidedAt);
    }
}
