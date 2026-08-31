using Cortex.Infrastructure;
using Cortex.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace Cortex.UI;

/// <summary>
/// Application entry point. Builds the DI container (Cortex.Infrastructure.AddCortexCore),
/// applies the persisted theme + language before the first window is shown, and hosts
/// the single <see cref="MainWindow"/> (CORTEX is a single-window, multi-pane "mission
/// control" app — no secondary top-level windows).
/// </summary>
public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static Window? MainAppWindow { get; private set; }

    public App()
    {
        InitializeComponent();

        var dataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CORTEX");

        var services = new ServiceCollection();
        services.AddCortexCore(dataDirectory);
        services.AddSingleton<ThemeService>();
        services.AddSingleton<LocalizationService>();
        services.AddSingleton<NavigationService>();
        Services = services.BuildServiceProvider();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        var localization = Services.GetRequiredService<LocalizationService>();
        localization.ApplyPersistedLanguage();

        MainAppWindow = new MainWindow();

        var theme = Services.GetRequiredService<ThemeService>();
        theme.ApplyPersistedTheme(MainAppWindow);

        MainAppWindow.Activate();
    }
}
