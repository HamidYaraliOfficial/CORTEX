namespace Cortex.Core.Models;

/// <summary>
/// Every kind of element CORTEX can represent as a node in the Code Knowledge Graph.
/// </summary>
public enum NodeType
{
    Solution,
    Project,
    Folder,
    File,
    Namespace,
    Class,
    Struct,
    Record,
    Interface,
    Enum,
    Method,
    Function,
    Property,
    Field,
    Event,
    Constructor,
    GenericTypeParameter,
    Package,
    ExternalDependency,
    ApiEndpoint,
    Controller,
    Dto,
    DatabaseTable,
    DatabaseQuery,
    ConfigurationKey,
    ServiceRegistration,
    TestClass,
    TestMethod,
    Module,
    Commit,
    Branch,
    Author
}

/// <summary>
/// Directed, typed relationship between two nodes in the Code Knowledge Graph.
/// Every edge carries enough evidence for the UI to answer "why does this relationship exist?".
/// </summary>
public enum EdgeType
{
    Calls,
    References,
    Inherits,
    Implements,
    DependsOn,
    Contains,
    Overrides,
    Uses,
    Exposes,
    Imports,
    Instantiates,
    Reads,
    Writes,
    Tests,
    Configures,
    ConnectsTo
}

/// <summary>
/// Coarse-grained severity used across Impact Analysis, Rules and Health Score.
/// This is always presented as an analytical indicator, never a certainty.
/// </summary>
public enum ImpactLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum RuleSeverity
{
    Info,
    Warning,
    Error
}

public enum AiProviderKind
{
    LocalOnnx,
    Cloud
}
