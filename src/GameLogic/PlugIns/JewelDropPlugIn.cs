// <copyright file="JewelDropPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Drops jewels from killed monsters with an own, independent roll per jewel,
/// scaled by tiers of the monster level. The regular drop (money, items) is not affected.
/// The chance of the regular jewel drop item group should be set to 0 when this plugin is active.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.JewelDropPlugIn_Name), Description = nameof(PlugInResources.JewelDropPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("C3E1A9F2-7D4B-4E6A-8B5C-2F9D1E3A7C64")]
public class JewelDropPlugIn : IAdditionalItemDropPlugIn, ISupportCustomConfiguration<JewelDropConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    /// <inheritdoc />
    public JewelDropConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new JewelDropConfiguration();

    /// <inheritdoc />
    public ValueTask AddItemDropsAsync(AdditionalItemDropArgs args)
    {
        var configuration = this.Configuration ??= (JewelDropConfiguration)this.CreateDefaultConfig();
        if (args.KilledObject.IsSummonedMonster
            || args.Monster.ObjectKind != NpcObjectKind.Monster
            || (!configuration.ApplyInMiniGames && args.Killer.CurrentMiniGame is { IsHuntingGround: false }))
        {
            return ValueTask.CompletedTask;
        }

        var monsterLevel = (int)args.Monster[Stats.Level];
        var multiplier = GetTierMultiplier(configuration, monsterLevel)
                         * GetMapMultiplier(configuration, args.Map.Definition.Number)
                         * GetMonsterMultiplier(configuration, args.Monster.Number);
        if (multiplier <= 0)
        {
            return ValueTask.CompletedTask;
        }

        foreach (var jewel in configuration.Jewels)
        {
            if (jewel.ChancePercent <= 0
                || monsterLevel < jewel.MinimumMonsterLevel
                || monsterLevel > jewel.MaximumMonsterLevel)
            {
                continue;
            }

            var chance = jewel.ChancePercent * multiplier / 100.0;
            if (Rand.NextDouble() >= chance)
            {
                continue;
            }

            var definition = args.Killer.GameContext.Configuration.Items
                .FirstOrDefault(i => i.Group == jewel.ItemGroup && i.Number == jewel.ItemNumber);
            if (definition is null)
            {
                args.Killer.Logger.LogWarning("Jewel drop: item {group}/{number} ({name}) not found in the configuration.", jewel.ItemGroup, jewel.ItemNumber, jewel.Name);
                continue;
            }

            args.Items.Add(new TemporaryItem { Definition = definition, Durability = 1 });
            var characterName = args.Killer.SelectedCharacter?.Name ?? string.Empty;
            var jewelName = definition.Name.ValueInNeutralLanguage;
            args.Killer.Logger.LogInformation(
                "[JewelDrop] {character}: {item} from monster {monsterNumber} {monsterName}, level {monsterLevel}",
                characterName,
                jewelName,
                args.Monster.Number,
                args.Monster.Designation.ValueInNeutralLanguage,
                monsterLevel);
            DropStatistics.AddJewel(characterName, jewelName);
        }

        return ValueTask.CompletedTask;
    }

    private static float GetTierMultiplier(JewelDropConfiguration configuration, int monsterLevel)
    {
        if (configuration.Tiers.Count == 0)
        {
            return 1f;
        }

        JewelDropConfiguration.JewelDropTier? result = null;
        foreach (var tier in configuration.Tiers)
        {
            if (tier.MinimumMonsterLevel <= monsterLevel
                && (result is null || tier.MinimumMonsterLevel > result.MinimumMonsterLevel))
            {
                result = tier;
            }
        }

        return result?.Multiplier ?? 0f;
    }

    private static float GetMapMultiplier(JewelDropConfiguration configuration, short mapNumber)
    {
        foreach (var entry in configuration.MapMultipliers)
        {
            if (entry.MapNumber == mapNumber)
            {
                return entry.Multiplier;
            }
        }

        return 1f;
    }

    private static float GetMonsterMultiplier(JewelDropConfiguration configuration, short monsterNumber)
    {
        foreach (var entry in configuration.MonsterMultipliers)
        {
            if (entry.MonsterNumber == monsterNumber)
            {
                return entry.Multiplier;
            }
        }

        return 1f;
    }
}
