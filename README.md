# Inventory Management System

A code-only C# WinForms inventory system targeting .NET 8. The application uses ADO.NET repositories, SQL Server as the primary provider, and SQLite as an optional local provider.

## Prerequisites

- Windows and Visual Studio 2022+ or the .NET 8 SDK
- SQL Server Express/Developer and SQL Server Management Studio for the primary setup
- .NET 8 desktop runtime

## Database setup

### SQL Server

1. Open `Database/SqlServer/00_SetupAll.sql` in SQL Server Management Studio.
2. Execute the entire script once. It creates `InventoryDB`, all tables, constraints, indexes, seed data, and a development administrator.
3. The default connection is `Server=.;Database=InventoryDB;Trusted_Connection=True;TrustServerCertificate=True;`.

### SQLite

Set `INVENTORY_DB_PROVIDER=SQLite`, then run `Database/SQLite/01_CreateTables.sql` and `Database/SQLite/02_SeedData.sql`. The default SQLite file is `inventory.db`.

SQL Server and SQLite connections are configured in `App.config`. The default SQL Server instance is `LAPTOP-TAKL7QQE\SQLEXPRESS`. Environment variables `INVENTORY_DB_PROVIDER`, `INVENTORY_SQLSERVER_CONNECTION`, and `INVENTORY_SQLITE_CONNECTION` override the file when supplied. Secrets should be supplied outside source control.

## Run

```powershell
dotnet restore
dotnet build
dotnet run
```

The project intentionally contains no Windows Forms Designer files or resource-based UI files. All controls are created in C#.

## Development account flow

1. Use the seeded development account: `admin` / `Admin@123`.
2. Change or deactivate this account before sharing the application.
3. Register additional users through the Register tab; administrators can assign roles from Users.

Passwords are PBKDF2-derived with a random per-user salt and are never stored in plaintext.

## Implemented workflow

- Secure registration and login with inactive-user rejection
- Admin-only product create, edit, and delete rules
- Product search, category filtering, pagination, sorting, low-stock highlighting, standards-compliant CSV export, and bordered Excel-compatible spreadsheet export
- Dashboard cards and product preview
- Multi-item sales with server-side price lookup and atomic sale/stock/audit transactions
- Date-range reports with daily/top-product tables, charts, CSV export, and print preview
- Admin-only user creation, role/active-state management, and settings persistence
- SQL Server and SQLite schema scripts with constraints and indexes
- Reusable repositories, services, validation utilities, and application events

## Architecture

`Forms` handles code-only UI, `Services` owns business rules, `Repositories` owns parameterized ADO.NET operations, `DataAccess` selects the database provider, and `Models` contains domain data.

## Production hardening

Before real deployment, add secrets management, a least-privilege SQL account, encrypted connections, backups and restore testing, account lockout/MFA, centralized logging, migrations, automated integration tests, monitoring, and a signed installer. The current seed and role-promotion flow is for development/demo use only.

## Future phases

Barcode scanning, suppliers, purchase orders, warehouses, notifications, richer audit-log browsing, and a REST API are intentionally not included in the student-project core.
