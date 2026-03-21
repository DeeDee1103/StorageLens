using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Azure.Identity;

namespace StorageLens.Shared.Infrastructure;

/// <summary>
/// Key Vault integration for secure credential management.
/// Provides utilities for loading secrets from Azure Key Vault instead of configuration files.
/// </summary>
public static class KeyVaultIntegration
{
    /// <summary>
    /// Adds Azure Key Vault as a configuration source using DefaultAzureCredential (managed identity or user auth).
    /// Call this in Program.cs before building the configuration.
    /// </summary>
    /// <example>
    /// var builder = WebApplication.CreateBuilder(args);
    /// KeyVaultIntegration.AddKeyVaultConfiguration(builder);
    /// var config = builder.Build();
    /// </example>
    public static void AddKeyVaultConfiguration(WebApplicationBuilder builder)
    {
        var keyVaultUrl = builder.Configuration["Azure:KeyVault:Url"];

        if (string.IsNullOrWhiteSpace(keyVaultUrl))
        {
            // Key Vault is optional - if not configured, use environment variables
            return;
        }

        try
        {
            var credential = new DefaultAzureCredential();
            builder.Configuration.AddAzureKeyVault(
                new Uri(keyVaultUrl),
                credential);
        }
        catch (Exception ex)
        {
            var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<object>>();
            logger.LogWarning($"Failed to connect to Key Vault at {keyVaultUrl}: {ex.Message}. Using environment variables instead.");
        }
    }

    /// <summary>
    /// Gets a secret from Key Vault with fallback to environment variables.
    /// </summary>
    /// <param name="configuration">The IConfiguration instance</param>
    /// <param name="secretName">The name of the secret to retrieve</param>
    /// <param name="fallbackEnvironmentVariable">Optional environment variable name to fall back to</param>
    /// <returns>The secret value or null if not found</returns>
    public static string? GetSecret(
        IConfiguration configuration,
        string secretName,
        string? fallbackEnvironmentVariable = null)
    {
        // Try configuration first (which includes Key Vault if configured)
        var value = configuration[secretName];
        if (!string.IsNullOrWhiteSpace(value))
            return value;

        // Fall back to environment variable
        if (!string.IsNullOrWhiteSpace(fallbackEnvironmentVariable))
        {
            value = Environment.GetEnvironmentVariable(fallbackEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return null;
    }

    /// <summary>
    /// Validates that all required credentials are present (either in Key Vault or environment).
    /// Throws an exception if any required credential is missing.
    /// </summary>
    /// <param name="configuration">The IConfiguration instance</param>
    /// <param name="requiredSecrets">Array of required secret names</param>
    /// <exception cref="InvalidOperationException">Thrown if any required secret is missing</exception>
    public static void ValidateRequiredSecrets(IConfiguration configuration, params string[] requiredSecrets)
    {
        var missingSecrets = new List<string>();

        foreach (var secretName in requiredSecrets)
        {
            var value = configuration[secretName];
            if (string.IsNullOrWhiteSpace(value))
                missingSecrets.Add(secretName);
        }

        if (missingSecrets.Count > 0)
        {
            throw new InvalidOperationException(
                $"Missing required secrets in Key Vault or environment: {string.Join(", ", missingSecrets)}. " +
                $"Please ensure these secrets are configured in Azure Key Vault or set as environment variables.");
        }
    }
}
