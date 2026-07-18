using LeaveFlow.Models;
using Microsoft.EntityFrameworkCore;

namespace LeaveFlow.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>().HasData(
            new Employee { Id = 1, Name = "王小明", Department = "研發部" },
            new Employee { Id = 2, Name = "林美惠", Department = "人資部" },
            new Employee { Id = 3, Name = "陳大文", Department = "業務部" }
        );
    }
}
