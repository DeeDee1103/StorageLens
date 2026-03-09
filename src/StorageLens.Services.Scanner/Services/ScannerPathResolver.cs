using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using StorageLens.Services.Scanner.Clients;

namespace StorageLens.Services.Scanner.Services;

public interface IScannerPathResolver
{
    string Resolve(string inputPath);
}

public class ScannerPathResolver(
    IOptionsMonitor<ScannerPathMappingOptions> optionsMonitor,
    ILogger<ScannerPathResolver> logger) : IScannerPathResolver
{
    private static readonly Regex WindowsDrivePattern =
        new(@"^(?<drive>[A-Za-z]):[\\/]*(?<rest>.*)$", RegexOptions.Compiled);

    private static readonly Regex UncPattern =
        new(@"^\\\\(?<server>[^\\/]+)\\(?<share>[^\\/]+)(?<rest>.*)$", RegexOptions.Compiled);

    public string Resolve(string inputPath)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            return inputPath;
        }

        var currentOptions = optionsMonitor.CurrentValue;
        var ruleResolvedPath = ResolveByRules(inputPath, currentOptions.Mappings);
        if (!string.Equals(ruleResolvedPath, inputPath, StringComparison.Ordinal))
        {
            return ruleResolvedPath;
        }

        if (!ShouldUseAutoMapping(currentOptions))
        {
            return inputPath;
        }

        // Auto-map any Windows drive path, for example C:\Users\User -> /host/drives/c/Users/User
        var windowsDriveMappedPath = TryAutoMapWindowsDrive(inputPath);
        if (windowsDriveMappedPath is not null)
        {
            return windowsDriveMappedPath;
        }

        // Auto-map UNC path, for example \\server\share\foo -> /host/unc/server/share/foo
        var uncMappedPath = TryAutoMapUnc(inputPath);
        if (uncMappedPath is not null)
        {
            return uncMappedPath;
        }

        return inputPath;
    }

    private static bool ShouldUseAutoMapping(ScannerPathMappingOptions options)
    {
        if (!options.EnableAutoMapping)
        {
            return false;
        }

        if (!options.AutoMapInContainerOnly)
        {
            return true;
        }

        var runningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
        return string.Equals(runningInContainer, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(runningInContainer, "1", StringComparison.OrdinalIgnoreCase);
    }

    private string ResolveByRules(string inputPath, IReadOnlyList<ScannerPathMappingRule> mappings)
    {
        var normalizedInputPath = NormalizePath(inputPath);

        foreach (var mapping in mappings)
        {
            if (!mapping.Enabled || string.IsNullOrWhiteSpace(mapping.Pattern) || string.IsNullOrWhiteSpace(mapping.Replacement))
            {
                continue;
            }

            var mappingMode = mapping.Mode?.Trim() ?? "Prefix";

            if (mappingMode.Equals("Regex", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var regexReplacedPath = Regex.Replace(inputPath, mapping.Pattern, mapping.Replacement, RegexOptions.IgnoreCase);
                    if (!string.Equals(regexReplacedPath, inputPath, StringComparison.Ordinal))
                    {
                        return NormalizePath(regexReplacedPath);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Invalid regex path mapping pattern: {Pattern}", mapping.Pattern);
                }

                continue;
            }

            var normalizedFromPattern = NormalizePath(mapping.Pattern);
            if (normalizedInputPath.StartsWith(normalizedFromPattern, StringComparison.OrdinalIgnoreCase))
            {
                var pathSuffix = normalizedInputPath[normalizedFromPattern.Length..].TrimStart('/');
                var resolvedReplacementPath = NormalizePath(mapping.Replacement);
                return string.IsNullOrWhiteSpace(pathSuffix) ? resolvedReplacementPath : $"{resolvedReplacementPath}/{pathSuffix}";
            }
        }

        return inputPath;
    }

    private static string? TryAutoMapWindowsDrive(string inputPath)
    {
        var match = WindowsDrivePattern.Match(inputPath);
        if (!match.Success)
        {
            return null;
        }

        var drive = match.Groups["drive"].Value.ToLowerInvariant();
        var remainingPath = NormalizePath(match.Groups["rest"].Value).TrimStart('/');
        return string.IsNullOrWhiteSpace(remainingPath)
            ? $"/host/drives/{drive}"
            : $"/host/drives/{drive}/{remainingPath}";
    }

    private static string? TryAutoMapUnc(string inputPath)
    {
        var match = UncPattern.Match(inputPath);
        if (!match.Success)
        {
            return null;
        }

        var server = match.Groups["server"].Value;
        var share = match.Groups["share"].Value;
        var remainingPath = NormalizePath(match.Groups["rest"].Value).TrimStart('/');

        var basePath = $"/host/unc/{server}/{share}";
        return string.IsNullOrWhiteSpace(remainingPath) ? basePath : $"{basePath}/{remainingPath}";
    }

    private static string NormalizePath(string path) =>
        path.Replace('\\', '/').Trim().TrimEnd('/');
}
