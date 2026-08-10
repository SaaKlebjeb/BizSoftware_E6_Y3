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

    public LoginForm(AppConfig configuration)
    {
        _configuration = configuration;
        _services = new ApplicationServices(configuration);
        Text = _configuration.ApplicationName;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(460, 360);
        ClientSize = new Size(560, 420);
        BuildUi();
    }

    private void BuildUi()
    {
        var title = new Label
        {
            Dock = DockStyle.Top,
            Height = 56,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            Text = _configuration.ApplicationName,
            TextAlign = ContentAlignment.MiddleCenter
        };

        var tabs = new TabControl { Dock = DockStyle.Fill, Name = "authenticationTabs" };
        tabs.TabPages.Add(BuildSignInPage());
        tabs.TabPages.Add(BuildRegistrationPage());

        var container = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24) };
        container.Controls.Add(tabs);
        Controls.Add(container);
        Controls.Add(_statusLabel);
        _statusLabel.Dock = DockStyle.Bottom;
        _statusLabel.Height = 34;
        _statusLabel.Padding = new Padding(24, 6, 24, 0);
    }

    private TabPage BuildSignInPage()
    {
        var page = new TabPage("Sign In") { Padding = new Padding(20) };
        var signInButton = new Button { Text = "Sign In", AutoSize = true, Name = "signInButton" };
        signInButton.Click += async (_, _) => await SignInAsync();
        page.Controls.Add(CreateFormLayout(
            ("Username", _loginUsername),
            ("Password", _loginPassword),
            ("", signInButton)));
        return page;
    }

    private TabPage BuildRegistrationPage()
    {
        var page = new TabPage("Register") { Padding = new Padding(20) };
        var registerButton = new Button { Text = "Register", AutoSize = true, Name = "registerButton" };
        registerButton.Click += async (_, _) => await RegisterAsync();
        page.Controls.Add(CreateFormLayout(
            ("Username", _registerUsername),
            ("Full name", _registerFullName),
            ("Password", _registerPassword),
            ("Confirm password", _registerConfirmation),
            ("", registerButton)));
        return page;
    }

    private static TableLayoutPanel CreateFormLayout(params (string Label, Control Control)[] fields)
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, RowCount = fields.Length, Padding = new Padding(4) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var row = 0; row < fields.Length; row++)
        {
            var field = fields[row];
            layout.Controls.Add(new Label { Text = field.Label, AutoSize = true, Anchor = AnchorStyles.Left, Padding = new Padding(0, 7, 0, 0) }, 0, row);
            field.Control.Dock = DockStyle.Top;
            layout.Controls.Add(field.Control, 1, row);
        }

        return layout;
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
