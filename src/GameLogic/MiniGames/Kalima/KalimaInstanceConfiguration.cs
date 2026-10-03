// <copyright file="KalimaInstanceConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Kalima;

using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// The configuration of the <see cref="KalimaInstancePlugIn"/>.
/// </summary>
public class KalimaInstanceConfiguration
{
    /// <summary>
    /// The first day of the day numbers, see <see cref="GetDayNumber"/>.
    /// </summary>
    private static readonly DateTime DayNumberEpoch = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

    /// <summary>
    /// Gets or sets the number of the npc which lets the players enter the instance.
    /// </summary>
    [Display(Name = "Gatekeeper NPC number", Description = "Talking to this npc (by default Lugard, who stands in Lorencia) enters the Kalima instance.")]
    public short GatekeeperNpcNumber { get; set; } = 540;

    /// <summary>
    /// Gets or sets the number of entries per character and day.
    /// </summary>
    [Display(Name = "Entries per day", Description = "How many instances a character can open per day. Re-entering the own, still running instance (e.g. after a death) is always free.")]
    public int EntriesPerDay { get; set; } = 1;

    /// <summary>
    /// Gets or sets the hour of the day at which the daily entries are reset.
    /// </summary>
    [Display(Name = "Daily reset hour", Description = "Hour of the day (0-23) at which the daily entries are available again.")]
    public int DailyResetHour { get; set; } = 6;

    /// <summary>
    /// Gets or sets the time zone of the <see cref="DailyResetHour"/>.
    /// </summary>
    [Display(Name = "Time zone", Description = "Time zone id of the daily reset hour, e.g. 'Europe/Moscow'. Empty means the local time of the server.")]
    public string TimeZoneId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum player count which is considered for the party scaling.
    /// </summary>
    [Display(Name = "Maximum scaled players", Description = "The scaling by players stops at this player count (a full party).")]
    public int MaximumScaledPlayers { get; set; } = 5;

    /// <summary>
    /// Gets or sets the health multiplier per additional player.
    /// </summary>
    [Display(Name = "Health per player", Description = "Health multiplier of the monsters for each player in the instance: factor = 1 + (value - 1) * (players - 1). 2 means double health with 2 players, five times with 5 players.")]
    public float HealthPerPlayer { get; set; } = 2.0f;

    /// <summary>
    /// Gets or sets the defense multiplier per additional player.
    /// </summary>
    [Display(Name = "Defense per player", Description = "Defense multiplier of the monsters for each player in the instance, same formula as the health.")]
    public float DefensePerPlayer { get; set; } = 2.0f;

    /// <summary>
    /// Gets or sets the damage multiplier per additional player.
    /// </summary>
    [Display(Name = "Damage per player", Description = "Damage multiplier of the monsters for each player in the instance, same formula as the health.")]
    public float DamagePerPlayer { get; set; } = 1.6f;

    /// <summary>
    /// Gets or sets the drop multiplier per additional player.
    /// </summary>
    [Display(Name = "Drop per player", Description = "Item drop multiplier for each player in the instance, same formula as the health. Each full multiple is an additional drop roll, the fraction is the chance for one more.")]
    public float DropPerPlayer { get; set; } = 2.1f;

    /// <summary>
    /// Gets or sets the bonus on the symbol chance per additional player.
    /// </summary>
    [Display(Name = "Symbol bonus per player", Description = "The symbols are personal: every player in the instance rolls for each kill. The chance grows by this value for each additional player: chance = base * (1 + value * (players - 1)), so 0.1 means +40 % for each member of a full party.")]
    public float SymbolBonusPerPlayer { get; set; } = 0.1f;

    /// <summary>
    /// Gets or sets the numbers of the boss monsters (Illusion of Kundun 1-7).
    /// </summary>
    [Display(Name = "Boss monster numbers", Description = "Monster numbers of the Illusion of Kundun 1-7; killing one gives 'Symbols for the boss' to each player in the instance.")]
    public ICollection<short> BossMonsterNumbers { get; set; } = new List<short> { 161, 181, 189, 197, 267, 275, 338 };

    /// <summary>
    /// Gets or sets the tiers of the instance.
    /// </summary>
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [Display(Name = "Tiers", Description = "The Kalima maps by resets. A party enters the highest tier which all of its members reached. The multipliers are the base values for a single player.")]
    public ICollection<KalimaInstanceTier> Tiers { get; set; } = CreateDefaultTiers();

    /// <summary>
    /// Gets the scaling factor for the specified player count.
    /// </summary>
    /// <param name="perPlayer">The multiplier per player.</param>
    /// <param name="playerCount">The player count.</param>
    /// <returns>The factor, at least 1 player and at most <see cref="MaximumScaledPlayers"/> are considered.</returns>
    public float GetPlayerFactor(float perPlayer, int playerCount)
    {
        var players = Math.Clamp(playerCount, 1, Math.Max(1, this.MaximumScaledPlayers));
        return Math.Max(0.01f, 1 + ((perPlayer - 1) * (players - 1)));
    }

