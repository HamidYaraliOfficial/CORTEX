namespace Cortex.Indexing;

/// <summary>
/// Watches a repository root for .cs/.csproj/.sln changes and raises a single debounced
/// "something changed, please re-index" event — batches rapid saves (e.g. a mass find &amp;
/// replace, or a branch switch touching hundreds of files) into one incremental run.
/// </summary>
public sealed class RepositoryFileWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private readonly System.Timers.Timer _debounceTimer;
    private readonly object _lock = new();
    private bool _pending;

    public event Action? ChangesDetected;

    public RepositoryFileWatcher(string repositoryRoot, TimeSpan? debounce = null)
    {
        _watcher = new FileSystemWatcher(repositoryRoot)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
        };
        _watcher.Filters.Add("*.cs");
        _watcher.Filters.Add("*.csproj");
        _watcher.Filters.Add("*.sln");

        _debounceTimer = new System.Timers.Timer((debounce ?? TimeSpan.FromSeconds(2)).TotalMilliseconds) { AutoReset = false };
        _debounceTimer.Elapsed += (_, _) =>
        {
            lock (_lock)
            {
                if (!_pending) return;
                _pending = false;
            }
            ChangesDetected?.Invoke();
        };

        _watcher.Changed += OnFsEvent;
        _watcher.Created += OnFsEvent;
        _watcher.Deleted += OnFsEvent;
        _watcher.Renamed += OnFsEvent;
    }

    private void OnFsEvent(object sender, FileSystemEventArgs e)
    {
        lock (_lock) { _pending = true; }
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    public void Start() => _watcher.EnableRaisingEvents = true;
    public void Stop() => _watcher.EnableRaisingEvents = false;

    public void Dispose()
    {
        _watcher.Dispose();
        _debounceTimer.Dispose();
    }
}
