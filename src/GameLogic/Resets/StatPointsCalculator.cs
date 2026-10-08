// <copyright file="StatPointsCalculator.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Resets;

using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// The stat points a character gets: the points of its resets (the tiers of the Reset Feature), the level-up points since
/// the level after the last reset and the points of the kill quests, which a reset keeps.
/// Used by the test build command and the stat check of the anti-cheat.
/// </summary>
public static class StatPointsCalculator
{
    /// <summary>
    /// Gets the stat points the character gets by its resets, levels and kill quests.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The points.</returns>
    public static long GetPointsOfResetsAndLevel(Player player)
    {
        if (player.Attributes is not { } attributes)
        {
            return 0;
        }

        var resets = (int)attributes[Stats.Resets];
        var level = (int)attributes[Stats.Level];
        var configuration = player.GameContext.FeaturePlugIns.GetPlugIn<ResetFeaturePlugIn>()?.Configuration;

        long resetPoints = 0;
        for (var reset = 1; reset <= resets && configuration is not null; reset++)
        {
            ResetConfiguration.ResetPointTier? tier = null;
            foreach (var candidate in configuration.PointsTiers)
            {
                if (candidate.MinimumResetCount <= reset && (tier is null || candidate.MinimumResetCount > tier.MinimumResetCount))
                {
                    tier = candidate;
                }
            }

            resetPoints += tier?.PointsGranted ?? 0;
        }

        var startLevel = resets > 0 ? configuration?.LevelAfterReset ?? 10 : 1;
        var levelPoints = Math.Max(0, level - startLevel) * (long)attributes[Stats.PointsPerLevelUp];
        return resetPoints + levelPoints + PlugIns.KillQuests.KillQuestsPlugIn.GetQuestPoints(player);
    }

    /// <summary>
    /// Gets the stat points the character has: the points invested in the stats the player can increase (above the
    /// base values of its class) plus the free level-up points.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The points.</returns>
    public static long GetOwnedPoints(Player player)
    {
        if (player.SelectedCharacter is not { CharacterClass: { } characterClass } character)
        {
            return 0;
        }

        long points = character.LevelUpPoints;
        foreach (var stat in characterClass.StatAttributes.Where(s => s.IncreasableByPlayer))
        {
            var value = character.Attributes.FirstOrDefault(a => a.Definition == stat.Attribute)?.Value ?? stat.BaseValue;
            points += (long)Math.Max(0, value - stat.BaseValue);
        }

        return points;
    }
}
