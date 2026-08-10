# C# WinForms Inventory Management System — Master Development Specification

## 1. ROLE

You are an expert C#/.NET desktop application developer, software architect, database designer, and UI/UX engineer.

Build a complete **Inventory Management System** as a professional C# Windows Forms desktop application.

The application is intended for a university/student project but should be structured using production-minded practices so that it is easy to understand, maintain, extend, test, and demonstrate.

The application must be implemented incrementally and should remain compilable after every major phase.

---

# 2. CORE TECHNOLOGY

Use:

* C#
* .NET 8 Windows Forms
* Visual Studio 2022+
* ADO.NET
* SQL Server as the primary database
* SQLite as an optional secondary database
* `System.Windows.Forms.DataVisualization.Charting` for charts

Do NOT use:

* Windows Forms Designer
* `.Designer.cs` files
* `.resx` files for UI construction
* Entity Framework Core
* Dapper
* third-party UI frameworks unless explicitly requested

Every UI control must be created programmatically in C#.

The project must compile and run using the generated code.

---

# 3. IMPORTANT DEVELOPMENT RULE

Do NOT generate the entire application as one huge uncontrolled response/file.

Build the project in logical phases.

After each phase:

1. Ensure the code compiles.
2. Fix namespace/reference/type errors.
3. Keep existing functionality working.
4. Do not rewrite unrelated working code.
5. Explain which files were created or modified.
6. Continue to the next phase only after the current architecture is consistent.

Recommended phases:

### Phase 1

Project structure, configuration, models, utilities, database connection.

### Phase 2

Database schema and repositories/data-access layer.

### Phase 3

Authentication and registration.

### Phase 4

Main application shell and sidebar.

### Phase 5

Dashboard.

### Phase 6

Products management.

### Phase 7

Sales management.

### Phase 8

Reports and charts.

### Phase 9

User management and settings.

### Phase 10

Audit logs, CSV export, printing, keyboard shortcuts, validation, and final polish.

### Phase 11

Testing, cleanup, documentation, and production-hardening recommendations.

---

# 4. PROJECT NAME

Use:

`InventoryManagementSystem`

Suggested namespaces:

```text
InventoryManagementSystem
InventoryManagementSystem.Forms
InventoryManagementSystem.Models
InventoryManagementSystem.Services
InventoryManagementSystem.DataAccess
InventoryManagementSystem.Repositories
InventoryManagementSystem.Utils
InventoryManagementSystem.Events
InventoryManagementSystem.Configuration
```

---

# 5. PROJECT ARCHITECTURE

Use a lightweight layered architecture.

```text
InventoryManagementSystem
│
├── Program.cs
│
├── Configuration
│   └── AppConfig.cs
│
├── Models
│   ├── User.cs
│   ├── Product.cs
│   ├── Category.cs
│   ├── Sale.cs
│   ├── SaleItem.cs
│   ├── AuditLog.cs
│   └── AppSetting.cs
│
├── DataAccess
│   ├── IDbConnectionFactory.cs
│   ├── SqlServerConnectionFactory.cs
│   ├── SqliteConnectionFactory.cs
│   ├── IDatabaseProvider.cs
│   └── DatabaseInitializer.cs
│
├── Repositories
│   ├── IUserRepository.cs
│   ├── UserRepository.cs
│   ├── IProductRepository.cs
│   ├── ProductRepository.cs
│   ├── ICategoryRepository.cs
│   ├── CategoryRepository.cs
│   ├── ISaleRepository.cs
│   ├── SaleRepository.cs
│   ├── IAuditLogRepository.cs
│   └── AuditLogRepository.cs
│
├── Services
│   ├── AuthenticationService.cs
│   ├── ProductService.cs
│   ├── SalesService.cs
│   ├── ReportService.cs
│   ├── UserService.cs
│   └── AuditService.cs
│
├── Events
│   └── InventoryEvents.cs
│
├── Utils
│   ├── PasswordHasher.cs
│   ├── ValidationHelper.cs
│   ├── CsvExporter.cs
│   ├── SecureComparison.cs
│   └── DateTimeHelper.cs
│
└── Forms
    ├── LoginForm.cs
    ├── MainForm.cs
    ├── DashboardForm.cs
    ├── ProductsForm.cs
    ├── ProductEditForm.cs
    ├── SalesForm.cs
    ├── SaleForm.cs
    ├── ReportsForm.cs
    ├── UsersForm.cs
    ├── SettingsForm.cs
    └── HistoryForm.cs
```

