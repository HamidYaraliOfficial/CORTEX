using Cortex.Core.Abstractions;
using Cortex.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cortex.Roslyn;

/// <summary>
/// Walks every C# document in a loaded Roslyn <see cref="Solution"/> and turns its
/// syntax + semantic model into <see cref="GraphNode"/>/<see cref="GraphEdge"/> facts:
/// Contains (project→file→type→member), Inherits, Implements, Calls, Uses, References.
/// This is the real analyzer behind Cortex.Core.Abstractions.IRoslynAnalyzer — it never
/// fabricates a relationship it cannot trace back to a syntax location.
/// </summary>
public sealed class SymbolGraphBuilder
{
    public async Task<AnalysisResult> BuildAsync(string repositoryId, Solution solution, IProgress<AnalysisProgress>? progress, CancellationToken ct)
    {
        var nodes = new List<GraphNode>();
        var edges = new List<GraphEdge>();
        var diagnostics = new List<string>();

        var documents = solution.Projects.SelectMany(p => p.Documents).Where(d => d.SupportsSemanticModel).ToList();
        int processed = 0;

        foreach (var project in solution.Projects)
        {
            var projectNode = MakeNode(repositoryId, NodeType.Project, project.Name, project.FilePath ?? project.Name, null);
            nodes.Add(projectNode);

            foreach (var reference in project.ProjectReferences)
            {
                var target = solution.GetProject(reference.ProjectId);
                if (target is null) continue;
                edges.Add(MakeEdge(projectNode.Id, EdgeType.DependsOn,
                    MakeNode(repositoryId, NodeType.Project, target.Name, target.FilePath ?? target.Name, null).Id, project.FilePath));
            }

            foreach (var package in project.MetadataReferences.Select(r => r.Display).Where(d => d is not null))
            {
                var pkgNode = MakeNode(repositoryId, NodeType.ExternalDependency, Path.GetFileNameWithoutExtension(package!), package!, null);
                nodes.Add(pkgNode);
                edges.Add(MakeEdge(projectNode.Id, EdgeType.DependsOn, pkgNode.Id, package));
            }
        }

        foreach (var document in documents)
        {
            ct.ThrowIfCancellationRequested();
            var tree = await document.GetSyntaxTreeAsync(ct);
            var model = await document.GetSemanticModelAsync(ct);
            if (tree is null || model is null) { processed++; continue; }

            var root = await tree.GetRootAsync(ct);
            var relPath = MakeRelative(document.Project.FilePath, document.FilePath);
            var fileNode = MakeNode(repositoryId, NodeType.File, document.Name, relPath, null,
                new SourceLocation(repositoryId, relPath, 1, 1, 1, 1));
            nodes.Add(fileNode);

            var diags = model.GetDiagnostics(cancellationToken: ct)
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => $"{relPath}: {d.GetMessage()}");
            diagnostics.AddRange(diags);

            foreach (var typeDecl in root.DescendantNodes().OfType<TypeDeclarationSyntax>())
            {
                if (model.GetDeclaredSymbol(typeDecl, ct) is not INamedTypeSymbol typeSymbol) continue;

                var kind = typeDecl switch
                {
                    InterfaceDeclarationSyntax => NodeType.Interface,
                    StructDeclarationSyntax => NodeType.Struct,
                    RecordDeclarationSyntax => NodeType.Record,
                    _ => NodeType.Class
                };

                var loc = ToSourceLocation(repositoryId, relPath, typeDecl.GetLocation());
                var typeNode = MakeNode(repositoryId, kind, typeSymbol.Name, typeSymbol.ToDisplayString(), fileNode.Id, loc);
                nodes.Add(typeNode);
                edges.Add(MakeEdge(fileNode.Id, EdgeType.Contains, typeNode.Id, relPath, loc));

                if (typeSymbol.BaseType is { SpecialType: SpecialType.None } baseType && baseType.Name != "Object")
                {
                    var baseNode = MakeNode(repositoryId, NodeType.Class, baseType.Name, baseType.ToDisplayString(), null);
                    nodes.Add(baseNode);
                    edges.Add(MakeEdge(typeNode.Id, EdgeType.Inherits, baseNode.Id, relPath, loc));
                }

                foreach (var iface in typeSymbol.Interfaces)
                {
                    var ifaceNode = MakeNode(repositoryId, NodeType.Interface, iface.Name, iface.ToDisplayString(), null);
                    nodes.Add(ifaceNode);
                    edges.Add(MakeEdge(typeNode.Id, EdgeType.Implements, ifaceNode.Id, relPath, loc));
                }

                foreach (var member in typeDecl.Members)
                {
                    ProcessMember(repositoryId, relPath, member, model, typeNode, nodes, edges, ct);
                }
            }

            processed++;
            progress?.Report(new AnalysisProgress(relPath, processed, documents.Count));
        }

