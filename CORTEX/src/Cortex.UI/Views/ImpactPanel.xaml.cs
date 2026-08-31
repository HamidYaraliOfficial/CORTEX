using Cortex.Core.Abstractions;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace Cortex.UI.Views;

/// <summary>Renders one <see cref="ImpactReport"/> — the output of the Change Impact Analyzer / Simulation Engine.</summary>
public sealed partial class ImpactPanel : Page
{
    public ImpactPanel()
    {
        InitializeComponent();
    }

    public void ShowReport(ImpactReport report)
    {
        SummaryText.Text = report.Summary;
        ImpactLevelText.Text = report.Level.ToString();
        ImpactLevelBadge.Background = new SolidColorBrush(report.Level switch
        {
            ImpactLevel.Low => Colors.SeaGreen,
            ImpactLevel.Medium => Colors.Goldenrod,
            ImpactLevel.High => Colors.OrangeRed,
            _ => Colors.Crimson
        });

        DirectList.ItemsSource = report.DirectlyImpacted.Select(i => $"{i.RelationToRoot}  →  {i.DisplayName}").ToList();
        IndirectList.ItemsSource = report.IndirectlyImpacted.Select(i => $"depth {i.Depth}: {i.DisplayName}").ToList();
        TestsList.ItemsSource = report.AffectedTests.Select(i => i.DisplayName).ToList();
    }
}
