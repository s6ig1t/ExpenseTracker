# ExpenseTracker — Setup & Usage Guide

A personal finance web app built with **ASP.NET Core 9**, **Entity Framework Core**, **SQL Server Express 2025**, and **Chart.js**.

---

## Prerequisites

| Tool | Version | Download |
|------|---------|----------|
| .NET SDK | 9.0+ | https://dotnet.microsoft.com/download |
| SQL Server Express | 2025 | https://www.microsoft.com/en-us/sql-server/sql-server-downloads |
| SQL Server Management Studio *(optional)* | Latest | https://aka.ms/ssmsfullsetup |

---

## 1 — Database Setup

**Option A — Run the SQL script directly (recommended for first setup):**

Open SSMS or `sqlcmd` and run:

```
sqlcmd -S .\SQLEXPRESS -E -i Database\schema.sql
```

This creates the `ExpenseTracker` database, all tables, indexes, views, and seeds the category lookup data plus your $6,000/month income record.

**Option B — Let EF Core auto-migrate on startup:**

The app calls `db.Database.Migrate()` at startup (see `Program.cs`), so if you skip Option A the schema will be created automatically the first time you run the app.

---

## 2 — Configure Connection String

Edit `appsettings.json` if your SQL Server Express instance name differs:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=ExpenseTracker;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

Common instance name variants: `.\SQLEXPRESS`, `localhost\SQLEXPRESS`, `(localdb)\MSSQLLocalDB`

---

## 3 — Run the App

```bash
cd ExpenseTracker
dotnet restore
dotnet run
```

Then open your browser at: **https://localhost:5001**

---

## 4 — Import Your Expense Files

Navigate to **Import Files** and drag & drop your `.txt` files.

### Supported file format

Each line must follow:
```
Description: $Amount
```

Examples:
```
Rent: $1500
Groceries: $320.50
Netflix: $15.99
Car insurance: $120
```

### Optional header lines

```
Period: 2024-03-01 to 2024-03-15
Date: 2024-03-01
```

- **Period header** — sets the pay period dates. If omitted, dates are inferred from the filename (e.g. `2024-03.txt` → March 2024).
- **Date header** — sets the expense date for all lines that follow it.
- Lines starting with `#` are treated as comments and skipped.

### Sample files

Three ready-to-import sample files are included in `SampleData/`:
- `2024-01.txt` — January 2024
- `2024-02.txt` — February 2024
- `2024-03.txt` — March 2024

---

## 5 — Updating Your Income

If your monthly disposable income changes, insert a new row in `IncomeSettings`:

```sql
-- Close the old record
UPDATE dbo.IncomeSettings
SET EffectiveTo = '2025-12-31'
WHERE EffectiveTo IS NULL;

-- Add the new amount effective Jan 2026
INSERT INTO dbo.IncomeSettings (MonthlyAmount, EffectiveFrom, Notes)
VALUES (6500.00, '2026-01-01', 'Raise effective Jan 2026');
```

---

## Project Structure

```
ExpenseTracker/
├── Controllers/
│   ├── HomeController.cs       # Dashboard
│   ├── ImportController.cs     # File upload & import
│   └── ExpensesController.cs   # Expense list & delete
├── Data/
│   └── AppDbContext.cs         # EF Core DbContext
├── Database/
│   └── schema.sql              # Raw SQL schema + views
├── Migrations/                 # EF Core migrations
├── Models/
│   ├── Entities.cs             # EF entity classes
│   └── ViewModels.cs           # Chart DTOs
├── Services/
│   ├── ExpenseFileParser.cs    # Parses .txt expense files
│   ├── CategoryMatcher.cs      # Keyword → category mapping
│   └── ImportService.cs        # Orchestrates parse + persist
├── Views/
│   ├── Home/Index.cshtml       # Dashboard + Chart.js
│   ├── Import/Index.cshtml     # Upload form
│   ├── Import/Results.cshtml   # Import summary
│   ├── Expenses/Index.cshtml   # Filterable expense table
│   └── Shared/_Layout.cshtml   # Sidebar layout
├── SampleData/                 # Example .txt files
├── appsettings.json
├── Program.cs
└── ExpenseTracker.csproj
```

---

## Auto-Category Mapping

The parser automatically classifies expenses by matching keywords:

| Category | Keywords matched |
|----------|-----------------|
| Housing | rent, mortgage, hoa, lease |
| Food & Dining | grocery, restaurant, doordash, ubereats |
| Transportation | gas, uber, lyft, toll, parking, car payment |
| Utilities | electric, water, internet, phone |
| Healthcare | doctor, dentist, pharmacy, prescription |
| Entertainment | netflix, spotify, hulu, gaming, concert |
| Shopping | amazon, target, clothing, electronics |
| Insurance | insurance, geico, allstate, premium |
| Savings | savings, 401k, ira, investment |
| Subscriptions | subscription, membership, patreon |
| Education | tuition, udemy, coursera, textbook |
| Travel | flight, hotel, airbnb, vacation |
| Personal Care | haircut, gym, salon, skincare |
| Miscellaneous | *(fallback for unmatched)* |

---

## Database Views

Two SQL views are available for custom reporting:

```sql
-- Monthly income vs expenses summary
SELECT * FROM dbo.vw_MonthlyIncomeVsExpenses ORDER BY MonthStart;

-- Expense breakdown by category per month
SELECT * FROM dbo.vw_MonthlyCategoryBreakdown ORDER BY [Year], [Month];
```