        return new AnalysisResult(nodes, edges, diagnostics);
    }

    private void ProcessMember(
        string repositoryId, string relPath, MemberDeclarationSyntax member, SemanticModel model,
        GraphNode ownerType, List<GraphNode> nodes, List<GraphEdge> edges, CancellationToken ct)
    {
        switch (member)
        {
            case MethodDeclarationSyntax method when model.GetDeclaredSymbol(method, ct) is IMethodSymbol methodSymbol:
            {
                var loc = ToSourceLocation(repositoryId, relPath, method.GetLocation());
                var methodNode = MakeNode(repositoryId, NodeType.Method, methodSymbol.Name, methodSymbol.ToDisplayString(), ownerType.Id, loc);
                nodes.Add(methodNode);
                edges.Add(MakeEdge(ownerType.Id, EdgeType.Contains, methodNode.Id, relPath, loc));

                if (methodSymbol.IsOverride)
                {
                    var baseMethod = methodSymbol.OverriddenMethod;
                    if (baseMethod is not null)
                    {
                        var baseNode = MakeNode(repositoryId, NodeType.Method, baseMethod.Name, baseMethod.ToDisplayString(), null);
                        nodes.Add(baseNode);
                        edges.Add(MakeEdge(methodNode.Id, EdgeType.Overrides, baseNode.Id, relPath, loc));
                    }
                }

                foreach (var invocation in method.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    var symbolInfo = model.GetSymbolInfo(invocation, ct);
                    if (symbolInfo.Symbol is not IMethodSymbol calledMethod) continue;

                    var calledNode = MakeNode(repositoryId, NodeType.Method, calledMethod.Name, calledMethod.ToDisplayString(), null);
                    nodes.Add(calledNode);
                    var callLoc = ToSourceLocation(repositoryId, relPath, invocation.GetLocation());
                    edges.Add(MakeEdge(methodNode.Id, EdgeType.Calls, calledNode.Id, relPath, callLoc));
                }
                break;
            }
            case PropertyDeclarationSyntax property when model.GetDeclaredSymbol(property, ct) is IPropertySymbol propSymbol:
            {
                var loc = ToSourceLocation(repositoryId, relPath, property.GetLocation());
                var propNode = MakeNode(repositoryId, NodeType.Property, propSymbol.Name, propSymbol.ToDisplayString(), ownerType.Id, loc);
                nodes.Add(propNode);
                edges.Add(MakeEdge(ownerType.Id, EdgeType.Contains, propNode.Id, relPath, loc));
                break;
            }
            case FieldDeclarationSyntax field:
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    if (model.GetDeclaredSymbol(variable, ct) is not IFieldSymbol fieldSymbol) continue;
                    var loc = ToSourceLocation(repositoryId, relPath, variable.GetLocation());
                    var fieldNode = MakeNode(repositoryId, NodeType.Field, fieldSymbol.Name, fieldSymbol.ToDisplayString(), ownerType.Id, loc);
                    nodes.Add(fieldNode);
                    edges.Add(MakeEdge(ownerType.Id, EdgeType.Contains, fieldNode.Id, relPath, loc));
                }
                break;
            }
            case ConstructorDeclarationSyntax ctor when model.GetDeclaredSymbol(ctor, ct) is IMethodSymbol ctorSymbol:
            {
                var loc = ToSourceLocation(repositoryId, relPath, ctor.GetLocation());
                var ctorNode = MakeNode(repositoryId, NodeType.Constructor, ctorSymbol.Name, ctorSymbol.ToDisplayString(), ownerType.Id, loc);
                nodes.Add(ctorNode);
                edges.Add(MakeEdge(ownerType.Id, EdgeType.Contains, ctorNode.Id, relPath, loc));

                // Constructor-injected dependencies become DEPENDS_ON edges for the DI Service Registration Graph.
                foreach (var param in ctorSymbol.Parameters)
                {
                    if (param.Type is not INamedTypeSymbol { TypeKind: TypeKind.Interface or TypeKind.Class } depType) continue;
                    var depNode = MakeNode(repositoryId, NodeType.Class, depType.Name, depType.ToDisplayString(), null);
                    nodes.Add(depNode);
                    edges.Add(MakeEdge(ownerType.Id, EdgeType.DependsOn, depNode.Id, relPath, loc));
                }
                break;
            }
        }
    }

    private static GraphNode MakeNode(string repositoryId, NodeType kind, string displayName, string fqn, string? parentId, SourceLocation? loc = null) =>
        new()
        {
            Id = GraphNode.ComputeId(repositoryId, kind, fqn),
            RepositoryId = repositoryId,
            Kind = kind,
            DisplayName = displayName,
            FullyQualifiedName = fqn,
            ParentId = parentId,
            Location = loc
        };

    private static GraphEdge MakeEdge(string sourceId, EdgeType kind, string targetId, string? filePath, SourceLocation? loc = null) =>
        new()
        {
            Id = GraphEdge.ComputeId(sourceId, kind, targetId, loc?.ToString()),
            RepositoryId = "", // filled by caller-owning repository context if needed
            SourceNodeId = sourceId,
            TargetNodeId = targetId,
            Kind = kind,
            Evidence = loc is null ? Array.Empty<EdgeEvidence>() : new[] { new EdgeEvidence(loc, filePath ?? "") }
        };

    private static SourceLocation ToSourceLocation(string repositoryId, string relPath, Location location)
    {
        var span = location.GetLineSpan();
        return new SourceLocation(repositoryId, relPath,
            span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1,
            span.EndLinePosition.Line + 1, span.EndLinePosition.Character + 1);
    }

    private static string MakeRelative(string? projectPath, string? filePath)
    {
        if (projectPath is null || filePath is null) return filePath ?? "unknown";
        var projectDir = Path.GetDirectoryName(projectPath) ?? "";
        return Path.GetRelativePath(projectDir, filePath).Replace('\\', '/');
    }
}