You may adjust the structure if there is a strong technical reason, but maintain clear separation of:

* UI
* business logic
* data access
* models
* utilities

---

# 6. CODE-ONLY UI REQUIREMENT

This is a strict requirement.

Do NOT create:

```text
Form1.Designer.cs
MainForm.Designer.cs
*.resx
```

Do not use the Visual Studio drag-and-drop designer.

Every control must be instantiated in C#.

For example:

```csharp
private void CreateSidebar()
{
    sidebar = new Panel();
    sidebar.Dock = DockStyle.Left;
    sidebar.Width = 220;
    sidebar.BackColor = Color.FromArgb(30, 115, 190);

    Controls.Add(sidebar);
}
```

Separate UI construction into readable methods such as:

```text
CreateSidebar()
CreateHeader()
CreateStatusBar()
CreateDashboardCards()
CreateSearchControls()
CreateDataGrid()
CreateButtons()
ConfigureEvents()
```

---

# 7. DATABASE

The primary database is SQL Server.

Database name:

```text
InventoryDB
```

Create a complete SQL Server script:

```text
Database/SqlServer/01_CreateDatabase.sql
Database/SqlServer/02_CreateTables.sql
Database/SqlServer/03_Indexes.sql
Database/SqlServer/04_SeedData.sql
```

Also provide SQLite-compatible scripts where practical:

```text
Database/SQLite/01_CreateTables.sql
Database/SQLite/02_SeedData.sql
```

---

# 8. NORMALIZED DATABASE DESIGN

Use at least these tables:

```text
Users
Categories
Products
Sales
SaleItems
AuditLogs
Settings
```

Relationships:

```text
Categories
    │
    └── Products
            │
            └── SaleItems
                    │
                    └── Sales

Users
    ├── Sales
    └── AuditLogs
```

---

# 9. USERS TABLE

Recommended fields:

```text
UserId
Username
PasswordHash
PasswordSalt
FullName
Role
IsActive
CreatedAt
UpdatedAt
```

Rules:

* `Username` must be unique.
* Role must be either `Admin` or `User`.
* Password must NEVER be stored as plaintext.
* Inactive users cannot sign in.

---

# 10. PASSWORD SECURITY

Do NOT use plain SHA-256 alone for password storage.

Use:

```text
PBKDF2
```

with:

* random per-user salt
* sufficiently high iteration count
* SHA-256 or SHA-512 PRF
* constant-time comparison

Create:

```text
PasswordHasher.HashPassword()
PasswordHasher.VerifyPassword()
```

The salt must be stored in the database together with the derived password hash.

Registration:

```text
Password
    ↓
Generate random salt
    ↓
PBKDF2
    ↓
PasswordHash + PasswordSalt
    ↓
Database
```

Login:

```text
Entered Password
    ↓
Retrieve stored salt
    ↓
PBKDF2
    ↓
Constant-time comparison
```

---

# 11. AUTHENTICATION

Create a `LoginForm`.

The same form must support:

* Sign In
* Register

Use tabs or a toggle panel.

### Sign In fields

```text
Username
Password
Sign In button
```

### Register fields

```text
Username
Full Name
Password
Confirm Password
Register button
```

Validation:

* Username required.
* Full name required.
* Password required.
* Password confirmation must match.
* Username must be unique.
* Password must meet minimum complexity requirements.
* Display friendly validation messages.

After successful login:

```text
LoginForm
     ↓
MainForm
```

Pass the authenticated user information into the main application.

Create a session object such as:

```text
CurrentUser
    UserId
    Username
    FullName
    Role
```

Do not store passwords in the session.

---

# 12. ROLE-BASED ACCESS CONTROL

Two roles:

```text
Admin
User
```

### Admin

Admin can:

* View dashboard
* Manage products
* Record sales
* View reports
* Manage users
* Manage categories
* Manage settings
* View audit logs
* Delete products
* Edit products

