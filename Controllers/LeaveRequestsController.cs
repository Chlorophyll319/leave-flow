using LeaveFlow.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeaveFlow.Controllers;

public class LeaveRequestsController : Controller
{
    private readonly AppDbContext _context;

    public LeaveRequestsController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var leaveRequests = await _context.LeaveRequests
            .Include(r => r.Employee)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return View(leaveRequests);
    }
}
