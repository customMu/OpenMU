// <copyright file="SkillCastTimeConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// The configuration of the <see cref="SkillCastTimePlugIn"/>.
/// </summary>
/// <remarks>
/// The tables between the generated markers come from tools/balance/skill_cast_time.py (the fix time of a skill = its
/// client animation at the attack / magic speed of 10 000 agility, not below 0.15 s) - must match the client
/// (GameLogic/Combat/SkillCastTime.h).
/// </remarks>
public class SkillCastTimeConfiguration
{
    /// <summary>
    /// Gets or sets the minimum time between two casts, whatever the options (milliseconds).
    /// </summary>
    [Display(Name = "Minimum cast time (ms)", Description = "Nothing casts faster, whatever the options of the weapon.")]
    public int MinimumCastMilliseconds { get; set; } = 120;

    /// <summary>
    /// Gets or sets the tolerance for network jitter (milliseconds): casts may come this much earlier in a row.
    /// </summary>
    [Display(Name = "Tolerance (ms)", Description = "Casts may arrive this much too early (network jitter) before they are refused.")]
    public int ToleranceMilliseconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the fix time of the skills; skills which are not listed are not checked.
    /// </summary>
    [Display(Name = "Fix time of the skills")]
    public ICollection<SkillFixTime> FixTimes { get; set; } = CreateFixTimes();

    /// <summary>
    /// Gets or sets the weapons with the harmony options which lower the fix time of 3 skills of their class.
    /// </summary>
    [Display(Name = "Weapons with fix time options", Description = "Harmony options 11, 12, 13 of these weapons lower the fix time of the 1st, 2nd, 3rd skill.")]
    public ICollection<WeaponFixSkills> Weapons { get; set; } = CreateWeapons();

    /// <summary>
    /// Gets or sets the factor of the option for the elf (agility is her damage stat, the extra agility to use the option is free).
    /// </summary>
    [Display(Name = "Option factor for the elf")]
    public float ElfOptionFactor { get; set; } = 0.6f;

    /// <summary>
    /// Gets or sets the attack speed of the fix time (10 000 agility + a median weapon), see the client SkillCastTime.h.
    /// </summary>
    [Display(Name = "Attack speed at the fix time")]
    public int AttackSpeedAtFix { get; set; } = 335;

    /// <summary>
    /// Gets or sets the magic speed of the fix time (10 000 agility + a median weapon), see the client SkillCastTime.h.
    /// </summary>
    [Display(Name = "Magic speed at the fix time")]
    public int MagicSpeedAtFix { get; set; } = 285;

    /// <summary>
    /// Gets or sets the offset of the speed curve of the attack speed (client SkillCastTimeOptions.h).
    /// </summary>
    /// <remarks>
    /// One curve for all skills: below the fix an action takes fix x (offset + speed at the fix) / (offset + speed). The
    /// offsets of attack and magic speed are equal in agility (0.03 and 0.025 per agility + 35 of a median weapon):
    /// (181 + 35) / 0.03 = (145 + 35) / 0.025 = 7 200, so every class is as much slower than its fix at the same agility.
    /// </remarks>
    [Display(Name = "Speed curve offset of the attack speed")]
    public float AttackSpeedCurveOffset { get; set; } = 181;

    /// <summary>
    /// Gets or sets the offset of the speed curve of the magic speed (client SkillCastTimeOptions.h).
    /// </summary>
    [Display(Name = "Speed curve offset of the magic speed")]
    public float MagicSpeedCurveOffset { get; set; } = 145;

    /// <summary>
    /// Gets or sets the share of the animation time at the speed of the player which is checked.
    /// </summary>
    /// <remarks>
    /// Leeway for the rounded fix times and the variants of an action (e.g. on a mount); 1 = the exact client time.
    /// </remarks>
    [Display(Name = "Share of the animation time at the player's speed", Description = "Below the fix speed a skill can't be cast faster than its animation at the attack / magic speed of the player, times this share.")]
    public float SpeedCheckShare { get; set; } = 0.9f;

