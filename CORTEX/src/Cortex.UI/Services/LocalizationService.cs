using Microsoft.UI.Xaml;
using Windows.Globalization;
using Windows.Storage;

namespace Cortex.UI.Services;

public enum CortexLanguage { English, Persian, Chinese }

/// <summary>
/// Switches the app's display language between English, Persian (فارسی) and Chinese
/// (中文) and sets the correct <see cref="FlowDirection"/> per language — Persian is
/// full right-to-left (mirrored NavigationView, mirrored icons, right-aligned text),
/// English and Chinese are left-to-right. All user-facing strings come from the .resw
/// resource files under Strings/&lt;lang-tag&gt;/Resources.resw — there is no
/// hardcoded, untranslatable UI text anywhere in Cortex.UI.
/// </summary>
public sealed class LocalizationService
{
    private const string SettingKey = "Cortex.Language";

    public CortexLanguage CurrentLanguage { get; private set; } = CortexLanguage.English;

    private static readonly Dictionary<CortexLanguage, string> LanguageTags = new()
    {
        [CortexLanguage.English] = "en-US",
        [CortexLanguage.Persian] = "fa-IR",
        [CortexLanguage.Chinese] = "zh-CN"
    };

    public void ApplyPersistedLanguage()
    {
        var stored = ApplicationData.Current.LocalSettings.Values[SettingKey] as string;
        var language = Enum.TryParse<CortexLanguage>(stored, out var parsed) ? parsed : CortexLanguage.English;
        Apply(language, persist: false);
    }

    public void Apply(CortexLanguage language, bool persist = true)
    {
        CurrentLanguage = language;
        var tag = LanguageTags[language];

        ApplicationLanguages.PrimaryLanguageOverride = tag;

        if (persist) ApplicationData.Current.LocalSettings.Values[SettingKey] = language.ToString();
    }

    public FlowDirection FlowDirectionFor(CortexLanguage language) =>
        language == CortexLanguage.Persian ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    public FlowDirection CurrentFlowDirection => FlowDirectionFor(CurrentLanguage);
}
