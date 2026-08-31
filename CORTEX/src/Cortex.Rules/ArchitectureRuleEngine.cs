using System.Text.RegularExpressions;
using Cortex.Core.Abstractions;
using Cortex.Core.Models;

namespace Cortex.Rules;

/// <summary>
/// Evaluates user-authored architecture rules — glob-style patterns like
/// "MyApp.UI.*" must-not-depend-on "MyApp.Infrastructure.*" — against every
/// DEPENDS_ON/REFERENCES/CALLS edge currently in the graph. Rules are simple by
/// design: two glob patterns, a severity, and a name; that is enough to express
/// layering, module-isolation and "Core has no Web dependency" constraints.
/// </summary>
public sealed class ArchitectureRuleEngine : IRuleEngine
{
    private readonly ICodeGraphEngine _graph;
    public ArchitectureRuleEngine(ICodeGraphEngine graph) => _graph = graph;

    public IReadOnlyList<RuleViolation> Evaluate(string repositoryId, IReadOnlyList<ArchitectureRule> rules)
    {
        var violations = new List<RuleViolation>();
        var nodesById = _graph.GetAllNodes(repositoryId).ToDictionary(n => n.Id);

        foreach (var rule in rules.Where(r => r.Enabled))
        {
            var sourceRegex = GlobToRegex(rule.SourcePattern);
            var targetRegex = GlobToRegex(rule.ForbiddenTargetPattern);

            foreach (var node in nodesById.Values.Where(n => sourceRegex.IsMatch(n.FullyQualifiedName)))
            {
                foreach (var edge in _graph.GetOutgoingEdges(node.Id))
                {
                    if (edge.Kind is not (EdgeType.DependsOn or EdgeType.References or EdgeType.Calls or EdgeType.Uses)) continue;
                    if (!nodesById.TryGetValue(edge.TargetNodeId, out var target)) continue;
                    if (!targetRegex.IsMatch(target.FullyQualifiedName)) continue;

                    violations.Add(new RuleViolation(rule, edge,
                        $"'{node.FullyQualifiedName}' matches rule source '{rule.SourcePattern}' but {edge.Kind} " +
                        $"'{target.FullyQualifiedName}', which matches the forbidden pattern '{rule.ForbiddenTargetPattern}'."));
                }
            }
        }

        return violations;
    }

    private static Regex GlobToRegex(string glob)
    {
        var escaped = Regex.Escape(glob).Replace(@"\*", ".*").Replace(@"\?", ".");
        return new Regex($"^{escaped}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }
}