### User

User can:

* View dashboard
* View inventory
* Record sales
* View sales information
* View reports where permitted

User cannot:

* Manage users
* Change system settings
* View sensitive administrative information
* Perform administrative operations

The UI must hide or disable unauthorized menu items.

Do not rely only on hiding buttons.

Services must also validate permissions.

---

# 13. MAIN APPLICATION UI

Create:

```text
MainForm
```

Layout:

```text
┌──────────────────────────────────────────────┐
│ Header                                       │
├──────────────┬───────────────────────────────┤
│              │                               │
│ Sidebar      │ Content Area                  │
│              │                               │
│ Dashboard    │                               │
│ Products     │                               │
│ Sales        │                               │
│ Reports      │                               │
│ Users        │                               │
│ Settings     │                               │
│              │                               │
│ Logout       │                               │
│              │                               │
├──────────────┴───────────────────────────────┤
│ Status Bar                                   │
└──────────────────────────────────────────────┘
```

---

# 14. SIDEBAR

Sidebar color:

```text
#1E73BE
```

Menu items:

```text
Dashboard
Products
Sales
Reports
Users
Settings
Logout
```

Users menu must only appear for Admin.

The active menu item must be visually highlighted.

Hover state should use a slightly lighter blue.

Active state should use a darker blue.

---

# 15. COLLAPSIBLE SIDEBAR

Sidebar width:

```text
Expanded = 220px
Collapsed = 60px
```

Add a toggle button.

When collapsed:

* Hide menu text.
* Keep icons or short visual indicators.
* Use tooltips to identify menu items.

The content area must resize automatically.

---

# 16. HEADER

Header must contain:

* Application title
* Global search
* Quick action buttons where appropriate
* Sidebar toggle

Use:

```text
Segoe UI
```

Avoid excessive colors.

Primary text:

```text
#000000
```

Content background:

```text
#F5F7FA
```

Primary accent:

```text
#1E73BE
```

Use subtle teal/orange accents for action buttons.

---

# 17. STATUS BAR

At the bottom display:

```text
User: John
Role: Admin
Date/Time: 10 Aug 2026 12:00
```

Date/time should update automatically.

---

# 18. DASHBOARD

Dashboard must display summary cards:

```text
Total Products
Low Stock Count
Today's Sales
Top Seller
```

Example:

```text
┌──────────────┐
│ Total        │
│ Products     │
│     125      │
└──────────────┘
```

Use clean cards with:

* title
* value
* optional icon
* subtle visual accent

Below the cards show a product preview DataGridView.

---

# 19. PRODUCTS

Products screen must support:

* View products
* Add product
* Edit product
* Delete product
* Search
* Category filter
* Sorting
* Low-stock highlighting
* Record sale
* View history
* Export CSV

Product fields:

```text
ProductId
SKU
Name
CategoryId
Price
Quantity
LowStockThreshold
CreatedAt
UpdatedAt
```

Rules:

* SKU unique.
* Name required.
* Price >= 0.
* Quantity >= 0.
* LowStockThreshold >= 0.

---

# 20. CATEGORY MANAGEMENT

Categories:

```text
CategoryId
Name
Description
CreatedAt
```

Category name must be unique.

Products reference categories through `CategoryId`.

Do not duplicate category names in the Products table.

---

# 21. REUSABLE DATAGRIDVIEW

Create reusable grid configuration/helper logic.

Every main DataGridView should:

```text
AllowUserToAddRows = false
ReadOnly = true
AutoGenerateColumns = false
```

Explicitly define columns.

Enable:

```text
Column sorting
Row selection
Double-click
Context menu
```

---

# 22. SEARCH

Provide a global search box.

For Products search across:

```text
Name
SKU
Category
```

Search should update results dynamically.

Use parameterized queries.

Do not concatenate user input directly into SQL.

---

# 23. FILTERING

Products:

```text
Category ComboBox
```

Sales:

```text
Start Date
End Date
```

Reports:

```text
Start Date
End Date
```

Allow filters to be combined.

---

# 24. PAGINATION

Design the repository/service layer to support pagination.

SQL Server should use:

```sql
OFFSET @Offset ROWS
FETCH NEXT @PageSize ROWS ONLY
```

