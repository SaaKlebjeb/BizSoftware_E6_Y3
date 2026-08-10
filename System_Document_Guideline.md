# Inventory Management System

## System Documentation and User Guide

**Document status:** Current system guideline  
**Technology:** C#, .NET 8 Windows Forms, ADO.NET  
**Database:** SQL Server primary; SQLite optional  
**Document purpose:** This file describes the implemented system, user workflows, technical architecture, business rules, database design, limitations, and future improvements. It can be copied into ChatGPT to generate a professional `.docx` document.

---

## 1. System Overview

The Inventory Management System is a desktop business application for managing products, categories, stock quantities, sales, invoices, reports, users, application settings, and audit information.

The system is suitable for small and medium-sized businesses such as retail stores, office-supply shops, small warehouses, general trading businesses, and inventory-based student projects.

The application is built with:

- C# and .NET 8
- Windows Forms
- ADO.NET
- SQL Server as the primary database
- SQLite as an optional local database
- Programmatic UI construction without Windows Forms Designer files

The system uses this layered architecture:

```text
Windows Forms UI
        |
Service Layer - business rules and authorization
        |
Repository Layer - database operations
        |
ADO.NET data-access provider
        |
SQL Server or SQLite
```

## 2. Main Objectives

The system provides the following business capabilities:

1. Maintain product master data.
2. Organize products into unlimited categories.
3. Generate SKUs automatically when required.
4. Track product stock quantities.
5. Identify low-stock products.
6. Record multi-product sales safely.
7. Preview and print invoices before final recording.
8. Prevent stock from becoming negative.
9. Produce sales reports and charts.
10. Manage users and permissions.
11. Review business activity through audit logs.
12. Export product, report, and audit information.
13. Support SQL Server and SQLite database providers.
14. Refresh related screens when inventory or sales data changes.

## 3. User Roles and Permissions

### 3.1 Administrator

Administrators can view the whole system and can:

- Create, edit, and delete products according to business rules.
- Restore stock and provide a reason.
- Add, edit, and delete unused categories.
- Record sales and print invoices.
- View reports.
- View audit logs.
- Create users and assign a role during creation.
- Activate and deactivate users.
- Change application settings.

### 3.2 Normal User

Normal users can:

- View the Dashboard.
- View, search, and filter products.
- Record sales.
- Preview and print invoices.
- View reports.
- Export available report information.

Normal users cannot manage products, categories, users, audit logs, or settings. Authorization is checked in the service layer as well as in the user interface.

## 4. Authentication and Registration

### Registration

The Register tab accepts:

- Username
- Full name
- Password
- Password confirmation

Validation rules:

- Username must contain 3–50 characters.
- Allowed username characters are letters, numbers, dots, underscores, and hyphens.
- Full name is required.
- Password must be at least 8 characters and contain uppercase, lowercase, and numeric characters.
- Password confirmation must match.
- Duplicate usernames are rejected.
- Newly registered users receive the normal User role.

### Password security

Passwords are never stored as plain text. The system uses PBKDF2 hashing and a different random salt for every account.

### Login

The login process validates the username, password, account status, and password hash before creating a session. Invalid credentials and inactive accounts are rejected.

### Development account note

The database seed scripts intentionally do not insert a plain-text password. The recommended development procedure is to create an account through Register and promote it to Administrator using the database setup instructions. Any development credentials must be changed before real deployment.

## 5. Main Application Layout

The main window contains:

- Application title
- Sidebar navigation
- Main content area
- Sidebar collapse button
- Current user name
- Current role
- Current date and time

Normal users see Dashboard, Products, Sales, Reports, Logout, and Quit. Administrators additionally see Categories, Audit Logs, Users, and Settings.

## 6. Dashboard

The Dashboard displays:

- **Total products:** Number of products in inventory.
- **Low stock:** Number of products where `Quantity < LowStockThreshold`.
- **Today’s sales:** Total sales amount for the current day.
- **Top seller:** Product with the highest quantity sold.

The product preview shows SKU, product name, category, quantity, and price. Low-stock products are highlighted. The Dashboard refreshes after product changes, stock restoration, and completed sales.

## 7. Category Management

The system supports unlimited categories. The initial General and Office categories are only seed data, not a two-category limitation.

Category fields:

- CategoryId
- Name
- Description
- CreatedAt

Administrators can add, edit, view, and delete categories.

Rules:

- Category name is required.
- Category name cannot exceed 100 characters.
- Category names are unique.
- Description cannot exceed 500 characters.
- A category containing products cannot be deleted.

To delete a used category, first move its products to another category.

## 8. Products and Inventory

Product fields:

- ProductId
- SKU
- Name
- CategoryId
- CategoryName
- Price
- Quantity
- LowStockThreshold
- CreatedAt
- UpdatedAt

Product validation:

- Product name is required and cannot exceed 200 characters.
- Category is required.
- Price cannot be negative.
- Quantity cannot be negative.
- Low-stock threshold cannot be negative.
- SKU cannot exceed 50 characters.
- SKU must be unique.

