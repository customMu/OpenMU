// <copyright file="KalimaInstanceEnterAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Kalima;

using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlayerActions.MiniGames;
using MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

/// <summary>
/// The action to enter the Kalima instance of the party (or the own one, without a party).
/// </summary>
public class KalimaInstanceEnterAction
{
    private readonly KalimaInstanceConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="KalimaInstanceEnterAction"/> class.
    /// </summary>
    /// <param name="configuration">The configuration of the Kalima instance.</param>
    public KalimaInstanceEnterAction(KalimaInstanceConfiguration configuration)
    {
        this._configuration = configuration;
    }

    /// <summary>
    /// Tries to enter the Kalima instance. An already running instance of the party is entered,
    /// otherwise a new one is created for the tier whose reset range contains the resets of the player; all party members have to be in this range.
    /// </summary>
    /// <param name="player">The player.</param>
    public async ValueTask TryEnterAsync(Player player)
    {
        if (player.SelectedCharacter is null || player.CurrentMiniGame is not null || player.DuelRoom is not null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KalimaInstanceNotAvailable)).ConfigureAwait(false);
            return;
        }

        var owner = player.Party?.PartyMaster?.Name ?? player.Name;
        var instance = await player.GameContext.FindMiniGameAsync(MiniGameType.KalimaInstance, owner).ConfigureAwait(false) as KalimaInstanceContext;
        if (instance is { IsAcceptingPlayers: true })
        {
            await this.EnterExistingInstanceAsync(player, instance).ConfigureAwait(false);
            return;
        }

        await this.EnterNewInstanceAsync(player).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets what the gatekeeper shows to the player: the tiers by resets, the tier of the player and the entries left today.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The information.</returns>
    public async ValueTask<KalimaInstanceInfo> GetInfoAsync(Player player)
    {
        var resets = GetResets(player);
        var tier = this._configuration.GetTierByResets(resets);
        var tiers = this._configuration.Tiers
            .OrderBy(t => t.MinimumResets)
            .Select(t => ((byte)t.Level, t.MinimumResets, this._configuration.GetMaximumResets(t)))
            .ToList();
        var today = this._configuration.GetDayNumber(DateTime.UtcNow);
        var usedEntries = (int)player.GetStoredStatValue(Stats.KalimaInstanceEntryDay) == today
            ? (int)player.GetStoredStatValue(Stats.KalimaInstanceEntries)
            : 0;
        var owner = player.Party?.PartyMaster?.Name ?? player.Name;
        var instance = await player.GameContext.FindMiniGameAsync(MiniGameType.KalimaInstance, owner).ConfigureAwait(false) as KalimaInstanceContext;
        var canReenter = instance is { IsAcceptingPlayers: true } && instance.IsRegistered(player);
        return new KalimaInstanceInfo(
            tiers,
            resets,
            (byte)(tier?.Level ?? 0),
            Math.Max(0, this._configuration.EntriesPerDay - usedEntries),
            this._configuration.EntriesPerDay,
            this._configuration.GetTimeUntilNextReset(DateTime.UtcNow),
            canReenter);
    }

    private static int GetResets(Player player) => (int)(player.Attributes?[Stats.Resets] ?? 0);

    private async ValueTask EnterExistingInstanceAsync(Player player, KalimaInstanceContext instance)
    {
        if (instance.IsRegistered(player))
        {
            // Entering the own instance again (e.g. after a death) is free.
            await this.EnterAsync(player, instance, false).ConfigureAwait(false);
            return;
        }

        if (!this.IsInRange(player, instance.Tier))
        {
            await this.ShowNotInRangeAsync(player, player, instance.Tier).ConfigureAwait(false);
            return;
        }

        if (!await this.CheckDailyEntriesAsync(player).ConfigureAwait(false))
        {
            return;
        }

        await this.EnterAsync(player, instance, true).ConfigureAwait(false);
    }

    private async ValueTask EnterNewInstanceAsync(Player player)
    {
        var tier = this._configuration.GetTierByResets(GetResets(player));
        if (tier is null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KalimaInstanceRequiresResetsFormat), this._configuration.GetMinimumResets(), 1).ConfigureAwait(false);
            return;
        }

        // The ranges are strict: every member of the party has to be in the range of the same tier.
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
            .FirstOrDefault(d => d.Type == MiniGameType.KalimaInstance && d.GameLevel == tier.Level);
        if (definition?.Entrance is null)
        {
            player.Logger.LogWarning("The mini game definition of the Kalima instance {level} is missing. Apply the data update which adds it.", tier.Level);
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KalimaInstanceNotAvailable)).ConfigureAwait(false);
            return;
        }

        if (!await this.CheckDailyEntriesAsync(player).ConfigureAwait(false))
        {
            return;
        }

        if (await player.GameContext.GetMiniGameAsync(definition, player).ConfigureAwait(false) is not KalimaInstanceContext instance)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KalimaInstanceNotAvailable)).ConfigureAwait(false);
            return;
        }

        await this.EnterAsync(player, instance, !instance.IsRegistered(player)).ConfigureAwait(false);
    }

    private bool IsInRange(Player player, KalimaInstanceTier tier)
    {
        return this._configuration.GetTierByResets(GetResets(player)) == tier;
    }

    private ValueTask ShowNotInRangeAsync(Player receiver, Player member, KalimaInstanceTier tier)
    {
        return receiver.ShowLocalizedBlueMessageAsync(
            nameof(PlayerMessage.KalimaInstanceMemberNotInRangeFormat),
            member.Name,
            tier.Level,
            this._configuration.GetResetRangeText(tier),
            GetResets(member));
    }

    private async ValueTask<bool> CheckDailyEntriesAsync(Player player)
    {
        if (!player.GameContext.Configuration.Attributes.Any(a => a.Id == Stats.KalimaInstanceEntryDay.Id)
            || !player.GameContext.Configuration.Attributes.Any(a => a.Id == Stats.KalimaInstanceEntries.Id))
        {
            player.Logger.LogWarning("The attributes of the daily Kalima entries are missing. Apply the data update which adds them.");
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KalimaInstanceNotAvailable)).ConfigureAwait(false);
            return false;
        }

        var today = this._configuration.GetDayNumber(DateTime.UtcNow);
        var usedEntries = (int)player.GetStoredStatValue(Stats.KalimaInstanceEntryDay) == today
            ? (int)player.GetStoredStatValue(Stats.KalimaInstanceEntries)
            : 0;
        if (usedEntries < this._configuration.EntriesPerDay)
        {
            return true;
        }

        var timeLeft = this._configuration.GetTimeUntilNextReset(DateTime.UtcNow);
        await player.ShowLocalizedBlueMessageAsync(
            nameof(PlayerMessage.KalimaInstanceDailyLimitFormat),
            this._configuration.DailyResetHour,
            (int)timeLeft.TotalHours,
            timeLeft.Minutes).ConfigureAwait(false);
        return false;
    }

    private bool TryUseDailyEntry(Player player, out int remainingEntries)
    {
        var today = this._configuration.GetDayNumber(DateTime.UtcNow);
        var usedEntries = (int)player.GetStoredStatValue(Stats.KalimaInstanceEntryDay) == today
            ? (int)player.GetStoredStatValue(Stats.KalimaInstanceEntries)
            : 0;
        usedEntries++;
        remainingEntries = Math.Max(0, this._configuration.EntriesPerDay - usedEntries);
        return player.TrySetStoredStatValue(Stats.KalimaInstanceEntryDay, today)
               && player.TrySetStoredStatValue(Stats.KalimaInstanceEntries, usedEntries);
    }

    private async ValueTask EnterAsync(Player player, KalimaInstanceContext instance, bool useEntry)
    {
        var entrance = instance.Definition.Entrance ?? throw new InvalidOperationException("The Kalima instance has no entrance.");
        var result = await instance.TryEnterAsync(player).ConfigureAwait(false);
        if (result != EnterResult.Success)
        {
            await player.ShowLocalizedBlueMessageAsync(
                result == EnterResult.Full ? nameof(PlayerMessage.KalimaInstanceFull) : nameof(PlayerMessage.KalimaInstanceNotAvailable)).ConfigureAwait(false);
            return;
        }

        if (useEntry)
        {
            if (!this.TryUseDailyEntry(player, out var remainingEntries))
            {
                player.Logger.LogWarning("The daily entry of the Kalima instance couldn't be stored for {player}.", player);
            }

            instance.Register(player);
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.KalimaInstanceEntryUsedFormat), remainingEntries).ConfigureAwait(false);
        }

        if (player.ShopStorage?.StoreOpen ?? false)
        {
            await new CloseStoreAction().CloseStoreAsync(player).ConfigureAwait(false);
        }

        await player.WarpToAsync(entrance).ConfigureAwait(false);
    }
}
