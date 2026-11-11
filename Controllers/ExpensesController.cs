using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpenseTracker.Data;

namespace ExpenseTracker.Controllers;

public class ExpensesController : Controller
{
    private readonly AppDbContext _db;
    public ExpensesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? year, int? month)
    {
        var now = DateTime.UtcNow;
        year  ??= now.Year;
        month ??= now.Month;

        var expenses = await _db.Expenses
            .Include(e => e.Category)
            .Include(e => e.PayPeriod)
            .Where(e => e.PayPeriod!.PeriodStart.Year  == year
                     && e.PayPeriod!.PeriodStart.Month == month)
            .OrderBy(e => e.Description)
            .ToListAsync();

        ViewBag.Year  = year;
        ViewBag.Month = month;
        ViewBag.Years = await _db.PayPeriods
            .Select(p => p.PeriodStart.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();

        return View(expenses);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await _db.Expenses.FindAsync(id);
        if (e != null) { _db.Expenses.Remove(e); await _db.SaveChangesAsync(); }
        return RedirectToAction("Index");
    }
}