    /// <summary>
    /// Gets or sets the minimum time between two normal attacks (milliseconds), 0 to not check them.
    /// </summary>
    /// <remarks>
    /// The client plays no attack action faster than 0.15 s (the floor of the fix time). Normal attacks and skills share
    /// the time: one action after the other.
    /// </remarks>
    [Display(Name = "Normal attack time (ms)", Description = "The minimum time between two normal attacks; 0 = not checked.")]
    public int NormalAttackMilliseconds { get; set; } = 150;

    /// <summary>
    /// Gets or sets the stat (attack / magic speed) whose curve the animation of each skill follows.
    /// </summary>
    /// <remarks>
    /// Skills which are not listed are checked at their fix time only, whatever the speed of the player.
    /// </remarks>
    [Display(Name = "Speed curves of the skills", Description = "The speed (attack / magic) the animation of a skill follows: fix x (offset + fix speed) / (offset + speed of the player).")]
    public ICollection<SkillSpeedCurve> SpeedCurves { get; set; } = CreateSpeedCurves();

    // generated:start
    private static ICollection<SkillFixTime> CreateFixTimes() => new List<SkillFixTime>
    {
        new() { SkillNumber = 1, Milliseconds = 279 }, // Poison
        new() { SkillNumber = 2, Milliseconds = 279 }, // Meteorite
        new() { SkillNumber = 3, Milliseconds = 279 }, // Lightning
        new() { SkillNumber = 4, Milliseconds = 279 }, // Fire Ball
        new() { SkillNumber = 5, Milliseconds = 279 }, // Flame
        new() { SkillNumber = 7, Milliseconds = 279 }, // Ice
        new() { SkillNumber = 8, Milliseconds = 279 }, // Twister
        new() { SkillNumber = 9, Milliseconds = 279 }, // Evil Spirit
        new() { SkillNumber = 10, Milliseconds = 673 }, // Hellfire
        new() { SkillNumber = 11, Milliseconds = 279 }, // Power Wave
        new() { SkillNumber = 12, Milliseconds = 536 }, // Aqua Beam
        new() { SkillNumber = 13, Milliseconds = 279 }, // Cometfall
        new() { SkillNumber = 14, Milliseconds = 444 }, // Inferno
        new() { SkillNumber = 17, Milliseconds = 279 }, // Energy Ball
        new() { SkillNumber = 19, Milliseconds = 195 }, // Falling Slash
        new() { SkillNumber = 20, Milliseconds = 171 }, // Lunge
        new() { SkillNumber = 21, Milliseconds = 174 }, // Uppercut
        new() { SkillNumber = 22, Milliseconds = 171 }, // Cyclone
        new() { SkillNumber = 23, Milliseconds = 177 }, // Slash
        new() { SkillNumber = 24, Milliseconds = 171 }, // Triple Shot
        new() { SkillNumber = 38, Milliseconds = 279 }, // Decay
        new() { SkillNumber = 39, Milliseconds = 279 }, // Ice Storm
        new() { SkillNumber = 41, Milliseconds = 329 }, // Twisting Slash
        new() { SkillNumber = 42, Milliseconds = 1158 }, // Rageful Blow
        new() { SkillNumber = 43, Milliseconds = 176 }, // Death Stab
        new() { SkillNumber = 44, Milliseconds = 195 }, // Crescent Moon Slash
        new() { SkillNumber = 45, Milliseconds = 279 }, // Lance
        new() { SkillNumber = 46, Milliseconds = 171 }, // Starfall
        new() { SkillNumber = 47, Milliseconds = 220 }, // Impale
        new() { SkillNumber = 51, Milliseconds = 171 }, // Ice Arrow
        new() { SkillNumber = 52, Milliseconds = 171 }, // Penetration
        new() { SkillNumber = 55, Milliseconds = 289 }, // Fire Slash
        new() { SkillNumber = 56, Milliseconds = 176 }, // Power Slash
        new() { SkillNumber = 57, Milliseconds = 253 }, // Spiral Slash
        new() { SkillNumber = 60, Milliseconds = 176 }, // Force
        new() { SkillNumber = 61, Milliseconds = 176 }, // Fire Burst
        new() { SkillNumber = 62, Milliseconds = 268 }, // Earthshake
        new() { SkillNumber = 66, Milliseconds = 176 }, // Force Wave
        new() { SkillNumber = 73, Milliseconds = 182 }, // Mana Rays
        new() { SkillNumber = 74, Milliseconds = 176 }, // Fire Blast
        new() { SkillNumber = 78, Milliseconds = 176 }, // Fire Scream
        new() { SkillNumber = 214, Milliseconds = 293 }, // Drain Life
        new() { SkillNumber = 215, Milliseconds = 244 }, // Chain Lightning
        new() { SkillNumber = 223, Milliseconds = 537 }, // Explosion
        new() { SkillNumber = 224, Milliseconds = 537 }, // Requiem
        new() { SkillNumber = 225, Milliseconds = 537 }, // Pollution
        new() { SkillNumber = 230, Milliseconds = 478 }, // Lightning Shock
        new() { SkillNumber = 232, Milliseconds = 268 }, // Strike of Destruction
        new() { SkillNumber = 235, Milliseconds = 171 }, // Multi-Shot
        new() { SkillNumber = 236, Milliseconds = 444 }, // Flame Strike
        new() { SkillNumber = 237, Milliseconds = 308 }, // Gigantic Storm
        new() { SkillNumber = 238, Milliseconds = 176 }, // Chaotic Diseier
        new() { SkillNumber = 260, Milliseconds = 449 }, // Killing Blow
        new() { SkillNumber = 263, Milliseconds = 536 }, // Dark Side
        new() { SkillNumber = 264, Milliseconds = 412 }, // Dragon Roar
        new() { SkillNumber = 269, Milliseconds = 171 }, // Charge
    };

