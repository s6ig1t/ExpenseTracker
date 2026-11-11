namespace ExpenseTracker.Models;

public record MonthlyChartPoint(
    string  Label,        // "Jan 2024"
    decimal Income,
    decimal Expenses,
    decimal NetSavings
);

public record CategoryChartSlice(
    string  Category,
    string  Color,
    decimal Amount
);

public record DashboardSummary(
    decimal MonthlyIncome,
    decimal CurrentMonthExpenses,
    decimal CurrentMonthSavings,
    decimal AverageMonthlyExpenses,
    int     TotalPayPeriods,
    List<MonthlyChartPoint>   MonthlyHistory,
    List<CategoryChartSlice>  CurrentMonthCategories
);
