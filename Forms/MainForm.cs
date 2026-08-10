using InventoryManagementSystem.Configuration;
using InventoryManagementSystem.Services;
using InventoryManagementSystem.Utils;

namespace InventoryManagementSystem.Forms;

public sealed class MainForm : Form
{
    private readonly AppConfig _configuration;
    private readonly ApplicationServices _services;
    private readonly Session _session;
    private readonly Panel _contentPanel = new() { Dock = DockStyle.Fill, BackColor = Color.FromArgb(245, 247, 250) };
    private readonly Label _contentTitle = new() { AutoSize = true, Font = new Font("Segoe UI", 20, FontStyle.Bold) };
    private readonly Label _contentDescription = new() { AutoSize = true, MaximumSize = new Size(700, 0) };
    private readonly Label _statusLabel = new() { Dock = DockStyle.Bottom, Height = 28, Padding = new Padding(12, 5, 0, 0) };
    private Panel? _sidebar;
    private bool _sidebarExpanded = true;
    private bool _logoutRequested;
    private Button? _activeMenuButton;

    public bool ExitApplicationRequested { get; private set; }

    public MainForm(AppConfig configuration, ApplicationServices services, Session session)
    {
        _configuration = configuration;
        _services = services;
        _session = session;
        Text = _configuration.ApplicationName;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(960, 620);
        WindowState = FormWindowState.Maximized;
        BuildUi();
        Shown += (_, _) => ShowDashboard();
    }

