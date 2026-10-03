// <copyright file="KundunChamberConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Kalima;

using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// The configuration of the <see cref="KundunChamberPlugIn"/>.
/// </summary>
public class KundunChamberConfiguration
{
    /// <summary>
    /// The first day of the day numbers of <see cref="GetUtcDayNumber"/>.
    /// </summary>
    public static readonly DateTime DayNumberEpoch = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// A monday, from which the weeks are counted.
    /// </summary>
    private static readonly DateTime WeekEpoch = new(2000, 1, 3, 0, 0, 0, DateTimeKind.Unspecified);

    /// <summary>
    /// Gets or sets the number of the npc which lets the players enter the chamber.
    /// </summary>
    [Display(Name = "Keeper NPC number", Description = "Talking to this npc (by default David, who stands in Lorencia) enters the chamber of Kundun.")]
    public short KeeperNpcNumber { get; set; } = 579;

    /// <summary>
    /// Gets or sets the number of entries per character and week.
    /// </summary>
    [Display(Name = "Entries per week", Description = "How many chambers a character can open per week. Re-entering the own, still running chamber (e.g. after a death) is always free.")]
    public int EntriesPerWeek { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of entries per week of a character with a chamber pass.
    /// </summary>
    [Display(Name = "Entries per week with a pass", Description = "Entries per week of a character whose chamber pass is active (attribute 'Kundun chamber pass until day', set e.g. by the website).")]
    public int PassEntriesPerWeek { get; set; } = 2;

    /// <summary>
    /// Gets or sets the day of the week at which the weekly entries are reset.
    /// </summary>
    [Display(Name = "Weekly reset day")]
    public DayOfWeek WeeklyResetDay { get; set; } = DayOfWeek.Monday;

    /// <summary>
    /// Gets or sets the hour of the day at which the weekly entries are reset.
    /// </summary>
    [Display(Name = "Weekly reset hour", Description = "Hour of the reset day (0-23) at which the weekly entries are available again.")]
    public int WeeklyResetHour { get; set; } = 6;

    /// <summary>
    /// Gets or sets the time zone of the <see cref="WeeklyResetHour"/>.
    /// </summary>
    [Display(Name = "Time zone", Description = "Time zone id of the weekly reset, e.g. 'Europe/Moscow'. Empty means the local time of the server.")]
    public string TimeZoneId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the duration of a chamber.
    /// </summary>
    [Display(Name = "Duration", Description = "How long a chamber runs. The game duration of the mini game definitions has to be longer.")]
    public TimeSpan Duration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets or sets the time after which a chamber without players is closed.
    /// </summary>
    [Display(Name = "Close when empty after")]
    public TimeSpan CloseWhenEmptyAfter { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the time after the victory until the chamber is closed, to pick up the drop.
    /// </summary>
    [Display(Name = "Close after victory")]
    public TimeSpan CloseAfterVictory { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Gets or sets the numbers of the monsters Kundun 1-7.
    /// </summary>
    [Display(Name = "Kundun monster numbers", Description = "Monster numbers of Kundun of the chamber levels 1-7 (in this order). If a monster is missing, the Illusion of Kundun of the Kalima map is used.")]
    public IList<short> KundunMonsterNumbers { get; set; } = new List<short> { 700, 701, 702, 703, 704, 705, 706 };

    /// <summary>
    /// Gets or sets the health of Kundun, relative to the Illusion of Kundun of the Kalima instance of the same level.
    /// </summary>
    [Display(Name = "Kundun health factor", Description = "Health of Kundun relative to the Illusion of Kundun of the Kalima instance of the same level.")]
    public float KundunHealthFactor { get; set; } = 3f;

    /// <summary>
    /// Gets or sets the damage of Kundun, relative to the Illusion of Kundun of the Kalima instance of the same level.
    /// </summary>
    [Display(Name = "Kundun damage factor")]
    public float KundunDamageFactor { get; set; } = 1.5f;

    /// <summary>
    /// Gets or sets the seconds of a phase without effects.
    /// </summary>
    [Display(Name = "Free seconds per phase", Description = "The time of a phase which doesn't heal or strengthen Kundun.")]
    public int FreeSecondsPerPhase { get; set; } = 20;

    /// <summary>
    /// Gets or sets the phases of the fight.
    /// </summary>
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [Display(Name = "Phases", Description = "The phases of the fight against Kundun, by their health threshold.")]
    public ICollection<KundunChamberPhase> Phases { get; set; } = CreateDefaultPhases();

    /// <summary>
    /// Gets or sets the essence for Kundun and chamber level.
    /// </summary>
    [Display(Name = "Essence for Kundun", Description = "Kundun Essence which each player in the chamber gets for Kundun, multiplied with the chamber level.")]
    public int RewardEssence { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether Kundun drops a Box of Kundun per player.
    /// </summary>
    [Display(Name = "Box of Kundun per player", Description = "Kundun drops a Box of Kundun +min(level, 5) per player in the chamber, distributed by the drop mode of the party.")]
    public bool RewardBoxOfKundun { get; set; } = true;

    /// <summary>
    /// Gets the number of the day in UTC, counted from 2000-01-01. The chamber pass is stored as such a day number.
    /// </summary>
    /// <param name="utcNow">The current time in UTC.</param>
    /// <returns>The day number.</returns>
    public static int GetUtcDayNumber(DateTime utcNow) => (int)(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc).Date - DayNumberEpoch).TotalDays;

    /// <summary>
    /// Gets the phases ordered by their threshold, the highest first.
    /// </summary>
    /// <returns>The ordered phases.</returns>
    public IReadOnlyList<KundunChamberPhase> GetOrderedPhases() => this.Phases.OrderByDescending(p => p.HealthThreshold).ToList();

    /// <summary>
    /// Gets the number of the monster Kundun for the chamber level.
    /// </summary>
    /// <param name="level">The chamber level.</param>
    /// <returns>The monster number, or <c>null</c>.</returns>
    public short? GetKundunMonsterNumber(int level) => level >= 1 && level <= this.KundunMonsterNumbers.Count ? this.KundunMonsterNumbers[level - 1] : null;

    /// <summary>
    /// Gets the number of the week of the specified time, considering the weekly reset day and hour.
    /// </summary>
    /// <param name="utcNow">The current time in UTC.</param>
    /// <returns>The number of the week.</returns>
    public int GetWeekNumber(DateTime utcNow)
    {
        var shifted = this.ToLocalTime(utcNow).AddHours(-Math.Clamp(this.WeeklyResetHour, 0, 23));
        var epoch = WeekEpoch.AddDays(((int)this.WeeklyResetDay - (int)DayOfWeek.Monday + 7) % 7);
        return (int)Math.Floor((shifted.Date - epoch).TotalDays / 7);
    }

    /// <summary>
    /// Gets the remaining time until the next weekly reset.
    /// </summary>
    /// <param name="utcNow">The current time in UTC.</param>
    /// <returns>The remaining time.</returns>
    public TimeSpan GetTimeUntilNextReset(DateTime utcNow)
    {
        var localTime = this.ToLocalTime(utcNow);
        var daysUntilResetDay = ((int)this.WeeklyResetDay - (int)localTime.DayOfWeek + 7) % 7;
        var nextReset = localTime.Date.AddDays(daysUntilResetDay).AddHours(Math.Clamp(this.WeeklyResetHour, 0, 23));
        if (nextReset <= localTime)
        {
            nextReset = nextReset.AddDays(7);
        }

        return nextReset - localTime;
    }

    // The free seconds are counted per phase. The examples of the design: a party which needs 90 seconds
    // for the Illusions (70 seconds after the free ones) lets Kundun heal 7 / 8.4 / 10.5 %, gives him
    // +14 % defense and +21 % damage.
    private static List<KundunChamberPhase> CreateDefaultPhases() =>
    [
        new() { HealthThreshold = 0.75f, IllusionCount = 1, IllusionHealthFactor = 0.5f, IllusionDamageFactor = 1.0f, HealPerSecond = 0.001f, MaximumHeal = 0.15f },
        new() { HealthThreshold = 0.50f, IllusionCount = 2, IllusionHealthFactor = 0.4f, IllusionDamageFactor = 1.1f, HealPerSecond = 0.0012f, MaximumHeal = 0.20f, DefensePerSecond = 0.002f, MaximumDefenseIncrease = 0.4f },
        new() { HealthThreshold = 0.25f, IllusionCount = 3, IllusionHealthFactor = 0.35f, IllusionDamageFactor = 1.2f, HealPerSecond = 0.0015f, MaximumHeal = 0.25f, DamagePerSecond = 0.003f, MaximumDamageIncrease = 0.5f },
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