    private static ICollection<WeaponFixSkills> CreateWeapons() => new List<WeaponFixSkills>
    {
        new() { Group = 0, Number = 17, ClassFamily = -1, Skill1 = 41, Skill2 = 43, Skill3 = 232 }, // Dark Breaker: Twisting Slash, Death Stab, Strike of Destruction
        new() { Group = 0, Number = 20, ClassFamily = -1, Skill1 = 41, Skill2 = 43, Skill3 = 232 }, // Knight Blade: Twisting Slash, Death Stab, Strike of Destruction
        new() { Group = 0, Number = 22, ClassFamily = -1, Skill1 = 41, Skill2 = 43, Skill3 = 232 }, // Bone Blade: Twisting Slash, Death Stab, Strike of Destruction
        new() { Group = 0, Number = 24, ClassFamily = -1, Skill1 = 41, Skill2 = 43, Skill3 = 232 }, // Daybreak: Twisting Slash, Death Stab, Strike of Destruction
        new() { Group = 0, Number = 18, ClassFamily = -1, Skill1 = 56, Skill2 = 41, Skill3 = 55 }, // Thunder Blade: Power Slash, Twisting Slash, Fire Slash
        new() { Group = 0, Number = 21, ClassFamily = -1, Skill1 = 56, Skill2 = 41, Skill3 = 55 }, // Dark Reign Blade: Power Slash, Twisting Slash, Fire Slash
        new() { Group = 0, Number = 23, ClassFamily = -1, Skill1 = 56, Skill2 = 41, Skill3 = 55 }, // Explosion Blade: Power Slash, Twisting Slash, Fire Slash
        new() { Group = 0, Number = 25, ClassFamily = -1, Skill1 = 56, Skill2 = 41, Skill3 = 55 }, // Sword Dancer: Power Slash, Twisting Slash, Fire Slash
        new() { Group = 0, Number = 31, ClassFamily = -1, Skill1 = 9, Skill2 = 237, Skill3 = 8 }, // Rune Blade: Evil Spirit, Gigantic Storm, Twister
        new() { Group = 5, Number = 11, ClassFamily = 12, Skill1 = 9, Skill2 = 237, Skill3 = 8 }, // Staff of Kundun: Evil Spirit, Gigantic Storm, Twister
        new() { Group = 5, Number = 11, ClassFamily = 0, Skill1 = 9, Skill2 = 38, Skill3 = 39 }, // Staff of Kundun: Evil Spirit, Decay, Ice Storm
        new() { Group = 5, Number = 9, ClassFamily = -1, Skill1 = 9, Skill2 = 38, Skill3 = 39 }, // Dragon Soul Staff: Evil Spirit, Decay, Ice Storm
        new() { Group = 5, Number = 12, ClassFamily = -1, Skill1 = 9, Skill2 = 38, Skill3 = 39 }, // Grand Viper Staff: Evil Spirit, Decay, Ice Storm
        new() { Group = 5, Number = 13, ClassFamily = -1, Skill1 = 9, Skill2 = 38, Skill3 = 39 }, // Platina Staff: Evil Spirit, Decay, Ice Storm
        new() { Group = 4, Number = 19, ClassFamily = -1, Skill1 = 235, Skill2 = 52, Skill3 = 46 }, // Great Reign Crossbow: Multi-Shot, Penetration, Starfall
        new() { Group = 4, Number = 20, ClassFamily = -1, Skill1 = 235, Skill2 = 52, Skill3 = 46 }, // Arrow Viper Bow: Multi-Shot, Penetration, Starfall
        new() { Group = 4, Number = 21, ClassFamily = -1, Skill1 = 235, Skill2 = 52, Skill3 = 46 }, // Sylph Wind Bow: Multi-Shot, Penetration, Starfall
        new() { Group = 4, Number = 22, ClassFamily = -1, Skill1 = 235, Skill2 = 52, Skill3 = 46 }, // Albatross Bow: Multi-Shot, Penetration, Starfall
        new() { Group = 2, Number = 12, ClassFamily = -1, Skill1 = 78, Skill2 = 61, Skill3 = 238 }, // Great Lord Scepter: Fire Scream, Fire Burst, Chaotic Diseier
        new() { Group = 2, Number = 14, ClassFamily = -1, Skill1 = 78, Skill2 = 61, Skill3 = 238 }, // Soleil Scepter: Fire Scream, Fire Burst, Chaotic Diseier
        new() { Group = 2, Number = 15, ClassFamily = -1, Skill1 = 78, Skill2 = 61, Skill3 = 238 }, // Shining Scepter: Fire Scream, Fire Burst, Chaotic Diseier
        new() { Group = 5, Number = 19, ClassFamily = -1, Skill1 = 230, Skill2 = 215, Skill3 = 225 }, // Storm Blitz Stick: Lightning Shock, Chain Lightning, Pollution
        new() { Group = 5, Number = 20, ClassFamily = -1, Skill1 = 230, Skill2 = 215, Skill3 = 225 }, // Eternal Wing Stick: Lightning Shock, Chain Lightning, Pollution
        new() { Group = 0, Number = 34, ClassFamily = -1, Skill1 = 264, Skill2 = 263, Skill3 = 260 }, // Piercing Blade Glove: Dragon Roar, Dark Side, Killing Blow
        new() { Group = 0, Number = 35, ClassFamily = -1, Skill1 = 264, Skill2 = 263, Skill3 = 260 }, // Phoenix Soul Star: Dragon Roar, Dark Side, Killing Blow
    };

