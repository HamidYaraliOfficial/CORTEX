using Cortex.Core.Abstractions;
using Cortex.Core.Models;

namespace Cortex.Impact;

/// <summary>
/// "What if?" analysis: the user describes a hypothetical change (rename, signature
/// change, removal, DTO property change, API response change, move module) and CORTEX
/// walks the existing graph to predict the blast radius — without touching a single
/// byte of source. Nothing here is applied; see Cortex.Impact.RefactoringImpactPreview
/// and the UI's Safe Apply Layer for the (separate, confirmation-gated) real edit path.
/// </summary>
public sealed class ImpactSimulationEngine
{
    public enum SimulatedChangeKind { RenameMethod, ChangeInterfaceSignature, RemoveClass, ChangeDtoProperty, ChangeApiResponse, MoveModule }

    public sealed record SimulatedChange(SimulatedChangeKind Kind, string TargetNodeId, string? NewName = null, string? Detail = null);
    public sealed record SimulationResult(SimulatedChange Change, ImpactReport ProjectedImpact, IReadOnlyList<string> Warnings);

    private readonly IImpactAnalyzer _impactAnalyzer;
    public ImpactSimulationEngine(IImpactAnalyzer impactAnalyzer) => _impactAnalyzer = impactAnalyzer;

    public SimulationResult Simulate(string repositoryId, SimulatedChange change)
    {
        var options = change.Kind switch
        {
            SimulatedChangeKind.RemoveClass or SimulatedChangeKind.ChangeInterfaceSignature =>
                new ImpactOptions(IncludeIndirect: true, MaxDepth: 8, IncludeTests: true, IncludeConfiguration: true),
            _ => new ImpactOptions(IncludeIndirect: true, MaxDepth: 4, IncludeTests: true, IncludeConfiguration: false)
        };

        var report = _impactAnalyzer.AnalyzeNodeImpact(repositoryId, change.TargetNodeId, options);

        var warnings = new List<string>();
        if (change.Kind is SimulatedChangeKind.RemoveClass && report.DirectlyImpacted.Count > 0)
            warnings.Add("This type still has active consumers — removing it will break compilation for every direct dependent listed below.");
        if (change.Kind is SimulatedChangeKind.ChangeInterfaceSignature or SimulatedChangeKind.ChangeApiResponse && report.AffectedApiContracts.Count > 0)
            warnings.Add("This change touches at least one public API contract — treat it as a breaking change for external consumers.");
        if (report.AffectedTests.Count == 0)
            warnings.Add("No tests currently cover this area — impact here is based purely on static structure.");

        return new SimulationResult(change, report, warnings);
    }
}
