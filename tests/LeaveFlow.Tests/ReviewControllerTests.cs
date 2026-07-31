using LeaveFlow.Controllers;
using LeaveFlow.Data;
using LeaveFlow.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeaveFlow.Tests;

public class ReviewControllerTests
{
    private static ReviewController CreateController(AppDbContext context)
    {
        var controller = new ReviewController(context);
        ControllerTestHelper.AttachTempData(controller);
        return controller;
    }

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

    [Theory]
    [InlineData(LeaveStatus.Approved)]
    [InlineData(LeaveStatus.Rejected)]
    [InlineData(LeaveStatus.Cancelled)]
    public async Task Approve_NonPendingRequest_RejectsAndLeavesDataUnchanged(LeaveStatus status)
    {
        await using var context = TestDbContextFactory.Create();
        var original = await SeedLeaveRequestAsync(context, status, decisionNote: "既有備註", decidedAt: DateTime.UtcNow);
        var controller = CreateController(context);

        var result = await controller.Approve(original.Id, "新備註");

        Assert.IsType<RedirectToActionResult>(result);
        var reloaded = await context.LeaveRequests.FindAsync(original.Id);
        Assert.Equal(status, reloaded!.Status);
        Assert.Equal("既有備註", reloaded.DecisionNote);
    }

    [Theory]
    [InlineData(LeaveStatus.Approved)]
    [InlineData(LeaveStatus.Rejected)]
    [InlineData(LeaveStatus.Cancelled)]
    public async Task Reject_NonPendingRequest_RejectsAndLeavesDataUnchanged(LeaveStatus status)
    {
        await using var context = TestDbContextFactory.Create();
        var original = await SeedLeaveRequestAsync(context, status, decisionNote: "既有備註", decidedAt: DateTime.UtcNow);
        var controller = CreateController(context);

        var result = await controller.Reject(original.Id, "新備註");

        Assert.IsType<RedirectToActionResult>(result);
        var reloaded = await context.LeaveRequests.FindAsync(original.Id);
        Assert.Equal(status, reloaded!.Status);
        Assert.Equal("既有備註", reloaded.DecisionNote);
    }

    [Fact]
    public async Task Approve_NoteExceeds200Chars_KeepsPendingAndDoesNotSetDecidedAt()
    {
        await using var context = TestDbContextFactory.Create();
        var original = await SeedLeaveRequestAsync(context, LeaveStatus.Pending);
        var controller = CreateController(context);
        var tooLongNote = new string('a', 201);

        var result = await controller.Approve(original.Id, tooLongNote);

        Assert.IsType<RedirectToActionResult>(result);
        var reloaded = await context.LeaveRequests.FindAsync(original.Id);
        Assert.Equal(LeaveStatus.Pending, reloaded!.Status);
        Assert.Null(reloaded.DecidedAt);
    }

    [Fact]
    public async Task Reject_NoteExceeds200Chars_KeepsPendingAndDoesNotSetDecidedAt()
    {
        await using var context = TestDbContextFactory.Create();
        var original = await SeedLeaveRequestAsync(context, LeaveStatus.Pending);
        var controller = CreateController(context);
        var tooLongNote = new string('a', 201);

        var result = await controller.Reject(original.Id, tooLongNote);

        Assert.IsType<RedirectToActionResult>(result);
        var reloaded = await context.LeaveRequests.FindAsync(original.Id);
        Assert.Equal(LeaveStatus.Pending, reloaded!.Status);
        Assert.Null(reloaded.DecidedAt);
    }

    [Fact]
    public async Task Approve_PendingWithNote_SetsApprovedStatusNoteAndDecidedAt()
    {
        await using var context = TestDbContextFactory.Create();
        var original = await SeedLeaveRequestAsync(context, LeaveStatus.Pending);
        var controller = CreateController(context);

        var result = await controller.Approve(original.Id, "核准備註");

        Assert.IsType<RedirectToActionResult>(result);
        var reloaded = await context.LeaveRequests.FindAsync(original.Id);
        Assert.Equal(LeaveStatus.Approved, reloaded!.Status);
        Assert.Equal("核准備註", reloaded.DecisionNote);
        Assert.NotNull(reloaded.DecidedAt);
    }

    [Fact]
    public async Task Reject_PendingWithoutNote_SetsRejectedStatusAndDecidedAtWithNullNote()
    {
        await using var context = TestDbContextFactory.Create();
        var original = await SeedLeaveRequestAsync(context, LeaveStatus.Pending);
        var controller = CreateController(context);

        var result = await controller.Reject(original.Id, note: null);

        Assert.IsType<RedirectToActionResult>(result);
        var reloaded = await context.LeaveRequests.FindAsync(original.Id);
        Assert.Equal(LeaveStatus.Rejected, reloaded!.Status);
        Assert.Null(reloaded.DecisionNote);
        Assert.NotNull(reloaded.DecidedAt);
    }
}
