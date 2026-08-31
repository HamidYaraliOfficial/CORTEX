using Cortex.UI.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace Cortex.UI.Views;

/// <summary>
/// Hosts the five docked regions of the Mission Control layout and owns the global
/// keyboard shortcuts: Ctrl+K command palette, Ctrl+P quick-open, Ctrl+Shift+F global
/// search, Ctrl+Shift+G focus the graph, F12 / Shift+F12 go-to-definition / find-references.
/// Panels are hosted in individually navigable Frames so each can later be popped out,
/// collapsed or reordered without touching this page.
/// </summary>
public sealed partial class ShellPage : Page
{
    public ShellPage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            RepositoryExplorerFrame.Navigate(typeof(RepositoryExplorerView));
            ArchitectureCanvasFrame.Navigate(typeof(ArchitectureCanvasView));
            InspectorFrame.Navigate(typeof(InspectorPanel));
            ImpactPanelFrame.Navigate(typeof(ImpactPanel));
        };

        KeyboardAccelerators.Add(MakeAccelerator(VirtualKey.K, VirtualKeyModifiers.Control, (_, _) => ToggleCommandPalette()));
        KeyboardAccelerators.Add(MakeAccelerator(VirtualKey.P, VirtualKeyModifiers.Control, (_, _) => ToggleCommandPalette()));
        KeyboardAccelerators.Add(MakeAccelerator(VirtualKey.F, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, (_, _) => FocusGlobalSearch()));
        KeyboardAccelerators.Add(MakeAccelerator(VirtualKey.G, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, (_, _) => FocusArchitectureCanvas()));
    }

    private static KeyboardAccelerator MakeAccelerator(VirtualKey key, VirtualKeyModifiers modifiers, TypedEventHandler<KeyboardAccelerator, KeyboardAcceleratorInvokedEventArgs> handler)
    {
        var accelerator = new KeyboardAccelerator { Key = key, Modifiers = modifiers };
        accelerator.Invoked += handler;
        return accelerator;
    }

    private void OnCommandPaletteRequested(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => ToggleCommandPalette();

    private void OnSettingsRequested(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var navigation = App.Services.GetService(typeof(NavigationService)) as NavigationService;
        navigation?.NavigateTo(typeof(SettingsPage));
    }

    private void ToggleCommandPalette()
    {
        var showing = CommandPaletteHost.Visibility == Microsoft.UI.Xaml.Visibility.Visible;
        if (showing)
        {
            CommandPaletteHost.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
        }
        else
        {
            CommandPaletteHost.Navigate(typeof(CommandPalette));
            CommandPaletteHost.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
        }
    }

    private void FocusGlobalSearch() => ToggleCommandPalette();
    private void FocusArchitectureCanvas() => ArchitectureCanvasFrame.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
}