    private void BuildUi()
    {
        var header = new Panel { Dock = DockStyle.Top, Height = 64, BackColor = Color.White, Padding = new Padding(16, 10, 16, 10) };
        var title = new Label { AutoSize = true, Font = new Font("Segoe UI", 14, FontStyle.Bold), Text = _configuration.ApplicationName, Dock = DockStyle.Left };
        var toggle = new Button { Text = "☰", Width = 42, Dock = DockStyle.Right, Name = "sidebarToggle" };
        toggle.Click += (_, _) => ToggleSidebar();
        header.Controls.Add(toggle);
        header.Controls.Add(title);

        _sidebar = BuildSidebar();
        var contentHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28) };
        contentHost.Controls.Add(_contentPanel);
        _contentPanel.Controls.Add(_contentDescription);
        _contentPanel.Controls.Add(_contentTitle);
        _contentDescription.Location = new Point(0, 52);
        _contentDescription.Text = "Use the navigation to manage products, record sales, and review reports.";
        _contentTitle.Text = "Dashboard";

        _statusLabel.Text = $"User: {_session.FullName}    Role: {_session.Role}    {DateTimeHelper.FormatForDisplay(DateTime.Now)}";
        var timer = new System.Windows.Forms.Timer { Interval = 30_000 };
        timer.Tick += (_, _) => _statusLabel.Text = $"User: {_session.FullName}    Role: {_session.Role}    {DateTimeHelper.FormatForDisplay(DateTime.Now)}";
        timer.Start();
        FormClosed += (_, _) => timer.Dispose();
        FormClosed += OnMainFormClosed;

        Controls.Add(contentHost);
        Controls.Add(_sidebar);
        Controls.Add(header);
        Controls.Add(_statusLabel);
    }

    private Panel BuildSidebar()
    {
        var sidebar = new Panel { Dock = DockStyle.Left, Width = 220, BackColor = Color.FromArgb(30, 115, 190), Padding = new Padding(8, 16, 8, 8), Name = "sidebar" };
        var menu = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        foreach (var item in new[] { "Dashboard", "Products", "Sales", "Reports" })
        {
            menu.Controls.Add(CreateMenuButton(item));
        }

        if (_session.IsAdmin)
        {
            menu.Controls.Add(CreateMenuButton("Users"));
            menu.Controls.Add(CreateMenuButton("Settings"));
        }

        menu.Controls.Add(CreateMenuButton("Logout"));
        menu.Controls.Add(CreateMenuButton("Quit"));
        sidebar.Controls.Add(menu);
        return sidebar;
    }

    private Button CreateMenuButton(string text)
    {
        var button = new Button { Text = text, Width = 196, Height = 42, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.FromArgb(30, 115, 190), TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0), Name = $"menu{text}", Tag = text };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(52, 139, 204);
        button.Click += (_, _) => SelectModule(text);
        button.MouseEnter += (_, _) => { if (!ReferenceEquals(button, _activeMenuButton)) button.BackColor = Color.FromArgb(52, 139, 204); };
        button.MouseLeave += (_, _) => { if (!ReferenceEquals(button, _activeMenuButton)) button.BackColor = Color.FromArgb(30, 115, 190); };
        return button;
    }

    private void SelectModule(string module)
    {
        if (module == "Logout")
        {
            _logoutRequested = true;
            Close();
            return;
        }

        if (module == "Quit")
        {
            ExitApplicationRequested = true;
            Application.Exit();
            return;
        }

        SetActiveMenu(module);

        if (module == "Products")
        {
            ShowProducts();
            return;
        }

        if (module == "Sales")
        {
            ShowSales();
            return;
        }

        if (module == "Dashboard")
        {
            ShowDashboard();
            return;
        }

        if (module == "Reports")
        {
            ShowReports();
            return;
        }

        if (module == "Users")
        {
            ShowUsers();
            return;
        }

        if (module == "Settings")
        {
            ShowSettings();
            return;
        }

        _contentPanel.Controls.Clear();
        _contentPanel.Controls.Add(_contentDescription);
        _contentPanel.Controls.Add(_contentTitle);
        _contentDescription.Location = new Point(0, 52);
        _contentTitle.Text = module;
        _contentDescription.Text = module == "Dashboard"
            ? "Dashboard metrics and inventory previews will appear here."
            : $"The {module.ToLowerInvariant()} module is connected to the shared application services and will be delivered in its implementation phase.";
    }

    private void ShowProducts()
    {
        _contentPanel.Controls.Clear();
        var productsForm = new ProductsForm(_services.Products, _session)
        {
            TopLevel = false,
            FormBorderStyle = FormBorderStyle.None,
            Dock = DockStyle.Fill
        };
        _contentPanel.Controls.Add(productsForm);
        productsForm.Show();
    }

    private void ShowSales()
    {
        _contentPanel.Controls.Clear();
        var salesForm = new SalesForm(_services.Products, _services.Sales, _session)
        {
            TopLevel = false,
            FormBorderStyle = FormBorderStyle.None,
            Dock = DockStyle.Fill
        };
        _contentPanel.Controls.Add(salesForm);
        salesForm.Show();
    }

    private void ShowDashboard()
    {
        SetActiveMenu("Dashboard");
        _contentPanel.Controls.Clear();
        var dashboardForm = new DashboardForm(_services.Dashboard, _session)
        {
            TopLevel = false,
            FormBorderStyle = FormBorderStyle.None,
            Dock = DockStyle.Fill
        };
        _contentPanel.Controls.Add(dashboardForm);
        dashboardForm.Show();
    }

    private void ShowReports()
    {
        _contentPanel.Controls.Clear();
        var reportsForm = new ReportsForm(_services.Reports, _session)
        {
            TopLevel = false,
            FormBorderStyle = FormBorderStyle.None,
            Dock = DockStyle.Fill
        };
        _contentPanel.Controls.Add(reportsForm);
        reportsForm.Show();
    }

    private void ShowUsers()
    {
        _contentPanel.Controls.Clear();
        var usersForm = new UsersForm(_services.Users, _session)
        {
            TopLevel = false,
            FormBorderStyle = FormBorderStyle.None,
            Dock = DockStyle.Fill
        };
        _contentPanel.Controls.Add(usersForm);
        usersForm.Show();
    }

    private void ShowSettings()
    {
        _contentPanel.Controls.Clear();
        var settingsForm = new SettingsForm(_services.Settings, _session)
        {
            TopLevel = false,
            FormBorderStyle = FormBorderStyle.None,
            Dock = DockStyle.Fill
        };
        _contentPanel.Controls.Add(settingsForm);
        settingsForm.Show();
    }

    private void ToggleSidebar()
    {
        if (_sidebar is null)
        {
            return;
        }

        _sidebarExpanded = !_sidebarExpanded;
        _sidebar.Width = _sidebarExpanded ? 220 : 60;
        if (_sidebar.Controls.Count == 1 && _sidebar.Controls[0] is FlowLayoutPanel menu)
        {
            foreach (Control control in menu.Controls)
            {
                if (control is Button button)
                {
                    button.Width = _sidebarExpanded ? 196 : 42;
                    var menuText = button.Tag?.ToString() ?? button.Name[4..];
                    button.Text = _sidebarExpanded ? menuText : menuText.Length > 0 ? menuText[..1] : "?";
                }
            }
        }
    }

    private void SetActiveMenu(string module)
    {
        if (_sidebar?.Controls.Count != 1 || _sidebar.Controls[0] is not FlowLayoutPanel menu)
        {
            return;
        }

        _activeMenuButton = menu.Controls.OfType<Button>().FirstOrDefault(button => string.Equals(button.Tag?.ToString(), module, StringComparison.Ordinal));
        foreach (var button in menu.Controls.OfType<Button>())
        {
            var isActive = ReferenceEquals(button, _activeMenuButton);
            button.BackColor = isActive ? Color.FromArgb(18, 83, 145) : Color.FromArgb(30, 115, 190);
            button.Font = new Font(button.Font, isActive ? FontStyle.Bold : FontStyle.Regular);
        }
    }

    private void OnMainFormClosed(object? sender, FormClosedEventArgs e)
    {
        if (!_logoutRequested && !ExitApplicationRequested)
        {
            ExitApplicationRequested = true;
            Application.Exit();
        }
    }
}
