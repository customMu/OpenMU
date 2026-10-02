// <copyright file="ItemDropByRankPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Drops armor, weapons and shields by rank: every item drops only from the monsters of its rank window,
/// always +0, without option, with luck and skill by chance. At most one item drops per kill.
/// The chance of the regular "random items" drop item group should be set to 0 when this plugin is active.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.ItemDropByRankPlugIn_Name), Description = nameof(PlugInResources.ItemDropByRankPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("4D2A8F63-1C7E-4B59-A0D3-8E6F2B1C9A75")]
public class ItemDropByRankPlugIn : IAdditionalItemDropPlugIn, ISupportCustomConfiguration<ItemDropByRankConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    private const int WeaponAndShieldStartSixth = 3;

    /// <summary>
    /// The start of an item inside the rank window, in sixths of the window, by item group.
    /// </summary>
    private static readonly Dictionary<byte, int> StartSixthByGroup = new()
    {
        { 10, 0 }, // gloves
        { 11, 1 }, // boots
        { 7, 2 }, // helm
        { 9, 3 }, // pants
        { 8, 4 }, // armor
    };

    private volatile CandidateCache? _cache;

    /// <inheritdoc />
    public ItemDropByRankConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new ItemDropByRankConfiguration();

    /// <inheritdoc />
    public ValueTask AddItemDropsAsync(AdditionalItemDropArgs args)
    {
        var configuration = this.Configuration ??= (ItemDropByRankConfiguration)this.CreateDefaultConfig();
        if (args.KilledObject.IsSummonedMonster
            || args.Monster.ObjectKind != NpcObjectKind.Monster
            || (!configuration.ApplyInMiniGames && args.Killer.CurrentMiniGame is not null))
        {
            return ValueTask.CompletedTask;
        }

        var cache = this._cache;
        if (cache is null || !ReferenceEquals(cache.Configuration, configuration))
        {
            cache = new CandidateCache(configuration, args.Killer.GameContext.Configuration.Items, args.Killer.Logger);
            this._cache = cache;
        }

        var monsterLevel = (int)args.Monster[Stats.Level];
        var candidates = cache.GetCandidates(monsterLevel);
        if (candidates.TotalChance <= 0)
        {
            return ValueTask.CompletedTask;
        }

        var roll = Rand.NextDouble();
        if (roll >= candidates.TotalChance)
        {
            return ValueTask.CompletedTask;
        }

        foreach (var candidate in candidates.Items)
        {
            if (roll < candidate.Chance)
            {
                args.Items.Add(CreateItem(candidate.Definition, configuration));
                LogDrop(args, candidate, monsterLevel);
                break;
            }

            roll -= candidate.Chance;
        }

        return ValueTask.CompletedTask;
    }

    private static void LogDrop(AdditionalItemDropArgs args, Candidate candidate, int monsterLevel)
    {
        var characterName = args.Killer.SelectedCharacter?.Name ?? string.Empty;
        args.Killer.Logger.LogInformation(
            "[RankDrop] {character}: {item} (rank {rank}) from monster {monsterNumber} {monsterName}, level {monsterLevel}",
            characterName,
            candidate.Definition.Name.ValueInNeutralLanguage,
            candidate.Rank,
            args.Monster.Number,
            args.Monster.Designation.ValueInNeutralLanguage,
            monsterLevel);
        DropStatistics.AddRankItem(characterName, candidate.Rank);
    }

    private static Item CreateItem(ItemDefinition definition, ItemDropByRankConfiguration configuration)
    {
        var item = new TemporaryItem { Definition = definition };
        if (Rand.NextRandomBool(configuration.LuckChancePercent / 100.0)
            && definition.PossibleItemOptions
                .SelectMany(o => o.PossibleOptions)
                .FirstOrDefault(o => object.Equals(o.OptionType, ItemOptionTypes.Luck)) is { } luck)
        {
            item.ItemOptions.Add(new ItemOptionLink { ItemOption = luck });
        }

        if (item.CanHaveSkill())
        {
            item.HasSkill = Rand.NextRandomBool(configuration.SkillChancePercent / 100.0);
        }

        item.Durability = item.GetMaximumDurabilityOfOnePiece();
        return item;
    }

    private static int GetStartSixth(ItemDefinition definition)
    {
        return StartSixthByGroup.TryGetValue(definition.Group, out var sixth) ? sixth : WeaponAndShieldStartSixth;
    }

    private sealed record Candidate(ItemDefinition Definition, int Rank, int MinimumMonsterLevel, int MaximumMonsterLevel, double Chance);

    private sealed record LevelCandidates(IReadOnlyList<Candidate> Items, double TotalChance);

    private sealed class CandidateCache
    {
        private readonly List<Candidate> _all = new();
        private readonly ConcurrentDictionary<int, LevelCandidates> _byLevel = new();

        public CandidateCache(ItemDropByRankConfiguration configuration, IEnumerable<ItemDefinition> definitions, ILogger logger)
        {
            this.Configuration = configuration;
            var definitionsById = definitions.ToDictionary(d => ((short)d.Group, d.Number));
            foreach (var entry in configuration.Items)
            {
                var rank = configuration.Ranks.FirstOrDefault(r => r.Rank == entry.Rank);
                if (rank is null)
                {
                    logger.LogWarning("Item drop by rank: rank {rank} of {name} ({group}/{number}) is not configured.", entry.Rank, entry.Name, entry.ItemGroup, entry.ItemNumber);
                    continue;
                }

                if (!definitionsById.TryGetValue((entry.ItemGroup, entry.ItemNumber), out var definition))
                {
                    logger.LogWarning("Item drop by rank: item {group}/{number} ({name}) not found in the configuration.", entry.ItemGroup, entry.ItemNumber, entry.Name);
                    continue;
                }

                var oneInKills = entry.OneInKills > 0 ? entry.OneInKills : rank.OneInKills;
                if (oneInKills <= 0)
                {
                    continue;
                }

                var minimumLevel = entry.MinimumMonsterLevel > 0
                    ? entry.MinimumMonsterLevel
                    : rank.MinimumMonsterLevel + ((rank.MaximumMonsterLevel - rank.MinimumMonsterLevel + 1) * GetStartSixth(definition) / 6);
                this._all.Add(new Candidate(definition, entry.Rank, minimumLevel, rank.MaximumMonsterLevel, 1.0 / oneInKills));
            }
        }

        public ItemDropByRankConfiguration Configuration { get; }

        public LevelCandidates GetCandidates(int monsterLevel)
        {
            return this._byLevel.GetOrAdd(monsterLevel, level =>
            {
                var items = this._all.Where(c => c.MinimumMonsterLevel <= level && level <= c.MaximumMonsterLevel).ToList();
                return new LevelCandidates(items, items.Sum(c => c.Chance));
            });
        }
    }
}
