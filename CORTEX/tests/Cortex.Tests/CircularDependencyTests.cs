using Cortex.Core.Models;
using Cortex.Graph;
using Xunit;

namespace Cortex.Tests;

public class CircularDependencyTests
{
    [Fact]
    public void DetectsSimpleThreeNodeCycle()
    {
        var graph = new CodeGraph();
        var a = TestGraphFactory.Node(NodeType.Project, "ModuleA");
        var b = TestGraphFactory.Node(NodeType.Project, "ModuleB");
        var c = TestGraphFactory.Node(NodeType.Project, "ModuleC");
        graph.AddNode(a); graph.AddNode(b); graph.AddNode(c);
        graph.AddEdge(TestGraphFactory.Edge(a, EdgeType.DependsOn, b));
        graph.AddEdge(TestGraphFactory.Edge(b, EdgeType.DependsOn, c));
        graph.AddEdge(TestGraphFactory.Edge(c, EdgeType.DependsOn, a));

        var cycles = new CircularDependencyDetector(graph).FindCycles(TestGraphFactory.Repo, EdgeType.DependsOn);

        Assert.Single(cycles);
        Assert.Equal(3, cycles[0].Nodes.Count);
    }

    [Fact]
    public void LinearChainHasNoCycles()
    {
        var graph = TestGraphFactory.BuildLinearChain(5, EdgeType.DependsOn);
        var cycles = new CircularDependencyDetector(graph).FindCycles(TestGraphFactory.Repo, EdgeType.DependsOn);
        Assert.Empty(cycles);
    }

    [Fact]
    public void DetectsDirectSelfLoop()
    {
        var graph = new CodeGraph();
        var a = TestGraphFactory.Node(NodeType.Method, "App.Recursive");
        graph.AddNode(a);
        graph.AddEdge(TestGraphFactory.Edge(a, EdgeType.Calls, a));

        var cycles = new CircularDependencyDetector(graph).FindCycles(TestGraphFactory.Repo, EdgeType.Calls);
        Assert.Single(cycles);
    }
}
