using LeaveFlow.Data;
using LeaveFlow.Models;
using LeaveFlow.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

    public async Task<IActionResult> Create()
    {
        ViewData["Employees"] = await GetEmployeeOptionsAsync();
        ViewData["LeaveTypes"] = GetLeaveTypeOptions();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeaveRequestFormViewModel vm)
    {
        if (vm.EmployeeId.HasValue && !await _context.Employees.AnyAsync(e => e.Id == vm.EmployeeId.Value))
        {
            ModelState.AddModelError(nameof(vm.EmployeeId), "員工不存在");
        }

        if (vm.LeaveType.HasValue && !Enum.IsDefined(vm.LeaveType.Value))
        {
            ModelState.AddModelError(nameof(vm.LeaveType), "假別選項無效");
        }

        if (!ModelState.IsValid)
        {
            ViewData["Employees"] = await GetEmployeeOptionsAsync();
            ViewData["LeaveTypes"] = GetLeaveTypeOptions();
            return View(vm);
        }

        var leaveRequest = new LeaveRequest
        {
            EmployeeId = vm.EmployeeId!.Value,
            LeaveType = vm.LeaveType!.Value,
            StartDate = vm.StartDate!.Value,
            EndDate = vm.EndDate!.Value,
            Reason = vm.Reason!,
            Status = LeaveStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.LeaveRequests.Add(leaveRequest);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task<IEnumerable<SelectListItem>> GetEmployeeOptionsAsync()
    {
        return await _context.Employees
            .OrderBy(e => e.Name)
            .Select(e => new SelectListItem($"{e.Name}（{e.Department}）", e.Id.ToString()))
            .ToListAsync();
    }

    private static IEnumerable<SelectListItem> GetLeaveTypeOptions()
    {
        return
        [
            new SelectListItem("特休", ((int)LeaveType.Annual).ToString()),
            new SelectListItem("病假", ((int)LeaveType.Sick).ToString()),
            new SelectListItem("事假", ((int)LeaveType.Personal).ToString()),
            new SelectListItem("其他", ((int)LeaveType.Other).ToString())
        ];
    }
}
