using InventoryManagementSystem.Configuration;
using InventoryManagementSystem.Services;
using InventoryManagementSystem.Utils;

namespace InventoryManagementSystem.Forms;

public sealed class LoginForm : Form
{
    private readonly AppConfig _configuration;
    private readonly ApplicationServices _services;
    private readonly TextBox _loginUsername = new() { Name = "loginUsername" };
    private readonly TextBox _loginPassword = new() { Name = "loginPassword", UseSystemPasswordChar = true };
    private readonly TextBox _registerUsername = new() { Name = "registerUsername" };
    private readonly TextBox _registerFullName = new() { Name = "registerFullName" };
    private readonly TextBox _registerPassword = new() { Name = "registerPassword", UseSystemPasswordChar = true };
    private readonly TextBox _registerConfirmation = new() { Name = "registerConfirmation", UseSystemPasswordChar = true };
    private readonly Label _statusLabel = new() { AutoSize = true, ForeColor = Color.Firebrick };
    private TabControl? _tabs;
    private Button? _signInButton;
    private Button? _registerButton;

    public LoginForm(AppConfig configuration)
    {
        _configuration = configuration;
        _services = new ApplicationServices(configuration);
        Text = _configuration.ApplicationName;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(243, 247, 251);
        MinimumSize = new Size(860, 620);
        ClientSize = new Size(960, 680);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BuildUi();
    }

    private void BuildUi()
    {
        var header = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 125,
            BackColor = Color.FromArgb(24, 79, 144),
            Padding = new Padding(28, 18, 28, 18),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false
        };

        var title = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            ForeColor = Color.White,
            Text = _configuration.ApplicationName,
            Margin = new Padding(0, 0, 0, 5)
        };

        var subtitle = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 12f, FontStyle.Regular),
            ForeColor = Color.FromArgb(235, 240, 250),
            Text = "Secure sign in and registration"
        };

        header.Controls.Add(title);
        header.Controls.Add(subtitle);


        _tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Name = "authenticationTabs",
            Font = new Font("Segoe UI", 10F),
            ItemSize = new Size(100, 28),
            SizeMode = TabSizeMode.Fixed
        };
        _tabs.SelectedIndexChanged += (_, _) => UpdateAcceptButton();
        _tabs.TabPages.Add(BuildSignInPage());
        _tabs.TabPages.Add(BuildRegistrationPage());

        var container = new Panel { Dock = DockStyle.Fill, Padding = new Padding(28, 22, 28, 18), BackColor = BackColor };
        container.Controls.Add(_tabs);
        Controls.Add(container);
        Controls.Add(header);
        Controls.Add(_statusLabel);
        _statusLabel.Dock = DockStyle.Bottom;
        _statusLabel.Height = 34;
        _statusLabel.Padding = new Padding(28, 6, 28, 0);
        _statusLabel.ForeColor = Color.Firebrick;

        UpdateAcceptButton();
    }

    private TabPage BuildSignInPage()
    {
        var page = new TabPage("Sign In")
        {
            Padding = new Padding(22),
            BackColor = Color.White
        };

        _signInButton = CreatePrimaryButton("Sign In", "signInButton");
        _signInButton.Click += async (_, _) => await SignInAsync();
        page.Controls.Add(CreateFormLayout(
            ("Username", _loginUsername),
            ("Password", _loginPassword),
            ("", _signInButton)));
        return page;
    }

    private TabPage BuildRegistrationPage()
    {
        var page = new TabPage("Register")
        {
            Padding = new Padding(22),
            BackColor = Color.White
        };

        _registerButton = CreatePrimaryButton("Register", "registerButton");
        _registerButton.Click += async (_, _) => await RegisterAsync();
        page.Controls.Add(CreateFormLayout(
            ("Username", _registerUsername),
            ("Full name", _registerFullName),
            ("Password", _registerPassword),
            ("Confirm password", _registerConfirmation),
            ("", _registerButton)));
        return page;
    }

    private static Button CreatePrimaryButton(string text, string name)
    {
        var button = new Button
        {
            Text = text,
            Name = name,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            BackColor = Color.FromArgb(28, 112, 200),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Padding = new Padding(10, 6, 10, 6),
            MinimumSize = new Size(120, 38)
        };

        button.FlatAppearance.BorderColor = Color.FromArgb(18, 83, 145);
        button.FlatAppearance.BorderSize = 1;
        return button;
    }

    private static TableLayoutPanel CreateFormLayout(params (string Label, Control Control)[] fields)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = fields.Length,
            Padding = new Padding(8),
            BackColor = Color.White
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var row = 0; row < fields.Length; row++)
        {
            var field = fields[row];
            layout.Controls.Add(new Label
            {
                Text = field.Label,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Padding = new Padding(0, 9, 0, 0),
                ForeColor = Color.FromArgb(45, 45, 45),
                Font = new Font("Segoe UI", 11F, FontStyle.Bold)
            }, 0, row);
            field.Control.Dock = DockStyle.Top;
            field.Control.Font = new Font("Segoe UI", 10F);
            field.Control.Margin = new Padding(0, 4, 0, 8);
            layout.Controls.Add(field.Control, 1, row);
        }

        return layout;
    }

    private void UpdateAcceptButton()
    {
        AcceptButton = _tabs?.SelectedTab?.Text == "Register" ? _registerButton : _signInButton;
    }

    private async Task SignInAsync()
    {
        await RunAuthenticationActionAsync(async () =>
        {
            var session = await _services.Authentication.SignInAsync(_loginUsername.Text, _loginPassword.Text);
            Hide();
            using var mainForm = new MainForm(_configuration, _services, session);
            mainForm.ShowDialog(this);
            if (mainForm.ExitApplicationRequested)
            {
                Close();
                return;
            }

            _loginPassword.Clear();
            Show();
        });
    }

    private async Task RegisterAsync()
    {
        await RunAuthenticationActionAsync(async () =>
        {
            await _services.Authentication.RegisterAsync(
                _registerUsername.Text,
                _registerFullName.Text,
                _registerPassword.Text,
                _registerConfirmation.Text);
            _statusLabel.ForeColor = Color.DarkGreen;
            _statusLabel.Text = "Registration successful. You can now sign in.";
            _registerPassword.Clear();
            _registerConfirmation.Clear();
        });
    }

    private async Task RunAuthenticationActionAsync(Func<Task> action)
    {
        try
        {
            _statusLabel.ForeColor = Color.DimGray;
            _statusLabel.Text = "Working...";
            await action();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or UnauthorizedAccessException)
        {
            _statusLabel.ForeColor = Color.Firebrick;
            _statusLabel.Text = UserMessageFormatter.From(exception);
        }
        catch (Exception exception)
        {
            _statusLabel.ForeColor = Color.Firebrick;
            _statusLabel.Text = UserMessageFormatter.From(exception);
        }
    }
}
