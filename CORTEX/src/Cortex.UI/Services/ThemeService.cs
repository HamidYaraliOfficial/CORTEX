using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Storage;

namespace Cortex.UI.Services;

public enum CortexThemeMode { WindowsDefault, Light, Dark, RedAccent, BlueAccent }

/// <summary>
/// Applies one of five theme modes — Windows Default (follows OS light/dark + accent),
/// forced Light, forced Dark, or the two custom CORTEX accents (Red/Blue) — and layers
/// a Mica backdrop on top for the native Windows 11 feel. Persisted to local settings so
/// the choice survives an app restart.
/// </summary>
public sealed class ThemeService
{
    private const string SettingKey = "Cortex.ThemeMode";

    public CortexThemeMode CurrentMode { get; private set; } = CortexThemeMode.WindowsDefault;

    public void ApplyPersistedTheme(Window window)
    {
        var stored = ApplicationData.Current.LocalSettings.Values[SettingKey] as string;
        var mode = Enum.TryParse<CortexThemeMode>(stored, out var parsed) ? parsed : CortexThemeMode.WindowsDefault;
        Apply(window, mode);
    }

    public void Apply(Window window, CortexThemeMode mode)
    {
        CurrentMode = mode;
        ApplicationData.Current.LocalSettings.Values[SettingKey] = mode.ToString();

        var root = (FrameworkElement)window.Content;
        var mergedDictionaries = Application.Current.Resources.MergedDictionaries;
        mergedDictionaries.Clear();

        var themeFile = mode switch
        {
            CortexThemeMode.Light => "Themes/LightTheme.xaml",
            CortexThemeMode.Dark => "Themes/DarkTheme.xaml",
            CortexThemeMode.RedAccent => "Themes/RedAccentTheme.xaml",
            CortexThemeMode.BlueAccent => "Themes/BlueAccentTheme.xaml",
            _ => "Themes/WindowsDefaultTheme.xaml"
        };
        mergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"ms-appx:///{themeFile}") });

        root.RequestedTheme = mode switch
        {
            CortexThemeMode.Light or CortexThemeMode.RedAccent => ElementTheme.Light,
            CortexThemeMode.Dark or CortexThemeMode.BlueAccent => ElementTheme.Dark,
            _ => ElementTheme.Default // follow OS
        };

        // Native Windows 11 Mica backdrop for the whole window chrome.
        window.SystemBackdrop = new MicaBackdrop { Kind = MicaKind.Base };
    }
}
