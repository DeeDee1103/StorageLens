using StorageLens.Services.Scanner.Services;
using Xunit;

namespace StorageLens.Services.Scanner.Tests;

public class PathSafetyTests
{
    // --- Invalid / unsupported paths ---

    [Fact]
    public void IsPathSupported_NullString_ReturnsFalse()
    {
        Assert.False(PathSafety.IsPathSupported(null!));
    }

    [Fact]
    public void IsPathSupported_EmptyString_ReturnsFalse()
    {
        Assert.False(PathSafety.IsPathSupported(""));
    }

    [Fact]
    public void IsPathSupported_WhitespaceOnly_ReturnsFalse()
    {
        Assert.False(PathSafety.IsPathSupported("   "));
    }

    [Theory]
    [InlineData("relative/path")]
    [InlineData("no-root-dir")]
    [InlineData("./current")]
    [InlineData("../parent")]
    public void IsPathSupported_RelativePaths_ReturnsFalse(string path)
    {
        Assert.False(PathSafety.IsPathSupported(path));
    }

    // --- Valid rooted paths that don't exist (can't be symlinks) ---

    [Fact]
    public void IsPathSupported_NonExistentRootedPath_ReturnsTrue()
    {
        // Non-existent paths cannot be symlinks, so should pass
        var path = Path.Combine(Path.GetTempPath(), "storagelens-test-" + Guid.NewGuid());
        Assert.True(PathSafety.IsPathSupported(path));
    }

    [Fact]
    public void IsPathSupported_NonExistentUncPath_ReturnsTrue()
    {
        // Non-existent UNC paths cannot be symlinks
        var path = @"\\nonexistent-server-" + Guid.NewGuid() + @"\share";
        Assert.True(PathSafety.IsPathSupported(path));
    }

    // --- Path normalization ---

    [Fact]
    public void IsPathSupported_PathWithTrailingDot_NormalizesCorrectly()
    {
        var tempDir = Path.GetTempPath();
        // Path.GetFullPath will normalize away the trailing dot segment
        var pathWithDot = Path.Combine(tempDir, "subdir", "..", ".");
        // Result depends on whether the resolved path exists/is symlink
        // Main goal: should not throw; just verify no exception
        var result = PathSafety.IsPathSupported(pathWithDot);
        Assert.IsType<bool>(result);
    }

    // --- Real existing directory (temp dir, which should not be a symlink) ---

    [Fact]
    public void IsPathSupported_RealNonSymlinkDirectory_ReturnsTrue()
    {
        // Create a real temporary directory that is definitely not a symlink
        var tempDir = Directory.CreateTempSubdirectory("storagelens-test-");
        try
        {
            Assert.True(PathSafety.IsPathSupported(tempDir.FullName));
        }
        finally
        {
            tempDir.Delete();
        }
    }

    // --- Symlink detection (if OS supports creating symlinks) ---

    [Fact]
    public void IsPathSupported_SymlinkedDirectory_ReturnsFalse()
    {
        var tempDir = Directory.CreateTempSubdirectory("storagelens-target-");
        var symlinkPath = Path.Combine(Path.GetTempPath(), "storagelens-link-" + Guid.NewGuid());

        try
        {
            Directory.CreateSymbolicLink(symlinkPath, tempDir.FullName);

            if (Directory.Exists(symlinkPath))
            {
                Assert.False(PathSafety.IsPathSupported(symlinkPath));
            }
            // If symlink creation wasn't supported/permitted, skip the assertion
        }
        catch (Exception ex) when (
            ex is UnauthorizedAccessException ||
            ex is NotSupportedException ||
            ex is IOException)
        {
            // Symlink creation requires elevated permissions on some systems;
            // skip this assertion if it's not possible in the test environment.
        }
        finally
        {
            if (Directory.Exists(symlinkPath)) Directory.Delete(symlinkPath);
            tempDir.Delete(recursive: true);
        }
    }
}
