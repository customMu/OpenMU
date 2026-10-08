// <copyright file="IllusionOfNoriaPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.IllusionOfNoria;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.Interfaces;
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The Illusion of Noria: an echo of Noria behind the Veil, where the monsters of Noria roam as strong illusions
/// (rank 7 weapons, 20 resets). Warden Eldrin (in Noria and in the town of the illusion) gives the quest of the
/// Whistle of the Veil, which Condra, the outcast of Karutan, stole; with the whistle back the character may enter.
/// In the illusion the warden gives a daily quest; every few thousand kills the Gilded Colossus awakens, its curse
/// burns everybody near it. The jewels of the harmony options (Jewel of Illusion, Lesser / Greater Mirage Stone)
/// come only from here.
/// </summary>
[PlugIn]
[Display(Name = "Illusion of Noria", Description = "The map Illusion of Noria: the warden with the quest of the whistle, the entry from 20 resets, the daily quest and the boss which awakens after a number of kills.")]
[Guid("6E2B9C41-7D3A-4F85-A1E6-3C8D5B2F9A70")]
public class IllusionOfNoriaPlugIn : IFeaturePlugIn, IPlayerTalkToNpcPlugIn, ISupportCustomConfiguration<IllusionOfNoriaConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <summary>The action of the dialog: take the quest of the whistle.</summary>
    public const byte ActionAcceptQuest = 1;

    /// <summary>The action of the dialog: give the whistle back.</summary>
    public const byte ActionTurnIn = 2;

    /// <summary>The action of the dialog: enter the illusion.</summary>
    public const byte ActionEnter = 3;

    /// <summary>The action of the dialog: take the daily quest.</summary>
    public const byte ActionTakeDaily = 4;

    /// <summary>The action of the dialog: claim the reward of the daily quest.</summary>
    public const byte ActionClaimDaily = 5;

    /// <summary>The action of the dialog: return to Noria.</summary>
    public const byte ActionReturn = 6;

    /// <summary>The action of the dialog: reset the harmony option of the weapon in the slot (+ slot: 0 left, 1 right hand).</summary>
    public const byte ActionResetOption = 0x10;

    /// <summary>The action of the dialog: buy an item of the shop (followed by the item group and number).</summary>
    public const byte ActionBuy = 0x20;

    private const int WardenRange = 10;
    private const int ProgressMessageStep = 25;

    private readonly object _bossLock = new();
    private int _killsSinceBoss;
    private Monster? _boss;

    /// <summary>
    /// Gets or sets the configuration.
    /// </summary>
    public IllusionOfNoriaConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new IllusionOfNoriaConfiguration();

    /// <summary>
    /// Gets the configuration, or the default one, if none is set.
    /// </summary>
    /// <returns>The configuration.</returns>
    public IllusionOfNoriaConfiguration GetConfiguration() => this.Configuration ??= new IllusionOfNoriaConfiguration();

    /// <summary>
    /// Gets the kills on the illusion map since the last boss (for tests and the GM).
    /// </summary>
    public int KillsSinceBoss => this._killsSinceBoss;

    /// <inheritdoc />
    public async ValueTask PlayerTalksToNpcAsync(Player player, NonPlayerCharacter npc, NpcTalkEventArgs eventArgs)
    {
        var configuration = this.GetConfiguration();
        if (npc.Definition.Number != configuration.WardenNpcNumber)
        {
            return;
        }

        // The dialog of the client is closed here already, because entering and returning warp the player.
        eventArgs.HasBeenHandled = true;
        eventArgs.LeavesDialogOpen = true;
        player.OpenedNpc = null;
        await player.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);
        await this.ShowDialogAsync(player).ConfigureAwait(false);
    }

    /// <summary>
    /// Handles an action of the dialog of the warden. The player has to be near the warden.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="action">The action, see the constants.</param>
    /// <param name="itemGroup">The item group of a purchase.</param>
    /// <param name="itemNumber">The item number of a purchase.</param>
    /// <returns>The task.</returns>
    public async ValueTask HandleActionAsync(Player player, byte action, byte itemGroup = 0, short itemNumber = 0)
    {
        var configuration = this.GetConfiguration();
        if (player.CurrentMap is not { } map
            || !map.GetNpcsInRange(player.Position, WardenRange).Any(npc => npc.Definition.Number == configuration.WardenNpcNumber))
        {
            return;
        }

        var inIllusion = map.Definition.Number == configuration.MapNumber;
        var questState = (int)player.GetStoredStatValue(Stats.IllusionQuestState);
        var hasResets = GetResets(player) >= configuration.RequiredResets;
        switch (action)
        {
            case ActionAcceptQuest when questState == 0:
                if (!hasResets)
                {
                    await player.ShowBlueMessageAsync($"Warden Eldrin: come back with {configuration.RequiredResets} resets - the outcast would tear you apart.").ConfigureAwait(false);
                    break;
                }

                player.TrySetStoredStatValue(Stats.IllusionQuestState, 1);
                await player.ShowBlueMessageAsync("Quest 'The Stolen Whistle': hunt Condra, the outcast of Karutan, and bring back the Whistle of the Veil.").ConfigureAwait(false);
                break;
            case ActionTurnIn when questState == 1:
                if (this.FindWhistle(player) is not { } whistle)
                {
                    await player.ShowBlueMessageAsync("Warden Eldrin: you don't have the whistle yet. Condra hides in Karutan.").ConfigureAwait(false);
                    break;
                }

                await RemoveItemAsync(player, whistle).ConfigureAwait(false);
                player.TrySetStoredStatValue(Stats.IllusionQuestState, 2);
                await player.ShowBlueMessageAsync("Warden Eldrin: the whistle sings again! From now on I can open the Veil for you.").ConfigureAwait(false);
                break;
            case ActionEnter when !inIllusion:
                if (questState != 2 || !hasResets)
                {
                    break;
                }

                await this.WarpAsync(player, configuration.MapNumber).ConfigureAwait(false);
                return;
            case ActionReturn when inIllusion:
                await this.WarpAsync(player, configuration.HomeMapNumber).ConfigureAwait(false);
                return;
            case ActionTakeDaily when inIllusion && questState == 2:
                await this.TakeDailyAsync(player, configuration).ConfigureAwait(false);
                break;
            case ActionClaimDaily when inIllusion:
                await this.ClaimDailyAsync(player, configuration).ConfigureAwait(false);
                break;
            case ActionBuy when questState == 2:
                await this.BuyAsync(player, itemGroup, itemNumber, configuration).ConfigureAwait(false);
                break;
            case ActionResetOption or ActionResetOption + 1:
                await this.ResetOptionAsync(player, (byte)(action - ActionResetOption), configuration).ConfigureAwait(false);
                break;
            default:
                return;
        }

        await this.ShowDialogAsync(player).ConfigureAwait(false);
    }

    /// <summary>
    /// Shows the dialog of the warden with the current state of the player.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The task.</returns>
    public async ValueTask ShowDialogAsync(Player player)
    {
        var info = this.GetInfo(player);
        if (player.ViewPlugIns.GetPlugIn<IIllusionOfNoriaViewPlugIn>() is { } view)
        {
            await view.ShowWardenDialogAsync(info).ConfigureAwait(false);
            return;
        }

        await player.ShowBlueMessageAsync(info.QuestState switch
        {
            0 => $"Warden Eldrin: the Whistle of the Veil was stolen. Come back with {info.RequiredResets} resets.",
            1 => "Warden Eldrin: bring back the Whistle of the Veil from Condra in Karutan.",
            _ => "Warden Eldrin: the Veil is open for you.",
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the state of the player for the dialog of the warden.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The state.</returns>
    public IllusionWardenInfo GetInfo(Player player)
    {
        var configuration = this.GetConfiguration();
        var now = DateTime.Now;
        var (state, monsters, kills) = this.GetDaily(player, configuration, now);
        return new IllusionWardenInfo(
            player.CurrentMap?.Definition.Number == configuration.MapNumber,
            (byte)Math.Clamp((int)player.GetStoredStatValue(Stats.IllusionQuestState), 0, 2),
            GetResets(player),
            configuration.RequiredResets,
            this.FindWhistle(player) is not null,
            state,
            monsters,
            kills,
            configuration.DailyKills,
            configuration.GetTimeUntilNextDay(now),
            this.GetResetWeapons(player, configuration),
            StacksOf(player, ShardPrice(configuration, 0)).Sum(i => Math.Max(1, (int)i.Durability)),
            (int)player.GetStoredStatValue(Stats.IllusionQuestState) == 2
                ? this.GetShop(player, configuration).Take(32).Select(e => (e.ItemGroup, e.ItemNumber, Math.Max(1, e.MinimumAmount))).ToList()
                : []);
    }

    /// <summary>
    /// Creates a stack of Illusion Shards.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <param name="amount">The amount.</param>
    /// <returns>The items (stacks of up to the maximum durability).</returns>
    public List<Item> CreateShards(IGameContext gameContext, int amount)
    {
        var configuration = this.GetConfiguration();
        return CreateRewardItems(gameContext, [new IllusionRewardItem { ItemGroup = configuration.ShardItemGroup, ItemNumber = configuration.ShardItemNumber, MinimumAmount = amount, MaximumAmount = amount }]);
    }

    private static IllusionRewardItem ShardPrice(IllusionOfNoriaConfiguration configuration, int amount)
    {
        return new IllusionRewardItem { Name = "Illusion Shard", ItemGroup = configuration.ShardItemGroup, ItemNumber = configuration.ShardItemNumber, MinimumAmount = amount, MaximumAmount = amount };
    }

    /// <summary>
    /// Counts a kill of a monster: the boss counter of the illusion and the daily quests of the killer and its party.
    /// </summary>
    /// <param name="killer">The killer.</param>
    /// <param name="monster">The killed monster.</param>
    /// <param name="map">The map.</param>
    /// <param name="position">The position of the killed monster.</param>
    /// <returns>The task.</returns>
    public async ValueTask CountKillAsync(Player killer, MonsterDefinition monster, GameMap map, Point position)
    {
        var configuration = this.GetConfiguration();
        if (map.Definition.Number != configuration.MapNumber || monster.Number == configuration.BossNumber)
        {
            return;
        }

        var spawnBoss = false;
        lock (this._bossLock)
        {
            if (this._boss is null && ++this._killsSinceBoss >= Math.Max(1, configuration.BossKills))
            {
                this._killsSinceBoss = 0;
                spawnBoss = true;
            }
        }

        if (spawnBoss)
        {
            await this.SpawnBossAsync(killer.GameContext, map, configuration).ConfigureAwait(false);
        }

        var players = killer.Party?.PartyList.OfType<Player>()
                          .Where(p => p == killer || (p.CurrentMap == map && p.IsAlive && p.Position.EuclideanDistanceTo(position) <= configuration.PartyRange))
                          .ToList()
                      ?? [killer];
        foreach (var player in players)
        {
            await this.AddDailyKillAsync(player, monster, configuration).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Spawns the boss at a random place of the illusion, if it's not alive yet (also for the GM).
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <param name="map">The map of the illusion.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The task.</returns>
    public async ValueTask SpawnBossAsync(IGameContext gameContext, GameMap map, IllusionOfNoriaConfiguration configuration)
    {
        if (gameContext.Configuration.Monsters.FirstOrDefault(m => m.Number == configuration.BossNumber) is not { } definition
            || map.Terrain.RandomWalkableCoordinate is not { } point)
        {
            return;
        }

        var area = new MonsterSpawnArea
        {
            GameMap = map.Definition,
            MonsterDefinition = definition,
            SpawnTrigger = SpawnTrigger.OnceAtEventStart,
            Quantity = 1,
            X1 = point.X,
            X2 = point.X,
            Y1 = point.Y,
            Y2 = point.Y,
        };
        var boss = new Monster(area, definition, map, gameContext.DropGenerator, new BasicMonsterIntelligence(), gameContext.PlugInManager, gameContext.PathFinderPool);
        lock (this._bossLock)
        {
            if (this._boss is not null)
            {
                return;
            }

            this._boss = boss;
        }

        boss.Died += (_, death) =>
        {
            lock (this._bossLock)
            {
                if (this._boss == boss)
                {
                    this._boss = null;
                }
            }

            _ = Task.Run(() => this.ShowMapMessageAsync(gameContext, map, string.Format(configuration.BossDefeatedMessage, definition.Designation, death.KillerName)).AsTask());
        };

        boss.Initialize();
        await map.AddAsync(boss).ConfigureAwait(false);
        boss.OnSpawn();
        await this.ShowMapMessageAsync(gameContext, map, string.Format(configuration.BossAwakenedMessage, definition.Designation)).ConfigureAwait(false);
        _ = Task.Run(() => this.CurseLoopAsync(gameContext, boss, map, configuration));
    }

    /// <summary>
    /// Gets a value indicating whether the whistle drops for the killer (the quest is taken, the resets are there and the
    /// whistle is not in the inventory yet).
    /// </summary>
    /// <param name="player">The killer.</param>
    /// <returns><c>true</c>, if the whistle may drop.</returns>
    public bool CanGetWhistle(Player player)
    {
        var configuration = this.GetConfiguration();
        return (int)player.GetStoredStatValue(Stats.IllusionQuestState) == 1
               && GetResets(player) >= configuration.RequiredResets
               && this.FindWhistle(player) is null;
    }

    /// <summary>
    /// Creates the items of a reward with random amounts (stacks of up to the maximum durability).
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <param name="rewards">The reward items.</param>
    /// <returns>The items.</returns>
    internal static List<Item> CreateRewardItems(IGameContext gameContext, IEnumerable<IllusionRewardItem> rewards)
    {
        var items = new List<Item>();
        foreach (var reward in rewards)
        {
            if (gameContext.Configuration.Items.FirstOrDefault(d => d.Group == reward.ItemGroup && d.Number == reward.ItemNumber) is not { } definition)
            {
                continue;
            }

            var amount = Rand.NextInt(Math.Max(0, reward.MinimumAmount), Math.Max(reward.MinimumAmount, reward.MaximumAmount) + 1);
            var stack = Math.Max(1, (int)definition.Durability);
            while (amount > 0)
            {
                var pieces = Math.Min(amount, stack);
                items.Add(new TemporaryItem { Definition = definition, Durability = pieces });
                amount -= pieces;
            }
        }

        return items;
    }

    private static int GetResets(Player player) => (int)(player.Attributes?[Stats.Resets] ?? 0);

    private static IEnumerable<Item> StacksOf(Player player, IllusionRewardItem price)
    {
        return player.Inventory?.Items
            .Where(i => i.Definition?.Group == price.ItemGroup && i.Definition.Number == price.ItemNumber && i.ItemSlot >= InventoryConstants.EquippableSlotsCount)
            ?? [];
    }

    private static bool CanPay(Player player, IEnumerable<IllusionRewardItem> price)
    {
        return price.All(p => StacksOf(player, p).Sum(i => Math.Max(1, (int)i.Durability)) >= p.MinimumAmount);
    }

    private static byte AmountOf(IEnumerable<IllusionRewardItem> price, short number)
    {
        return (byte)Math.Clamp(price.Where(p => p.ItemGroup == 14 && p.ItemNumber == number).Sum(p => p.MinimumAmount), 0, 255);
    }

    private static async ValueTask TakeItemsAsync(Player player, IllusionRewardItem price)
    {
        var remaining = price.MinimumAmount;
        foreach (var stack in StacksOf(player, price).OrderBy(i => i.Durability).ToList())
        {
            if (remaining <= 0)
            {
                break;
            }

            var pieces = Math.Max(1, (int)stack.Durability);
            if (pieces <= remaining)
            {
                remaining -= pieces;
                await RemoveItemAsync(player, stack).ConfigureAwait(false);
                continue;
            }

            stack.Durability = pieces - remaining;
            remaining = 0;
            await player.InvokeViewPlugInAsync<IItemDurabilityChangedPlugIn>(p => p.ItemDurabilityChangedAsync(stack, true)).ConfigureAwait(false);
        }
    }

    private List<IllusionResetWeapon> GetResetWeapons(Player player, IllusionOfNoriaConfiguration configuration)
    {
        var result = new List<IllusionResetWeapon>();
        foreach (var slot in new[] { InventoryConstants.LeftHandSlot, InventoryConstants.RightHandSlot })
        {
            if (player.Inventory?.GetItem(slot) is not { Definition: { } definition } item
                || item.ItemOptions.FirstOrDefault(o => o.ItemOption?.OptionType == ItemOptionTypes.HarmonyOption) is not { ItemOption: { } option } link)
            {
                continue;
            }

            var (rank, price) = configuration.GetResetPrice(definition.Group, definition.Number);
            result.Add(new IllusionResetWeapon(
                slot,
                definition.Group,
                definition.Number,
                (byte)option.Number,
                (byte)link.Level,
                (byte)rank,
                AmountOf(price, 42),
                AmountOf(price, 43),
                AmountOf(price, 44),
                CanPay(player, price)));
        }

        return result;
    }

    private async ValueTask ResetOptionAsync(Player player, byte slot, IllusionOfNoriaConfiguration configuration)
    {
        if (player.Inventory?.GetItem(slot) is not { Definition: { } definition } item
            || item.ItemOptions.FirstOrDefault(o => o.ItemOption?.OptionType == ItemOptionTypes.HarmonyOption) is not { } link)
        {
            return;
        }

        var (_, price) = configuration.GetResetPrice(definition.Group, definition.Number);
        if (!CanPay(player, price))
        {
            await player.ShowBlueMessageAsync("Warden Eldrin: you don't carry the price of the reset.").ConfigureAwait(false);
            return;
        }

        foreach (var part in price.Where(p => p.MinimumAmount > 0))
        {
            await TakeItemsAsync(player, part).ConfigureAwait(false);
        }

        item.ItemOptions.Remove(link);
        await player.PersistenceContext.DeleteAsync(link).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IItemUpgradedPlugIn>(p => p.ItemUpgradedAsync(item)).ConfigureAwait(false);
        await player.ShowBlueMessageAsync($"Warden Eldrin: the harmony option of {definition.Name} is gone - a new Jewel of Illusion can be set.").ConfigureAwait(false);
    }

    private static async ValueTask RemoveItemAsync(Player player, Item item)
    {
        var slot = item.ItemSlot;
        await player.Inventory!.RemoveItemAsync(item).ConfigureAwait(false);
        await player.InvokeViewPlugInAsync<IItemRemovedPlugIn>(p => p.RemoveItemAsync(slot)).ConfigureAwait(false);
        await player.PersistenceContext.DeleteAsync(item).ConfigureAwait(false);
    }

    private static async ValueTask GiveItemAsync(Player player, Item item)
    {
        // With the slot, the inventory makes the temporary item persistent; the stored item is the one to show.
        if (player.Inventory is { } inventory
            && inventory.CheckInvSpace(item) is { } slot
            && await inventory.AddItemAsync(slot, item).ConfigureAwait(false)
            && inventory.GetItem(slot) is { } stored)
        {
            await player.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(stored)).ConfigureAwait(false);
            return;
        }

        if (player.CurrentMap is { } map)
        {
            await map.AddAsync(new DroppedItem(item, player.RandomPosition, map, player, player.GetAsEnumerable())).ConfigureAwait(false);
        }
    }

    private Item? FindWhistle(Player player)
    {
        var configuration = this.GetConfiguration();
        return player.Inventory?.Items.FirstOrDefault(i => i.Definition?.Group == configuration.WhistleItemGroup && i.Definition.Number == configuration.WhistleItemNumber);
    }

    private async ValueTask WarpAsync(Player player, short mapNumber)
    {
        var gate = player.GameContext.Configuration.Maps.FirstOrDefault(m => m.Number == mapNumber)?.ExitGates.Where(g => g.IsSpawnGate).SelectRandom();
        if (gate is null)
        {
            player.Logger.LogWarning("Illusion of Noria: no spawn gate on map {map}.", mapNumber);
            return;
        }

        await player.WarpToAsync(gate).ConfigureAwait(false);
    }

    private (byte State, IReadOnlyList<short> Monsters, IReadOnlyList<int> Kills) GetDaily(Player player, IllusionOfNoriaConfiguration configuration, DateTime now)
    {
        var today = configuration.GetDayNumber(now);
        if ((int)player.GetStoredStatValue(Stats.IllusionDailyClaimedDay) == today)
        {
            return (3, [], []);
        }

        if ((int)player.GetStoredStatValue(Stats.IllusionDailyDay) != today)
        {
            return (0, [], []);
        }

        var all = configuration.GetMonsterNumbers();
        var encoded = (int)player.GetStoredStatValue(Stats.IllusionDailyMonsters);
        var monsters = new List<short>();
        var kills = new List<int>();
        AttributeDefinition[] killStats = [Stats.IllusionDailyKills1, Stats.IllusionDailyKills2, Stats.IllusionDailyKills3];
        for (var i = 0; i < killStats.Length; i++)
        {
            var index = ((encoded >> (4 * i)) & 0xF) - 1;
            if (index < 0 || index >= all.Count)
            {
                continue;
            }

            monsters.Add(all[index]);
            kills.Add((int)player.GetStoredStatValue(killStats[i]));
        }

        var done = monsters.Count > 0 && kills.All(k => k >= configuration.DailyKills);
        return (done ? (byte)2 : (byte)1, monsters, kills);
    }

    private async ValueTask TakeDailyAsync(Player player, IllusionOfNoriaConfiguration configuration)
    {
        var now = DateTime.Now;
        if (this.GetDaily(player, configuration, now).State != 0)
        {
            return;
        }

        var all = configuration.GetMonsterNumbers();
        var kinds = Math.Clamp(configuration.DailyKinds, 1, Math.Min(3, Math.Min(all.Count, 15)));
        var picked = Enumerable.Range(0, Math.Min(all.Count, 15)).OrderBy(_ => Rand.NextDouble()).Take(kinds).ToList();
        var encoded = 0;
        for (var i = 0; i < picked.Count; i++)
        {
            encoded |= (picked[i] + 1) << (4 * i);
        }

        player.TrySetStoredStatValue(Stats.IllusionDailyDay, configuration.GetDayNumber(now));
        player.TrySetStoredStatValue(Stats.IllusionDailyMonsters, encoded);
        player.TrySetStoredStatValue(Stats.IllusionDailyKills1, 0);
        player.TrySetStoredStatValue(Stats.IllusionDailyKills2, 0);
        player.TrySetStoredStatValue(Stats.IllusionDailyKills3, 0);
        var names = picked.Select(i => this.GetMonsterName(player, all[i]));
        await player.ShowBlueMessageAsync($"Daily quest: kill {configuration.DailyKills} each of {string.Join(", ", names)}.").ConfigureAwait(false);
    }

    private async ValueTask ClaimDailyAsync(Player player, IllusionOfNoriaConfiguration configuration)
    {
        var now = DateTime.Now;
        if (this.GetDaily(player, configuration, now).State != 2)
        {
            return;
        }

        var items = this.CreateShards(player.GameContext, configuration.DailyShards);
        if (player.Inventory is not { } inventory || items.Any(item => inventory.CheckInvSpace(item) is null))
        {
            await player.ShowBlueMessageAsync("Warden Eldrin: make room in your inventory for the reward first.").ConfigureAwait(false);
            return;
        }

        player.TrySetStoredStatValue(Stats.IllusionDailyClaimedDay, configuration.GetDayNumber(now));
        foreach (var item in items)
        {
            await GiveItemAsync(player, item).ConfigureAwait(false);
        }

        await player.ShowBlueMessageAsync($"Daily quest done: +{configuration.DailyShards} Illusion Shards. Come back tomorrow.").ConfigureAwait(false);
    }

    private IEnumerable<IllusionRewardItem> GetShop(Player player, IllusionOfNoriaConfiguration configuration)
    {
        // an Echo only for the characters who know its skill
        return configuration.Shop.Where(e => configuration.GetEchoSkill(e.ItemGroup, e.ItemNumber) is not { } skill
                                             || player.SkillList?.ContainsSkill((ushort)skill) == true);
    }

    private async ValueTask BuyAsync(Player player, byte group, short number, IllusionOfNoriaConfiguration configuration)
    {
        if (this.GetShop(player, configuration).FirstOrDefault(e => e.ItemGroup == group && e.ItemNumber == number) is not { } entry
            || player.GameContext.Configuration.Items.FirstOrDefault(d => d.Group == entry.ItemGroup && d.Number == entry.ItemNumber) is not { } definition)
        {
            return;
        }

        var price = ShardPrice(configuration, Math.Max(1, entry.MinimumAmount));
        var shards = StacksOf(player, price).Sum(i => Math.Max(1, (int)i.Durability));
        if (shards < price.MinimumAmount)
        {
            await player.ShowBlueMessageAsync($"Warden Eldrin: {definition.Name} costs {price.MinimumAmount} Illusion Shards, you have {shards}.").ConfigureAwait(false);
            return;
        }

        var item = new TemporaryItem { Definition = definition, Durability = 1 };
        if (player.Inventory is not { } inventory || inventory.CheckInvSpace(item) is null)
        {
            await player.ShowBlueMessageAsync("Warden Eldrin: make room in your inventory first.").ConfigureAwait(false);
            return;
        }

        await TakeItemsAsync(player, price).ConfigureAwait(false);
        await GiveItemAsync(player, item).ConfigureAwait(false);
        await player.ShowBlueMessageAsync($"Warden Eldrin: {definition.Name} for {price.MinimumAmount} Illusion Shards.").ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the consume handler of an Echo, or <c>null</c> if the item is no Echo.
    /// </summary>
    /// <param name="player">The player who uses the item.</param>
    /// <param name="item">The item.</param>
    /// <returns>The handler.</returns>
    internal PlayerActions.ItemConsumeActions.EchoOfIllusionConsumeHandler? GetEchoHandler(Player player, Item item)
    {
        if (item.Definition is not { } definition
            || this.GetConfiguration().GetEchoSkill(definition.Group, definition.Number) is not { } skill
            || player.GameContext.FeaturePlugIns.GetPlugIn<SkillCastTimePlugIn>()?.Configuration is not { } castTime)
        {
            return null;
        }

        var family = (player.SelectedCharacter?.CharacterClass?.Number ?? 0) / 4 * 4;
        return new PlayerActions.ItemConsumeActions.EchoOfIllusionConsumeHandler(skill, family, castTime.Weapons);
    }

    private async ValueTask AddDailyKillAsync(Player player, MonsterDefinition monster, IllusionOfNoriaConfiguration configuration)
    {
        var (state, monsters, kills) = this.GetDaily(player, configuration, DateTime.Now);
        if (state != 1)
        {
            return;
        }

        AttributeDefinition[] killStats = [Stats.IllusionDailyKills1, Stats.IllusionDailyKills2, Stats.IllusionDailyKills3];
        for (var i = 0; i < monsters.Count; i++)
        {
            if (monsters[i] != monster.Number || kills[i] >= configuration.DailyKills)
            {
                continue;
            }

            var count = kills[i] + 1;
            player.TrySetStoredStatValue(killStats[i], count);
            if (count == configuration.DailyKills)
            {
                var allDone = kills.Where((_, k) => k != i).All(k => k >= configuration.DailyKills);
                await player.ShowBlueMessageAsync(allDone
                    ? "Daily quest completed! Warden Eldrin waits in the town of the illusion."
                    : $"Daily quest: {monster.Designation} {count}/{configuration.DailyKills} - done.").ConfigureAwait(false);
            }
            else if (count % ProgressMessageStep == 0)
            {
                await player.ShowBlueMessageAsync($"Daily quest: {monster.Designation} {count}/{configuration.DailyKills}").ConfigureAwait(false);
            }

            return;
        }
    }

    private string GetMonsterName(Player player, short number)
    {
        return player.GameContext.Configuration.Monsters.FirstOrDefault(m => m.Number == number)?.Designation ?? $"#{number}";
    }

    private async ValueTask ShowMapMessageAsync(IGameContext gameContext, GameMap map, string message)
    {
        await gameContext.ForEachPlayerAsync(p => p.CurrentMap == map
            ? p.InvokeViewPlugInAsync<IShowMessagePlugIn>(v => v.ShowMessageAsync(message, MessageType.GoldenCenter)).AsTask()
            : Task.CompletedTask).ConfigureAwait(false);
    }

    private async Task CurseLoopAsync(IGameContext gameContext, Monster boss, GameMap map, IllusionOfNoriaConfiguration configuration)
    {
        if (gameContext.Configuration.MagicEffects.FirstOrDefault(m => m.Number == configuration.CurseEffectNumber) is not { } definition)
        {
            gameContext.LoggerFactory.CreateLogger<IllusionOfNoriaPlugIn>().LogWarning("Illusion of Noria: the magic effect {effect} of the curse is missing.", configuration.CurseEffectNumber);
            return;
        }

        try
        {
            while (boss.IsAlive && boss.CurrentMap == map)
            {
                foreach (var player in map.GetAttackablesInRange(boss.Position, configuration.CurseRange).OfType<Player>().Where(p => p.IsAlive).ToList())
                {
                    // the same effect renews its time while the player stays near; the first one keeps burning
                    var curse = new GoldenCurseMagicEffect(definition, configuration.CurseDuration, boss, player, configuration.CurseDamagePerSecond);
                    await player.MagicEffectList.AddEffectAsync(curse).ConfigureAwait(false);
                }

                await Task.Delay(1000).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            gameContext.LoggerFactory.CreateLogger<IllusionOfNoriaPlugIn>().LogError(ex, "Illusion of Noria: error in the curse of the boss.");
        }
    }
}
