using Microsoft.UI.Xaml.Controls;

namespace Cortex.UI.Views;

/// <summary>
/// Left-hand pane: Workspaces → Repositories → Solution → Projects → Folders → Files,
/// plus a Favorites / Recent section at the top of the tree. Selecting a node here
/// drives what the Architecture Canvas, Inspector and Source Viewer show.
/// </summary>
public sealed partial class RepositoryExplorerView : Page
{
    public RepositoryExplorerView()
    {
        InitializeComponent();
        // Bind RepositoryTree.RootNodes from IWorkspaceRepositoryService (application-layer
        // service composing Cortex.Core.Models.WorkspaceDescriptor) once a workspace is open.
    }
}
