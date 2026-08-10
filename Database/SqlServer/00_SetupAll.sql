/*
    Inventory Management System - one-time SQL Server setup
    Run this entire file in SQL Server Management Studio.

    Development administrator account created by this script:
      Username: admin
      Password: Admin@123
    Change this password immediately after the first login.
    The password is stored only as a PBKDF2 hash and salt.
*/

USE master;
GO

IF DB_ID(N'InventoryDB') IS NULL
BEGIN
    CREATE DATABASE InventoryDB;
END;
GO

USE InventoryDB;
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserId INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        Username NVARCHAR(50) NOT NULL CONSTRAINT UQ_Users_Username UNIQUE,
        PasswordHash VARBINARY(64) NOT NULL,
        PasswordSalt VARBINARY(32) NOT NULL,
        FullName NVARCHAR(150) NOT NULL,
        Role NVARCHAR(10) NOT NULL CONSTRAINT CK_Users_Role CHECK (Role IN (N'Admin', N'User')),
        IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Users_UpdatedAt DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Categories
    (
        CategoryId INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL CONSTRAINT UQ_Categories_Name UNIQUE,
        Description NVARCHAR(500) NOT NULL CONSTRAINT DF_Categories_Description DEFAULT (N''),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Categories_CreatedAt DEFAULT (SYSUTCDATETIME())
    );
END;
GO

IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        ProductId INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
        SKU NVARCHAR(50) NOT NULL CONSTRAINT UQ_Products_SKU UNIQUE,
        Name NVARCHAR(200) NOT NULL,
        CategoryId INT NOT NULL,
        Price DECIMAL(18, 2) NOT NULL CONSTRAINT CK_Products_Price CHECK (Price >= 0),
        Quantity INT NOT NULL CONSTRAINT CK_Products_Quantity CHECK (Quantity >= 0),
        LowStockThreshold INT NOT NULL CONSTRAINT CK_Products_LowStockThreshold CHECK (LowStockThreshold >= 0),
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Products_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Products_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(CategoryId)
    );
END;
GO

IF OBJECT_ID(N'dbo.Sales', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sales
    (
        SaleId INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_Sales PRIMARY KEY,
        UserId INT NOT NULL,
        TotalAmount DECIMAL(18, 2) NOT NULL CONSTRAINT CK_Sales_TotalAmount CHECK (TotalAmount >= 0),
        SaleDate DATETIME2(0) NOT NULL CONSTRAINT DF_Sales_SaleDate DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_Sales_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
    );
END;
GO

IF OBJECT_ID(N'dbo.SaleItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SaleItems
    (
        SaleItemId INT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_SaleItems PRIMARY KEY,
        SaleId INT NOT NULL,
        ProductId INT NOT NULL,
        Quantity INT NOT NULL CONSTRAINT CK_SaleItems_Quantity CHECK (Quantity > 0),
        UnitPrice DECIMAL(18, 2) NOT NULL CONSTRAINT CK_SaleItems_UnitPrice CHECK (UnitPrice >= 0),
        Subtotal AS (CONVERT(DECIMAL(18, 2), Quantity * UnitPrice)) PERSISTED,
        CONSTRAINT FK_SaleItems_Sales FOREIGN KEY (SaleId) REFERENCES dbo.Sales(SaleId),
        CONSTRAINT FK_SaleItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId)
    );
END;
GO

IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs
    (
        AuditLogId BIGINT IDENTITY(1, 1) NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
        UserId INT NULL,
        Action NVARCHAR(50) NOT NULL,
        EntityName NVARCHAR(100) NOT NULL,
        EntityId INT NULL,
        Description NVARCHAR(1000) NOT NULL,
        CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
    );
END;
GO

IF OBJECT_ID(N'dbo.Settings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Settings
    (
        [Key] NVARCHAR(100) NOT NULL CONSTRAINT PK_Settings PRIMARY KEY,
        [Value] NVARCHAR(500) NOT NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_Name' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE INDEX IX_Products_Name ON dbo.Products(Name);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Products_CategoryId' AND object_id = OBJECT_ID(N'dbo.Products'))
    CREATE INDEX IX_Products_CategoryId ON dbo.Products(CategoryId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Sales_SaleDate' AND object_id = OBJECT_ID(N'dbo.Sales'))
    CREATE INDEX IX_Sales_SaleDate ON dbo.Sales(SaleDate);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SaleItems_ProductId' AND object_id = OBJECT_ID(N'dbo.SaleItems'))
    CREATE INDEX IX_SaleItems_ProductId ON dbo.SaleItems(ProductId);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_AuditLogs_CreatedAt' AND object_id = OBJECT_ID(N'dbo.AuditLogs'))
    CREATE INDEX IX_AuditLogs_CreatedAt ON dbo.AuditLogs(CreatedAt);
GO

INSERT INTO dbo.Settings ([Key], [Value])
SELECT N'ApplicationName', N'Inventory Management System'
WHERE NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE [Key] = N'ApplicationName');

INSERT INTO dbo.Settings ([Key], [Value])
SELECT N'LowStockDefault', N'10'
WHERE NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE [Key] = N'LowStockDefault');

INSERT INTO dbo.Categories (Name, Description)
SELECT N'General', N'General inventory items'
WHERE NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Name = N'General');

INSERT INTO dbo.Categories (Name, Description)
SELECT N'Office', N'Office supplies'
WHERE NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Name = N'Office');

INSERT INTO dbo.Products (SKU, Name, CategoryId, Price, Quantity, LowStockThreshold)
SELECT N'GEN-001', N'Sample Item', CategoryId, 10.00, 100, 10
FROM dbo.Categories
WHERE Name = N'General'
  AND NOT EXISTS (SELECT 1 FROM dbo.Products WHERE SKU = N'GEN-001');

INSERT INTO dbo.Users (Username, PasswordHash, PasswordSalt, FullName, Role, IsActive)
SELECT N'admin',
       0x6B818E27EF852AAEBF4DAE252BB621AB0AFBAC266C7DF9820BE8BD2753C3FCBA,
       0x298F40C362B7F25DC0492D08CE611452,
       N'System Administrator', N'Admin', 1
WHERE NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'admin');
GO

PRINT N'InventoryDB setup completed successfully.';
PRINT N'Development login: admin / Admin@123 - change this password immediately.';
