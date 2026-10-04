// <copyright file="ResetEventsConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.ResetEvents;

using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// The configuration of the <see cref="ResetEventsPlugIn"/>.
/// </summary>
public class ResetEventsConfiguration
{
    /// <summary>
    /// The first day of the day numbers.
    /// </summary>
    public static readonly DateTime DayNumberEpoch = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

    /// <summary>
    /// Gets or sets the number of entries per character, day and event.
    /// </summary>
    [Display(Name = "Entries per day", Description = "Entries into Blood Castle and into Devil Square per character and day (each event counts separately). The counters are the character attributes 'Blood Castle entries' / 'Devil Square entries', e.g. reset by the website.")]
    public int EntriesPerDay { get; set; } = 2;

    /// <summary>
    /// Gets or sets the hour of the day at which the daily entries are reset.
    /// </summary>
    [Display(Name = "Daily reset hour")]
    public int DailyResetHour { get; set; } = 6;

    /// <summary>
    /// Gets or sets the chance of the jewel of the players who finished without winning.
    /// </summary>
    [Display(Name = "Finisher jewel chance", Description = "Chance (0.5 = 50 %) of one jewel of the level for the players who finished without winning (Blood Castle: alive at the end; Devil Square: from the 4th place). The kind by the jewels of the level.")]
    public float FinisherJewelChance { get; set; } = 0.5f;

    /// <summary>
    /// Gets or sets the weights of the levels of the chaos weapons, from +1.
    /// </summary>
    [MemberOfAggregate]
    [Display(Name = "Chaos weapon level weights", Description = "Weights of the levels +1, +2, ... of the chaos weapons.")]
    public IList<int> ChaosWeaponLevelWeights { get; set; } = new List<int> { 35, 25, 20, 12, 8 };

    /// <summary>
    /// Gets or sets the chance of the option +8 of the chaos weapons (otherwise +4).
    /// </summary>
    [Display(Name = "Chaos weapon option +8 chance")]
    public float ChaosWeaponHighOptionChance { get; set; } = 0.3f;

    /// <summary>
    /// Gets or sets the levels of Blood Castle.
    /// </summary>
    [MemberOfAggregate]
    [Display(Name = "Blood Castle levels")]
    public ICollection<ResetEventTier> BloodCastle { get; set; } = CreateDefaultBloodCastle();

    /// <summary>
    /// Gets or sets the levels of Devil Square.
    /// </summary>
    [MemberOfAggregate]
    [Display(Name = "Devil Square levels")]
    public ICollection<ResetEventTier> DevilSquare { get; set; } = CreateDefaultDevilSquare();

    /// <summary>
    /// Gets the levels of the event type.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The levels, or <c>null</c> if the type has no levels by resets.</returns>
    public ICollection<ResetEventTier>? GetTiers(MiniGameType type) => type switch
    {
        MiniGameType.BloodCastle => this.BloodCastle,
        MiniGameType.DevilSquare => this.DevilSquare,
        _ => null,
    };

    /// <summary>
    /// Gets the level of the event for the reset count: the level with the highest minimum resets which the count reaches.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="resets">The reset count.</param>
    /// <returns>The level.</returns>
    public ResetEventTier? GetTierByResets(MiniGameType type, int resets) =>
        this.GetTiers(type)?.Where(t => t.MinimumResets <= resets).OrderByDescending(t => t.MinimumResets).FirstOrDefault();

    /// <summary>
    /// Gets the number of the day of the time (in the local time of the server), by the daily reset hour.
    /// </summary>
    /// <param name="localNow">The local time.</param>
    /// <returns>The day number.</returns>
    public int GetDayNumber(DateTime localNow) => (int)(localNow.AddHours(-Math.Clamp(this.DailyResetHour, 0, 23)).Date - DayNumberEpoch).TotalDays;

    // Blood Castle and Devil Square have the same levels 1-7 by resets; Blood Castle 8 is for 50 resets.
    private static List<ResetEventTier> CreateDefaultBloodCastle() =>
    [
        .. CreateDefaultDevilSquare(),
        new() { Level = 8, MinimumResets = 50, MonsterLevel = 400, Money = 700_000, LifeJewels = 2, CreationJewels = 2, GuardianJewels = 1 },
    ];

    private static List<ResetEventTier> CreateDefaultDevilSquare() =>
    [
        new() { Level = 1, MinimumResets = 0, MonsterLevel = 40, Money = 60_000, ChaosJewels = 2, BlessJewels = 1, FeatherChance = 0.01f, CrestChance = 0.01f },
        new() { Level = 2, MinimumResets = 5, MonsterLevel = 140, Money = 150_000, BlessJewels = 2, SoulJewels = 1, ChaosWeaponChance = 0.2f, FeatherChance = 0.025f, CrestChance = 0.025f },
        new() { Level = 3, MinimumResets = 10, MonsterLevel = 190, Money = 220_000, BlessJewels = 1, SoulJewels = 2, ChaosWeaponChance = 0.2f, FeatherChance = 0.04f, CrestChance = 0.04f },
        new() { Level = 4, MinimumResets = 15, MonsterLevel = 240, Money = 300_000, SoulJewels = 2, LifeJewels = 1, ChaosWeaponChance = 0.2f, FeatherChance = 0.06f, CrestChance = 0.06f },
        new() { Level = 5, MinimumResets = 22, MonsterLevel = 300, Money = 420_000, SoulJewels = 1, LifeJewels = 1, CreationJewels = 1 },
        new() { Level = 6, MinimumResets = 30, MonsterLevel = 340, Money = 520_000, LifeJewels = 2, CreationJewels = 1 },
        new() { Level = 7, MinimumResets = 38, MonsterLevel = 375, Money = 600_000, LifeJewels = 2, CreationJewels = 1, GuardianJewels = 1 },
    ];
}
