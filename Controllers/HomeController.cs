using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpenseTracker.Data;
using ExpenseTracker.Models;

namespace ExpenseTracker.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var income  = await _db.IncomeSettings
                               .OrderByDescending(i => i.EffectiveFrom)
                               .FirstOrDefaultAsync();
        var monthly = income?.MonthlyAmount ?? 6000m;

        var now         = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart  = new DateOnly(now.Year, now.Month, 1);

        // All pay periods ordered chronologically
        var periods = await _db.PayPeriods
            .Include(p => p.Expenses)
            .ThenInclude(e => e.Category)
            .OrderBy(p => p.PeriodStart)
            .ToListAsync();

        // Build monthly aggregates
        var monthly_history = periods
            .GroupBy(p => new { p.PeriodStart.Year, p.PeriodStart.Month })
            .Select(g => new MonthlyChartPoint(
                Label:      $"{System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(g.Key.Month)} {g.Key.Year}",
                Income:     monthly,
                Expenses:   g.SelectMany(p => p.Expenses).Sum(e => e.Amount),
                NetSavings: monthly - g.SelectMany(p => p.Expenses).Sum(e => e.Amount)
            ))
            .ToList();

        // Current month category breakdown
        var currentPeriods = periods
            .Where(p => p.PeriodStart.Year  == now.Year
                     && p.PeriodStart.Month == now.Month)
            .ToList();

        var currentCats = currentPeriods
            .SelectMany(p => p.Expenses)
            .GroupBy(e => new { e.Category!.Name, e.Category.Color })
            .Select(g => new CategoryChartSlice(
                g.Key.Name,
                g.Key.Color ?? "#95A5A6",
                g.Sum(e => e.Amount)))
            .OrderByDescending(x => x.Amount)
            .ToList();

        var currentExpenses = currentCats.Sum(c => c.Amount);
        var avgExpenses     = monthly_history.Any()
                              ? monthly_history.Average(m => m.Expenses) : 0m;

        var summary = new DashboardSummary(
            MonthlyIncome:             monthly,
            CurrentMonthExpenses:      currentExpenses,
            CurrentMonthSavings:       monthly - currentExpenses,
            AverageMonthlyExpenses:    avgExpenses,
            TotalPayPeriods:           periods.Count,
            MonthlyHistory:            monthly_history,
            CurrentMonthCategories:    currentCats
        );

        return View(summary);
    }
}
