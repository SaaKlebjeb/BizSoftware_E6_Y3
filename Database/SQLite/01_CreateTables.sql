PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Users
(
    UserId INTEGER PRIMARY KEY AUTOINCREMENT,
    Username TEXT NOT NULL COLLATE NOCASE UNIQUE,
    PasswordHash BLOB NOT NULL,
    PasswordSalt BLOB NOT NULL,
    FullName TEXT NOT NULL,
    Role TEXT NOT NULL CHECK (Role IN ('Admin', 'User')),
    IsActive INTEGER NOT NULL DEFAULT 1 CHECK (IsActive IN (0, 1)),
    CreatedAt TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    UpdatedAt TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'))
);

CREATE TABLE IF NOT EXISTS Categories
(
    CategoryId INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL COLLATE NOCASE UNIQUE,
    Description TEXT NOT NULL DEFAULT '',
    CreatedAt TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'))
);

CREATE TABLE IF NOT EXISTS Products
(
    ProductId INTEGER PRIMARY KEY AUTOINCREMENT,
    SKU TEXT NOT NULL COLLATE NOCASE UNIQUE,
    Name TEXT NOT NULL,
    CategoryId INTEGER NOT NULL,
    Price NUMERIC NOT NULL CHECK (Price >= 0),
    Quantity INTEGER NOT NULL CHECK (Quantity >= 0),
    LowStockThreshold INTEGER NOT NULL CHECK (LowStockThreshold >= 0),
    CreatedAt TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    UpdatedAt TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId)
);

CREATE TABLE IF NOT EXISTS Sales
(
    SaleId INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER NOT NULL,
    TotalAmount NUMERIC NOT NULL CHECK (TotalAmount >= 0),
    SaleDate TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

CREATE TABLE IF NOT EXISTS SaleItems
(
    SaleItemId INTEGER PRIMARY KEY AUTOINCREMENT,
    SaleId INTEGER NOT NULL,
    ProductId INTEGER NOT NULL,
    Quantity INTEGER NOT NULL CHECK (Quantity > 0),
    UnitPrice NUMERIC NOT NULL CHECK (UnitPrice >= 0),
    Subtotal NUMERIC NOT NULL GENERATED ALWAYS AS (Quantity * UnitPrice) STORED,
    FOREIGN KEY (SaleId) REFERENCES Sales(SaleId),
    FOREIGN KEY (ProductId) REFERENCES Products(ProductId)
);

CREATE TABLE IF NOT EXISTS AuditLogs
(
    AuditLogId INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId INTEGER,
    Action TEXT NOT NULL,
    EntityName TEXT NOT NULL,
    EntityId INTEGER,
    Sku TEXT,
    Description TEXT NOT NULL,
    CreatedAt TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

CREATE TABLE IF NOT EXISTS Settings
(
    [Key] TEXT PRIMARY KEY,
    [Value] TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_Products_Name ON Products(Name);
CREATE INDEX IF NOT EXISTS IX_Products_CategoryId ON Products(CategoryId);
CREATE INDEX IF NOT EXISTS IX_Sales_SaleDate ON Sales(SaleDate);
CREATE INDEX IF NOT EXISTS IX_SaleItems_ProductId ON SaleItems(ProductId);
CREATE INDEX IF NOT EXISTS IX_AuditLogs_CreatedAt ON AuditLogs(CreatedAt);
