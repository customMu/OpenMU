// <copyright file="KillQuestsPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.KillQuests;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.Character;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A chain of quests "kill N monsters of a kind", one quest at a time per character, without an NPC: the first quests
/// give the starter gear of the class (+0), the following ones stat points, which stay after resets (a reset with
/// "Replace points per reset" adds them to the points of the resets). The progress is stored with the character
/// (<see cref="Stats.KillQuestStep"/>, <see cref="Stats.KillQuestKills"/>, <see cref="Stats.KillQuestPoints"/>,
/// <see cref="Stats.KillQuestRewardWaiting"/>) and shown in the quest window of the client (key T).
/// A quest is handed in when its reward is given: an item reward without space in the inventory waits (the chain
/// doesn't go on) until the player takes it in the quest window.
/// </summary>
[PlugIn]
[Display(Name = "Kill quests", Description = "A chain of quests 'kill N monsters': starter gear first, then stat points which stay after resets. Quest window: T; /quest shows the current quest.")]
[Guid("3F6B9D21-7C48-4E5A-9B13-D2E8A4C7F059")]
public class KillQuestsPlugIn : IFeaturePlugIn, ISupportCustomConfiguration<KillQuestsConfiguration>, ISupportDefaultCustomConfiguration
{
    private static readonly TimeSpan ReminderInterval = TimeSpan.FromMinutes(3);

    private readonly ConditionalWeakTable<Player, StrongBox<DateTime>> _lastReminders = new();

    /// <summary>
    /// Gets or sets the configuration.
    /// </summary>
    public KillQuestsConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new KillQuestsConfiguration();

    /// <summary>
    /// Gets the stat points of the completed quests of the player, which a reset keeps.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The points.</returns>
    public static int GetQuestPoints(Player player) => (int)player.GetStoredStatValue(Stats.KillQuestPoints);

    /// <summary>
    /// Counts the kill of the monster for the killer and the party members around it.
    /// </summary>
    /// <param name="killer">The player who got the kill (the owner of the drop).</param>
    /// <param name="monster">The monster definition.</param>
    /// <param name="map">The map.</param>
    /// <param name="position">The position of the killed monster.</param>
    /// <returns>The task.</returns>
    public async ValueTask CountKillAsync(Player killer, MonsterDefinition monster, GameMap map, Point position)
    {
        var configuration = this.GetConfiguration();
        var players = killer.Party?.PartyList.OfType<Player>()
                          .Where(p => p == killer || (p.CurrentMap == map && p.IsAlive && p.Position.EuclideanDistanceTo(position) <= configuration.PartyRange))
                          .ToList()
                      ?? [killer];
        foreach (var player in players)
        {
            await this.AddKillAsync(player, monster, configuration).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Shows the current quest and its progress to the player: in the quest window and in the chat.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The task.</returns>
    public async ValueTask ShowCurrentQuestAsync(Player player)
    {
        await this.SendStateAsync(player).ConfigureAwait(false);
        var quests = this.GetConfiguration().Quests.ToList();
        var step = (int)player.GetStoredStatValue(Stats.KillQuestStep);
        if (step >= quests.Count)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KillQuestAllDoneFormat), GetQuestPoints(player)).ConfigureAwait(false);
            return;
        }

