using Cortex.UI.Services;
using Cortex.UI.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using Microsoft.UI;

namespace Cortex.UI;

/// <summary>
/// The single top-level window. CORTEX is intentionally a one-window "Mission Control"
/// app — every panel (Repository Explorer, Architecture Canvas, Inspector, bottom
/// Code/Timeline/Console/Impact tabs) lives inside <see cref="ShellPage"/>, not in
/// separate OS windows, so the layout state (dock/float/collapse) is one coherent whole.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Title = "CORTEX — Intelligent Codebase Observatory";

        var appWindow = AppWindow;
        appWindow.Resize(new Windows.Graphics.SizeInt32(1600, 1000));
        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = true;
            presenter.IsResizable = true;
        }

        var navigation = App.Services.GetService(typeof(NavigationService)) as NavigationService;
        navigation?.Initialize(RootFrame);
        RootFrame.Navigate(typeof(ShellPage));
    }
}