### Automatic SKU

When creating a product, the SKU field may be left blank. The system generates a stable internal SKU such as:

```text
SKU-000004
```

### Manual SKU

Businesses may enter their own SKU or supplier code. Existing SKU values are protected while editing so historical product identity is not accidentally changed.

### Add product procedure

1. Open Products.
2. Select Add Product.
3. Enter the product name.
4. Select a category.
5. Enter price and opening quantity.
6. Set the low-stock threshold.
7. Leave SKU blank for automatic generation or enter a manual SKU.
8. Select Save.
9. Confirm that the new product appears in the grid.

### Restore stock procedure

1. Select a product.
2. Select Restore Stock.
3. Enter a positive quantity.
4. Enter a reason, such as New Delivery Received.
5. Select Restore.
6. Confirm that quantity increased.
7. Review the audit history if required.

### Product deletion

Administrators may delete products without historical sales. Products referenced by SaleItems cannot be deleted. A future production version should provide product archiving instead of permanent deletion.

## 9. Search, Filtering, and Pagination

Products can be searched by:

- Product name
- SKU
- Category name

Products can be filtered by category. Pagination supports 25, 50, or 100 records per page. SQL Server uses `OFFSET/FETCH`; SQLite uses `LIMIT/OFFSET`.

## 10. Sales Workflow

The sales process has two stages:

```text
Select products
      ↓
Add products to cart
      ↓
Preview invoice
      ↓
Print or review invoice
      ↓
Confirm and record sale
      ↓
Update stock and audit log
```

### Add items to cart

1. Open Sales.
2. Select an available product.
3. Select the quantity.
4. Select Add to Sale.
5. Repeat for additional products.
6. Review quantity, price, subtotal, and total.

Only products with available stock are displayed. The user cannot add more units than the current available quantity.

### Invoice preview

Selecting Preview Invoice prepares a sale snapshot and opens a dialog containing:

- System name
- Sales invoice title
- Date
- Cashier
- SKU
- Product name
- Quantity
- Unit price
- Subtotal
- Total

Previewing or printing does not reduce inventory.

### Final confirmation

Selecting Confirm & Record Sale rechecks product existence and available stock. Only after successful validation does the system write the sale and reduce inventory.

## 11. Invoice Printing

The invoice supports print preview and printer output. The printed layout includes:

- System name at the top
- Sales Invoice title
- Date and cashier
- Aligned SKU and product columns
- Right-aligned quantity and prices
- Subtotals
- Clearly separated total amount

If a printer or Windows Print Spooler is unavailable, the system shows an explanatory message.

## 12. Sale Transaction Safety

Sale recording is atomic:

1. Validate the sale.
2. Validate every product.
3. Validate available stock.
4. Insert the Sales record.
5. Reduce Products.Quantity.
6. Insert SaleItems.
7. Insert a RECORD_SALE audit record.
8. Commit the transaction.

If any step fails, the database transaction is rolled back. This prevents a sale from being recorded without stock reduction or stock reduction without a sale.

## 13. Reports

The Reports module supports a start date and end date.

### Daily Sales

Displays:

- Date
- Number of sales
- Total sales amount

### Top-Selling Products

Displays:

- Product name
- Quantity sold
- Revenue

### Charts

The system displays a daily sales chart and a top-products chart.

### Export and printing

Daily sales can be exported to CSV and printed through print preview. Future versions should provide export and print for every report tab.

## 14. Audit Logs

Administrators can review audit records by date range. Product history can be filtered by entity and entity ID.

Audit fields include:

- AuditLogId
- UserId
- Username
- Action
- EntityName
- EntityId
- Description
- CreatedAt

Current transaction examples include RECORD_SALE and RESTORE_STOCK. The audit framework can be extended to record all create, update, delete, login, user, and settings actions.

## 15. User Management

Administrators can create users, assign a role during creation, and activate or deactivate accounts. Users are displayed with username, full name, role, and active status. An administrator cannot deactivate their own current account.

Future improvement: provide a dedicated interface for changing an existing user’s role after creation.

## 16. Settings

Administrator settings include:

- ApplicationName
- LowStockDefault

ApplicationName appears in the main header and printed invoices. The low-stock default is stored for configuration purposes. A future improvement should automatically apply the setting when creating new products.

## 17. Data Export

The system supports:

- Product CSV export
- Product Excel-compatible export
- Product print preview
- Daily sales CSV export
- Daily sales print preview
- Audit-log Excel-compatible export
- Audit-log print preview
- Invoice print preview

CSV files include headers and use UTF-8 encoding. Commas and quotation marks are escaped.

## 18. Database Design

### Users

Stores user accounts, password hashes, roles, and active status.

### Categories

Stores category names and descriptions.

### Products

Stores SKU, name, category, price, quantity, and low-stock threshold.

### Sales

Stores sale header information, cashier, total, and date.

### SaleItems

Stores each product line, quantity, unit price, and calculated subtotal.

### AuditLogs

Stores user activity and business history.

### Settings

Stores application key-value settings.

