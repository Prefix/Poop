namespace Prefix.Poop.Interfaces.PoopModule;

/// <summary>
/// Interface for poop size generation and description
/// </summary>
internal interface IPoopSizeGenerator : IModule
{
    /// <summary>
    /// Generates a random poop size based on the configured rarity system
    /// </summary>
    /// <returns>Random poop size between MinPoopSize and MaxPoopSize</returns>
    float GetRandomSize();

    /// <summary>
    /// Gets a descriptive name for a poop size with color codes using localization
    /// </summary>
    /// <param name="size">The poop size to describe</param>
    /// <returns>Localized description of the size (e.g., "Legendary", "Massive", "Tiny")</returns>
    string GetSizeDescription(float size);

    /// <summary>
    /// Checks if a size is considered massive (>= 2.0)
    /// </summary>
    bool IsMassive(float size);
}
