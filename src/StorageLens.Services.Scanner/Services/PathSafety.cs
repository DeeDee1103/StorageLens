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
        return path.StartsWith("\\\\", StringComparison.Ordinal) || Path.IsPathRooted(path);
    }
}
