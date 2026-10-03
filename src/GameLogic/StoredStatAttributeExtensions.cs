// <copyright file="StoredStatAttributeExtensions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

using MUnique.OpenMU.AttributeSystem;

/// <summary>
/// Extensions to read and write values which are stored as stat attributes of the selected character,
/// e.g. counters and currencies which are not part of a character class.
/// </summary>
public static class StoredStatAttributeExtensions
{
    /// <summary>
    /// Gets the stored value of the specified attribute of the selected character.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="definition">The attribute definition.</param>
    /// <returns>The stored value, or 0 if the character doesn't have the attribute yet.</returns>
    public static float GetStoredStatValue(this Player player, AttributeDefinition definition)
    {
        return FindStatAttribute(player, definition)?.Value ?? 0;
    }

    /// <summary>
    /// Sets the stored value of the specified attribute of the selected character.
    /// The attribute is added to the character, if it doesn't have it yet.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="definition">The attribute definition.</param>
    /// <param name="value">The new value.</param>
    /// <returns><c>true</c>, if the value has been stored; <c>false</c>, if the attribute is not part of the game configuration.</returns>
    public static bool TrySetStoredStatValue(this Player player, AttributeDefinition definition, float value)
    {
        if (FindStatAttribute(player, definition) is { } existing)
        {
            existing.Value = value;
            return true;
        }

        if (player.SelectedCharacter is not { } character)
        {
            return false;
        }

        // The definition has to be the persistent one of the configuration, otherwise the
        // persistence would try to add it as a new entity.
        var trackedDefinition = player.GameContext.Configuration.Attributes.FirstOrDefault(a => a.Id == definition.Id);
        if (trackedDefinition is null)
        {
            player.Logger.LogWarning("The attribute {attribute} is not part of the game configuration, the value can't be stored. Apply the data update which adds it.", definition);
            return false;
        }

        var stat = player.PersistenceContext.CreateNew<StatAttribute>(trackedDefinition, value);
        character.Attributes.Add(stat);
        return true;
    }

    private static StatAttribute? FindStatAttribute(Player player, AttributeDefinition definition)
    {
        return player.SelectedCharacter?.Attributes.FirstOrDefault(a => a.Definition is { } d && d.Id == definition.Id);
    }
}
