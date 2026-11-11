-- ============================================================
--  ExpenseTracker - SQL Server Express 2025
--  Schema: Income, PayPeriods, ExpenseCategories, Expenses
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'ExpenseTracker')
BEGIN
    CREATE DATABASE ExpenseTracker;
END
GO

USE ExpenseTracker;
GO

-- ------------------------------------------------------------
-- 1. IncomeSettings
--    Stores the user's monthly disposable income over time.
--    Allows tracking changes to income across years.
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.IncomeSettings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.IncomeSettings (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        MonthlyAmount   DECIMAL(18,2)   NOT NULL,
        EffectiveFrom   DATE            NOT NULL,
        EffectiveTo     DATE            NULL,         -- NULL = currently active
        Notes           NVARCHAR(500)   NULL,
        CreatedAt       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME()
    );

    -- Seed the user's known $6,000/month disposable income
    INSERT INTO dbo.IncomeSettings (MonthlyAmount, EffectiveFrom, Notes)
    VALUES (6000.00, '2020-01-01', 'Initial monthly disposable income');
END
GO

-- ------------------------------------------------------------
-- 2. ExpenseCategories
--    Lookup table for normalised category names.
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.ExpenseCategories', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExpenseCategories (
        Id          INT IDENTITY(1,1) PRIMARY KEY,
        Name        NVARCHAR(100)   NOT NULL,
        Description NVARCHAR(500)   NULL,
        Color       NVARCHAR(7)     NULL,   -- hex color for charts e.g. #FF5733
        CONSTRAINT UQ_ExpenseCategories_Name UNIQUE (Name)
    );

    -- Seed common categories
    INSERT INTO dbo.ExpenseCategories (Name, Color) VALUES
        ('Housing',         '#E74C3C'),
        ('Food & Dining',   '#E67E22'),
        ('Transportation',  '#F1C40F'),
        ('Utilities',       '#2ECC71'),
        ('Healthcare',      '#1ABC9C'),
        ('Entertainment',   '#3498DB'),
        ('Shopping',        '#9B59B6'),
        ('Insurance',       '#34495E'),
        ('Savings',         '#27AE60'),
        ('Subscriptions',   '#16A085'),
        ('Education',       '#8E44AD'),
        ('Travel',          '#2980B9'),
        ('Personal Care',   '#D35400'),
        ('Miscellaneous',   '#95A5A6');
END
GO

-- ------------------------------------------------------------
-- 3. PayPeriods
--    One record per imported pay-period file.
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.PayPeriods', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PayPeriods (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        PeriodStart     DATE            NOT NULL,
        PeriodEnd       DATE            NOT NULL,
        SourceFileName  NVARCHAR(260)   NULL,          -- original file name
        ImportedAt      DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        Notes           NVARCHAR(1000)  NULL,
        CONSTRAINT UQ_PayPeriods_Dates UNIQUE (PeriodStart, PeriodEnd)
    );
END
GO

-- ------------------------------------------------------------
-- 4. Expenses
--    One row per parsed expense line.
-- ------------------------------------------------------------
IF OBJECT_ID('dbo.Expenses', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Expenses (
        Id              INT IDENTITY(1,1) PRIMARY KEY,
        PayPeriodId     INT             NOT NULL,
        CategoryId      INT             NOT NULL,
        Description     NVARCHAR(255)   NOT NULL,      -- raw label from file
        Amount          DECIMAL(18,2)   NOT NULL,
        ExpenseDate     DATE            NULL,           -- if determinable from file
        RawLine         NVARCHAR(1000)  NULL,           -- original unparsed line
        CreatedAt       DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_Expenses_PayPeriods
            FOREIGN KEY (PayPeriodId) REFERENCES dbo.PayPeriods(Id)
            ON DELETE CASCADE,

        CONSTRAINT FK_Expenses_Categories
            FOREIGN KEY (CategoryId)  REFERENCES dbo.ExpenseCategories(Id)
            ON DELETE CASCADE
    );

    CREATE INDEX IX_Expenses_PayPeriodId ON dbo.Expenses(PayPeriodId);
    CREATE INDEX IX_Expenses_CategoryId  ON dbo.Expenses(CategoryId);
    CREATE INDEX IX_Expenses_ExpenseDate ON dbo.Expenses(ExpenseDate);
END
GO

-- ------------------------------------------------------------
-- 5. Useful views
-- ------------------------------------------------------------

-- Monthly summary: income vs total expenses
CREATE OR ALTER VIEW dbo.vw_MonthlyIncomeVsExpenses AS
SELECT
    YEAR(pp.PeriodStart)            AS [Year],
    MONTH(pp.PeriodStart)           AS [Month],
    DATEFROMPARTS(
        YEAR(pp.PeriodStart),
        MONTH(pp.PeriodStart), 1)   AS MonthStart,
    COALESCE(ins.MonthlyAmount, 0)  AS MonthlyIncome,
    COALESCE(SUM(e.Amount), 0)      AS TotalExpenses,
    COALESCE(ins.MonthlyAmount, 0)
        - COALESCE(SUM(e.Amount),0) AS NetSavings
FROM dbo.PayPeriods pp
LEFT JOIN dbo.Expenses e
    ON e.PayPeriodId = pp.Id
LEFT JOIN dbo.IncomeSettings ins
    ON  DATEFROMPARTS(YEAR(pp.PeriodStart), MONTH(pp.PeriodStart), 1)
            >= ins.EffectiveFrom
    AND (ins.EffectiveTo IS NULL
         OR DATEFROMPARTS(YEAR(pp.PeriodStart), MONTH(pp.PeriodStart), 1)
            <= ins.EffectiveTo)
GROUP BY
    YEAR(pp.PeriodStart),
    MONTH(pp.PeriodStart),
    ins.MonthlyAmount;
GO

-- Category breakdown per month
CREATE OR ALTER VIEW dbo.vw_MonthlyCategoryBreakdown AS
SELECT
    YEAR(pp.PeriodStart)  AS [Year],
    MONTH(pp.PeriodStart) AS [Month],
    c.Name                AS Category,
    c.Color,
    SUM(e.Amount)         AS TotalAmount
FROM dbo.Expenses e
JOIN dbo.PayPeriods pp        ON pp.Id = e.PayPeriodId
JOIN dbo.ExpenseCategories c  ON c.Id  = e.CategoryId
GROUP BY
    YEAR(pp.PeriodStart),
    MONTH(pp.PeriodStart),
    c.Name,
    c.Color;
GO
