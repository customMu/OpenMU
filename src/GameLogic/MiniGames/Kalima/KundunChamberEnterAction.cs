// <copyright file="KundunChamberEnterAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Kalima;

using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlayerActions.MiniGames;
using MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;
using MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>
/// The action to enter the chamber of Kundun of the party (or the own one, without a party).
/// Each character pays the entry with a Lost Map of the level of the chamber and has a weekly limit of entries.
/// </summary>
public class KundunChamberEnterAction
{
    private readonly KundunChamberConfiguration _configuration;
    private readonly KalimaInstanceConfiguration _kalimaConfiguration;

    /// <summary>
    /// Initializes a new instance of the <see cref="KundunChamberEnterAction"/> class.
    /// </summary>
    /// <param name="configuration">The configuration of the chamber.</param>
    /// <param name="kalimaConfiguration">The configuration of the Kalima instance, which defines the tiers and their reset ranges.</param>
    public KundunChamberEnterAction(KundunChamberConfiguration configuration, KalimaInstanceConfiguration kalimaConfiguration)
    {
        this._configuration = configuration;
        this._kalimaConfiguration = kalimaConfiguration;
    }

    /// <summary>
    /// Tries to enter the chamber of Kundun. An already running chamber of the party is entered,
    /// otherwise a new one is created for the tier whose reset range contains the resets of the player;
    /// all party members have to be in this range.
    /// </summary>
    /// <param name="player">The player.</param>
    public async ValueTask TryEnterAsync(Player player)
    {
        if (player.SelectedCharacter is null || player.CurrentMiniGame is not null || player.DuelRoom is not null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KundunChamberNotAvailable)).ConfigureAwait(false);
            return;
        }

        var owner = player.Party?.PartyMaster?.Name ?? player.Name;
        var chamber = await player.GameContext.FindMiniGameAsync(MiniGameType.KundunChamber, owner).ConfigureAwait(false) as KundunChamberContext;
        if (chamber is { IsAcceptingPlayers: true })
        {
            await this.EnterExistingChamberAsync(player, chamber).ConfigureAwait(false);
            return;
        }

        await this.EnterNewChamberAsync(player).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the information for the entry dialog of the keeper.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The information.</returns>
    public async ValueTask<KundunChamberInfo> GetInfoAsync(Player player)
    {
        var resets = GetResets(player);
        var tier = this._kalimaConfiguration.GetTierByResets(resets);
        var tiers = this._kalimaConfiguration.Tiers
            .OrderBy(t => t.MinimumResets)
            .Select(t => ((byte)t.Level, t.MinimumResets, this._kalimaConfiguration.GetMaximumResets(t)))
            .ToList();
        var owner = player.Party?.PartyMaster?.Name ?? player.Name;
        var chamber = await player.GameContext.FindMiniGameAsync(MiniGameType.KundunChamber, owner).ConfigureAwait(false) as KundunChamberContext;
        var canReenter = chamber is { IsAcceptingPlayers: true } && chamber.IsRegistered(player);
        return new KundunChamberInfo(
            tiers,
            resets,
            (byte)(tier?.Level ?? 0),
            Math.Max(0, this.GetEntriesPerWeek(player) - this.GetUsedEntries(player)),
            this.GetEntriesPerWeek(player),
            this._configuration.GetTimeUntilNextReset(DateTime.UtcNow),
            canReenter,
            tier is not null && FindLostMap(player, tier.Level) is not null);
    }

    private static int GetResets(Player player) => (int)(player.Attributes?[Stats.Resets] ?? 0);

    private static Item? FindLostMap(Player player, int level)
    {
        return player.Inventory?.Items.FirstOrDefault(item => item.IsLostMap() && item.Level == level);
    }

    private async ValueTask EnterExistingChamberAsync(Player player, KundunChamberContext chamber)
    {
        if (chamber.IsRegistered(player))
        {
            // Entering the own chamber again (e.g. after a death) is free.
            await this.EnterAsync(player, chamber, false).ConfigureAwait(false);
            return;
        }

        if (!this.IsInRange(player, chamber.Tier))
        {
            await this.ShowNotInRangeAsync(player, player, chamber.Tier).ConfigureAwait(false);
            return;
        }

        if (!await this.CheckEntryAsync(player, chamber.Tier).ConfigureAwait(false))
        {
            return;
        }

        await this.EnterAsync(player, chamber, true).ConfigureAwait(false);
    }

    private async ValueTask EnterNewChamberAsync(Player player)
    {
        var tier = this._kalimaConfiguration.GetTierByResets(GetResets(player));
        if (tier is null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KundunChamberRequiresResetsFormat), this._kalimaConfiguration.GetMinimumResets()).ConfigureAwait(false);
            return;
        }

        var membersNotInRange = player.Party?.PartyList.OfType<Player>().Where(member => !this.IsInRange(member, tier)).ToList() ?? [];
        if (membersNotInRange.Count > 0)
        {
            foreach (var member in membersNotInRange)
            {
                await this.ShowNotInRangeAsync(player, member, tier).ConfigureAwait(false);
            }

            return;
        }

        var definition = player.GameContext.Configuration.MiniGameDefinitions
            .FirstOrDefault(d => d.Type == MiniGameType.KundunChamber && d.GameLevel == tier.Level);
        if (definition?.Entrance is null)
        {
            player.Logger.LogWarning("The mini game definition of the chamber of Kundun {level} is missing. Apply the data update which adds it.", tier.Level);
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KundunChamberNotAvailable)).ConfigureAwait(false);
            return;
        }

        if (!await this.CheckEntryAsync(player, tier).ConfigureAwait(false))
        {
            return;
        }

        if (await player.GameContext.GetMiniGameAsync(definition, player).ConfigureAwait(false) is not KundunChamberContext chamber)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KundunChamberNotAvailable)).ConfigureAwait(false);
            return;
        }

        await this.EnterAsync(player, chamber, !chamber.IsRegistered(player)).ConfigureAwait(false);
    }

    private bool IsInRange(Player player, KalimaInstanceTier tier)
    {
        return this._kalimaConfiguration.GetTierByResets(GetResets(player)) == tier;
    }

    private ValueTask ShowNotInRangeAsync(Player receiver, Player member, KalimaInstanceTier tier)
    {
        return receiver.ShowLocalizedBlueMessageAsync(
            nameof(PlayerMessage.KundunChamberMemberNotInRangeFormat),
            member.Name,
            tier.Level,
            this._kalimaConfiguration.GetResetRangeText(tier),
            GetResets(member));
    }

    private int GetEntriesPerWeek(Player player)
    {
        var passUntil = (int)player.GetStoredStatValue(Stats.KundunChamberPassUntilDay);
        return passUntil > 0 && passUntil >= KundunChamberConfiguration.GetUtcDayNumber(DateTime.UtcNow)
            ? Math.Max(this._configuration.PassEntriesPerWeek, this._configuration.EntriesPerWeek)
            : this._configuration.EntriesPerWeek;
    }

    private int GetUsedEntries(Player player)
    {
        var week = this._configuration.GetWeekNumber(DateTime.UtcNow);
        return (int)player.GetStoredStatValue(Stats.KundunChamberEntryWeek) == week
            ? (int)player.GetStoredStatValue(Stats.KundunChamberEntries)
            : 0;
    }

    /// <summary>
    /// Checks if the player can pay the entry: a free weekly entry and a lost map of the level of the tier.
    /// </summary>
    private async ValueTask<bool> CheckEntryAsync(Player player, KalimaInstanceTier tier)
    {
        var attributes = player.GameContext.Configuration.Attributes;
        if (!attributes.Any(a => a.Id == Stats.KundunChamberEntryWeek.Id) || !attributes.Any(a => a.Id == Stats.KundunChamberEntries.Id))
        {
            player.Logger.LogWarning("The attributes of the weekly entries into the chamber of Kundun are missing. Apply the data update which adds them.");
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KundunChamberNotAvailable)).ConfigureAwait(false);
            return false;
        }

        if (this.GetUsedEntries(player) >= this.GetEntriesPerWeek(player))
        {
            var timeLeft = this._configuration.GetTimeUntilNextReset(DateTime.UtcNow);
            await player.ShowLocalizedBlueMessageAsync(
                nameof(PlayerMessage.KundunChamberWeeklyLimitFormat),
                timeLeft.Days,
                timeLeft.Hours,
                timeLeft.Minutes).ConfigureAwait(false);
            return false;
        }

        if (FindLostMap(player, tier.Level) is null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KundunChamberNeedsLostMapFormat), tier.Level).ConfigureAwait(false);
            return false;
        }

        return true;
    }

    private async ValueTask<bool> TryPayEntryAsync(Player player, KalimaInstanceTier tier)
    {
        if (FindLostMap(player, tier.Level) is not { } lostMap)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KundunChamberNeedsLostMapFormat), tier.Level).ConfigureAwait(false);
            return false;
        }

        var week = this._configuration.GetWeekNumber(DateTime.UtcNow);
        var usedEntries = this.GetUsedEntries(player) + 1;
        if (!player.TrySetStoredStatValue(Stats.KundunChamberEntryWeek, week)
            || !player.TrySetStoredStatValue(Stats.KundunChamberEntries, usedEntries))
        {
            player.Logger.LogWarning("The weekly entry into the chamber of Kundun couldn't be stored for {player}.", player);
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KundunChamberNotAvailable)).ConfigureAwait(false);
            return false;
        }

        await player.DestroyInventoryItemAsync(lostMap).ConfigureAwait(false);
        await player.ShowLocalizedBlueMessageAsync(
            nameof(PlayerMessage.KundunChamberEntryUsedFormat),
            tier.Level,
            Math.Max(0, this.GetEntriesPerWeek(player) - usedEntries)).ConfigureAwait(false);
        return true;
    }

    private async ValueTask EnterAsync(Player player, KundunChamberContext chamber, bool payEntry)
    {
        var entrance = chamber.Definition.Entrance ?? throw new InvalidOperationException("The chamber of Kundun has no entrance.");
        if (payEntry && FindLostMap(player, chamber.Tier.Level) is null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KundunChamberNeedsLostMapFormat), chamber.Tier.Level).ConfigureAwait(false);
            return;
        }

        var result = await chamber.TryEnterAsync(player).ConfigureAwait(false);
        if (result != EnterResult.Success)
        {
            await player.ShowLocalizedBlueMessageAsync(
                result == EnterResult.Full ? nameof(PlayerMessage.KundunChamberFull) : nameof(PlayerMessage.KundunChamberNotAvailable)).ConfigureAwait(false);
            return;
        }

        if (payEntry)
        {
            if (!await this.TryPayEntryAsync(player, chamber.Tier).ConfigureAwait(false))
            {
                return;
            }

            chamber.Register(player);
        }

        if (player.ShopStorage?.StoreOpen ?? false)
        {
            await new CloseStoreAction().CloseStoreAsync(player).ConfigureAwait(false);
        }

        await player.WarpToAsync(entrance).ConfigureAwait(false);
    }
}
