using Cortex.Core.Abstractions;
using Cortex.Core.Models;

namespace Cortex.Rules;

/// <summary>
/// Checks a declared layering order (e.g. UI → Application → Domain → Infrastructure,
/// where each layer may only depend on layers to its right) and reports every
/// DEPENDS_ON edge that points "backwards" against that order.
/// </summary>
public sealed class DependencyDirectionAnalyzer
{
    private readonly ICodeGraphEngine _graph;
    public DependencyDirectionAnalyzer(ICodeGraphEngine graph) => _graph = graph;

    public sealed record LayerViolation(GraphNode Source, GraphNode Target, int SourceLayerIndex, int TargetLayerIndex);

    /// <param name="layersOrderedTopToBottom">e.g. ["UI", "Application", "Domain", "Infrastructure"] — index 0 may depend on anything below it, never the reverse.</param>
    /// <param name="layerMatcher">Given a node's fully qualified name, return which layer name it belongs to, or null if unclassified.</param>
    public IReadOnlyList<LayerViolation> Analyze(string repositoryId, IReadOnlyList<string> layersOrderedTopToBottom, Func<string, string?> layerMatcher)
    {
        var violations = new List<LayerViolation>();
        var nodes = _graph.GetAllNodes(repositoryId).ToList();
        var layerIndex = nodes.ToDictionary(n => n.Id, n =>
        {
            var layer = layerMatcher(n.FullyQualifiedName);
            return layer is null ? -1 : layersOrderedTopToBottom.ToList().IndexOf(layer);
        });

        foreach (var node in nodes)
        {
            var sourceLayer = layerIndex[node.Id];
            if (sourceLayer < 0) continue;

            foreach (var edge in _graph.GetOutgoingEdges(node.Id, EdgeType.DependsOn))
            {
                if (!layerIndex.TryGetValue(edge.TargetNodeId, out var targetLayer) || targetLayer < 0) continue;
                if (targetLayer < sourceLayer) // depends on a layer "above" it → wrong direction
                {
                    var targetNode = _graph.GetNode(edge.TargetNodeId);
                    if (targetNode is not null) violations.Add(new LayerViolation(node, targetNode, sourceLayer, targetLayer));
                }
            }
        }
        return violations;
    }
}
