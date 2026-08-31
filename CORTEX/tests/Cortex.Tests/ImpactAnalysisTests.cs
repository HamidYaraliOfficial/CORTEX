using Cortex.Core.Abstractions;
using Cortex.Core.Models;
using Cortex.Graph;
using Cortex.Impact;
using Xunit;

namespace Cortex.Tests;

public class ImpactAnalysisTests
{
    [Fact]
    public void DirectCaller_IsReportedAsDirectlyImpacted()
    {
        var graph = new CodeGraph();
        var target = TestGraphFactory.Node(NodeType.Method, "App.Service.DoWork");
        var caller = TestGraphFactory.Node(NodeType.Method, "App.Controller.Handle");
        graph.AddNode(target); graph.AddNode(caller);
        graph.AddEdge(TestGraphFactory.Edge(caller, EdgeType.Calls, target));

        var analyzer = new ChangeImpactAnalyzer(graph);
        var report = analyzer.AnalyzeNodeImpact(TestGraphFactory.Repo, target.Id, new ImpactOptions());

        Assert.Contains(report.DirectlyImpacted, i => i.NodeId == caller.Id);
    }

    [Fact]
    public void TransitiveCaller_IsReportedAsIndirectlyImpacted()
    {
        var graph = TestGraphFactory.BuildLinearChain(4, EdgeType.Calls); // 0 -> 1 -> 2 -> 3
        var nodes = graph.GetAllNodes(TestGraphFactory.Repo).OrderBy(n => n.FullyQualifiedName).ToList();
        var analyzer = new ChangeImpactAnalyzer(graph);

        var report = analyzer.AnalyzeNodeImpact(TestGraphFactory.Repo, nodes.Last().Id, new ImpactOptions(MaxDepth: 5));

        Assert.Contains(report.IndirectlyImpacted, i => i.NodeId == nodes.First().Id);
    }

    [Fact]
    public void InterfaceWithManyImplementers_ScoresHigherImpact()
    {
        var graph = new CodeGraph();
        var iface = TestGraphFactory.Node(NodeType.Interface, "App.IRepository");
        graph.AddNode(iface);
        for (var i = 0; i < 6; i++)
        {
            var impl = TestGraphFactory.Node(NodeType.Class, $"App.Repository{i}");
            graph.AddNode(impl);
            graph.AddEdge(TestGraphFactory.Edge(impl, EdgeType.Implements, iface));
        }

        var analyzer = new ChangeImpactAnalyzer(graph);
        var report = analyzer.AnalyzeNodeImpact(TestGraphFactory.Repo, iface.Id, new ImpactOptions());

        Assert.True(report.Level is ImpactLevel.Medium or ImpactLevel.High or ImpactLevel.Critical);
    }

    [Fact]
    public void NoConsumers_ScoresLowImpact()
    {
        var graph = new CodeGraph();
        var isolated = TestGraphFactory.Node(NodeType.Class, "App.Unused");
        graph.AddNode(isolated);

        var analyzer = new ChangeImpactAnalyzer(graph);
        var report = analyzer.AnalyzeNodeImpact(TestGraphFactory.Repo, isolated.Id, new ImpactOptions());

        Assert.Equal(ImpactLevel.Low, report.Level);
    }
}
