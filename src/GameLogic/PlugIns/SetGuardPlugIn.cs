// <copyright file="SetGuardPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Set Guard: a complete armor set decreases the received damage by a percentage of its rank, which grows
/// with the lowest enhancement level of the set. It's an own multiplier on <see cref="Stats.DamageReceiveDecrement"/>,
/// like wings and pets, so it multiplies with all other damage decreases instead of adding up.
/// A set is complete when every piece of the set which the character class can wear is equipped and active
/// (e.g. a Magic Gladiator wears no helm, a Rage Fighter no gloves). Pieces of different sets don't count together.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.SetGuardPlugIn_Name), Description = nameof(PlugInResources.SetGuardPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("7B2D4C61-3E9A-4F05-9C1E-58A6D2F0B314")]
public class SetGuardPlugIn : IFeaturePlugIn, ISupportCustomConfiguration<SetGuardConfiguration>, ISupportDefaultCustomConfiguration
{
    private const byte FirstArmorGroup = 7;
    private const byte LastArmorGroup = 11;

    /// <inheritdoc />
    public SetGuardConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new SetGuardConfiguration();

    /// <summary>
    /// Creates the power up of the Set Guard for the equipped items, if they form a complete set.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="activeEquippedItems">The equipped items which give their bonuses.</param>
    /// <param name="attributeHolder">The attribute holder.</param>
    /// <returns>The power up, or <c>null</c> if no complete set is equipped.</returns>
    public PowerUpWrapper? CreatePowerUp(Player player, IReadOnlyCollection<Item> activeEquippedItems, AttributeSystem attributeHolder)
    {
        var percent = this.GetDamageDecreasePercent(player, activeEquippedItems);
        if (percent <= 0)
        {
            return null;
        }

        var multiplier = 1f - (Math.Min(percent, 100f) / 100f);
        return new PowerUpWrapper(new SimpleElement(multiplier, AggregateType.Multiplicate), Stats.DamageReceiveDecrement, attributeHolder);
    }

    /// <summary>
    /// Gets the damage decrease in percent of the complete set which the player wears.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="activeEquippedItems">The equipped items which give their bonuses.</param>
    /// <returns>The damage decrease in percent; 0 if no complete set is equipped.</returns>
    public float GetDamageDecreasePercent(Player player, IReadOnlyCollection<Item> activeEquippedItems)
    {
        var configuration = this.Configuration ??= (SetGuardConfiguration)this.CreateDefaultConfig();
        var characterClass = player.SelectedCharacter?.CharacterClass;
        if (characterClass is null)
        {
            return 0;
        }

        var armorPieces = activeEquippedItems
            .Where(item => item.Durability > 0 && item.Definition is { } definition && definition.Group >= FirstArmorGroup && definition.Group <= LastArmorGroup)
            .ToList();
        if (armorPieces.Count == 0)
        {
            return 0;
        }

        var setNumber = armorPieces[0].Definition!.Number;
        if (armorPieces.Any(item => item.Definition!.Number != setNumber)
            || configuration.Sets.FirstOrDefault(s => s.ItemNumber == setNumber) is not { } set
            || configuration.Ranks.FirstOrDefault(r => r.Rank == set.Rank) is not { } rank)
        {
            return 0;
        }

        var requiredGroups = player.GameContext.Configuration.Items
            .Where(definition => definition.Number == setNumber
                                 && definition.Group >= FirstArmorGroup
                                 && definition.Group <= LastArmorGroup
                                 && definition.QualifiedCharacters.Contains(characterClass))
            .Select(definition => definition.Group)
            .Distinct()
            .ToList();
        if (requiredGroups.Count == 0
            || requiredGroups.Any(group => armorPieces.All(item => item.Definition!.Group != group)))
        {
            return 0;
        }

        var lowestLevel = armorPieces.Min(item => item.Level);
        var reachedSteps = ParseStepLevels(configuration.StepLevels).Count(level => lowestLevel >= level);
        return rank.Percent * (1f + (reachedSteps * configuration.StepBonusPercent / 100f));
    }

    private static IEnumerable<int> ParseStepLevels(string? stepLevels)
    {
        if (string.IsNullOrWhiteSpace(stepLevels))
        {
            yield break;
        }

        foreach (var part in stepLevels.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(part, out var level))
            {
                yield return level;
            }
        }
    }
}
