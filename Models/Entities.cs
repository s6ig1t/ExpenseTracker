namespace ExpenseTracker.Models;

public class IncomeSettings
{
    public int     Id            { get; set; }
    public decimal MonthlyAmount { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo  { get; set; }
    public string?  Notes        { get; set; }
    public DateTime CreatedAt    { get; set; }
}

public class ExpenseCategory
{
    public int     Id          { get; set; }
    public string  Name        { get; set; } = "";
    public string? Description { get; set; }
    public string? Color       { get; set; }

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}

public class PayPeriod
{
    public int     Id             { get; set; }
    public DateOnly PeriodStart   { get; set; }
    public DateOnly PeriodEnd     { get; set; }
    public string?  SourceFileName { get; set; }
    public DateTime ImportedAt    { get; set; }
    public string?  Notes         { get; set; }

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}

public class Expense
{
    public int      Id          { get; set; }
    public int      PayPeriodId { get; set; }
    public int      CategoryId  { get; set; }
    public string   Description { get; set; } = "";
    public decimal  Amount      { get; set; }
    public DateOnly? ExpenseDate { get; set; }
    public string?  RawLine     { get; set; }
    public DateTime CreatedAt   { get; set; }

    public PayPeriod?        PayPeriod { get; set; }
    public ExpenseCategory?  Category  { get; set; }
}
