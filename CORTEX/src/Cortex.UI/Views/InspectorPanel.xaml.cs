using Cortex.Core.Abstractions;
using Cortex.Core.Models;
using Microsoft.UI.Xaml.Controls;

namespace Cortex.UI.Views;

/// <summary>
/// Right-hand pane: everything CORTEX knows about the currently selected symbol —
/// its metrics, who consumes it, and which tests exercise it. Populated whenever the
/// Architecture Canvas raises <see cref="ArchitectureCanvasView.NodeSelected"/> or the
/// Source Viewer's "Go To Definition" resolves onto a new symbol.
/// </summary>
public sealed partial class InspectorPanel : Page
{
    public InspectorPanel()
    {
        InitializeComponent();
    }

    public void ShowNode(GraphNode node, ModuleMetrics? metrics, IReadOnlyList<ImpactedItem> consumers, IReadOnlyList<ImpactedItem> tests)
    {
        SymbolNameText.Text = node.DisplayName;
        SymbolKindText.Text = node.Kind.ToString();
        SymbolLocationText.Text = node.Location?.ToString() ?? "";

        MetricsPanel.Children.Clear();
        if (metrics is not null)
        {
            AddMetricLine("LOC", metrics.LinesOfCode.ToString());
            AddMetricLine("Cyclomatic Complexity", metrics.CyclomaticComplexity.ToString());
            AddMetricLine("Fan-In / Fan-Out", $"{metrics.FanIn} / {metrics.FanOut}");
        }

        ConsumersList.ItemsSource = consumers.Select(c => $"{c.RelationToRoot}  →  {c.DisplayName}").ToList();
        TestsList.ItemsSource = tests.Select(t => t.DisplayName).ToList();
    }

    private void AddMetricLine(string label, string value) =>
        MetricsPanel.Children.Add(new TextBlock { Text = $"{label}: {value}" });
}