        var quest = quests[step];
        if (IsRewardWaiting(player))
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KillQuestRewardReminderFormat), MonsterNameFor(player, quest)).ConfigureAwait(false);
            return;
        }

        var kills = (int)player.GetStoredStatValue(Stats.KillQuestKills);
        await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KillQuestCurrentFormat), step + 1, quests.Count, quest.Kills, MonsterNameFor(player, quest), kills, this.DescribeReward(player, quest)).ConfigureAwait(false);
    }

    /// <summary>
    /// Gives the waiting reward of the current quest, when the inventory has space for it now (quest window, key T).
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The task.</returns>
    public async ValueTask ClaimRewardAsync(Player player)
    {
        var configuration = this.GetConfiguration();
        var quests = configuration.Quests.ToList();
        var step = (int)player.GetStoredStatValue(Stats.KillQuestStep);
        if (!IsRewardWaiting(player) || step >= quests.Count)
        {
            await this.SendStateAsync(player).ConfigureAwait(false);
            return;
        }

        await this.TryHandInAsync(player, step, quests[step], configuration).ConfigureAwait(false);
    }

    private static bool IsRewardWaiting(Player player) => player.GetStoredStatValue(Stats.KillQuestRewardWaiting) > 0;

    /// <summary>
    /// Gets the variant of the quest for the home map of the class of the player (elf: Noria, summoner: Elvenland).
    /// </summary>
    private static KillQuestVariant? GetVariant(Player player, KillQuest quest)
        => player.SelectedCharacter?.CharacterClass?.HomeMap is { } homeMap
            ? quest.Variants.FirstOrDefault(v => v.HomeMapNumber == homeMap.Number)
            : null;

    /// <summary>
    /// Gets the monster name of the quest as shown to the player: the one of the variant of the home map, if any.
    /// </summary>
    private static string MonsterNameFor(Player player, KillQuest quest)
    {
        var variant = GetVariant(player, quest);
        var name = variant?.MonsterName ?? quest.MonsterName;
        var location = variant is not null ? variant.Location : quest.Location;
        return string.IsNullOrWhiteSpace(location) ? name : $"{name} ({location})";
    }

    /// <summary>
    /// Determines whether the killed monster counts for the quest: the monster of the quest or of its variant for the
    /// home map of the player, or another definition with the same name and level (the normal and the elite monsters of Vulcanus).
    /// </summary>
    private static bool Counts(Player player, KillQuest quest, MonsterDefinition killed, GameConfiguration configuration)
    {
        if (killed.Number == quest.MonsterNumber || killed.Number == GetVariant(player, quest)?.MonsterNumber)
        {
            return true;
        }

        return killed.Designation == quest.MonsterName
               && configuration.Monsters.FirstOrDefault(m => m.Number == quest.MonsterNumber) is { } questMonster
               && questMonster[Stats.Level] == killed[Stats.Level];
    }

    private KillQuestsConfiguration GetConfiguration() => this.Configuration ??= new KillQuestsConfiguration();

    private async ValueTask AddKillAsync(Player player, MonsterDefinition monster, KillQuestsConfiguration configuration)
    {
        if (player.SelectedCharacter is null)
        {
            return;
        }

        if (IsRewardWaiting(player))
        {
            await this.RemindAsync(player, configuration).ConfigureAwait(false);
            return;
        }

        var quests = configuration.Quests.ToList();
        KillQuest quest;
        int step;
        int kills;
        lock (player)
        {
            step = (int)player.GetStoredStatValue(Stats.KillQuestStep);
            if (step >= quests.Count || !Counts(player, quests[step], monster, player.GameContext.Configuration))
            {
                return;
            }

            quest = quests[step];
            kills = Math.Min(quest.Kills, (int)player.GetStoredStatValue(Stats.KillQuestKills) + 1);
            if (!player.TrySetStoredStatValue(Stats.KillQuestKills, kills))
            {
                return;
            }
        }

        if (kills < quest.Kills)
        {
            var every = Math.Max(1, (int)Math.Round(quest.Kills * Math.Clamp(configuration.ProgressMessageShare, 0.01f, 1f)));
            if (kills % every == 0 || quest.Kills - kills <= 5)
            {
                await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KillQuestProgressFormat), MonsterNameFor(player, quest), kills, quest.Kills).ConfigureAwait(false);
            }

            await this.SendStateAsync(player).ConfigureAwait(false);
            return;
        }

        await this.TryHandInAsync(player, step, quest, configuration).ConfigureAwait(false);
    }

    /// <summary>
    /// Hands the completed quest in: gives its reward and starts the next quest, or, when the items of the reward don't
    /// fit into the inventory, keeps the quest with a waiting reward.
    /// </summary>
    private async ValueTask TryHandInAsync(Player player, int step, KillQuest quest, KillQuestsConfiguration configuration)
    {
        var items = this.GetRewardItems(player, quest, configuration);
        var money = this.GetRewardMoney(player, quest, configuration) + Math.Max(0, quest.Money);
        if (items.Count > 0 && !HasSpace(player, items))
        {
            player.TrySetStoredStatValue(Stats.KillQuestRewardWaiting, 1);
            this._lastReminders.GetValue(player, _ => new StrongBox<DateTime>(DateTime.MinValue)).Value = DateTime.UtcNow; // the next reminder in ReminderInterval
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KillQuestRewardWaitingFormat), MonsterNameFor(player, quest)).ConfigureAwait(false);
            await this.SendStateAsync(player).ConfigureAwait(false);
            return;
        }

        foreach (var (gear, definition) in items)
        {
            await GiveItemAsync(player, CreateItem(gear, definition)).ConfigureAwait(false);
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KillQuestItemRewardFormat), MonsterNameFor(player, quest), Describe(gear, definition)).ConfigureAwait(false);
        }

        if (money > 0 && player.TryAddMoney(money))
        {
            await player.InvokeViewPlugInAsync<IUpdateMoneyPlugIn>(p => p.UpdateMoneyAsync()).ConfigureAwait(false);
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KillQuestItemRewardFormat), MonsterNameFor(player, quest), DescribeMoney(money)).ConfigureAwait(false);
        }

        if (quest.StatPoints > 0 && player.SelectedCharacter is { } character)
        {
            player.TrySetStoredStatValue(Stats.KillQuestPoints, GetQuestPoints(player) + quest.StatPoints);
            character.LevelUpPoints += quest.StatPoints;
            await player.InvokeViewPlugInAsync<IUpdateLevelPlugIn>(p => p.UpdateLevelAsync()).ConfigureAwait(false);
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KillQuestPointsRewardFormat), MonsterNameFor(player, quest), quest.StatPoints, GetQuestPoints(player)).ConfigureAwait(false);
        }

        lock (player)
        {
            player.TrySetStoredStatValue(Stats.KillQuestStep, step + 1);
            player.TrySetStoredStatValue(Stats.KillQuestKills, 0);
            player.TrySetStoredStatValue(Stats.KillQuestRewardWaiting, 0);
        }

        player.Logger.LogInformation("Kill quest {Step} ({Quest}) completed by {Character}.", step + 1, quest, player.SelectedCharacter?.Name);
        await this.ShowCurrentQuestAsync(player).ConfigureAwait(false);
    }

    private async ValueTask RemindAsync(Player player, KillQuestsConfiguration configuration)
    {
        var last = this._lastReminders.GetValue(player, _ => new StrongBox<DateTime>(DateTime.MinValue));
        if (DateTime.UtcNow - last.Value < ReminderInterval)
        {
            return;
        }

        last.Value = DateTime.UtcNow;
        var quests = configuration.Quests.ToList();
        var step = (int)player.GetStoredStatValue(Stats.KillQuestStep);
        if (step < quests.Count)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KillQuestRewardReminderFormat), MonsterNameFor(player, quests[step])).ConfigureAwait(false);
        }
    }

    private async ValueTask SendStateAsync(Player player)
    {
        var quests = this.GetConfiguration().Quests.ToList();
        var step = (int)player.GetStoredStatValue(Stats.KillQuestStep);
        var state = step < quests.Count
            ? new KillQuestState(step + 1, quests.Count, MonsterNameFor(player, quests[step]), (int)player.GetStoredStatValue(Stats.KillQuestKills), quests[step].Kills,
                quests[step].StatPoints, this.DescribeItems(player, quests[step]), IsRewardWaiting(player), GetQuestPoints(player))
            : new KillQuestState(quests.Count + 1, quests.Count, string.Empty, 0, 0, 0, string.Empty, false, GetQuestPoints(player));
        await player.InvokeViewPlugInAsync<IKillQuestViewPlugIn>(p => p.ShowKillQuestAsync(state)).ConfigureAwait(false);
    }

    private List<(KillQuestGearItem Gear, ItemDefinition Definition)> GetRewardItems(Player player, KillQuest quest, KillQuestsConfiguration configuration)
    {
        if (quest.GearStep <= 0 || player.SelectedCharacter?.CharacterClass is not { } characterClass)
        {
            return [];
        }

        var baseClass = characterClass.Number & ~3;
        return configuration.Gear
            .Where(g => g.ClassNumber == baseClass && g.Step == quest.GearStep && g.Money <= 0)
            .Select(g => (Gear: g, Definition: player.GameContext.Configuration.Items.FirstOrDefault(d => d.Group == g.ItemGroup && d.Number == g.ItemNumber)))
            .Where(p => p.Definition is not null)
            .Select(p => (p.Gear, p.Definition!))
            .ToList();
    }

    private int GetRewardMoney(Player player, KillQuest quest, KillQuestsConfiguration configuration)
    {
        if (quest.GearStep <= 0 || player.SelectedCharacter?.CharacterClass is not { } characterClass)
        {
            return 0;
        }

        var baseClass = characterClass.Number & ~3;
        return configuration.Gear.Where(g => g.ClassNumber == baseClass && g.Step == quest.GearStep).Sum(g => Math.Max(0, g.Money));
    }

    private static string DescribeMoney(int money) => $"{money:N0} Zen";

    private static Item CreateItem(KillQuestGearItem gear, ItemDefinition definition)
    {
        var item = new TemporaryItem { Definition = definition };
        if (gear.Luck && FindLuck(definition) is { } luck)
        {
            item.ItemOptions.Add(new ItemOptionLink { ItemOption = luck });
        }

        item.HasSkill = gear.Skill && item.CanHaveSkill();
        item.Durability = item.GetMaximumDurabilityOfOnePiece();
        return item;
    }

    private static IncreasableItemOption? FindLuck(ItemDefinition definition)
        => definition.PossibleItemOptions
            .SelectMany(o => o.PossibleOptions)
            .FirstOrDefault(o => object.Equals(o.OptionType, ItemOptionTypes.Luck));

    /// <summary>
    /// The name of the reward item with what it really comes with: "Short Bow +Skill +Luck".
    /// </summary>
    private static string Describe(KillQuestGearItem gear, ItemDefinition definition)
    {
        var text = definition.Name.ValueInNeutralLanguage;
        if (gear.Skill && definition.Skill is not null)
        {
            text += " +Skill";
        }

        if (gear.Luck && FindLuck(definition) is not null)
        {
            text += " +Luck";
        }

        return text;
    }

    private static bool HasSpace(Player player, IReadOnlyList<(KillQuestGearItem Gear, ItemDefinition Definition)> items)
    {
        // each item on its own free place: the reward is one item, or the bow with the arrows
        if (player.Inventory is not { } inventory)
        {
            return false;
        }

        return items.All(p => inventory.CheckInvSpace(new TemporaryItem { Definition = p.Definition }) is not null);
    }

    private string DescribeItems(Player player, KillQuest quest)
    {
        var configuration = this.GetConfiguration();
        var parts = this.GetRewardItems(player, quest, configuration).Select(p => Describe(p.Gear, p.Definition)).ToList();
        if (this.GetRewardMoney(player, quest, configuration) + Math.Max(0, quest.Money) is > 0 and var money)
        {
            parts.Add(DescribeMoney(money));
        }

        return string.Join(", ", parts);
    }

    private string DescribeReward(Player player, KillQuest quest)
    {
        var parts = new List<string>();
        var items = this.DescribeItems(player, quest);
        if (items.Length > 0)
        {
            parts.Add(items);
        }

        if (quest.StatPoints > 0)
        {
            parts.Add($"+{quest.StatPoints} stat points");
        }

        return parts.Count == 0 ? "-" : string.Join(", ", parts);
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
}

