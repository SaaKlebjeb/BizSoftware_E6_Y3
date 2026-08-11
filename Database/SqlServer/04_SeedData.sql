USE InventoryDB;
GO

INSERT INTO dbo.Settings ([Key], [Value])
SELECT N'ApplicationName', N'Inventory Management System'
WHERE NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE [Key] = N'ApplicationName');

INSERT INTO dbo.Settings ([Key], [Value])
SELECT N'LowStockDefault', N'10'
WHERE NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE [Key] = N'LowStockDefault');

INSERT INTO dbo.Settings ([Key], [Value])
SELECT N'DefaultPageSize', N'25'
WHERE NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE [Key] = N'DefaultPageSize');

INSERT INTO dbo.Settings ([Key], [Value])
SELECT N'CurrencySymbol', N'$'
WHERE NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE [Key] = N'CurrencySymbol');

INSERT INTO dbo.Settings ([Key], [Value])
SELECT N'ReceiptFooter', N'Thank you for your business.'
WHERE NOT EXISTS (SELECT 1 FROM dbo.Settings WHERE [Key] = N'ReceiptFooter');

INSERT INTO dbo.Categories (Name, Description)
SELECT N'General', N'General inventory items'
WHERE NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Name = N'General');

INSERT INTO dbo.Categories (Name, Description)
SELECT N'Office', N'Office supplies'
WHERE NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE Name = N'Office');

INSERT INTO dbo.Products (SKU, Name, CategoryId, Price, Quantity, LowStockThreshold)
SELECT N'GEN-001', N'Sample Item', c.CategoryId, 10.00, 100, 10
FROM dbo.Categories c
WHERE c.Name = N'General'
  AND NOT EXISTS (SELECT 1 FROM dbo.Products WHERE SKU = N'GEN-001');

-- Create the first account through the Register tab, then promote it for development only:
-- UPDATE dbo.Users SET Role = N'Admin' WHERE Username = N'your-registered-user';
-- Passwords are intentionally never inserted by this script.
GO