    // generated:end

    // The speed the client action of a skill follows (client SetAttackSpeed). The actions were matched to the fix times
    // above: their frames come out whole at the fix speed (e.g. 6 frames of the DW spells at 279 ms; the skills of the
    // weapons and the first spells since 09.10.2026). Not listed (fix time
    // only): Rageful Blow (its speed doesn't depend on the stats), Lance, Spiral Slash, Fire Slash, Mana Rays and Charge
    // (the action can't be told from the fix time). Earthshake follows the attack speed since 08.10.2026 (DL command build).
    private static ICollection<SkillSpeedCurve> CreateSpeedCurves() => new List<SkillSpeedCurve>
    {
        new() { SkillNumber = 1, Speed = SkillSpeedStat.MagicSpeed }, // Poison
        new() { SkillNumber = 2, Speed = SkillSpeedStat.MagicSpeed }, // Meteorite
        new() { SkillNumber = 3, Speed = SkillSpeedStat.MagicSpeed }, // Lightning
        new() { SkillNumber = 4, Speed = SkillSpeedStat.MagicSpeed }, // Fire Ball
        new() { SkillNumber = 5, Speed = SkillSpeedStat.MagicSpeed }, // Flame
        new() { SkillNumber = 7, Speed = SkillSpeedStat.MagicSpeed }, // Ice
        new() { SkillNumber = 8, Speed = SkillSpeedStat.MagicSpeed }, // Twister
        new() { SkillNumber = 9, Speed = SkillSpeedStat.MagicSpeed }, // Evil Spirit
        new() { SkillNumber = 10, Speed = SkillSpeedStat.MagicSpeed }, // Hellfire
        new() { SkillNumber = 11, Speed = SkillSpeedStat.MagicSpeed }, // Power Wave
        new() { SkillNumber = 12, Speed = SkillSpeedStat.MagicSpeed }, // Aqua Beam
        new() { SkillNumber = 13, Speed = SkillSpeedStat.MagicSpeed }, // Cometfall
        new() { SkillNumber = 14, Speed = SkillSpeedStat.MagicSpeed }, // Inferno
        new() { SkillNumber = 17, Speed = SkillSpeedStat.MagicSpeed }, // Energy Ball
        new() { SkillNumber = 19, Speed = SkillSpeedStat.AttackSpeed }, // Falling Slash
        new() { SkillNumber = 20, Speed = SkillSpeedStat.AttackSpeed }, // Lunge
        new() { SkillNumber = 21, Speed = SkillSpeedStat.AttackSpeed }, // Uppercut
        new() { SkillNumber = 22, Speed = SkillSpeedStat.AttackSpeed }, // Cyclone
        new() { SkillNumber = 23, Speed = SkillSpeedStat.AttackSpeed }, // Slash
        new() { SkillNumber = 24, Speed = SkillSpeedStat.AttackSpeed }, // Triple Shot
        new() { SkillNumber = 38, Speed = SkillSpeedStat.MagicSpeed }, // Decay
        new() { SkillNumber = 39, Speed = SkillSpeedStat.MagicSpeed }, // Ice Storm
        new() { SkillNumber = 41, Speed = SkillSpeedStat.AttackSpeed }, // Twisting Slash
        new() { SkillNumber = 43, Speed = SkillSpeedStat.AttackSpeed }, // Death Stab
        new() { SkillNumber = 44, Speed = SkillSpeedStat.AttackSpeed }, // Crescent Moon Slash
        new() { SkillNumber = 46, Speed = SkillSpeedStat.AttackSpeed }, // Starfall
        new() { SkillNumber = 47, Speed = SkillSpeedStat.AttackSpeed }, // Impale
        new() { SkillNumber = 51, Speed = SkillSpeedStat.AttackSpeed }, // Ice Arrow
        new() { SkillNumber = 52, Speed = SkillSpeedStat.AttackSpeed }, // Penetration
        new() { SkillNumber = 56, Speed = SkillSpeedStat.AttackSpeed }, // Power Slash
        new() { SkillNumber = 60, Speed = SkillSpeedStat.AttackSpeed }, // Force
        new() { SkillNumber = 61, Speed = SkillSpeedStat.AttackSpeed }, // Fire Burst
        new() { SkillNumber = 62, Speed = SkillSpeedStat.AttackSpeed }, // Earthshake
        new() { SkillNumber = 66, Speed = SkillSpeedStat.AttackSpeed }, // Force Wave
        new() { SkillNumber = 74, Speed = SkillSpeedStat.AttackSpeed }, // Fire Blast
        new() { SkillNumber = 78, Speed = SkillSpeedStat.AttackSpeed }, // Fire Scream
        new() { SkillNumber = 214, Speed = SkillSpeedStat.MagicSpeed }, // Drain Life
        new() { SkillNumber = 215, Speed = SkillSpeedStat.MagicSpeed }, // Chain Lightning
        new() { SkillNumber = 223, Speed = SkillSpeedStat.MagicSpeed }, // Explosion
        new() { SkillNumber = 224, Speed = SkillSpeedStat.MagicSpeed }, // Requiem
        new() { SkillNumber = 225, Speed = SkillSpeedStat.MagicSpeed }, // Pollution
        new() { SkillNumber = 230, Speed = SkillSpeedStat.MagicSpeed }, // Lightning Shock
        new() { SkillNumber = 232, Speed = SkillSpeedStat.AttackSpeed }, // Strike of Destruction
        new() { SkillNumber = 235, Speed = SkillSpeedStat.AttackSpeed }, // Multi-Shot
        new() { SkillNumber = 236, Speed = SkillSpeedStat.MagicSpeed }, // Flame Strike
        new() { SkillNumber = 237, Speed = SkillSpeedStat.MagicSpeed }, // Gigantic Storm
        new() { SkillNumber = 238, Speed = SkillSpeedStat.AttackSpeed }, // Chaotic Diseier
        new() { SkillNumber = 260, Speed = SkillSpeedStat.AttackSpeed }, // Killing Blow
        new() { SkillNumber = 263, Speed = SkillSpeedStat.AttackSpeed }, // Dark Side
        new() { SkillNumber = 264, Speed = SkillSpeedStat.AttackSpeed }, // Dragon Roar
    };
}

