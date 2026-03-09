using Microsoft.Extensions.Options;

namespace StorageLens.Web.Services;

public interface ITierAccessService
{
    /// <summary>Gets the currently active subscription tier, or <c>null</c> if none is configured.</summary>
    PricingTier? ActiveTier { get; }

    /// <summary>All configured pricing tiers in display order.</summary>
    IReadOnlyList<PricingTier> AllTiers { get; }

    /// <summary>
    /// Returns <c>true</c> if the service with the given ID is included in the active tier,
    /// or if no active tier is configured (open access).
    /// </summary>
    bool IsServiceAvailable(string serviceId);
}

/// <summary>
/// Scoped service that resolves the active subscription tier from <see cref="PricingOptions"/>
/// and exposes helper methods for tier-based feature gating.
/// Uses <see cref="IOptionsSnapshot{T}"/> for per-request option caching with change notification.
/// </summary>
public class TierAccessService : ITierAccessService
{
    public TierAccessService(IOptionsSnapshot<PricingOptions> options)
    {
        var pricing = options.Value;
        ActiveTier = string.IsNullOrWhiteSpace(pricing.ActiveTierId)
            ? null
            : pricing.Tiers.FirstOrDefault(t => string.Equals(t.Id, pricing.ActiveTierId, StringComparison.OrdinalIgnoreCase));
        AllTiers = pricing.Tiers;
    }

    /// <inheritdoc />
    public PricingTier? ActiveTier { get; }

    /// <inheritdoc />
    public IReadOnlyList<PricingTier> AllTiers { get; }

    /// <inheritdoc />
    public bool IsServiceAvailable(string serviceId)
    {
        if (ActiveTier == null) return true; // No tier configured → allow everything.
        return ActiveTier.IncludedServiceIds.Contains(serviceId, StringComparer.OrdinalIgnoreCase);
    }
}
