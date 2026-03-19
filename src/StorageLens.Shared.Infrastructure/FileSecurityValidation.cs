using System.IO;

namespace StorageLens.Shared.Infrastructure;

/// <summary>
/// File security utilities for validating file operations.
/// Prevents file-based denial-of-service and path traversal attacks.
/// </summary>
public static class FileSecurityValidation
{
    /// <summary>
    /// Default maximum file size for hashing operations (4 GB).
    /// Large files should be handled with streaming to prevent memory exhaustion.
    /// </summary>
    private const long DefaultMaxFileSizeBytes = 4 * 1024L * 1024L * 1024L; // 4 GB

    /// <summary>
    /// Validates that a file exists and doesn't exceed the maximum allowed size.
    /// Should be called BEFORE opening a file stream to prevent memory DoS.
    /// </summary>
    /// <param name="filePath">The full path to the file to validate</param>
    /// <param name="maxSizeBytes">Optional maximum file size (defaults to 4GB)</param>
    /// <returns>True if the file is valid and within size limits; otherwise false</returns>
    /// <exception cref="FileNotFoundException">Thrown if file does not exist</exception>
    /// <exception cref="InvalidOperationException">Thrown if file exceeds maximum size</exception>
    public static bool ValidateFileBeforeProcessing(string filePath, long? maxSizeBytes = null)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        var fileInfo = new FileInfo(filePath);
        var maxSize = maxSizeBytes ?? DefaultMaxFileSizeBytes;

        if (fileInfo.Length > maxSize)
            throw new InvalidOperationException(
                $"File exceeds maximum allowed size. File: {fileInfo.Length} bytes, Max: {maxSize} bytes.");

        return true;
    }

    /// <summary>
    /// Validates file path safety to prevent directory traversal attacks.
    /// </summary>
    /// <param name="filePath">The file path to validate</param>
    /// <param name="allowedRootPath">Optional root path that files must be within</param>
    /// <returns>True if the path is safe; otherwise false</returns>
    public static bool IsPathSafe(string filePath, string? allowedRootPath = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            var fullPath = Path.GetFullPath(filePath);

            // If a root path is specified, ensure the file is within it
            if (!string.IsNullOrWhiteSpace(allowedRootPath))
            {
                var fullRootPath = Path.GetFullPath(allowedRootPath);
                if (!fullPath.StartsWith(fullRootPath, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
