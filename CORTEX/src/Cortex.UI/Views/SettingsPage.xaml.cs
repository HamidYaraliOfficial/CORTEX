using Cortex.Core.Abstractions;
using Cortex.Core.Models;
using Cortex.Infrastructure;
using Cortex.UI.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace Cortex.UI.Views;

/// <summary>
/// Settings: Appearance (theme + language), AI provider configuration (Cloud AI is
/// opt-in and its key goes straight into <see cref="ICredentialStore"/> / DPAPI, never
/// held anywhere else), and the "working hours" background-indexing scheduler — the user
/// enters a daily window, an interval and active days, and this page continuously shows
/// exactly when the next scheduled run is and how long remains until then.
/// </summary>
public sealed partial class SettingsPage : Page
{
    private readonly ThemeService _themeService;
    private readonly LocalizationService _localizationService;
    private readonly ICredentialStore _credentials;
    private readonly WorkingHoursScheduleService _scheduleService;
    private readonly IndexingScheduleSettings _scheduleSettings = new();
    private readonly Microsoft.UI.Xaml.DispatcherTimer _clockTimer = new() { Interval = TimeSpan.FromSeconds(1) };

    public SettingsPage()
    {
        InitializeComponent();

        _themeService = App.Services.GetRequiredService<ThemeService>();
        _localizationService = App.Services.GetRequiredService<LocalizationService>();
        _credentials = App.Services.GetRequiredService<ICredentialStore>();
        _scheduleService = App.Services.GetRequiredService<WorkingHoursScheduleService>();

        ThemeComboBox.SelectedIndex = (int)_themeService.CurrentMode;
        LanguageComboBox.SelectedIndex = (int)_localizationService.CurrentLanguage;

        WindowStartPicker.Time = _scheduleSettings.WindowStart;
        WindowEndPicker.Time = _scheduleSettings.WindowEnd;
        IntervalMinutesBox.Value = _scheduleSettings.IntervalMinutes;

        _clockTimer.Tick += (_, _) => RefreshNextRunDisplay();
        _clockTimer.Start();
        RefreshNextRunDisplay();
    }

    private void OnThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeComboBox.SelectedItem is not ComboBoxItem item || App.MainAppWindow is null) return;
        var mode = Enum.Parse<CortexThemeMode>((string)item.Tag);
        _themeService.Apply(App.MainAppWindow, mode);
    }

    private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageComboBox.SelectedItem is not ComboBoxItem item) return;
        var language = Enum.Parse<CortexLanguage>((string)item.Tag);
        _localizationService.Apply(language);
        // A language switch changes the reading direction (Persian ⇄ RTL) and every
        // .resw-bound string; CORTEX asks for a restart rather than re-flowing live XAML.
    }

    private void OnCloudAiToggled(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        CloudApiKeyBox.IsEnabled = CloudAiToggle.IsOn;
    }

    private void OnSaveApiKey(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(CloudApiKeyBox.Password))
        {
            _credentials.Save("cloud-ai-api-key", CloudApiKeyBox.Password);
            CloudApiKeyBox.Password = string.Empty;
        }
    }

    private void OnScheduleSettingChanged(object sender, object e)
    {
        _scheduleSettings.Enabled = ScheduleEnabledToggle.IsOn;
        _scheduleSettings.WindowStart = WindowStartPicker.Time;
        _scheduleSettings.WindowEnd = WindowEndPicker.Time;
        _scheduleSettings.IntervalMinutes = (int)IntervalMinutesBox.Value;

        _scheduleSettings.ActiveDays.Clear();
        foreach (var child in ActiveDaysPanel.Children.OfType<CheckBox>())
        {
            if (child.IsChecked == true) _scheduleSettings.ActiveDays.Add(Enum.Parse<DayOfWeek>((string)child.Tag));
        }

        RefreshNextRunDisplay();
    }

    private void RefreshNextRunDisplay()
    {
        var status = _scheduleService.GetStatus(_scheduleSettings, DateTimeOffset.Now);
        NextRunInfoBar.Title = status.IsInsideWindowNow ? "Inside working hours" : "Outside working hours";
        NextRunInfoBar.Message =
            $"Next scheduled indexing run: {status.NextRunAtLocal:g}  ·  in {Format(status.TimeUntilNextRun)}";
    }

    private static string Format(TimeSpan t) =>
        t.TotalHours >= 1 ? $"{(int)t.TotalHours}h {t.Minutes}m" : $"{t.Minutes}m {t.Seconds}s";
}
