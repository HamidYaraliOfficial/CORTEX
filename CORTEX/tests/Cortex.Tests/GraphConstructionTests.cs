using Cortex.Core.Models;
using Cortex.Graph;
using Xunit;

namespace Cortex.Tests;

public class GraphConstructionTests
{
    [Fact]
    public void AddNode_ThenGetNode_ReturnsSameNode()
    {
        var graph = new CodeGraph();
        var node = TestGraphFactory.Node(NodeType.Class, "App.Foo");
        graph.AddNode(node);

        Assert.Equal(node.Id, graph.GetNode(node.Id)?.Id);
    }

    [Fact]
    public void AddEdge_ExposesBothOutgoingAndIncoming()
    {
        var graph = new CodeGraph();
        var a = TestGraphFactory.Node(NodeType.Class, "App.A");
        var b = TestGraphFactory.Node(NodeType.Class, "App.B");
        graph.AddNode(a); graph.AddNode(b);
        graph.AddEdge(TestGraphFactory.Edge(a, EdgeType.Calls, b));

        Assert.Single(graph.GetOutgoingEdges(a.Id));
        Assert.Single(graph.GetIncomingEdges(b.Id));
    }

    [Fact]
    public void RemoveNodesFromFile_DropsNodeAndDanglingEdges()
    {
        var graph = new CodeGraph();
        var a = TestGraphFactory.Node(NodeType.Class, "App.A") with { };
        var located = new GraphNode
        {
            Id = a.Id, RepositoryId = a.RepositoryId, Kind = a.Kind, DisplayName = a.DisplayName,
            FullyQualifiedName = a.FullyQualifiedName,
            Location = new SourceLocation(TestGraphFactory.Repo, "A.cs", 1, 1, 1, 1)
        };
        var b = TestGraphFactory.Node(NodeType.Class, "App.B");
        graph.AddNode(located); graph.AddNode(b);
        graph.AddEdge(TestGraphFactory.Edge(located, EdgeType.Calls, b));

        graph.RemoveNodesFromFile(TestGraphFactory.Repo, "A.cs");

        Assert.Null(graph.GetNode(located.Id));
        Assert.Empty(graph.GetIncomingEdges(b.Id));
    }

    [Fact]
    public void FindByName_IsCaseInsensitiveSubstringMatch()
    {
        var graph = new CodeGraph();
        graph.AddNode(TestGraphFactory.Node(NodeType.Class, "App.Services.OrderService"));

        var results = graph.FindByName(TestGraphFactory.Repo, "orderservice");
        Assert.Single(results);
    }
}
