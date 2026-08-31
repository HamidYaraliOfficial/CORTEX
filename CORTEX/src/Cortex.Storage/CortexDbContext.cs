using Cortex.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Storage;

/// <summary>
/// Local, per-workspace SQLite database: persisted Code Knowledge Graph, repository
/// metadata, architecture rules, snapshots and the (never-content-bearing) audit log.
/// Every schema change bumps <see cref="SchemaVersion"/> so a stale on-disk cache from an
/// older CORTEX build is detected and rebuilt instead of silently corrupted.
/// </summary>
public sealed class CortexDbContext : DbContext
{
    public const int SchemaVersion = 1;

    public DbSet<NodeEntity> Nodes => Set<NodeEntity>();
    public DbSet<EdgeEntity> Edges => Set<EdgeEntity>();
    public DbSet<RepositoryEntity> Repositories => Set<RepositoryEntity>();
    public DbSet<WorkspaceEntity> Workspaces => Set<WorkspaceEntity>();
    public DbSet<SnapshotEntity> Snapshots => Set<SnapshotEntity>();
    public DbSet<AuditEventEntity> AuditEvents => Set<AuditEventEntity>();
    public DbSet<RuleEntity> Rules => Set<RuleEntity>();
    public DbSet<FileHashEntity> FileHashes => Set<FileHashEntity>();

    private readonly string _databasePath;
    public CortexDbContext(string databasePath) => _databasePath = databasePath;

    protected override void OnConfiguring(DbContextOptionsBuilder options) =>
        options.UseSqlite($"Data Source={_databasePath}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NodeEntity>(e =>
        {
            e.HasKey(n => n.Id);
            e.HasIndex(n => new { n.RepositoryId, n.Kind });
            e.HasIndex(n => n.RelativeFilePath);
        });
        modelBuilder.Entity<EdgeEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.SourceNodeId);
            e.HasIndex(x => x.TargetNodeId);
            e.HasIndex(x => x.Kind);
        });
        modelBuilder.Entity<RepositoryEntity>().HasKey(r => r.Id);
        modelBuilder.Entity<WorkspaceEntity>().HasKey(w => w.Id);
        modelBuilder.Entity<SnapshotEntity>().HasKey(s => s.Id);
        modelBuilder.Entity<AuditEventEntity>().HasKey(a => a.Id);
        modelBuilder.Entity<RuleEntity>().HasKey(r => r.Id);
        modelBuilder.Entity<FileHashEntity>().HasKey(f => new { f.RepositoryId, f.RelativePath });
    }
}