SQLite should use:

```sql
LIMIT @PageSize OFFSET @Offset
```

Do not load millions of records into memory unnecessarily.

Use a configurable page size:

```text
25
50
100
```

---

# 25. LOW-STOCK HIGHLIGHTING

If:

```text
Quantity < LowStockThreshold
```

highlight the row.

Example:

```csharp
row.DefaultCellStyle.BackColor = Color.LightSalmon;
```

Also display the low-stock count on the dashboard.

---

# 26. CONTEXT MENU

Right-clicking a product should display:

```text
Edit
Delete
Record Sale
View History
```

Permissions must be checked before executing the action.

---

# 27. DOUBLE-CLICK

Double-clicking a product opens:

```text
ProductEditForm
```

The form must be created entirely in code.

---

# 28. CSV EXPORT

Add:

```text
Export CSV
```

The CSV exporter should:

* Handle commas correctly.
* Escape quotes.
* Include column headers.
* Use UTF-8.
* Allow the user to choose the save location.

Create:

```text
CsvExporter.Export()
```

---

# 29. SALES

Sales must support:

```text
Sale
SaleItems
```

A sale can contain one or more products.

Recommended fields:

### Sales

```text
SaleId
UserId
TotalAmount
SaleDate
```

### SaleItems

```text
SaleItemId
SaleId
ProductId
Quantity
UnitPrice
Subtotal
```

---

# 30. RECORD SALE TRANSACTION

Recording a sale must be atomic.

Use a database transaction:

```text
BEGIN TRANSACTION

1. Validate product exists.
2. Validate sufficient stock.
3. Insert Sale.
4. Insert SaleItems.
5. Reduce Product.Quantity.
6. Insert AuditLog.

COMMIT
```

If anything fails:

```text
ROLLBACK
```

Never allow:

```text
Sale recorded
but stock not reduced
```

or:

```text
Stock reduced
but sale not recorded
```

---

# 31. STOCK VALIDATION

Before recording a sale:

```text
RequestedQuantity <= AvailableQuantity
```

If insufficient stock:

```text
Do not modify the database.
Show a friendly error.
```

---

# 32. INTER-MENU EVENTS

When data changes, other screens must update automatically.

Create a lightweight event system such as:

```text
InventoryEvents
```

Events may include:

```text
ProductChanged
SaleRecorded
UserChanged
CategoryChanged
```

Examples:

When a sale is recorded:

```text
Sales
 ↓
Product quantity decreases
 ↓
Products grid refreshes
 ↓
Dashboard refreshes
 ↓
Reports refresh
 ↓
Low-stock count refreshes
```

When a product is added:

```text
Products
 ↓
Dashboard product count updates
 ↓
Reports can see the product
```

Avoid tightly coupling every form to every other form.

---

# 33. DELETE PRODUCT

When deleting a product:

1. Check whether historical SaleItems exist.
2. If historical sales exist, warn the administrator.
3. Prefer preventing destructive deletion when historical records exist.
4. Recommend deactivation/archiving instead.

Do not silently destroy historical business records.

---

# 34. AUDIT LOG

Create an `AuditLogs` table.

Store:

```text
AuditLogId
UserId
Action
EntityName
EntityId
Description
CreatedAt
```

Examples:

```text
LOGIN
CREATE_PRODUCT
UPDATE_PRODUCT
DELETE_PRODUCT
RECORD_SALE
CREATE_USER
UPDATE_USER
CHANGE_SETTINGS
```

Admin can view audit logs.

---

# 35. REPORTS

Create:

```text
ReportsForm
```

Reports should include:

### Daily Sales

```text
Date
NumberOfSales
TotalSales
```

### Top-Selling Products

```text
Product
QuantitySold
Revenue
```

### Sales by Date Range

```text
Date
Sales
```

Use parameterized queries.

---

# 36. CHARTS

Use:

```text
System.Windows.Forms.DataVisualization.Charting.Chart
```

Create charts programmatically.

Include:

```text
Daily sales line chart
Top products bar chart
```

Do not use chart images generated externally.

---

# 37. REPORT EXPORT

Allow report data to be exported to CSV.

Also implement:

