using Cortex.Core.Abstractions;
using Cortex.Core.Models;
using Cortex.Graph;
using Cortex.Rules;
using Xunit;

namespace Cortex.Tests;

public class RuleEngineTests
{
    [Fact]
    public void ViolatingEdge_IsReported()
    {
        var graph = new CodeGraph();
        var ui = TestGraphFactory.Node(NodeType.Class, "App.UI.MainView");
        var infra = TestGraphFactory.Node(NodeType.Class, "App.Infrastructure.SqlClient");
        graph.AddNode(ui); graph.AddNode(infra);
        graph.AddEdge(TestGraphFactory.Edge(ui, EdgeType.DependsOn, infra));

        var rule = new ArchitectureRule("r1", "UI must not depend on Infrastructure", "App.UI.*", "App.Infrastructure.*", RuleSeverity.Error);
        var violations = new ArchitectureRuleEngine(graph).Evaluate(TestGraphFactory.Repo, new[] { rule });

        Assert.Single(violations);
        Assert.Equal("r1", violations[0].Rule.Id);
    }

    [Fact]
    public void NonViolatingEdge_ProducesNoResult()
    {
        var graph = new CodeGraph();
        var ui = TestGraphFactory.Node(NodeType.Class, "App.UI.MainView");
        var app = TestGraphFactory.Node(NodeType.Class, "App.Application.OrderHandler");
        graph.AddNode(ui); graph.AddNode(app);
        graph.AddEdge(TestGraphFactory.Edge(ui, EdgeType.DependsOn, app));

        var rule = new ArchitectureRule("r1", "UI must not depend on Infrastructure", "App.UI.*", "App.Infrastructure.*", RuleSeverity.Error);
        var violations = new ArchitectureRuleEngine(graph).Evaluate(TestGraphFactory.Repo, new[] { rule });

        Assert.Empty(violations);
    }

    [Fact]
    public void DisabledRule_IsSkipped()
    {
        var graph = new CodeGraph();
        var ui = TestGraphFactory.Node(NodeType.Class, "App.UI.MainView");
        var infra = TestGraphFactory.Node(NodeType.Class, "App.Infrastructure.SqlClient");
        graph.AddNode(ui); graph.AddNode(infra);
        graph.AddEdge(TestGraphFactory.Edge(ui, EdgeType.DependsOn, infra));

        var rule = new ArchitectureRule("r1", "disabled rule", "App.UI.*", "App.Infrastructure.*", RuleSeverity.Error, Enabled: false);
        var violations = new ArchitectureRuleEngine(graph).Evaluate(TestGraphFactory.Repo, new[] { rule });

        Assert.Empty(violations);
    }
}
