using LeaveFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaveFlow.Tests;

public class TestDbContextFactoryTests
{
    [Fact]
    public async Task Create_SeedsEmployeesAndPersistsLeaveRequest()
    {
        await using var context = TestDbContextFactory.Create();

        Assert.Equal(3, await context.Employees.CountAsync());

        context.LeaveRequests.Add(new LeaveRequest
        {
            EmployeeId = 1,
            LeaveType = LeaveType.Annual,
            StartDate = new DateOnly(2026, 8, 1),
            EndDate = new DateOnly(2026, 8, 1),
            Reason = "smoke test",
            Status = LeaveStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var saved = await context.LeaveRequests.SingleAsync();
        Assert.Equal("smoke test", saved.Reason);
    }
}
