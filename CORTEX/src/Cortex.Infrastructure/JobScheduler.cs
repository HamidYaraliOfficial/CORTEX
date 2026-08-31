using System.Collections.Concurrent;
using System.Threading.Channels;
using Cortex.Core.Abstractions;

namespace Cortex.Infrastructure;

/// <summary>
/// Channel-backed background job runner for every long operation in CORTEX (indexing,
/// Roslyn analysis, graph building, search indexing, AI embedding, report generation,
/// git analysis). The UI thread is never blocked: work items are queued here, progress
/// is reported via <see cref="IProgress{T}"/>, and every job is cancellable and retryable.
/// </summary>
public sealed class JobScheduler : IJobScheduler, IAsyncDisposable
{
    private readonly Channel<(string Id, string Name, Func<IProgress<JobProgress>, CancellationToken, Task> Work)> _channel =
        Channel.CreateUnbounded<(string, string, Func<IProgress<JobProgress>, CancellationToken, Task>)>();

    private readonly ConcurrentDictionary<string, JobStatus> _statuses = new();
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _cancellations = new();
    private readonly Task _workerLoop;

    public JobScheduler(int workerCount = 2)
    {
        _workerLoop = Task.WhenAll(Enumerable.Range(0, workerCount).Select(_ => Task.Run(WorkerLoopAsync)));
    }

    public string Enqueue(string jobName, Func<IProgress<JobProgress>, CancellationToken, Task> work)
    {
        var id = Guid.NewGuid().ToString("N");
        _statuses[id] = new JobStatus(id, jobName, JobState.Queued, 0, "Queued", DateTimeOffset.UtcNow, null, null);
        _cancellations[id] = new CancellationTokenSource();
        _channel.Writer.TryWrite((id, jobName, work));
        return id;
    }

    public JobStatus? GetStatus(string jobId) => _statuses.GetValueOrDefault(jobId);
    public IReadOnlyList<JobStatus> GetAllJobs() => _statuses.Values.OrderByDescending(j => j.StartedAtUtc).ToList();

    public void Cancel(string jobId)
    {
        if (_cancellations.TryGetValue(jobId, out var cts)) cts.Cancel();
    }

    private async Task WorkerLoopAsync()
    {
        await foreach (var (id, name, work) in _channel.Reader.ReadAllAsync())
        {
            var cts = _cancellations[id];
            _statuses[id] = _statuses[id] with { State = JobState.Running, LastMessage = "Started" };

            var progress = new Progress<JobProgress>(p =>
                _statuses[id] = _statuses[id] with { PercentComplete = p.PercentComplete, LastMessage = p.Message });

            try
            {
                await work(progress, cts.Token);
                _statuses[id] = _statuses[id] with { State = JobState.Completed, PercentComplete = 100, FinishedAtUtc = DateTimeOffset.UtcNow };
            }
            catch (OperationCanceledException)
            {
                _statuses[id] = _statuses[id] with { State = JobState.Cancelled, FinishedAtUtc = DateTimeOffset.UtcNow };
            }
            catch (Exception ex)
            {
                _statuses[id] = _statuses[id] with { State = JobState.Failed, FinishedAtUtc = DateTimeOffset.UtcNow, Error = ex.Message };
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _channel.Writer.Complete();
        await _workerLoop;
    }
}
