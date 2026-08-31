using Cortex.Core.Abstractions;
using Cortex.Core.Models;

namespace Cortex.Graph;

/// <summary>
/// Finds circular dependencies (strongly connected components with more than one node,
/// or a node with a self-loop) using Tarjan's algorithm, restricted to a chosen edge type
/// — typically <see cref="EdgeType.DependsOn"/> for project/module cycles, or
/// <see cref="EdgeType.Calls"/> for recursive call cycles.
/// </summary>
public sealed class CircularDependencyDetector
{
    private readonly ICodeGraphEngine _graph;
    public CircularDependencyDetector(ICodeGraphEngine graph) => _graph = graph;

    public sealed record Cycle(IReadOnlyList<GraphNode> Nodes, IReadOnlyList<GraphEdge> ClosingEdges);

    public IReadOnlyList<Cycle> FindCycles(string repositoryId, EdgeType edgeType)
    {
        var index = new Dictionary<string, int>();
        var lowLink = new Dictionary<string, int>();
        var onStack = new HashSet<string>();
        var stack = new Stack<string>();
        var cycles = new List<Cycle>();
        int counter = 0;

        var allNodeIds = _graph.GetAllNodes(repositoryId).Select(n => n.Id).ToList();

        void StrongConnect(string v)
        {
            index[v] = counter;
            lowLink[v] = counter;
            counter++;
            stack.Push(v);
            onStack.Add(v);

            foreach (var edge in _graph.GetOutgoingEdges(v, edgeType))
            {
                var w = edge.TargetNodeId;
                if (!index.ContainsKey(w))
                {
                    StrongConnect(w);
                    lowLink[v] = Math.Min(lowLink[v], lowLink[w]);
                }
                else if (onStack.Contains(w))
                {
                    lowLink[v] = Math.Min(lowLink[v], index[w]);
                }
            }

            if (lowLink[v] == index[v])
            {
                var component = new List<string>();
                string w;
                do
                {
                    w = stack.Pop();
                    onStack.Remove(w);
                    component.Add(w);
                } while (w != v);

                var isRealCycle = component.Count > 1 ||
                    _graph.GetOutgoingEdges(component[0], edgeType).Any(e => e.TargetNodeId == component[0]);

                if (isRealCycle)
                {
                    var nodes = component.Select(_graph.GetNode).Where(n => n is not null).Select(n => n!).ToList();
                    var closingEdges = component
                        .SelectMany(id => _graph.GetOutgoingEdges(id, edgeType))
                        .Where(e => component.Contains(e.TargetNodeId))
                        .ToList();
                    cycles.Add(new Cycle(nodes, closingEdges));
                }
            }
        }

        foreach (var nodeId in allNodeIds)
        {
            if (!index.ContainsKey(nodeId)) StrongConnect(nodeId);
        }

        return cycles;
    }
}
