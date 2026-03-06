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

        var current = optionsMonitor.CurrentValue;
        var fromRule = ResolveByRules(inputPath, current.Mappings);
        if (!string.Equals(fromRule, inputPath, StringComparison.Ordinal))
        {
            return fromRule;
        }

        if (!ShouldUseAutoMapping(current))
        {
            return inputPath;
        }

        // Auto-map any Windows drive path, for example C:\Users\User -> /host/drives/c/Users/User
        var driveMapped = TryAutoMapWindowsDrive(inputPath);
        if (driveMapped is not null)
        {
            return driveMapped;
        }

        // Auto-map UNC path, for example \\server\share\foo -> /host/unc/server/share/foo
        var uncMapped = TryAutoMapUnc(inputPath);
        if (uncMapped is not null)
        {
            return uncMapped;
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
        var normalizedInput = NormalizePath(inputPath);

        foreach (var mapping in mappings)
        {
            if (!mapping.Enabled || string.IsNullOrWhiteSpace(mapping.Pattern) || string.IsNullOrWhiteSpace(mapping.Replacement))
            {
                continue;
            }

            var mode = mapping.Mode?.Trim() ?? "Prefix";

            if (mode.Equals("Regex", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var replaced = Regex.Replace(inputPath, mapping.Pattern, mapping.Replacement, RegexOptions.IgnoreCase);
                    if (!string.Equals(replaced, inputPath, StringComparison.Ordinal))
                    {
                        return NormalizePath(replaced);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Invalid regex path mapping pattern: {Pattern}", mapping.Pattern);
                }

                continue;
            }

            var normalizedFrom = NormalizePath(mapping.Pattern);
            if (normalizedInput.StartsWith(normalizedFrom, StringComparison.OrdinalIgnoreCase))
            {
                var suffix = normalizedInput[normalizedFrom.Length..].TrimStart('/');
                var mapped = NormalizePath(mapping.Replacement);
                return string.IsNullOrWhiteSpace(suffix) ? mapped : $"{mapped}/{suffix}";
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
        var rest = NormalizePath(match.Groups["rest"].Value).TrimStart('/');
        return string.IsNullOrWhiteSpace(rest)
            ? $"/host/drives/{drive}"
            : $"/host/drives/{drive}/{rest}";
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
        var rest = NormalizePath(match.Groups["rest"].Value).TrimStart('/');

        var basePath = $"/host/unc/{server}/{share}";
        return string.IsNullOrWhiteSpace(rest) ? basePath : $"{basePath}/{rest}";
    }

    private static string NormalizePath(string path) =>
        path.Replace('\\', '/').Trim().TrimEnd('/');
}