```text
Print Preview
```

for report output where practical.

---

# 38. SETTINGS

Settings should include at minimum:

```text
LowStockDefault
ApplicationName
```

Admin-only access.

When a new product is created, use `LowStockDefault` as the default threshold.

---

# 39. DATABASE ACCESS

Use ADO.NET.

All database queries must be parameterized.

Bad:

```csharp
"SELECT * FROM Products WHERE Name = '" + name + "'"
```

Good:

```csharp
"SELECT * FROM Products WHERE Name = @Name"
```

Use:

```text
SqlConnection
SqlCommand
SqlDataReader
SqlTransaction
```

for SQL Server.

For SQLite use:

```text
Microsoft.Data.Sqlite
```

Create a database abstraction so services are not tightly coupled to one provider.

---

# 40. CONNECTION CONFIGURATION

Provide a central configuration mechanism.

Example SQL Server:

```text
Server=.;
Database=InventoryDB;
Trusted_Connection=True;
TrustServerCertificate=True;
```

Example SQLite:

```text
Data Source=inventory.db;
```

Do not hard-code connection strings throughout the application.

---

# 41. REPOSITORY RULE

Repositories are responsible for database operations.

Services are responsible for business rules.

Forms are responsible for UI.

Example:

```text
ProductsForm
     ↓
ProductService
     ↓
ProductRepository
     ↓
SQL Server
```

Do not put SQL queries directly inside Forms.

---

# 42. SERVICE LAYER

Create testable service methods.

Examples:

```text
CalculateLowStock()
ComputeDailySales()
CalculateSaleTotal()
ValidateStock()
RecordSale()
CreateProduct()
UpdateProduct()
DeleteProduct()
```

Keep business logic out of UI event handlers.

---

# 43. UNIT-TESTABLE METHODS

Create pure or mostly isolated methods where possible.

Example:

```csharp
public int CalculateLowStock(IEnumerable<Product> products)
```

and:

```csharp
public decimal CalculateSaleTotal(IEnumerable<SaleItem> items)
```

These should be easy to test without launching the WinForms UI.

If practical, create:

```text
InventoryManagementSystem.Tests
```

using a standard .NET test framework.

---

# 44. VALIDATION

Create reusable validation helpers.

Validate:

* Required fields
* Numeric values
* Price
* Quantity
* SKU
* Username
* Password
* Date ranges

Do not allow invalid data to reach the database.

Database constraints should also enforce important rules.

---

# 45. ERROR HANDLING

Do not expose raw SQL exceptions to users.

Bad:

```text
SqlException: Violation of UNIQUE KEY...
```

Instead show:

```text
A product with this SKU already exists.
```

Log technical details where appropriate.

---

# 46. KEYBOARD SHORTCUTS

Implement:

```text
Ctrl + N = New Product
Ctrl + F = Focus Search
F3       = Focus Search
Esc      = Close current dialog where appropriate
```

Use proper WinForms key handling.

---

# 47. ACCESSIBILITY

Controls should have:

* Meaningful `Name`
* Accessible text where applicable
* Tooltips
* Logical tab order

Buttons should have descriptive text.

Do not rely only on color to communicate status.

---

# 48. RESPONSIVE UI

The application should resize correctly.

Use:

```text
Dock
Anchor
TableLayoutPanel
FlowLayoutPanel
SplitContainer
```

where appropriate.

Avoid hardcoding every control position.

The main content area must resize when:

* Window is maximized.
* Window is resized.
* Sidebar collapses.

---

# 49. SEED DATA

Provide SQL seed data for:

```text
Categories
Products
Admin user
Normal user
Sample sales
Settings
```

Do NOT hard-code a plaintext password into the Users table.

Provide a clear development seed procedure that generates or inserts a properly derived password hash and salt.

For production, users should change default credentials immediately.

---

# 50. SQL SERVER INDEXES

Create appropriate indexes.

At minimum consider:

```text
Users.Username
Products.SKU
Products.Name
Products.CategoryId
Sales.SaleDate
SaleItems.ProductId
AuditLogs.CreatedAt
```

Explain why each important index exists.

---

# 51. DATABASE CONSTRAINTS

Use:

