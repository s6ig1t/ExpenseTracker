using Microsoft.EntityFrameworkCore;
using ExpenseTracker.Data;
using ExpenseTracker.Models;

namespace ExpenseTracker.Services;

public class ImportService
{
    private readonly AppDbContext       _db;
    private readonly ExpenseFileParser  _parser;
    private readonly CategoryMatcher    _matcher;

    public ImportService(AppDbContext db, ExpenseFileParser parser, CategoryMatcher matcher)
    {
        _db      = db;
        _parser  = parser;
        _matcher = matcher;
    }

    public async Task<ImportSummary> ImportFileAsync(string content, string fileName)
    {
        var parsed = _parser.Parse(content, fileName);
        var summary = new ImportSummary
        {
            FileName     = fileName,
            SkippedLines = parsed.SkippedLines
        };

        if (parsed.PeriodStart == null)
        {
            summary.Error = "Could not determine pay period dates.";
            return summary;
        }

        // Upsert PayPeriod (avoid duplicates)
        var period = await _db.PayPeriods
            .FirstOrDefaultAsync(p => p.PeriodStart == parsed.PeriodStart
                                   && p.PeriodEnd   == parsed.PeriodEnd);

        if (period == null)
        {
            period = new PayPeriod
            {
                PeriodStart    = parsed.PeriodStart.Value,
                PeriodEnd      = parsed.PeriodEnd!.Value,
                SourceFileName = fileName,
                ImportedAt     = DateTime.UtcNow
            };
            _db.PayPeriods.Add(period);
            await _db.SaveChangesAsync();
        }

        // Load all categories once
        var categories = await _db.ExpenseCategories.ToListAsync();

        foreach (var pe in parsed.ParsedExpenses)
        {
            var category = _matcher.Match(pe.Description, categories);

            _db.Expenses.Add(new Expense
            {
                PayPeriodId = period.Id,
                CategoryId  = category.Id,
                Description = pe.Description,
                Amount      = pe.Amount,
                ExpenseDate = pe.ExpenseDate,
                RawLine     = pe.RawLine,
                CreatedAt   = DateTime.UtcNow
            });

            summary.ImportedCount++;
        }

        await _db.SaveChangesAsync();
        summary.PeriodStart = period.PeriodStart;
        summary.PeriodEnd   = period.PeriodEnd;
        return summary;
    }
}

public class ImportSummary
{
    public string         FileName      { get; set; } = "";
    public int            ImportedCount { get; set; }
    public DateOnly       PeriodStart   { get; set; }
    public DateOnly       PeriodEnd     { get; set; }
    public List<string>   SkippedLines  { get; set; } = new();
    public string?        Error         { get; set; }
    public bool           Success       => Error == null;
}
