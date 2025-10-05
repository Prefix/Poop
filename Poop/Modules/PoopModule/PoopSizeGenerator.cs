using System;
using Microsoft.Extensions.Logging;
using Prefix.Poop.Interfaces;
using Prefix.Poop.Interfaces.Managers;
using Prefix.Poop.Interfaces.PoopModule;

namespace Prefix.Poop.Modules.PoopModule;

/// <summary>
/// Handles poop size generation and description with configurable rarity system
/// </summary>
internal sealed class PoopSizeGenerator(
    ILogger<PoopSizeGenerator> logger,
    IConfigManager config,
    ILocaleManager locale)
    : IPoopSizeGenerator
{
    private readonly Random _random = new();

    /// <summary>
    /// Generates a random poop size based on rarity configuration
    /// Uses dynamic generation tiers from config for full customization
    /// </summary>
    public float GetRandomSize()
    {
        float defaultSize = config.DefaultPoopSize;
        float minSize = config.MinPoopSize;
        float maxSize = config.MaxPoopSize;

        var tiers = config.GenerationTiers;
        if (tiers == null || tiers.Count == 0)
        {
            // Fallback to default size if no tiers configured
            logger.LogWarning("No generation tiers configured, using default size");
            return defaultSize;
        }

        // Calculate cumulative chances and roll
        int roll = _random.Next(1, 101); // Roll 1-100
        int cumulative = 0;

        foreach (var tier in tiers)
        {
            cumulative += tier.Chance;
            if (roll <= cumulative)
            {
                // This tier was selected
                float sizeValue;

                if (tier.SubTiers != null && tier.SubTiers.Count > 0)
                {
                    // Use configurable sub-tier system for weighted distribution
                    sizeValue = GetSubTierSize(tier, defaultSize, minSize, maxSize);
                }
                else
                {
                    // Standard uniform random within tier's multiplier range
                    float min = defaultSize * tier.MinMultiplier;
                    float max = defaultSize * tier.MaxMultiplier;
                    
                    // Respect absolute min/max boundaries
                    min = Math.Max(min, minSize);
                    max = Math.Min(max, maxSize);
                    
                    sizeValue = min + (float)(_random.NextDouble() * (max - min));
                }

                // Clamp to configured min/max
                sizeValue = Math.Clamp(sizeValue, minSize, maxSize);

                // Round to 3 decimal places
                sizeValue = MathF.Round(sizeValue * 1000) / 1000;

                logger.LogDebug("Generated {tierName} poop with size {size:F3}", tier.Name, sizeValue);

                return sizeValue;
            }
        }

        // Fallback (shouldn't reach here if tier chances sum to 100)
        logger.LogWarning("Tier chances don't sum to 100%, using default size");
        return defaultSize;
    }

    /// <summary>
    /// Generates size using configurable sub-tier system
    /// Sub-tiers use weighted probability within the parent tier's range
    /// </summary>
    private float GetSubTierSize(PoopSizeGenerationTier parentTier, float defaultSize, float minSize, float maxSize)
    {
        if (parentTier.SubTiers == null || parentTier.SubTiers.Count == 0)
        {
            logger.LogWarning("Tier '{name}' has no SubTiers configured, falling back to standard generation", parentTier.Name);
            // Fallback to standard generation
            float min = defaultSize * parentTier.MinMultiplier;
            float max = defaultSize * parentTier.MaxMultiplier;
            min = Math.Max(min, minSize);
            max = Math.Min(max, maxSize);
            return min + (float)(_random.NextDouble() * (max - min));
        }

        // Calculate total weight
        int totalWeight = 0;
        foreach (var subTier in parentTier.SubTiers)
        {
            totalWeight += subTier.Weight;
        }

        if (totalWeight == 0)
        {
            logger.LogWarning("Tier '{name}' has SubTiers with zero total weight", parentTier.Name);
            float min = defaultSize * parentTier.MinMultiplier;
            float max = defaultSize * parentTier.MaxMultiplier;
            min = Math.Max(min, minSize);
            max = Math.Min(max, maxSize);
            return min + (float)(_random.NextDouble() * (max - min));
        }

        // Roll for sub-tier selection
        int subTierRoll = _random.Next(1, totalWeight + 1);
        int cumulativeWeight = 0;

        foreach (var subTier in parentTier.SubTiers)
        {
            cumulativeWeight += subTier.Weight;
            if (subTierRoll <= cumulativeWeight)
            {
                // This sub-tier was selected - calculate size within its range
                float parentMin = defaultSize * parentTier.MinMultiplier;
                float parentMax = defaultSize * parentTier.MaxMultiplier;
                float parentRange = parentMax - parentMin;

                // Calculate sub-tier's actual size range
                float subTierMin = parentMin + (parentRange * subTier.MinRangePercent);
                float subTierMax = parentMin + (parentRange * subTier.MaxRangePercent);

                // Respect absolute boundaries
                subTierMin = Math.Max(subTierMin, minSize);
                subTierMax = Math.Min(subTierMax, maxSize);

                // Generate random size within sub-tier range
                float sizeValue = subTierMin + (float)(_random.NextDouble() * (subTierMax - subTierMin));

                logger.LogDebug("Generated {parentName} > {subTierName} poop with size {size:F3}", 
                    parentTier.Name, subTier.Name, sizeValue);

                return sizeValue;
            }
        }

        // Fallback (shouldn't reach here)
        logger.LogWarning("Sub-tier selection failed for tier '{name}'", parentTier.Name);
        float fallbackMin = defaultSize * parentTier.MinMultiplier;
        float fallbackMax = defaultSize * parentTier.MaxMultiplier;
        fallbackMin = Math.Max(fallbackMin, minSize);
        fallbackMax = Math.Min(fallbackMax, maxSize);
        return fallbackMin + (float)(_random.NextDouble() * (fallbackMax - fallbackMin));
    }

    /// <summary>
    /// Gets a descriptive name for a poop size with color codes using localization
    /// </summary>
    public string GetSizeDescription(float size)
    {
        // Iterate through categories (should be sorted from largest to smallest threshold)
        foreach (var category in config.SizeCategories)
        {
            if (size >= category.Threshold)
            {
                return locale.GetString(category.LocaleKey);
            }
        }

        // Fallback (should never reach here if categories are configured correctly)
        return locale.GetString("size.desc_microscopic");
    }

    /// <summary>
    /// Checks if a size is considered massive (>= configurable threshold)
    /// </summary>
    public bool IsMassive(float size)
    {
        return size >= config.MassiveAnnouncementThreshold;
    }

    public bool Init()
    {
        return true;
    }
}