```text
PRIMARY KEY
FOREIGN KEY
UNIQUE
NOT NULL
CHECK
DEFAULT
```

where appropriate.

For example:

```text
Products.Price >= 0
Products.Quantity >= 0
Products.LowStockThreshold >= 0
```

---

# 52. TRANSACTION SAFETY

Use transactions for all multi-step operations.

Especially:

```text
Record Sale
Delete operations involving related records
User operations that affect multiple tables
```

Keep transaction scope short.

---

# 53. UI NAVIGATION

MainForm should host or display child views/forms cleanly.

Recommended approach:

```text
MainForm
 └── ContentPanel
       ├── DashboardForm/View
       ├── ProductsForm/View
       ├── SalesForm/View
       ├── ReportsForm/View
       ├── UsersForm/View
       └── SettingsForm/View
```

Avoid opening unlimited duplicate windows when repeatedly clicking a menu item.

---

# 54. LOGOUT

Logout must:

1. Confirm if appropriate.
2. Close/clear the current session.
3. Return to LoginForm.
4. Prevent access to MainForm without authentication.

Do not terminate the entire application unless the user explicitly chooses Exit.

---

# 55. SECURITY REQUIREMENTS

Minimum security requirements:

* Salted password hashing.
* PBKDF2.
* Parameterized SQL.
* Role-based authorization.
* No plaintext passwords.
* No passwords in logs.
* Database constraints.
* Transactions.
* Input validation.
* Least-privilege database account recommendation.
* Avoid exposing database exceptions directly to users.

Production recommendations may additionally discuss:

* SQL Server encryption.
* Secrets management.
* Database backups.
* Account lockout.
* MFA.
* Encryption at rest.
* Secure deployment.

---

# 56. README

Create:

```text
README.md
```

Include:

## Prerequisites

* Windows
* Visual Studio 2022+
* .NET 8 SDK
* SQL Server Express/Developer
* SQL Server Management Studio

## Database setup

Explain:

1. Create database.
2. Run schema.
3. Run indexes.
4. Run seed data.
5. Configure connection string.

## Application setup

Explain how to:

1. Clone/open project.
2. Restore packages.
3. Configure connection.
4. Build.
5. Run.

## Default accounts

Clearly identify development-only accounts.

Tell the developer to change passwords after first login.

## User flow

Explain:

```text
Register
 ↓
Login
 ↓
Dashboard
 ↓
Products
 ↓
Sales
 ↓
Reports
```

## Admin flow

Explain administrative features.

## Maintenance

Explain:

* Database backup
* Password changes
* Log review
* Resetting development database

---

# 57. EXTENSION IDEAS

At the end, provide optional future improvements:

```text
Barcode scanner support
Purchase orders
Supplier management
Customer management
Inventory adjustments
Stock transfer
Low-stock notifications
PDF reports
Email notifications
Multi-warehouse support
Dashboard analytics
Cloud database
REST API
Mobile application
MFA
```

Do not implement these unless explicitly requested.

---

# 58. PRODUCTION HARDENING

Provide a separate section explaining improvements that would be required before real-world production deployment.

Examples:

* Proper secrets management.
* Database backup strategy.
* Account lockout.
* MFA.
* Centralized logging.
* Monitoring.
* Encryption.
* Least privilege.
* Database migrations.
* Automated testing.
* Installer/deployment strategy.
* Versioning.

Clearly distinguish these from the student-project implementation.

---

# 59. CODING STYLE

Follow standard C# naming conventions.

Types:

```text
PascalCase
```

Methods:

```text
PascalCase
```

Properties:

```text
PascalCase
```

Private fields:

```text
_camelCase
```

Local variables:

```text
camelCase
```

Constants:

```text
PascalCase
```

Use nullable reference types where appropriate.

Use:

```text
async/await
```

for database operations where it improves responsiveness.

Do not block the UI thread with long-running database operations.

---

# 60. COMMENTS

Write useful comments.

Do NOT comment every obvious line.

Good:

```csharp
// Use a transaction because recording a sale and reducing stock
// must succeed or fail together.
```

Avoid:

```csharp
// Create a button
button = new Button();
```

---

# 61. FILE ORGANIZATION

Keep each class focused.

