using Cortex.Core.Abstractions;

namespace Cortex.Impact;

/// <summary>
/// Wraps <see cref="ImpactSimulationEngine"/> with the Rule Engine to produce a single,
/// human-readable "should I do this refactor?" preview: breaking-change risk, which
/// architecture rules would newly apply to the moved/changed code, and which tests are
/// the safety net (or the gap) for the change.
/// </summary>
public sealed class RefactoringImpactPreview
{
    private readonly ImpactSimulationEngine _simulation;
    private readonly IRuleEngine _ruleEngine;

    public RefactoringImpactPreview(ImpactSimulationEngine simulation, IRuleEngine ruleEngine)
    {
        _simulation = simulation;
        _ruleEngine = ruleEngine;
    }

    public sealed record PreviewReport(
        ImpactSimulationEngine.SimulationResult Simulation,
        IReadOnlyList<RuleViolation> NewRuleViolations,
        bool HasCompilationRisk,
        bool HasTestSafetyNet);

    public PreviewReport Build(string repositoryId, ImpactSimulationEngine.SimulatedChange change, IReadOnlyList<ArchitectureRule> activeRules)
    {
        var simulation = _simulation.Simulate(repositoryId, change);
        var violations = _ruleEngine.Evaluate(repositoryId, activeRules)
            .Where(v => v.OffendingEdge.SourceNodeId == change.TargetNodeId || v.OffendingEdge.TargetNodeId == change.TargetNodeId)
            .ToList();

        var compilationRisk = change.Kind is ImpactSimulationEngine.SimulatedChangeKind.RemoveClass
            or ImpactSimulationEngine.SimulatedChangeKind.ChangeInterfaceSignature
            or ImpactSimulationEngine.SimulatedChangeKind.RenameMethod
            && simulation.ProjectedImpact.DirectlyImpacted.Count > 0;

        return new PreviewReport(simulation, violations, compilationRisk, simulation.ProjectedImpact.AffectedTests.Count > 0);
    }
}
