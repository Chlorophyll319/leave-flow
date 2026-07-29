using LeaveFlow.Data;
using LeaveFlow.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LeaveFlow.Controllers;

public class ReviewController : Controller
{
    private readonly AppDbContext _context;

    public ReviewController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var leaveRequests = await _context.LeaveRequests
            .Include(r => r.Employee)
            .Where(r => r.Status == LeaveStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return View(leaveRequests);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? note)
    {
        return await DecideAsync(id, note, LeaveStatus.Approved);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? note)
    {
        return await DecideAsync(id, note, LeaveStatus.Rejected);
    }

    private async Task<IActionResult> DecideAsync(int id, string? note, LeaveStatus decision)
    {
        var leaveRequest = await _context.LeaveRequests.FirstOrDefaultAsync(r => r.Id == id);

        if (leaveRequest is null)
        {
            return NotFound();
        }

        if (leaveRequest.Status != LeaveStatus.Pending)
        {
            TempData["ErrorMessage"] = "此申請已完成簽核或已取消";
            return RedirectToAction(nameof(Index));
        }

        if (note is { Length: > 200 })
        {
            TempData["ErrorMessage"] = "簽核備註不得超過 200 字";
            return RedirectToAction(nameof(Index));
        }

        leaveRequest.Status = decision;
        leaveRequest.DecisionNote = string.IsNullOrEmpty(note) ? null : note;
        leaveRequest.DecidedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}
