using System.Text.RegularExpressions;
using ExpenseTracker.Models;

namespace ExpenseTracker.Services;

/// <summary>
/// Parses bill-payment schedule text files in the format:
///
///   Description    DueDt        MinAmt       AmtToPay     SchedDt       FromAcct  PaymentType   Status
///   AmEx 81008     09/01/2025   $   57.41    $  355.71    09/01/2025    Ally...   Automatic...  DONE
///
/// Rules:
///   - The 4th dollar amount on the line (amt 2 pay) is recorded as the expense amount.
///   - All rows are imported regardless of status (DONE, to do, etc.).
///   - Header/separator/balance/summary lines are skipped automatically.
///   - The period start is inferred from the earliest scheduled date in the file;
///     period end from the latest. Falls back to filename (YYYY-MM) or current month.
/// </summary>
public class ExpenseFileParser
{
    // Matches any line that contains at least two MM/DD/YYYY dates and at least two dollar amounts.
    // Captures:
    //   group 'desc'    - everything before the first date (the expense name)
    //   group 'duedt'   - first date  (due date)
    //   group 'minamt'  - first $amount (minimum amount — we skip this)
    //   group 'amt'     - second $amount (amount to pay — we record this)
    //   group 'scheddt' - second date (scheduled date — used as ExpenseDate)
    private static readonly Regex ExpenseLineRegex = new(
        @"^(?<desc>.+?)\s{2,}" +                          // description (2+ spaces before date)
        @"(?<duedt>\d{2}/\d{2}/\d{4})\s+" +              // due date
        @"\$\s*(?<minamt>[\d,]+\.\d{2})\s+" +             // minimum amount
        @"\$\s*(?<amt>[\d,]+\.\d{2})\s+" +                // amount to pay  ← we use this
        @"(?<scheddt>\d{2}/\d{2}/\d{4})",                 // scheduled date
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Lines to unconditionally skip
    private static readonly Regex SkipLineRegex = new(
        @"^(\s*[-=]+\s*$" +                               // separator lines  ---- or ====
        @"|.*\bbalance\s+on\b.*"  +                       // "Balance on MM/DD/YYYY"
        @"|\s*\$\s*[\d,]*\.?\d*\s*$" +                   // lone dollar amount lines (totals)
        @"|\bbills\s+2\s+pay\b.*"  +                      // column header row
        @"|\busual\s+pay\s+distribution\b.*"  +           // "usual pay distribution" section header
        @"|NOTE:.*"  +                                     // NOTE: lines
        @"|\bdue\s+dt\b.*"  +                             // sub-header "due dt."
        @")",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Known account names — lines that are ONLY an account name + optional amount are balance rows
    private static readonly Regex AccountBalanceRegex = new(
        @"^(Ally\s+(Checking|Savings|Rupa|Sri)|Chase\s*\(?\w*\)?)\s*\$?\s*[\d,]*\.?\d*\s*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public ParseResult Parse(string fileContent, string fileName)
    {
        var result = new ParseResult { SourceFileName = fileName };
        var lines  = fileContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            // Skip blank lines
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Skip separator / header / balance / summary lines
            if (SkipLineRegex.IsMatch(line.Trim()))
                continue;

            if (AccountBalanceRegex.IsMatch(line.Trim()))
                continue;

            // Try to match an expense data row
            var m = ExpenseLineRegex.Match(line);
            if (m.Success)
            {
                var amtStr = m.Groups["amt"].Value.Replace(",", "").Trim();
                var schedDtStr = m.Groups["scheddt"].Value.Trim();

                if (!decimal.TryParse(amtStr, out var amount))
                {
                    result.SkippedLines.Add(rawLine);
                    continue;
                }

                DateOnly? expenseDate = null;
                if (DateOnly.TryParseExact(schedDtStr, "MM/dd/yyyy",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var parsedDate))
                {
                    expenseDate = parsedDate;
                }

                result.ParsedExpenses.Add(new ParsedExpense
                {
                    Description = m.Groups["desc"].Value.Trim(),
                    Amount      = amount,
                    ExpenseDate = expenseDate,
                    RawLine     = rawLine
                });
            }
            else
            {
                // Line had content but didn't match — record for review
                result.SkippedLines.Add(rawLine);
            }
        }

        // Derive period start/end from the scheduled dates found in the file
        var dates = result.ParsedExpenses
            .Where(e => e.ExpenseDate.HasValue)
            .Select(e => e.ExpenseDate!.Value)
            .ToList();

        if (dates.Any())
        {
            result.PeriodStart = dates.Min();
            result.PeriodEnd   = dates.Max();
        }
        else
        {
            // Fallback: infer from filename (e.g. 2025-09.txt or Sep2025.txt)
            result.PeriodStart = InferDateFromFileName(fileName)
                                 ?? DateOnly.FromDateTime(DateTime.UtcNow);
            result.PeriodEnd   = result.PeriodStart.Value.AddMonths(1).AddDays(-1);
        }

        return result;
    }

    private static DateOnly? InferDateFromFileName(string name)
    {
        // Try YYYY-MM or YYYY_MM
        var m = Regex.Match(name, @"(?<year>\d{4})[-_](?<month>\d{2})");
        if (m.Success
            && int.TryParse(m.Groups["year"].Value,  out int yr)
            && int.TryParse(m.Groups["month"].Value, out int mo)
            && mo >= 1 && mo <= 12)
        {
            return new DateOnly(yr, mo, 1);
        }

        // Try month name + year e.g. "Sep2025" or "September_2025"
        var m2 = Regex.Match(name,
            @"(?<mon>Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)[a-z]*[-_\s]*(?<year>\d{4})",
            RegexOptions.IgnoreCase);
        if (m2.Success
            && int.TryParse(m2.Groups["year"].Value, out int yr2)
            && DateTime.TryParseExact(m2.Groups["mon"].Value, "MMM",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dt))
        {
            return new DateOnly(yr2, dt.Month, 1);
        }

        return null;
    }
}

public class ParseResult
{
    public string              SourceFileName { get; set; } = "";
    public DateOnly?           PeriodStart    { get; set; }
    public DateOnly?           PeriodEnd      { get; set; }
    public List<ParsedExpense> ParsedExpenses { get; set; } = new();
    public List<string>        SkippedLines   { get; set; } = new();
}

public class ParsedExpense
{
    public string    Description { get; set; } = "";
    public decimal   Amount      { get; set; }
    public DateOnly? ExpenseDate { get; set; }
    public string    RawLine     { get; set; } = "";
}
