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

    public async Task<IActionResult> Index(LeaveStatus? status, string? sort)
    {
        var validStatus = status.HasValue && Enum.IsDefined(status.Value) ? status : null;
        var normalizedSort = string.Equals(sort, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";

        var query = _context.LeaveRequests
            .Include(r => r.Employee)
            .AsQueryable();

        if (validStatus.HasValue)
        {
            query = query.Where(r => r.Status == validStatus.Value);
        }

        query = normalizedSort == "asc"
            ? query.OrderBy(r => r.CreatedAt).ThenBy(r => r.Id)
            : query.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id);

        var leaveRequests = await query.ToListAsync();

        ViewData["StatusOptions"] = GetStatusFilterOptions(validStatus);
        ViewData["SortOptions"] = GetSortOptions(normalizedSort);

        return View(leaveRequests);
    }

    public async Task<IActionResult> Details(int id)
    {
        var leaveRequest = await _context.LeaveRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (leaveRequest is null)
        {
            return NotFound();
        }

        return View(leaveRequest);
    }

    public async Task<IActionResult> Create()
    {
        ViewData["Employees"] = await GetEmployeeOptionsAsync();
        ViewData["LeaveTypes"] = GetLeaveTypeOptions();
        return View();
    }

    public async Task<IActionResult> Edit(int id)
    {
        var leaveRequest = await _context.LeaveRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (leaveRequest is null)
        {
            return NotFound();
        }

        if (leaveRequest.Status != LeaveStatus.Pending)
        {
            TempData["ErrorMessage"] = "僅待審核的申請可以編輯";
            return RedirectToAction(nameof(Index));
        }

        var vm = new LeaveRequestFormViewModel
        {
            EmployeeId = leaveRequest.EmployeeId,
            LeaveType = leaveRequest.LeaveType,
            StartDate = leaveRequest.StartDate,
            EndDate = leaveRequest.EndDate,
            Reason = leaveRequest.Reason
        };

        ViewData["LeaveTypes"] = GetLeaveTypeOptions();
        ViewData["EmployeeName"] = leaveRequest.Employee?.Name;
        ViewData["EmployeeDepartment"] = leaveRequest.Employee?.Department;

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LeaveRequestFormViewModel vm)
    {
        var leaveRequest = await _context.LeaveRequests
            .Include(r => r.Employee)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (leaveRequest is null)
        {
            return NotFound();
        }

        if (leaveRequest.Status != LeaveStatus.Pending)
        {
            TempData["ErrorMessage"] = "僅待審核的申請可以編輯";
            return RedirectToAction(nameof(Index));
        }

        if (vm.LeaveType.HasValue && !Enum.IsDefined(vm.LeaveType.Value))
        {
            ModelState.AddModelError(nameof(vm.LeaveType), "假別選項無效");
        }

        if (!ModelState.IsValid)
        {
            ViewData["LeaveTypes"] = GetLeaveTypeOptions();
            ViewData["EmployeeName"] = leaveRequest.Employee?.Name;
            ViewData["EmployeeDepartment"] = leaveRequest.Employee?.Department;
            return View(vm);
        }

        leaveRequest.LeaveType = vm.LeaveType!.Value;
        leaveRequest.StartDate = vm.StartDate!.Value;
        leaveRequest.EndDate = vm.EndDate!.Value;
        leaveRequest.Reason = vm.Reason!;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "申請已更新";
        return RedirectToAction(nameof(Index));
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

        TempData["SuccessMessage"] = "申請已成功送出，狀態為待審核";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var leaveRequest = await _context.LeaveRequests.FirstOrDefaultAsync(r => r.Id == id);

        if (leaveRequest is null)
        {
            return NotFound();
        }

        if (leaveRequest.Status != LeaveStatus.Pending)
        {
            TempData["ErrorMessage"] = "僅待審核的申請可以取消";
            return RedirectToAction(nameof(Index));
        }

        leaveRequest.Status = LeaveStatus.Cancelled;
        leaveRequest.DecisionNote = null;
        leaveRequest.DecidedAt = null;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "申請已取消";
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

    private static IEnumerable<SelectListItem> GetStatusFilterOptions(LeaveStatus? selected)
    {
        return
        [
            new SelectListItem("全部", "") { Selected = selected is null },
            new SelectListItem("待審核", ((int)LeaveStatus.Pending).ToString()) { Selected = selected == LeaveStatus.Pending },
            new SelectListItem("已核准", ((int)LeaveStatus.Approved).ToString()) { Selected = selected == LeaveStatus.Approved },
            new SelectListItem("已駁回", ((int)LeaveStatus.Rejected).ToString()) { Selected = selected == LeaveStatus.Rejected },
            new SelectListItem("已取消", ((int)LeaveStatus.Cancelled).ToString()) { Selected = selected == LeaveStatus.Cancelled }
        ];
    }

    private static IEnumerable<SelectListItem> GetSortOptions(string sort)
    {
        return
        [
            new SelectListItem("建立時間：新到舊", "desc") { Selected = sort == "desc" },
            new SelectListItem("建立時間：舊到新", "asc") { Selected = sort == "asc" }
        ];
    }
}