Database constraints include primary keys, foreign keys, unique values, required fields, defaults, and non-negative numeric checks.

## 19. Configuration and Installation

### SQL Server setup

1. Open SQL Server Management Studio.
2. Run `Database/SqlServer/00_SetupAll.sql`.
3. Verify that InventoryDB was created.
4. Verify tables, constraints, indexes, and seed categories.
5. Create an account through the Register tab.
6. Promote the account to Administrator according to the development instructions.
7. Verify the connection string in App.config.

### SQLite setup

1. Set `INVENTORY_DB_PROVIDER=SQLite`.
2. Run the SQLite table script.
3. Run the SQLite seed script.
4. Confirm that `inventory.db` is accessible.

Configuration can be overridden using:

```text
INVENTORY_DB_PROVIDER
INVENTORY_SQLSERVER_CONNECTION
INVENTORY_SQLITE_CONNECTION
```

## 20. Error Handling

The system provides user-friendly messages for:

- Duplicate SKU or category name
- Missing product or category
- Insufficient stock
- Invalid values
- Unauthorized operations
- SQL Server unavailable
- General database failure

Technical errors should be logged securely in a production version without exposing passwords or connection secrets.

## 21. Current Limitations and Future Features

The current system does not yet include:

- Customer management
- Supplier management
- Purchase orders
- Receiving workflow
- Barcode scanning
- Warehouses or branches
- Stock transfers
- Tax and discount calculation
- Payment methods
- Returns and refunds
- Sale cancellation
- Sale history and invoice reprinting from stored sales
- Product or category archive status
- Low-stock notifications
- Email notifications
- Automated backups
- Database migrations
- Multi-factor authentication
- Account lockout
- REST API or mobile application

These should be documented as future enhancements, not existing features.

## 22. Production Recommendations

Before real business deployment, add:

1. Secure secrets management.
2. Least-privilege database credentials.
3. SQL Server encryption.
4. Automated backups and restore testing.
5. Product and category archiving.
6. Customer, tax, discount, payment, refund, and return workflows.
7. Formal invoice numbering.
8. Sale history and invoice reprinting.
9. Centralized technical logging.
10. Database migrations.
11. Automated unit and integration tests.
12. Account lockout and multi-factor authentication.
13. Monitoring and health checks.
14. Signed installer and controlled versioning.

## 23. Acceptance Testing Checklist

### Authentication

- Registration works.
- Duplicate usernames are rejected.
- Strong password rules work.
- Login succeeds with valid credentials.
- Invalid credentials are rejected.
- Inactive users cannot log in.

### Authorization

- Administrators can access administration modules.
- Normal users cannot access administration modules.
- Service-layer permissions are enforced.

### Categories

- Categories can be added.
- Categories can be edited.
- Duplicate category names are rejected.
- Categories containing products cannot be deleted.

### Products

- Products can be added.
- Automatic SKU generation works.
- Manual SKU entry works.
- Duplicate SKUs are rejected.
- Search works.
- Category filtering works.
- Pagination works.
- Low-stock highlighting works.
- Historical products cannot be deleted.

### Inventory

- Stock restoration works.
- Positive quantity is required.
- Restoration reason is required.
- Stock restoration creates history.

### Sales

- Multiple products can be added to a sale.
- Insufficient stock is rejected.
- Invoice preview displays correct values.
- Invoice columns are aligned.
- Invoice printing works.
- Preview does not reduce stock.
- Final confirmation reduces stock.
- Sale and stock changes are atomic.
- Failed sales roll back.

### Reports

- Date filtering works.
- Daily sales totals are correct.
- Top products are correct.
- Charts display data.
- CSV export works.
- Print preview works.

### Administration

- Users can be created.
- Roles are assigned correctly.
- Accounts can be activated and deactivated.
- Administrators cannot deactivate themselves.
- Settings can be saved.
- Audit logs can be filtered and exported.

## 24. Glossary

**SKU:** Stock Keeping Unit, a unique product identification code.  
**Inventory:** The quantity of products currently available.  
**Low-stock threshold:** The minimum quantity at which a product is considered low stock.  
**Sale:** The sale header containing cashier, date, and total amount.  
**Sale item:** One product line inside a sale.  
**Invoice preview:** The review and printing stage before final recording.  
**Audit log:** A history record of important activities.  
**Repository:** A class responsible for database operations.  
**Service:** A class responsible for validation and business rules.  
**Transaction:** A group of database operations that all succeed or all roll back.

## 25. Document Generation Instruction

Convert this content into a professional `.docx` document with:

- Cover page
- Table of contents
- Page numbers
- Header containing “Inventory Management System”
- Footer containing document version and date
- Blue and gray business theme
- Heading styles
- Tables for roles, database tables, and acceptance testing
- Clearly marked Implemented Features and Future Enhancements
- Screenshot placeholders for Login, Dashboard, Products, Sales, Invoice Preview, Reports, Users, Categories, Settings, and Audit Logs
- Revision history table

Do not invent features that are not described as implemented. Clearly distinguish current functionality, limitations, and recommendations for future production use.
