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
            // Check the full path node first if it exists.
            if (HasReparsePoint(path))
            {
                return true;
            }

            // Walk existing ancestors to ensure traversal cannot cross a reparse point.
            var current = new DirectoryInfo(path);
            while (current is not null)
            {
                if (current.Exists && (current.LinkTarget is not null || (current.Attributes & FileAttributes.ReparsePoint) != 0))
                {
                    return true;
                }

                current = current.Parent;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static bool HasReparsePoint(string path)
    {
        var directory = new DirectoryInfo(path);
        if (directory.Exists)
        {
            return directory.LinkTarget is not null ||
                   (directory.Attributes & FileAttributes.ReparsePoint) != 0;
        }

        var file = new FileInfo(path);
        if (file.Exists)
        {
            return file.LinkTarget is not null ||
                   (file.Attributes & FileAttributes.ReparsePoint) != 0;
        }

        return false;
    }
}
