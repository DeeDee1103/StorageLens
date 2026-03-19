namespace StorageLens.Services.Scanner.Services;

public static class PathSafety
{
    public static bool IsPathSupported(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        // Support UNC shares and rooted local paths.
        if (!path.StartsWith("\\\\", StringComparison.Ordinal) && !Path.IsPathRooted(path))
        {
            return false;
        }

        try
        {
            // Normalize to resolve any .. traversal sequences.
            var fullPath = Path.GetFullPath(path);

            // Reject symbolic links and junctions to prevent traversal via symlinks.
            if (IsSymlinkOrJunction(fullPath))
            {
                return false;
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool IsSymlinkOrJunction(string path)
    {
        try
        {
            var dirInfo = new DirectoryInfo(path);
            if (dirInfo.Exists)
            {
                return dirInfo.LinkTarget is not null ||
                       (dirInfo.Attributes & FileAttributes.ReparsePoint) != 0;
            }

            var fileInfo = new FileInfo(path);
            if (fileInfo.Exists)
            {
                return fileInfo.LinkTarget is not null ||
                       (fileInfo.Attributes & FileAttributes.ReparsePoint) != 0;
            }

            // Path doesn't exist yet — allow (existence check happens during scan)
            return false;
        }
        catch
        {
            return false;
        }
    }
}