    /// <summary>
    /// Gets the chance per kill that a player gets a Symbol of Kundun.
    /// </summary>
    /// <param name="tier">The tier.</param>
    /// <param name="playerCount">The player count.</param>
    /// <returns>The chance; values above 1 give several symbols.</returns>
    public float GetSymbolChance(KalimaInstanceTier tier, int playerCount)
    {
        var players = Math.Clamp(playerCount, 1, Math.Max(1, this.MaximumScaledPlayers));
        return Math.Max(0, tier.SymbolChancePerKill * (1 + (this.SymbolBonusPerPlayer * (players - 1))));
    }

    /// <summary>
    /// Gets the tier of the specified level.
    /// </summary>
    /// <param name="level">The level of the tier (and the mini game).</param>
    /// <returns>The tier, if configured.</returns>
    public KalimaInstanceTier? GetTier(int level)
    {
        return this.Tiers.FirstOrDefault(t => t.Level == level);
    }

    /// <summary>
    /// Gets the highest tier which is available for the specified reset count.
    /// </summary>
    /// <param name="resets">The reset count.</param>
    /// <returns>The highest available tier, or <c>null</c>, if no tier is available yet.</returns>
    public KalimaInstanceTier? GetHighestTier(int resets)
    {
        return this.Tiers
            .Where(t => t.MinimumResets <= resets)
            .MaxBy(t => t.MinimumResets);
    }

    /// <summary>
    /// Gets the lowest required reset count of all tiers.
    /// </summary>
    /// <returns>The lowest required reset count.</returns>
    public int GetMinimumResets()
    {
        return this.Tiers.Count == 0 ? 0 : this.Tiers.Min(t => t.MinimumResets);
    }

    /// <summary>
    /// Gets the number of the day of the specified time, considering the <see cref="DailyResetHour"/>.
    /// The day starts at the reset hour, so the time before is counted to the previous day.
    /// </summary>
    /// <param name="utcNow">The current time in UTC.</param>
    /// <returns>The number of the day.</returns>
    public int GetDayNumber(DateTime utcNow)
    {
        var localTime = this.ToLocalTime(utcNow);
        var shifted = localTime.AddHours(-Math.Clamp(this.DailyResetHour, 0, 23));
        return (int)(shifted.Date - DayNumberEpoch).TotalDays;
    }

    /// <summary>
    /// Gets the remaining time until the next daily reset.
    /// </summary>
    /// <param name="utcNow">The current time in UTC.</param>
    /// <returns>The remaining time until the next daily reset.</returns>
    public TimeSpan GetTimeUntilNextReset(DateTime utcNow)
    {
        var localTime = this.ToLocalTime(utcNow);
        var resetToday = localTime.Date.AddHours(Math.Clamp(this.DailyResetHour, 0, 23));
        var nextReset = localTime < resetToday ? resetToday : resetToday.AddDays(1);
        return nextReset - localTime;
    }

    private static List<KalimaInstanceTier> CreateDefaultTiers() =>
    [
        new() { Level = 1, MinimumResets = 5, HealthMultiplier = 6.0f, DefenseMultiplier = 3.0f, DamageMultiplier = 3.0f, DropMultiplier = 1.5f, SymbolChancePerKill = 0.06f, BossSymbols = 2 },
        new() { Level = 2, MinimumResets = 10, HealthMultiplier = 5.0f, DefenseMultiplier = 2.5f, DamageMultiplier = 2.5f, DropMultiplier = 1.5f, SymbolChancePerKill = 0.07f, BossSymbols = 4 },
        new() { Level = 3, MinimumResets = 15, HealthMultiplier = 4.0f, DefenseMultiplier = 2.2f, DamageMultiplier = 2.2f, DropMultiplier = 1.5f, SymbolChancePerKill = 0.08f, BossSymbols = 6 },
        new() { Level = 4, MinimumResets = 22, HealthMultiplier = 3.5f, DefenseMultiplier = 2.0f, DamageMultiplier = 2.0f, DropMultiplier = 1.5f, SymbolChancePerKill = 0.09f, BossSymbols = 8 },
        new() { Level = 5, MinimumResets = 30, HealthMultiplier = 3.0f, DefenseMultiplier = 1.8f, DamageMultiplier = 1.8f, DropMultiplier = 1.5f, SymbolChancePerKill = 0.1f, BossSymbols = 10 },
        new() { Level = 6, MinimumResets = 40, HealthMultiplier = 3.0f, DefenseMultiplier = 1.6f, DamageMultiplier = 1.6f, DropMultiplier = 1.5f, SymbolChancePerKill = 0.11f, BossSymbols = 12 },
        new() { Level = 7, MinimumResets = 50, HealthMultiplier = 3.0f, DefenseMultiplier = 1.5f, DamageMultiplier = 1.5f, DropMultiplier = 1.5f, SymbolChancePerKill = 0.12f, BossSymbols = 15 },
    ];

    private DateTime ToLocalTime(DateTime utcNow)
    {
        var utc = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        if (!string.IsNullOrWhiteSpace(this.TimeZoneId))
        {
            try
            {
                return TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById(this.TimeZoneId));
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                // fall back to the local time of the server
            }
        }

        return utc.ToLocalTime();
    }
}
