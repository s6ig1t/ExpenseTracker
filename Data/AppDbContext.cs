using Microsoft.EntityFrameworkCore;
using ExpenseTracker.Models;

namespace ExpenseTracker.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<IncomeSettings>   IncomeSettings    { get; set; }
    public DbSet<ExpenseCategory>  ExpenseCategories { get; set; }
    public DbSet<PayPeriod>        PayPeriods        { get; set; }
    public DbSet<Expense>          Expenses          { get; set; }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // IncomeSettings
        mb.Entity<IncomeSettings>(e =>
        {
            e.ToTable("IncomeSettings");
            e.Property(x => x.MonthlyAmount).HasColumnType("decimal(18,2)");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        // ExpenseCategory
        mb.Entity<ExpenseCategory>(e =>
        {
            e.ToTable("ExpenseCategories");
            e.HasIndex(x => x.Name).IsUnique();
        });

        // PayPeriod
        mb.Entity<PayPeriod>(e =>
        {
            e.ToTable("PayPeriods");
            e.HasIndex(x => new { x.PeriodStart, x.PeriodEnd }).IsUnique();
            e.Property(x => x.ImportedAt).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        // Expense
        mb.Entity<Expense>(e =>
        {
            e.ToTable("Expenses");
            e.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(x => x.PayPeriod)
             .WithMany(p => p.Expenses)
             .HasForeignKey(x => x.PayPeriodId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Category)
             .WithMany(c => c.Expenses)
             .HasForeignKey(x => x.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
