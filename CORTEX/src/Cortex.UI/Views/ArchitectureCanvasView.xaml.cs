using Cortex.Core.Abstractions;
using Cortex.Core.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;

namespace Cortex.UI.Views;

/// <summary>
/// The Interactive Architecture Map. Renders <see cref="GraphNode"/>/<see cref="GraphEdge"/>
/// data as shapes on a <see cref="Canvas"/> hosted inside a zoomable/pannable
/// <see cref="ScrollViewer"/> (native WinUI pinch-zoom + mouse-wheel zoom). For large
/// graphs, call <see cref="RenderNeighborhood"/> (Focus Mode) instead of <see cref="RenderFull"/>
/// so only a bounded node/edge count is ever laid out and drawn at once — that bound is
/// what keeps panning smooth on graphs with tens of thousands of symbols.
/// </summary>
public sealed partial class ArchitectureCanvasView : Page
{
    private const double NodeDiameter = 64;
    private const double GridSpacing = 140;

    public ArchitectureCanvasView()
    {
        InitializeComponent();
    }

    /// <summary>Simple grid layout for now — swap in a force-directed or hierarchical layout engine as the graph grows.</summary>
    public void RenderFull(IReadOnlyList<GraphNode> nodes, IReadOnlyList<GraphEdge> edges)
    {
        GraphCanvas.Children.Clear();
        var positions = LayoutGrid(nodes);
        DrawEdges(edges, positions);
        DrawNodes(nodes, positions);
    }

    /// <summary>Focus Mode: render only a node's local neighborhood so the canvas stays legible.</summary>
    public void RenderNeighborhood(IReadOnlyList<GraphNode> nodes, IReadOnlyList<GraphEdge> edges, string centerNodeId) =>
        RenderFull(nodes, edges); // same renderer, just called with the smaller neighborhood set from GraphTraversal.Neighborhood

    private static Dictionary<string, Point> LayoutGrid(IReadOnlyList<GraphNode> nodes)
    {
        var columns = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(nodes.Count)));
        var positions = new Dictionary<string, Point>();
        for (var i = 0; i < nodes.Count; i++)
        {
            var col = i % columns;
            var row = i / columns;
            positions[nodes[i].Id] = new Point(60 + col * GridSpacing, 60 + row * GridSpacing);
        }
        return positions;
    }

    private void DrawNodes(IReadOnlyList<GraphNode> nodes, Dictionary<string, Point> positions)
    {
        foreach (var node in nodes)
        {
            if (!positions.TryGetValue(node.Id, out var point)) continue;

            var shape = new Ellipse
            {
                Width = NodeDiameter,
                Height = NodeDiameter,
                Fill = (Brush)Application_Resource("CortexNodeFillBrush"),
                Stroke = (Brush)Application_Resource("CortexEdgeStrokeBrush"),
                StrokeThickness = 1.5,
                Tag = node.Id
            };
            ToolTipService.SetToolTip(shape, $"{node.Kind}: {node.FullyQualifiedName}");
            shape.PointerPressed += (_, _) => NodeSelected?.Invoke(node);

            var label = new TextBlock
            {
                Text = Truncate(node.DisplayName, 14),
                FontSize = 11,
                TextAlignment = Microsoft.UI.Xaml.TextAlignment.Center,
                Width = NodeDiameter + 24
            };

            Canvas.SetLeft(shape, point.X);
            Canvas.SetTop(shape, point.Y);
            Canvas.SetLeft(label, point.X - 12);
            Canvas.SetTop(label, point.Y + NodeDiameter + 2);

            GraphCanvas.Children.Add(shape);
            GraphCanvas.Children.Add(label);
        }
    }

    private void DrawEdges(IReadOnlyList<GraphEdge> edges, Dictionary<string, Point> positions)
    {
        foreach (var edge in edges)
        {
            if (!positions.TryGetValue(edge.SourceNodeId, out var from) || !positions.TryGetValue(edge.TargetNodeId, out var to)) continue;

            var line = new Line
            {
                X1 = from.X + NodeDiameter / 2, Y1 = from.Y + NodeDiameter / 2,
                X2 = to.X + NodeDiameter / 2, Y2 = to.Y + NodeDiameter / 2,
                Stroke = (Brush)Application_Resource("CortexEdgeStrokeBrush"),
                StrokeThickness = edge.Kind == EdgeType.Inherits ? 2 : 1,
                StrokeDashArray = edge.Kind == EdgeType.Uses ? new Microsoft.UI.Xaml.Media.DoubleCollection { 4, 2 } : null,
                Opacity = 0.7
            };
            GraphCanvas.Children.Insert(0, line); // edges behind nodes
        }
    }

    private static object Application_Resource(string key) =>
        Microsoft.UI.Xaml.Application.Current.Resources.TryGetValue(key, out var value) ? value : new SolidColorBrush(Colors.Gray);

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";

    public event Action<GraphNode>? NodeSelected;

    private void OnZoomIn(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) =>
        CanvasScrollViewer.ChangeView(null, null, Math.Min(4, CanvasScrollViewer.ZoomFactor * 1.2f));

    private void OnZoomOut(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) =>
        CanvasScrollViewer.ChangeView(null, null, Math.Max(0.1f, CanvasScrollViewer.ZoomFactor / 1.2f));

    private void OnFitToScreen(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) =>
        CanvasScrollViewer.ChangeView(0, 0, 1);

    private void OnToggleFocusMode(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // Application layer listens for this and swaps RenderFull(...) for RenderNeighborhood(...).
    }
}
