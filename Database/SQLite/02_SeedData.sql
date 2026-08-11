INSERT OR IGNORE INTO Settings ([Key], [Value]) VALUES
    ('ApplicationName', 'Inventory Management System'),
    ('LowStockDefault', '10'),
    ('DefaultPageSize', '25'),
    ('CurrencySymbol', '$'),
    ('ReceiptFooter', 'Thank you for your business.');

INSERT OR IGNORE INTO Categories (Name, Description) VALUES
    ('General', 'General inventory items'),
    ('Office', 'Office supplies');

-- Generate password hashes with InventoryManagementSystem.Utils.PasswordHasher.
-- Do not insert plaintext passwords into this database.
