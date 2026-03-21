using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace StorageLens.Shared.Infrastructure;

/// <summary>
/// Records security-relevant, user-visible state changes for compliance and
/// post-incident analysis.
///
/// Audit events are written as structured log entries at the Information level
/// with the "Audit" category so they can be routed to a separate sink
/// (e.g., Azure Monitor, Datadog, Splunk) without affecting operational logs.
///
/// Covered events:
///   - Configuration changes (scan settings, alert thresholds, pricing tiers)
///   - Entity lifecycle changes (location created/deleted)
///   - Security-related events (API key use, auth failures)
///
/// Usage:
///   Inject <see cref="IAuditLogger"/> and call the appropriate method.
///   The actor is typically the authenticated service name or user ID extracted
///   from the current <see cref="Microsoft.AspNetCore.Http.HttpContext"/>.
/// </summary>
public interface IAuditLogger
{
    void LogCreated(string entityType, string entityId, string actor, object? details = null);
    void LogUpdated(string entityType, string entityId, string actor, object? before = null, object? after = null);
    void LogDeleted(string entityType, string entityId, string actor, object? details = null);
    void LogConfigurationChanged(string settingKey, string actor, object? before = null, object? after = null);
    void LogSecurityEvent(string eventName, string actor, string? description = null);
}

/// <summary>
/// Structured-logging implementation of <see cref="IAuditLogger"/>.
/// Each audit event includes a consistent set of properties so log aggregation
/// tools can reliably filter and alert on audit activity.
/// </summary>
public sealed class AuditLogger : IAuditLogger
{
    // Dedicated category keeps audit logs filterable regardless of the calling class.
    private readonly ILogger _logger;

    public AuditLogger(ILoggerFactory loggerFactory)
        => _logger = loggerFactory.CreateLogger("StorageLens.Audit");

    public void LogCreated(string entityType, string entityId, string actor, object? details = null)
        => _logger.LogInformation(
            "[AUDIT] {Action} | Entity: {EntityType} | Id: {EntityId} | Actor: {Actor} | Details: {@Details}",
            "CREATED", entityType, entityId, actor, details);

    public void LogUpdated(string entityType, string entityId, string actor, object? before = null, object? after = null)
        => _logger.LogInformation(
            "[AUDIT] {Action} | Entity: {EntityType} | Id: {EntityId} | Actor: {Actor} | Before: {@Before} | After: {@After}",
            "UPDATED", entityType, entityId, actor, before, after);

    public void LogDeleted(string entityType, string entityId, string actor, object? details = null)
        => _logger.LogInformation(
            "[AUDIT] {Action} | Entity: {EntityType} | Id: {EntityId} | Actor: {Actor} | Details: {@Details}",
            "DELETED", entityType, entityId, actor, details);

    public void LogConfigurationChanged(string settingKey, string actor, object? before = null, object? after = null)
        => _logger.LogInformation(
            "[AUDIT] {Action} | Setting: {SettingKey} | Actor: {Actor} | Before: {@Before} | After: {@After}",
            "CONFIG_CHANGED", settingKey, actor, before, after);

    public void LogSecurityEvent(string eventName, string actor, string? description = null)
        => _logger.LogWarning(
            "[AUDIT] {Action} | Event: {EventName} | Actor: {Actor} | Description: {Description}",
            "SECURITY", eventName, actor, description ?? string.Empty);
}

public static class AuditLoggerExtensions
{
    /// <summary>
    /// Registers <see cref="IAuditLogger"/> as a singleton so it is always available
    /// for DI injection.  Called automatically from
    /// <see cref="ObservabilityExtensions.AddStorageLensObservability"/>.
    /// </summary>
    public static IServiceCollection AddAuditLogger(this IServiceCollection services)
    {
        services.AddSingleton<IAuditLogger, AuditLogger>();
        return services;
    }
}
