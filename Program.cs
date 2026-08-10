using InventoryManagementSystem.Configuration;
using InventoryManagementSystem.Forms;

namespace InventoryManagementSystem;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var configuration = AppConfig.Load();
        Application.Run(new LoginForm(configuration));
    }
}
