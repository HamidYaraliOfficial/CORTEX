using Microsoft.UI.Xaml.Controls;

namespace Cortex.UI.Views;

/// <summary>
/// Ctrl+K / Ctrl+P command palette: fuzzy-matches across repositories, symbols, projects,
/// commits, branches, architecture rules, graph views, reports and "ask AI ..." queries,
/// so the keyboard-first workflow never requires reaching for the mouse.
/// </summary>
public sealed partial class CommandPalette : Page
{
    public sealed record PaletteItem(string Category, string Title, string Subtitle, Action Activate);

    private IReadOnlyList<PaletteItem> _allItems = Array.Empty<PaletteItem>();

    public CommandPalette()
    {
        InitializeComponent();
    }

    public void SetSource(IReadOnlyList<PaletteItem> items) => _allItems = items;

    private void OnQueryChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput) return;
        var query = sender.Text.Trim();

        ResultsList.ItemsSource = string.IsNullOrEmpty(query)
            ? _allItems.Take(20).Select(Format).ToList()
            : _allItems
                .Where(i => i.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                            i.Category.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Take(20)
                .Select(Format)
                .ToList();
    }

    private void OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        var match = _allItems.FirstOrDefault(i => i.Title.Equals(args.QueryText, StringComparison.OrdinalIgnoreCase));
        match?.Activate();
    }

    private void OnResultClicked(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not string formatted) return;
        var match = _allItems.FirstOrDefault(i => Format(i) == formatted);
        match?.Activate();
    }

    private static string Format(PaletteItem item) => $"[{item.Category}] {item.Title} — {item.Subtitle}";
}
