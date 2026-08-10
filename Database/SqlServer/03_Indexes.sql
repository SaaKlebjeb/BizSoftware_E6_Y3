USE InventoryDB;
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
