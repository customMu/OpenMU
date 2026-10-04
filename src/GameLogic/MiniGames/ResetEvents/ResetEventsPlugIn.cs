// <copyright file="ResetEventsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.ResetEvents;

using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.MiniGames.Kalima;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Blood Castle and Devil Square by the resets of the character: the level of the event by the resets (instead of the
/// character level), a limit of entries per day, monsters with the strength of the reset ladder, and the rewards of the level
/// (money, jewels, the materials of the wings).
/// </summary>
[PlugIn]
[Display(Name = "Events by resets", Description = "Blood Castle and Devil Square by the resets of the character: the level by the resets, entries per day, monsters of the strength of the level, rewards: money, jewels and the materials of the wings.")]
[Guid("D4B7E2A1-6C93-4F58-8A1E-3B9C5D7F2E46")]
public class ResetEventsPlugIn : IFeaturePlugIn, ISupportCustomConfiguration<ResetEventsConfiguration>, ISupportDefaultCustomConfiguration
{
    private const int StrengthLevelRange = 15;

    private const int WideStrengthLevelRange = 40;

    private readonly ConcurrentDictionary<(MiniGameType Type, int Level), KalimaTierScaling?> _scalings = new();

    private ResetEventsConfiguration? _configurationOfScalings;

    /// <summary>
    /// How a player shares the rewards of the level.
    /// </summary>
    /// <param name="Jewels">The multiplier of the jewels of the level.</param>
    /// <param name="Money">The multiplier of the money of the level.</param>
    /// <param name="Materials">The multiplier of the chances of the materials of the wings.</param>
    /// <param name="FinisherJewel">If set to <c>true</c>, the player gets one jewel of the level by the finisher chance.</param>
    public sealed record RewardShare(float Jewels, float Money, float Materials, bool FinisherJewel);

    /// <inheritdoc />
    public ResetEventsConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new ResetEventsConfiguration();

    /// <summary>
    /// Determines whether the event type has levels by resets.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns><c>true</c>, if the type has levels by resets.</returns>
    public bool HandlesType(MiniGameType type) => this.GetConfiguration().GetTiers(type) is { Count: > 0 };

    /// <summary>
    /// Gets the level of the event for the player by its resets.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="type">The type.</param>
    /// <returns>The level, or <c>null</c> if the player has not enough resets.</returns>
    public ResetEventTier? GetTier(Player player, MiniGameType type) =>
        this.GetConfiguration().GetTierByResets(type, (int)(player.Attributes?[Stats.Resets] ?? 0));

    /// <summary>
    /// Gets the minimum resets of the first level of the type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The minimum resets.</returns>
    public int GetMinimumResets(MiniGameType type) => this.GetConfiguration().GetTiers(type)?.Min(t => t.MinimumResets) ?? 0;

    /// <summary>
    /// Checks the entries of today; shows a message if they are used.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="type">The type.</param>
    /// <returns><c>true</c>, if the player can enter.</returns>
    public async ValueTask<bool> CheckEntriesAsync(Player player, MiniGameType type)
    {
        var configuration = this.GetConfiguration();
        if (GetCounterAttributes(type) is not var (dayAttribute, entriesAttribute) || !HasAttributes(player, dayAttribute, entriesAttribute))
        {
            return true;
        }

        var today = configuration.GetDayNumber(DateTime.Now);
        var used = (int)player.GetStoredStatValue(dayAttribute) == today ? (int)player.GetStoredStatValue(entriesAttribute) : 0;
        if (used < configuration.EntriesPerDay)
        {
            return true;
        }

        await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.ResetEventDailyLimitFormat), configuration.EntriesPerDay, Math.Clamp(configuration.DailyResetHour, 0, 23)).ConfigureAwait(false);
        return false;
    }

    /// <summary>
    /// Uses one entry of today.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="type">The type.</param>
    public void UseEntry(Player player, MiniGameType type)
    {
        if (GetCounterAttributes(type) is not var (dayAttribute, entriesAttribute) || !HasAttributes(player, dayAttribute, entriesAttribute))
        {
            return;
        }

        var today = this.GetConfiguration().GetDayNumber(DateTime.Now);
        var used = (int)player.GetStoredStatValue(dayAttribute) == today ? (int)player.GetStoredStatValue(entriesAttribute) : 0;
        player.TrySetStoredStatValue(dayAttribute, today);
        player.TrySetStoredStatValue(entriesAttribute, used + 1);
    }

    /// <summary>
    /// Applies the strength of the level of the event to a monster which was added to the map of the event.
    /// The differences between the monsters of the event (e.g. the bosses) stay.
    /// </summary>
    /// <param name="definition">The definition of the event.</param>
    /// <param name="monster">The monster.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    public void ApplyStrength(MiniGameDefinition definition, Monster monster, GameConfiguration gameConfiguration)
    {
        var configuration = this.GetConfiguration();
        if (!ReferenceEquals(configuration, this._configurationOfScalings))
        {
            this._scalings.Clear();
            this._configurationOfScalings = configuration;
        }

        var tier = configuration.GetTiers(definition.Type)?.FirstOrDefault(t => t.Level == definition.GameLevel);
        if (tier is null)
        {
            return;
        }

        var scaling = this._scalings.GetOrAdd((definition.Type, definition.GameLevel), _ => CreateScaling(gameConfiguration, definition, tier));
        scaling?.ApplyRelative(monster);
    }

    /// <summary>
    /// Gives the rewards of the level of the event to the player: money, jewels (into the inventory, or dropped if it's full)
    /// and by chance the materials of the wings.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="definition">The definition of the event.</param>
    /// <param name="share">The share of the player.</param>
    /// <returns>The given money.</returns>
    public async ValueTask<int> GiveRewardsAsync(Player player, MiniGameDefinition definition, RewardShare share)
    {
        var configuration = this.GetConfiguration();
        if (configuration.GetTiers(definition.Type)?.FirstOrDefault(t => t.Level == definition.GameLevel) is not { } tier)
        {
            return 0;
        }

        var items = player.GameContext.Configuration.Items;
        var rewards = new List<Item>();
        (byte Group, short Number, float Count)[] jewels =
        [
            (12, 15, tier.ChaosJewels),
            (14, 13, tier.BlessJewels),
            (14, 14, tier.SoulJewels),
            (14, 16, tier.LifeJewels),
            (14, 22, tier.CreationJewels),
            (14, 31, tier.GuardianJewels),
        ];
        foreach (var (group, number, count) in jewels)
        {
            var amount = count * share.Jewels;
            var whole = (int)Math.Floor(amount);
            if (amount - whole > 0 && Rand.NextRandomBool(amount - whole))
            {
                whole++;
            }

            for (var i = 0; i < whole; i++)
            {
                AddItem(rewards, items, group, number, 0);
            }
        }

        if (share.FinisherJewel && Rand.NextRandomBool(Math.Clamp(configuration.FinisherJewelChance, 0, 1)))
        {
            var index = KalimaDrops.RollWeighted(jewels.Select(j => (int)Math.Round(j.Count * 100)).ToList());
            if (index >= 0)
            {
                AddItem(rewards, items, jewels[index].Group, jewels[index].Number, 0);
            }
        }

        if (Rand.NextRandomBool(Math.Clamp(tier.ChaosWeaponChance * share.Materials, 0, 1)))
        {
            rewards.Add(CreateChaosWeapon(items, configuration));
        }

        if (Rand.NextRandomBool(Math.Clamp(tier.FeatherChance * share.Materials, 0, 1)))
        {
            AddItem(rewards, items, 13, 14, 0); // Loch's Feather
        }

        if (Rand.NextRandomBool(Math.Clamp(tier.CrestChance * share.Materials, 0, 1)))
        {
            AddItem(rewards, items, 13, 14, 1); // Crest of Monarch
        }

        foreach (var item in rewards.Where(i => i.Definition is not null))
        {
            await GiveItemAsync(player, item).ConfigureAwait(false);
        }

        var money = (int)Math.Round(tier.Money * share.Money);
        if (money > 0 && !player.TryAddMoney(money))
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.AwardMoneyFailByFullInventory)).ConfigureAwait(false);
            money = 0;
        }

        return money;
    }

    private static (AttributeDefinition Day, AttributeDefinition Entries)? GetCounterAttributes(MiniGameType type) => type switch
    {
        MiniGameType.BloodCastle => (Stats.BloodCastleEntryDay, Stats.BloodCastleEntries),
        MiniGameType.DevilSquare => (Stats.DevilSquareEntryDay, Stats.DevilSquareEntries),
        _ => null,
    };

    private static bool HasAttributes(Player player, AttributeDefinition day, AttributeDefinition entries)
    {
        var attributes = player.GameContext.Configuration.Attributes;
        var result = attributes.Any(a => a.Id == day.Id) && attributes.Any(a => a.Id == entries.Id);
        if (!result)
        {
            player.Logger.LogWarning("Events by resets: the attributes of the daily entries are missing in the configuration, the entries are not limited.");
        }

        return result;
    }

    private static void AddItem(List<Item> rewards, IEnumerable<ItemDefinition> items, byte group, short number, byte level)
    {
        if (items.FirstOrDefault(d => d.Group == group && d.Number == number) is { } definition)
        {
            var item = new TemporaryItem { Definition = definition, Level = level };
            item.Durability = item.GetMaximumDurabilityOfOnePiece();
            rewards.Add(item);
        }
    }

    private static Item CreateChaosWeapon(IEnumerable<ItemDefinition> items, ResetEventsConfiguration configuration)
    {
        (byte Group, short Number)[] weapons = [(2, 6), (4, 6), (5, 7)]; // Chaos Dragon Axe, Chaos Nature Bow, Chaos Lightning Staff
        var (group, number) = weapons[Rand.NextInt(0, weapons.Length)];
        var definition = items.First(d => d.Group == group && d.Number == number);
        var level = 1 + Math.Max(0, KalimaDrops.RollWeighted(configuration.ChaosWeaponLevelWeights));
        var item = ItemDropByRankPlugIn.CreateItem(definition, (byte)level, false, false);
        if (definition.PossibleItemOptions.SelectMany(o => o.PossibleOptions).FirstOrDefault(o => object.Equals(o.OptionType, ItemOptionTypes.Option)) is { } option)
        {
            var optionLevel = Rand.NextRandomBool(Math.Clamp(configuration.ChaosWeaponHighOptionChance, 0, 1)) ? 2 : 1;
            item.ItemOptions.Add(new ItemOptionLink { ItemOption = option, Level = optionLevel });
        }

        return item;
    }

    private static async ValueTask GiveItemAsync(Player player, Item item)
    {
        // With the slot, the inventory makes the temporary item persistent; the stored item is the one to show.
        if (player.Inventory is { } inventory
            && inventory.CheckInvSpace(item) is { } slot
            && await inventory.AddItemAsync(slot, item).ConfigureAwait(false)
            && inventory.GetItem(slot) is { } stored)
        {
            await player.InvokeViewPlugInAsync<Views.Inventory.IItemAppearPlugIn>(p => p.ItemAppearAsync(stored)).ConfigureAwait(false);
            return;
        }

        if (player.CurrentMap is { } map)
        {
            await map.AddAsync(new DroppedItem(item, player.RandomPosition, map, player, player.GetAsEnumerable())).ConfigureAwait(false);
        }
    }

    private static KalimaTierScaling? CreateScaling(GameConfiguration gameConfiguration, MiniGameDefinition definition, ResetEventTier tier)
    {
        var eventMaps = gameConfiguration.MiniGameDefinitions.Select(d => d.Entrance?.Map).Where(m => m is not null).Select(m => m!).ToHashSet();
        var monsters = gameConfiguration.Maps
            .Where(map => !eventMaps.Contains(map))
            .SelectMany(map => map.MonsterSpawns)
            .Where(spawn => spawn is { SpawnTrigger: SpawnTrigger.Automatic, MonsterDefinition.ObjectKind: NpcObjectKind.Monster })
            .Select(spawn => spawn.MonsterDefinition!)
            .Distinct()
            .ToList();
        var near = Around(monsters, tier.MonsterLevel, StrengthLevelRange);
        if (near.Count < 3)
        {
            near = Around(monsters, tier.MonsterLevel, WideStrengthLevelRange);
        }

        if (near.Count == 0 || definition.Entrance?.Map is not { } eventMap)
        {
            return null;
        }

        var strength = new KalimaStrength(
            tier.MonsterLevel,
            Median(near, Stats.MaximumHealth) * tier.HealthFactor,
            Median(near, Stats.MaximumPhysBaseDmg) * tier.DamageFactor,
            Median(near, Stats.DefenseBase) * tier.DefenseFactor);
        var eventSpawns = eventMap.MonsterSpawns
            .Where(spawn => spawn.MonsterDefinition?.ObjectKind == NpcObjectKind.Monster)
            .ToList();
        return new KalimaTierScaling(strength, eventSpawns);
    }

    private static List<MonsterDefinition> Around(List<MonsterDefinition> monsters, int level, int range) =>
        monsters.Where(m => Math.Abs(KalimaStrengthCalculator.GetValue(m, Stats.Level) - level) <= range).ToList();

    private static float Median(List<MonsterDefinition> monsters, AttributeDefinition attribute)
    {
        var values = monsters.Select(m => KalimaStrengthCalculator.GetValue(m, attribute)).Order().ToList();
        var middle = values.Count / 2;
        return values.Count % 2 == 1 ? values[middle] : (values[middle - 1] + values[middle]) / 2;
    }

    private ResetEventsConfiguration GetConfiguration() => this.Configuration ??= (ResetEventsConfiguration)this.CreateDefaultConfig();
}
