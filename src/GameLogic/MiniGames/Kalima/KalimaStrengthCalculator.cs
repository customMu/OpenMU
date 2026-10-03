// <copyright file="KalimaStrengthCalculator.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Kalima;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// Calculates the strength of the regular monsters of the Kalima instance tiers, based on the
/// monsters of the reference maps as they are stored in the game configuration (database).
/// </summary>
public static class KalimaStrengthCalculator
{
    /// <summary>
    /// Calculates the strength of all tiers. A tier is at least by the minimum growth stronger than the previous one.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="configuration">The configuration of the Kalima instance.</param>
    /// <returns>The strength by tier level.</returns>
    public static IReadOnlyDictionary<int, KalimaStrength> Calculate(GameConfiguration gameConfiguration, KalimaInstanceConfiguration configuration)
    {
        var result = new Dictionary<int, KalimaStrength>();
        KalimaStrength? previous = null;
        foreach (var tier in configuration.Tiers.OrderBy(t => t.MinimumResets).ThenBy(t => t.Level))
        {
            if (GetReferenceStrength(gameConfiguration, tier.ReferenceMapNumber) is not { } reference)
            {
                continue;
            }

            var strength = new KalimaStrength(
                reference.Level * tier.LevelFactor,
                reference.Health * tier.HealthFactor,
                reference.Damage * tier.DamageFactor,
                reference.Defense * tier.DefenseFactor);
            if (previous is not null)
            {
                var growth = 1 + Math.Max(0, configuration.MinimumTierGrowth);
                strength = new KalimaStrength(
                    Math.Max(strength.Level, previous.Level * growth),
                    Math.Max(strength.Health, previous.Health * growth),
                    Math.Max(strength.Damage, previous.Damage * growth),
                    Math.Max(strength.Defense, previous.Defense * growth));
            }

            if (configuration.MaximumMonsterLevel > 0)
            {
                strength = strength with { Level = Math.Min(strength.Level, configuration.MaximumMonsterLevel) };
            }

            result[tier.Level] = strength;
            previous = strength;
        }

        return result;
    }

    /// <summary>
    /// Gets the median strength of the regular monsters of a map.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <param name="mapNumber">The map number.</param>
    /// <returns>The median strength, or <c>null</c> if the map has no regular monsters.</returns>
    public static KalimaStrength? GetReferenceStrength(GameConfiguration gameConfiguration, short mapNumber)
    {
        var monsters = gameConfiguration.Maps
            .Where(map => map.Number == mapNumber)
            .SelectMany(map => map.MonsterSpawns)
            .Where(spawn => spawn is { SpawnTrigger: SpawnTrigger.Automatic, MonsterDefinition.ObjectKind: NpcObjectKind.Monster })
            .Select(spawn => spawn.MonsterDefinition!)
            .Distinct()
            .ToList();
        if (monsters.Count == 0)
        {
            return null;
        }

        return new KalimaStrength(
            Median(monsters, Stats.Level),
            Median(monsters, Stats.MaximumHealth),
            Median(monsters, Stats.MaximumPhysBaseDmg),
            Median(monsters, Stats.DefenseBase));
    }

    /// <summary>
    /// Gets the value of an attribute of a monster definition.
    /// </summary>
    /// <param name="monster">The monster definition.</param>
    /// <param name="attribute">The attribute.</param>
    /// <returns>The value, or 0.</returns>
    public static float GetValue(MonsterDefinition monster, AttributeDefinition attribute)
    {
        return monster.Attributes.FirstOrDefault(a => Equals(a.AttributeDefinition, attribute))?.Value ?? 0;
    }

    private static float Median(IEnumerable<MonsterDefinition> monsters, AttributeDefinition attribute)
    {
        var values = monsters.Select(m => GetValue(m, attribute)).Order().ToList();
        var middle = values.Count / 2;
        return values.Count % 2 == 1 ? values[middle] : (values[middle - 1] + values[middle]) / 2;
    }
}