/// <summary>
/// The fix (minimum) cast time of a skill.
/// </summary>
public class SkillFixTime
{
    /// <summary>
    /// Gets or sets the skill number.
    /// </summary>
    public short SkillNumber { get; set; }

    /// <summary>
    /// Gets or sets the fix time in milliseconds.
    /// </summary>
    public int Milliseconds { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"{this.SkillNumber}: {this.Milliseconds} ms";
}

/// <summary>
/// A weapon whose harmony options 11, 12, 13 lower the fix time of 3 skills.
/// </summary>
public class WeaponFixSkills
{
    /// <summary>
    /// Gets or sets the item group of the weapon.
    /// </summary>
    public byte Group { get; set; }

    /// <summary>
    /// Gets or sets the item number of the weapon.
    /// </summary>
    public short Number { get; set; }

    /// <summary>
    /// Gets or sets the class family (number of the 1st class: 0 DW, 4 DK, 8 elf, 12 MG, 16 DL, 20 SUM, 24 RF), or -1 for any.
    /// </summary>
    public int ClassFamily { get; set; } = -1;

    /// <summary>
    /// Gets or sets the skill of harmony option 11.
    /// </summary>
    public short Skill1 { get; set; }

    /// <summary>
    /// Gets or sets the skill of harmony option 12.
    /// </summary>
    public short Skill2 { get; set; }

    /// <summary>
    /// Gets or sets the skill of harmony option 13.
    /// </summary>
    public short Skill3 { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"{this.Group}/{this.Number} ({this.ClassFamily}): {this.Skill1}, {this.Skill2}, {this.Skill3}";
}
