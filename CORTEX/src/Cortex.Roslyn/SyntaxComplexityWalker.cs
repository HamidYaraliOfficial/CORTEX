using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cortex.Roslyn;

/// <summary>
/// Computes McCabe cyclomatic complexity for a single method body by counting decision
/// points (if/else-if, loops, case labels, catch clauses, logical &amp;&amp;/||, ternary,
/// pattern-match arms, null-coalescing). Feeds Cortex.Metrics.ComplexityMetricsCalculator.
/// </summary>
public sealed class SyntaxComplexityWalker : CSharpSyntaxWalker
{
    public int Complexity { get; private set; } = 1; // base path

    public override void VisitIfStatement(IfStatementSyntax node) { Complexity++; base.VisitIfStatement(node); }
    public override void VisitForStatement(ForStatementSyntax node) { Complexity++; base.VisitForStatement(node); }
    public override void VisitForEachStatement(ForEachStatementSyntax node) { Complexity++; base.VisitForEachStatement(node); }
    public override void VisitWhileStatement(WhileStatementSyntax node) { Complexity++; base.VisitWhileStatement(node); }
    public override void VisitDoStatement(DoStatementSyntax node) { Complexity++; base.VisitDoStatement(node); }
    public override void VisitCatchClause(CatchClauseSyntax node) { Complexity++; base.VisitCatchClause(node); }
    public override void VisitCaseSwitchLabel(CaseSwitchLabelSyntax node) { Complexity++; base.VisitCaseSwitchLabel(node); }
    public override void VisitSwitchExpressionArm(SwitchExpressionArmSyntax node) { Complexity++; base.VisitSwitchExpressionArm(node); }
    public override void VisitConditionalExpression(ConditionalExpressionSyntax node) { Complexity++; base.VisitConditionalExpression(node); }

    public override void VisitBinaryExpression(BinaryExpressionSyntax node)
    {
        if (node.IsKind(SyntaxKind.LogicalAndExpression) || node.IsKind(SyntaxKind.LogicalOrExpression) ||
            node.IsKind(SyntaxKind.CoalesceExpression))
        {
            Complexity++;
        }
        base.VisitBinaryExpression(node);
    }

    public static int Compute(MethodDeclarationSyntax method)
    {
        var walker = new SyntaxComplexityWalker();
        walker.Visit(method);
        return walker.Complexity;
    }
}
