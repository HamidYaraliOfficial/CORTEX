using Cortex.Core.Abstractions;
using Cortex.Graph;
using Cortex.Impact;
using Cortex.Metrics;
using Cortex.Rules;
using Cortex.Search;
using Cortex.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Cortex.Infrastructure;

/// <summary>Wires every subsystem behind its Cortex.Core.Abstractions interface into one DI container.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCortexCore(this IServiceCollection services, string workspaceDataDirectory)
    {
        Directory.CreateDirectory(workspaceDataDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(Path.Combine(workspaceDataDirectory, "logs", "cortex-.log"), rollingInterval: RollingInterval.Day)
            .CreateLogger();

        services.AddLogging(builder => builder.AddSerilog(dispose: true));

        services.AddSingleton<ICodeGraphEngine, CodeGraph>();
        services.AddSingleton(sp => new GraphTraversal(sp.GetRequiredService<ICodeGraphEngine>()));
        services.AddSingleton(sp => new CircularDependencyDetector(sp.GetRequiredService<ICodeGraphEngine>()));
        services.AddSingleton(sp => new GraphQueryEngine(sp.GetRequiredService<ICodeGraphEngine>()));

        services.AddSingleton<ISearchEngine>(_ => new SqliteFtsSearchEngine(Path.Combine(workspaceDataDirectory, "search.db")));
        services.AddSingleton<IMetricsEngine, ComplexityMetricsCalculator>();
        services.AddSingleton<IRuleEngine, ArchitectureRuleEngine>();
        services.AddSingleton<IImpactAnalyzer, ChangeImpactAnalyzer>();
        services.AddSingleton(sp => new ImpactSimulationEngine(sp.GetRequiredService<IImpactAnalyzer>()));
        services.AddSingleton(sp => new RefactoringImpactPreview(
            sp.GetRequiredService<ImpactSimulationEngine>(), sp.GetRequiredService<IRuleEngine>()));

        services.AddSingleton<ICredentialStore>(_ => new DpapiCredentialStore());
        services.AddSingleton<IAuditLogger>(_ => new AuditLogger(Path.Combine(workspaceDataDirectory, "audit.log")));
        services.AddSingleton<IJobScheduler>(_ => new JobScheduler());
        services.AddSingleton<WorkingHoursScheduleService>();

        return services;
    }
}
