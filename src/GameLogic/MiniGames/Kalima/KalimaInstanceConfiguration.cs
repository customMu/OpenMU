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
    /// Gets or sets the duration of an instance.
    /// </summary>
    [Display(Name = "Duration", Description = "How long an instance runs. The game duration of the mini game definitions has to be longer.")]
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(40);

    /// <summary>
    /// Gets or sets the time after which an instance without players is closed.
    /// </summary>
    [Display(Name = "Close when empty after", Description = "An instance without players is closed after this time (players who died have this time to come back).")]
    public TimeSpan CloseWhenEmptyAfter { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the number of packs before the boss.
    /// </summary>
    [Display(Name = "Packs", Description = "Number of monster packs along the way to the Illusion of Kundun. The next pack appears when the previous one is killed, the boss after the last one.")]
    public int PackCount { get; set; } = 10;

    /// <summary>
    /// Gets or sets the number of monsters per pack.
    /// </summary>
    [Display(Name = "Monsters per pack")]
    public int MonstersPerPack { get; set; } = 8;

    /// <summary>
    /// Gets or sets the growth of the health and damage of the monsters from one pack to the next.
    /// </summary>
    [Display(Name = "Pack strength growth", Description = "Multiplier of the health and damage from one pack to the next (1.06 = +6 % per pack). The middle pack has the strength of the tier, the first ones are weaker, the last ones stronger. 1 = all packs equal.")]
    public float PackStrengthGrowth { get; set; } = 1.06f;

    /// <summary>
    /// Gets or sets the monsters which are placed along the way instead of at their own spots.
    /// </summary>
    [Display(Name = "Midway monsters", Description = "Monsters (start of the name, e.g. 'Aegis'; several separated by commas) whose spots on the map are next to the boss. They are taken out of the way and ambush the players at the spot of the pack they just killed, once after each pack between 'Midway from' and 'Midway to'. Empty = monsters at their own spots.")]
    public string MidwayMonsters { get; set; } = "Aegis";

    /// <summary>
    /// Gets or sets the start of the part of the way with the midway monsters, as a share of the packs.
    /// </summary>
    [Display(Name = "Midway from", Description = "Start of the part of the way with the midway ambushes, as a share of the packs (0.6 of 10 packs = the first ambush after the 6th pack).")]
    public double MidwayFrom { get; set; } = 0.6;

    /// <summary>
    /// Gets or sets the end of the part of the way with the midway monsters, as a share of the packs.
    /// </summary>
    [Display(Name = "Midway to", Description = "End of the part of the way with the midway ambushes, as a share of the packs (0.6 to 0.8 of 10 packs = 2 ambushes). The number of packs stays the same.")]
    public double MidwayTo { get; set; } = 0.8;

    /// <summary>
    /// Gets or sets the experience multiplier in the instance.
    /// </summary>
    [Display(Name = "Experience multiplier", Description = "Multiplier of the experience for the monsters of the instance.")]
    public float ExperienceMultiplier { get; set; } = 20f;

    /// <summary>
    /// Gets or sets the chance per kill that a player near the killed monster gets essence.
    /// </summary>
    [Display(Name = "Essence chance per kill", Description = "Chance (0.05 = 5 %) per killed monster that each living player near it gets 'Essence per kill' x Kalima level of Kundun Essence (the currency of the essence shop).")]
    public float EssenceChancePerKill { get; set; } = 0.05f;

    /// <summary>
    /// Gets or sets the essence per kill and Kalima level.
    /// </summary>
    [Display(Name = "Essence per kill", Description = "Kundun Essence per successful roll, multiplied with the Kalima level.")]
    public int EssencePerKill { get; set; } = 1;

    /// <summary>
    /// Gets or sets the essence for the boss and Kalima level.
    /// </summary>
    [Display(Name = "Essence for the boss", Description = "Kundun Essence which each player in the instance gets for the Illusion of Kundun, multiplied with the Kalima level.")]
    public int BossEssence { get; set; } = 5;

    /// <summary>
    /// Gets or sets the range around a killed monster, in which players get essence.
    /// </summary>
    [Display(Name = "Essence range", Description = "Players within this distance (in tiles) of a killed monster roll for essence.")]
    public int EssenceRange { get; set; } = 15;

    /// <summary>
    /// Gets or sets the chance that a regular monster drops a Symbol of Kundun.
    /// </summary>
    [Display(Name = "Symbol drop chance", Description = "Chance (0.01 = 1 %) that a regular monster of Kalima N drops a Symbol of Kundun +N. 5 symbols of the same level are combined to a Lost Map of this level.")]
    public float SymbolDropChance { get; set; } = 0.01f;

    /// <summary>
    /// Gets or sets the number of symbols which the boss drops per player in the instance.
    /// </summary>
    [Display(Name = "Boss symbols per player", Description = "The Illusion of Kundun of Kalima N drops this number of Symbols of Kundun +N per player in the instance. The items are distributed by the drop mode of the party.")]
    public int BossSymbolsPerPlayer { get; set; } = 1;

    /// <summary>
    /// Gets or sets the chance that the boss drops a lost map.
    /// </summary>
    [Display(Name = "Boss lost map chance", Description = "Chance (0.03 = 3 %) that the Illusion of Kundun of Kalima N additionally drops a Lost Map +N, the entry fee of the chamber of Kundun.")]
    public float BossLostMapChance { get; set; } = 0.03f;

    /// <summary>
    /// Gets or sets a value indicating whether the boss drops a Box of Kundun for the party.
    /// </summary>
    [Display(Name = "Boss Box of Kundun", Description = "The Illusion of Kundun of Kalima N drops one Box of Kundun +min(N, 5) for the party (or the single player).")]
    public bool BossBoxOfKundun { get; set; } = true;

    /// <summary>
    /// Gets or sets the chance that the boss drops a weapon of the ranks of the tier.
    /// </summary>
    [Display(Name = "Boss weapon chance", Description = "Chance (0.05 = 5 %) that the Illusion of Kundun drops a weapon of the item ranks of its Kalima, with the level by the weights below.")]
    public float BossWeaponChance { get; set; } = 0.05f;

    /// <summary>
    /// Gets or sets the weights of the levels of the weapon of the boss, from +1.
    /// </summary>
    [MemberOfAggregate]
    [Display(Name = "Boss weapon level weights", Description = "Weights of the levels +1, +2, ... of the weapon of the boss.")]
    public IList<int> BossWeaponLevelWeights { get; set; } = new List<int> { 30, 25, 20, 13, 8, 4 };

    /// <summary>
    /// Gets or sets the chance that a killed monster drops money.
    /// </summary>
    [Display(Name = "Money chance", Description = "Chance (0.5 = 50 %) that a killed monster drops money: monster level x 'money per monster level' x Kalima level.")]
    public float MoneyChance { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the money per monster level and Kalima level.
    /// </summary>
    [Display(Name = "Money per monster level")]
    public int MoneyPerMonsterLevel { get; set; } = 10;

    /// <summary>
    /// Gets or sets the multiplier of the item chances of the plugin 'Item drop by rank'.
    /// </summary>
    [Display(Name = "Item chance multiplier", Description = "The monsters drop the items of the ranks of the tier with the chance of the plugin 'Item drop by rank' x this.")]
    public float ItemChanceMultiplier { get; set; } = 50f;

    /// <summary>
    /// Gets or sets the health of the boss in relation to the health of the regular monsters.
    /// </summary>
    [Display(Name = "Boss health factor", Description = "The health of the Illusion of Kundun is this factor times the health of a regular monster of the tier.")]
    public float BossHealthFactor { get; set; } = 50f;

    /// <summary>
    /// Gets or sets the minimum growth of the strength from one tier to the next.
    /// </summary>
    [Display(Name = "Minimum growth per tier", Description = "Each tier is at least this much stronger than the previous one (0.05 = 5 %), also when its reference map is weaker.")]
    public float MinimumTierGrowth { get; set; } = 0.05f;

    /// <summary>
    /// Gets or sets the global factor of the health of all monsters of Kalima (also the bosses and Kundun).
    /// </summary>
    [Display(Name = "Global health factor", Description = "Multiplies the health of all monsters of Kalima and of the chamber of Kundun, after the tiers (0.1 = a tenth).")]
    public float GlobalHealthFactor { get; set; } = 0.1f;

    /// <summary>
    /// Gets or sets the global factor of the damage of all monsters of Kalima (also the bosses and Kundun).
    /// </summary>
    [Display(Name = "Global damage factor", Description = "Multiplies the damage of all monsters of Kalima and of the chamber of Kundun, after the tiers (0.2 = a fifth).")]
    public float GlobalDamageFactor { get; set; } = 0.2f;

    /// <summary>
    /// Gets or sets the maximum monster level.
    /// </summary>
    [Display(Name = "Maximum monster level", Description = "The monster level of a tier doesn't exceed this value. 0 = no limit.")]
    public int MaximumMonsterLevel { get; set; } = 400;

    /// <summary>
    /// Gets or sets the numbers of the boss monsters (Illusion of Kundun 1-7).
    /// </summary>
    [Display(Name = "Boss monster numbers", Description = "Monster numbers of the Illusion of Kundun 1-7; killing one gives 'Essence for the boss' x Kalima level to each player in the instance and drops the symbols.")]
    public ICollection<short> BossMonsterNumbers { get; set; } = new List<short> { 161, 181, 189, 197, 267, 275, 338 };

    /// <summary>
    /// Gets or sets the tiers of the instance.
    /// </summary>
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [Display(Name = "Tiers", Description = "The Kalima maps by resets. Each tier is for the resets from its minimum up to the minimum of the next tier minus one (strict ranges, the last tier is open); all members of a party have to be in the range of the same tier. The monsters get the strength of the reference map times the factors, their differences among each other stay. A tier is at least by the minimum growth stronger than the previous one.")]
    public ICollection<KalimaInstanceTier> Tiers { get; set; } = CreateDefaultTiers();

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
    /// Gets the tier whose reset range contains the specified reset count. The ranges are strict:
    /// a tier reaches from its minimum resets up to the minimum resets of the next tier minus one,
    /// the last tier is open.
    /// </summary>
    /// <param name="resets">The reset count.</param>
    /// <returns>The tier, or <c>null</c>, if the reset count is below the first tier.</returns>
    public KalimaInstanceTier? GetTierByResets(int resets)
    {
        return this.Tiers
            .Where(t => t.MinimumResets <= resets)
            .MaxBy(t => t.MinimumResets);
    }

    /// <summary>
    /// Gets the maximum reset count of the tier, which is one less than the minimum resets of the next tier.
    /// </summary>
    /// <param name="tier">The tier.</param>
    /// <returns>The maximum reset count, or <c>null</c>, if the tier is the last one.</returns>
    public int? GetMaximumResets(KalimaInstanceTier tier)
    {
        var next = this.Tiers
            .Where(t => t.MinimumResets > tier.MinimumResets)
            .MinBy(t => t.MinimumResets);
        return next is null ? null : next.MinimumResets - 1;
    }

    /// <summary>
    /// Gets the reset range of the tier as text, e.g. "5-9" or "45+".
    /// </summary>
    /// <param name="tier">The tier.</param>
    /// <returns>The reset range as text.</returns>
    public string GetResetRangeText(KalimaInstanceTier tier)
    {
        return this.GetMaximumResets(tier) is { } maximum
            ? $"{tier.MinimumResets}-{maximum}"
            : $"{tier.MinimumResets}+";
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

    // The reference maps follow the reset ladder about 5 resets above the tier: Kanturu Ruins (37),
    // Aida (33), Karutan 2 (81), Raklion (57) and Kanturu Relics (38). The damage of the tiers which
    // are based on Raklion is lower, because its monsters already hit very hard.
    private static List<KalimaInstanceTier> CreateDefaultTiers() =>
    [
        new() { Level = 1, MinimumResets = 5, ReferenceMapNumber = 37, LevelFactor = 1.1f, DamageFactor = 2f, JewelChancePercent = 2f, ChaosWeight = 40, BlessWeight = 35, SoulWeight = 20, LifeWeight = 5, CreationWeight = 0, GuardianWeight = 0, MinimumItemRank = 3, MaximumItemRank = 4, ItemLevel = 0 },
        new() { Level = 2, MinimumResets = 10, ReferenceMapNumber = 33, LevelFactor = 1.1f, DamageFactor = 2f, JewelChancePercent = 3f, ChaosWeight = 35, BlessWeight = 33, SoulWeight = 22, LifeWeight = 7, CreationWeight = 3, GuardianWeight = 0, MinimumItemRank = 5, MaximumItemRank = 5, ItemLevel = 0 },
        new() { Level = 3, MinimumResets = 15, ReferenceMapNumber = 81, LevelFactor = 1.1f, DamageFactor = 2f, JewelChancePercent = 4f, ChaosWeight = 30, BlessWeight = 30, SoulWeight = 24, LifeWeight = 10, CreationWeight = 6, GuardianWeight = 0, MinimumItemRank = 6, MaximumItemRank = 6, ItemLevel = 0 },
        new() { Level = 4, MinimumResets = 22, ReferenceMapNumber = 81, LevelFactor = 1.2f, DamageFactor = 2.2f, JewelChancePercent = 5.5f, ChaosWeight = 25, BlessWeight = 28, SoulWeight = 25, LifeWeight = 13, CreationWeight = 9, GuardianWeight = 0, MinimumItemRank = 7, MaximumItemRank = 8, ItemLevel = 0 },
        new() { Level = 5, MinimumResets = 30, ReferenceMapNumber = 57, LevelFactor = 1.1f, DamageFactor = 1.3f, JewelChancePercent = 7f, ChaosWeight = 20, BlessWeight = 26, SoulWeight = 26, LifeWeight = 16, CreationWeight = 12, GuardianWeight = 0, MinimumItemRank = 8, MaximumItemRank = 8, ItemLevel = 1 },
        new() { Level = 6, MinimumResets = 38, ReferenceMapNumber = 57, LevelFactor = 1.2f, DamageFactor = 1.4f, JewelChancePercent = 8.5f, ChaosWeight = 15, BlessWeight = 25, SoulWeight = 27, LifeWeight = 18, CreationWeight = 12, GuardianWeight = 3, MinimumItemRank = 8, MaximumItemRank = 8, ItemLevel = 2 },
        new() { Level = 7, MinimumResets = 45, ReferenceMapNumber = 38, LevelFactor = 1.2f, DamageFactor = 2f, JewelChancePercent = 10f, ChaosWeight = 10, BlessWeight = 24, SoulWeight = 28, LifeWeight = 20, CreationWeight = 13, GuardianWeight = 5, MinimumItemRank = 8, MaximumItemRank = 8, ItemLevel = 3 },
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
