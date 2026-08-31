using Cortex.Core.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cortex.Roslyn;

/// <summary>
/// Identifies the public API surface of a project: public types/members, ASP.NET Core
/// Minimal API endpoints (app.MapGet/MapPost/...) and Controller-based endpoints
/// ([HttpGet]/[Route] on classes deriving from ControllerBase), producing ApiEndpoint
/// nodes with route, HTTP verb and referenced-service metadata for the API Surface Analyzer.
/// </summary>
public sealed class ApiSurfaceExtractor
{
    private static readonly HashSet<string> MinimalApiVerbs = new(StringComparer.OrdinalIgnoreCase)
        { "MapGet", "MapPost", "MapPut", "MapDelete", "MapPatch" };

    public sealed record EndpointInfo(
        string RouteTemplate, string HttpVerb, string HandlerDisplayName,
        SourceLocation Location, bool RequiresAuthorization, IReadOnlyList<string> ReferencedServiceTypes);

    public async Task<IReadOnlyList<EndpointInfo>> ExtractAsync(string repositoryId, Document document, CancellationToken ct)
    {
        var results = new List<EndpointInfo>();
        var tree = await document.GetSyntaxTreeAsync(ct);
        var model = await document.GetSemanticModelAsync(ct);
        if (tree is null || model is null) return results;
        var root = await tree.GetRootAsync(ct);

        // --- Minimal API: app.MapGet("/route", (Deps deps) => ...) ---
        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax { Name.Identifier.ValueText: var methodName } access) continue;
            if (!MinimalApiVerbs.Contains(methodName)) continue;
            if (invocation.ArgumentList.Arguments.Count < 2) continue;

            var routeArg = invocation.ArgumentList.Arguments[0].Expression;
            var route = model.GetConstantValue(routeArg, ct).Value as string ?? "(dynamic route)";
            var handlerArg = invocation.ArgumentList.Arguments[1].Expression;

            var services = handlerArg switch
            {
                ParenthesizedLambdaExpressionSyntax lambda => lambda.ParameterList.Parameters
                    .Select(p => model.GetTypeInfo(p, ct).Type?.ToDisplayString())
                    .Where(t => t is not null).Select(t => t!).ToList(),
                _ => new List<string>()
            };

            results.Add(new EndpointInfo(
                route, methodName.Replace("Map", "").ToUpperInvariant(),
                $"{document.Name}:{invocation.GetLocation().GetLineSpan().StartLinePosition.Line + 1}",
                ToLocation(repositoryId, document, invocation.GetLocation()),
                RequiresAuthorization: invocation.ToString().Contains("RequireAuthorization"),
                services));
        }

        // --- Controller-based: [ApiController] class with [HttpGet]/[Route] methods ---
        foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
        {
            if (model.GetDeclaredSymbol(classDecl, ct) is not INamedTypeSymbol classSymbol) continue;
            if (!InheritsFromController(classSymbol)) continue;

            var classRoute = GetAttributeArgument(classDecl.AttributeLists, "Route") ?? "";

            foreach (var method in classDecl.Members.OfType<MethodDeclarationSyntax>())
            {
                var verbAttr = FindHttpVerbAttribute(method.AttributeLists);
                if (verbAttr is null) continue;

                var methodRoute = GetAttributeArgument(method.AttributeLists, verbAttr) ?? "";
                var fullRoute = CombineRoutes(classRoute, methodRoute);
                var requiresAuth = classDecl.AttributeLists.Concat(method.AttributeLists)
                    .SelectMany(l => l.Attributes)
                    .Any(a => a.Name.ToString().Contains("Authorize"));

                results.Add(new EndpointInfo(
                    fullRoute, verbAttr.Replace("Http", "").ToUpperInvariant(),
                    $"{classSymbol.Name}.{method.Identifier.ValueText}",
                    ToLocation(repositoryId, document, method.GetLocation()),
                    requiresAuth,
                    method.ParameterList.Parameters
                        .Select(p => model.GetTypeInfo(p, ct).Type?.ToDisplayString())
                        .Where(t => t is not null).Select(t => t!).ToList()));
            }
        }

        return results;
    }

    private static bool InheritsFromController(INamedTypeSymbol symbol)
    {
        var current = symbol.BaseType;
        while (current is not null)
        {
            if (current.Name is "ControllerBase" or "Controller") return true;
            current = current.BaseType;
        }
        return symbol.GetAttributes().Any(a => a.AttributeClass?.Name == "ApiControllerAttribute");
    }

    private static string? FindHttpVerbAttribute(SyntaxList<AttributeListSyntax> lists) =>
        lists.SelectMany(l => l.Attributes)
            .Select(a => a.Name.ToString())
            .FirstOrDefault(n => n is "HttpGet" or "HttpPost" or "HttpPut" or "HttpDelete" or "HttpPatch");

    private static string? GetAttributeArgument(SyntaxList<AttributeListSyntax> lists, string attributeName) =>
        lists.SelectMany(l => l.Attributes)
            .FirstOrDefault(a => a.Name.ToString() == attributeName)
            ?.ArgumentList?.Arguments.FirstOrDefault()?.ToString().Trim('"');

    private static string CombineRoutes(string prefix, string suffix) =>
        "/" + string.Join('/', new[] { prefix, suffix }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim('/')));

    private static SourceLocation ToLocation(string repositoryId, Document document, Location location)
    {
        var span = location.GetLineSpan();
        return new SourceLocation(repositoryId, document.Name,
            span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1,
            span.EndLinePosition.Line + 1, span.EndLinePosition.Character + 1);
    }
}
