using Microsoft.UI.Xaml.Controls;

namespace Cortex.UI.Services;

/// <summary>Thin wrapper around the Shell's content Frame, used by the Command Palette and NavigationView alike.</summary>
public sealed class NavigationService
{
    private Frame? _frame;
    public void Initialize(Frame frame) => _frame = frame;

    public bool NavigateTo(Type pageType, object? parameter = null) =>
        _frame?.Navigate(pageType, parameter) ?? false;

    public bool GoBack()
    {
        if (_frame is { CanGoBack: true }) { _frame.GoBack(); return true; }
        return false;
    }
}
