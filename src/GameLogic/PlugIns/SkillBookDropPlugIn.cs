// <copyright file="SkillBookDropPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Drops skill books (scrolls, orbs, crystals, parchments) from killed monsters with an own roll per book, within the
/// monster level range of the book - they are not sold in the shops. The stronger the skill, the lower the chance.
/// </summary>
[PlugIn]
[Display(Name = "Skill book drop", Description = "Skill books drop from monsters (an own roll per book and kill, within its monster level range) instead of being sold in the shops.")]
[Guid("5B8E2D47-9C1A-4F63-A8E0-7D3C6B1F4A92")]
public class SkillBookDropPlugIn : IAdditionalItemDropPlugIn, ISupportCustomConfiguration<SkillBookDropConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc />
    public SkillBookDropConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new SkillBookDropConfiguration();

    /// <inheritdoc />
    public ValueTask AddItemDropsAsync(AdditionalItemDropArgs args)
    {
        var configuration = this.Configuration ??= (SkillBookDropConfiguration)this.CreateDefaultConfig();
        if (args.KilledObject.IsSummonedMonster
            || args.Monster.ObjectKind != NpcObjectKind.Monster
            || (!configuration.ApplyInMiniGames && args.Killer.CurrentMiniGame is { IsHuntingGround: false }))
        {
            return ValueTask.CompletedTask;
        }

        var monsterLevel = (int)args.Monster[Stats.Level];
        var multiplier = MonsterDropMultiplier.Get(configuration.MonsterMultipliers, args.Monster.Number);
        foreach (var book in configuration.Books)
        {
            if (book.ChancePercent <= 0
                || monsterLevel < book.MinimumMonsterLevel
                || monsterLevel > book.MaximumMonsterLevel
                || Rand.NextDouble() >= book.ChancePercent * multiplier * GetLevelFactor(configuration, book, monsterLevel) / 100.0)
            {
                continue;
            }

            var definition = args.Killer.GameContext.Configuration.Items
                .FirstOrDefault(i => i.Group == book.ItemGroup && i.Number == book.ItemNumber);
            if (definition is null)
            {
                args.Killer.Logger.LogWarning("Skill book drop: item {group}/{number} ({name}) not found in the configuration.", book.ItemGroup, book.ItemNumber, book.Name);
                continue;
            }

            args.Items.Add(new TemporaryItem { Definition = definition, Durability = 1, Level = Math.Min(book.ItemLevel, definition.MaximumItemLevel) });
            args.Killer.Logger.LogInformation(
                "[SkillDrop] {character}: {item} from monster {monsterNumber} {monsterName}, level {monsterLevel}",
                args.Killer.SelectedCharacter?.Name,
                definition.Name.ValueInNeutralLanguage,
                args.Monster.Number,
                args.Monster.Designation.ValueInNeutralLanguage,
                monsterLevel);
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Gets the factor of the chance by the position of the monster level inside the window of the book:
    /// x0.5 at the first level up to x1.5 at the last one (x1 on average). Must match tools/balance/skill_books.py.
    /// </summary>
    private static double GetLevelFactor(SkillBookDropConfiguration configuration, SkillBookDropConfiguration.SkillBookDropEntry book, int monsterLevel)
    {
        if (!configuration.GrowingChance || book.MaximumMonsterLevel <= book.MinimumMonsterLevel)
        {
            return 1.0;
        }

        return 0.5 + ((double)(monsterLevel - book.MinimumMonsterLevel) / (book.MaximumMonsterLevel - book.MinimumMonsterLevel));
    }
}