Do not create giant 2,000-line Forms.

If a form becomes too large, extract:

```text
UI builders
Services
Validators
Grid configuration
Dialogs
```

into appropriate classes.

---

# 62. ACCEPTANCE CRITERIA

The project is considered complete only when:

### Authentication

* [ ] Register works.
* [ ] Duplicate usernames are rejected.
* [ ] Password confirmation works.
* [ ] Passwords are securely hashed.
* [ ] Login works.
* [ ] Invalid credentials are rejected.
* [ ] Inactive users cannot login.

### Authorization

* [ ] Admin can access administration.
* [ ] User cannot access administration.
* [ ] Service layer checks authorization.

### Dashboard

* [ ] Product count works.
* [ ] Low-stock count works.
* [ ] Today's sales works.
* [ ] Top seller works.
* [ ] Product preview loads.

### Products

* [ ] Add works.
* [ ] Edit works.
* [ ] Delete works according to historical-sales rules.
* [ ] Search works.
* [ ] Category filter works.
* [ ] Sorting works.
* [ ] Low-stock highlighting works.
* [ ] CSV export works.

### Sales

* [ ] Sale can be recorded.
* [ ] Stock decreases correctly.
* [ ] Insufficient stock is rejected.
* [ ] Sale and stock update use a transaction.
* [ ] Dashboard refreshes after sale.

### Reports

* [ ] Daily sales report works.
* [ ] Top-selling products works.
* [ ] Date filtering works.
* [ ] Charts work.
* [ ] CSV export works.
* [ ] Print preview works.

### Users

* [ ] Admin can view users.
* [ ] Admin can create users.
* [ ] Admin can change roles.
* [ ] Admin can deactivate users.

### Audit

* [ ] Important operations create audit records.
* [ ] Passwords are never logged.

### UI

* [ ] No Designer files.
* [ ] Sidebar works.
* [ ] Sidebar collapses.
* [ ] Active menu is highlighted.
* [ ] Window resizing works.
* [ ] Status bar works.
* [ ] Tooltips work.
* [ ] Keyboard shortcuts work.

---

# 63. IMPORTANT IMPLEMENTATION CONSTRAINTS

Do not:

* Use plaintext passwords.
* Put SQL directly inside Forms.
* Concatenate user input into SQL.
* Use static global database connections.
* Duplicate business logic across Forms.
* Create Designer files.
* Create unnecessary dependencies.
* Delete historical sales without warning.
* Allow users to bypass role restrictions by manually triggering UI events.
* Make the UI freeze during long database operations.

---

# 64. FINAL DELIVERABLES

The completed project must contain:

```text
1. Complete .NET WinForms project
2. Code-only UI
3. Authentication
4. Registration
5. Role-based authorization
6. Dashboard
7. Products
8. Categories
9. Sales
10. Reports
11. Users
12. Settings
13. Audit Logs
14. Search
15. Filtering
16. Sorting
17. Pagination
18. Low-stock highlighting
19. Context menu
20. CSV export
21. Charts
22. Print preview
23. Keyboard shortcuts
24. SQL Server scripts
25. SQLite scripts where supported
26. Seed data
27. README.md
28. Unit-testable service methods
29. Tests where practical
30. Production-hardening recommendations
```

---

# 65. FINAL INSTRUCTION TO CODEX

Before writing code:

1. Analyze the complete specification.
2. Identify any contradictions.
3. Choose sensible defaults.
4. Do not ask unnecessary questions.
5. Create a clear implementation plan.
6. Build the project incrementally.
7. Keep the project compilable after every phase.
8. Prefer simple, understandable code over unnecessary abstraction.
9. Do not introduce third-party libraries unless required.
10. Do not generate Designer files.
11. Do not skip database transactions.
12. Do not skip validation.
13. Do not skip authorization checks.
14. Do not store plaintext passwords.
15. Do not place business logic inside UI forms.

When completing each phase, report:

```text
PHASE:
Files created:
Files modified:
Features implemented:
Database changes:
How to test:
Known limitations:
Next phase:
```

The final result should be a clean, understandable, maintainable **C# WinForms Inventory Management System** suitable for a university project demonstration and structured so that it can later be extended into a production application.