/// <summary>
/// The state of the kill quests of a character, for the quest window of the client.
/// </summary>
/// <param name="Number">The number of the current quest, from 1; the count + 1 when all quests are done.</param>
/// <param name="Count">The number of quests.</param>
/// <param name="MonsterName">The monster of the current quest.</param>
/// <param name="Kills">The kills so far.</param>
/// <param name="KillsNeeded">The kills needed.</param>
/// <param name="RewardPoints">The stat points of the reward.</param>
/// <param name="RewardText">The items of the reward.</param>
/// <param name="RewardWaiting">A value indicating whether the quest is completed, but its reward waits for space in the inventory.</param>
/// <param name="QuestPoints">The stat points from all completed quests.</param>
public sealed record KillQuestState(int Number, int Count, string MonsterName, int Kills, int KillsNeeded, int RewardPoints, string RewardText, bool RewardWaiting, int QuestPoints);

/// <summary>
/// Counts the kills for the <see cref="KillQuestsPlugIn"/> (once per kill, for the owner of the drop and its party).
/// </summary>
[PlugIn]
[Display(Name = "Kill quests: kill counter", Description = "Counts the kills of monsters for the kill quests.")]
[Guid("8D2E5A17-4B9C-4F63-A0D8-6E1C3B7F9A42")]
public class KillQuestsKillCounterPlugIn : IAdditionalItemDropPlugIn
{
    /// <inheritdoc />
    public async ValueTask AddItemDropsAsync(AdditionalItemDropArgs args)
    {
        if (args.KilledObject.IsSummonedMonster
            || args.Monster.ObjectKind != NpcObjectKind.Monster
            || args.Killer.CurrentMiniGame is not null
            || args.Killer.GameContext.FeaturePlugIns.GetPlugIn<KillQuestsPlugIn>() is not { } quests)
        {
            return;
        }

        await quests.CountKillAsync(args.Killer, args.Monster, args.Map, args.KilledObject.Position).ConfigureAwait(false);
    }
}

