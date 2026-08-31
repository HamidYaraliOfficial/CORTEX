using Cortex.Indexing;
using Xunit;

namespace Cortex.Tests;

public class IncrementalIndexingTests
{
    [Fact]
    public async Task NewFile_IsReportedAsAdded()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var file = Path.Combine(root, "A.cs");
            await File.WriteAllTextAsync(file, "class A {}");

            var tracker = new FileHashTracker();
            var changes = await tracker.DetectChangesAsync(root, new[] { "A.cs" }, CancellationToken.None);

            Assert.Contains("A.cs", changes.Added);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task UnmodifiedFile_IsReportedAsUnchangedOnSecondPass()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var file = Path.Combine(root, "A.cs");
            await File.WriteAllTextAsync(file, "class A {}");

            var tracker = new FileHashTracker();
            await tracker.DetectChangesAsync(root, new[] { "A.cs" }, CancellationToken.None);
            var second = await tracker.DetectChangesAsync(root, new[] { "A.cs" }, CancellationToken.None);

            Assert.Contains("A.cs", second.Unchanged);
            Assert.DoesNotContain("A.cs", second.Modified);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task ModifiedFile_IsReportedAsModified()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var file = Path.Combine(root, "A.cs");
            await File.WriteAllTextAsync(file, "class A {}");

            var tracker = new FileHashTracker();
            await tracker.DetectChangesAsync(root, new[] { "A.cs" }, CancellationToken.None);

            await Task.Delay(10);
            File.SetLastWriteTimeUtc(file, DateTime.UtcNow.AddSeconds(5));
            await File.WriteAllTextAsync(file, "class A { void M() {} }");

            var second = await tracker.DetectChangesAsync(root, new[] { "A.cs" }, CancellationToken.None);
            Assert.Contains("A.cs", second.Modified);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public async Task DeletedFile_IsRemovedFromKnownState()
    {
        var root = Directory.CreateTempSubdirectory().FullName;
        try
        {
            var file = Path.Combine(root, "A.cs");
            await File.WriteAllTextAsync(file, "class A {}");

            var tracker = new FileHashTracker();
            await tracker.DetectChangesAsync(root, new[] { "A.cs" }, CancellationToken.None);

            var afterDelete = await tracker.DetectChangesAsync(root, Array.Empty<string>(), CancellationToken.None);
            Assert.Contains("A.cs", afterDelete.Deleted);
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