/// <summary>
/// Shows the current kill quest when a character enters the world.
/// </summary>
[PlugIn]
[Display(Name = "Kill quests: reminder", Description = "Shows the current kill quest when a character enters the world.")]
[Guid("C5A1E8F3-2D7B-4A96-8E04-B9F3D6C2A715")]
public class KillQuestsReminderPlugIn : IPlayerStateChangedPlugIn
{
    /// <inheritdoc />
    public async ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
    {
        if (previousState != PlayerState.CharacterSelection || currentState != PlayerState.EnteredWorld
            || player.GameContext.FeaturePlugIns.GetPlugIn<KillQuestsPlugIn>() is not { } quests)
        {
            return;
        }

        await quests.ShowCurrentQuestAsync(player).ConfigureAwait(false);
    }
}

/// <summary>
/// The chat command /quest: shows the current kill quest.
/// </summary>
[PlugIn]
[Display(Name = "Kill quests: /quest", Description = "Shows the current kill quest and its progress: /quest")]
[Guid("E4B7C2A9-6F15-4D38-B1E7-3A9D5C8F2B06")]
[ChatCommandHelp(Command, CharacterStatus.Normal)]
public class KillQuestsChatCommandPlugIn : IChatCommandPlugIn
{
    private const string Command = "/quest";

    /// <inheritdoc />
    public string Key => Command;

    /// <inheritdoc />
    public CharacterStatus MinCharacterStatusRequirement => CharacterStatus.Normal;

    /// <inheritdoc />
    public async ValueTask HandleCommandAsync(Player player, string command)
    {
        if (player.GameContext.FeaturePlugIns.GetPlugIn<KillQuestsPlugIn>() is { } quests)
        {
            await quests.ShowCurrentQuestAsync(player).ConfigureAwait(false);
        }
    }
}
