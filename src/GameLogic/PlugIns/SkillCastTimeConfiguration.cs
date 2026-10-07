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
    /// Gets or sets the animation speed curves of the skills: how the animation time grows below the fix speed.
    /// </summary>
    /// <remarks>
    /// Skills which are not listed are checked at their fix time only, whatever the speed of the player.
    /// </remarks>
    [Display(Name = "Speed curves of the skills", Description = "Below the fix speed the animation of a skill takes fix x (base + factor x fix speed) / (base + factor x speed of the player).")]
    public ICollection<SkillSpeedCurve> SpeedCurves { get; set; } = CreateSpeedCurves();

    // generated:start
    private static ICollection<SkillFixTime> CreateFixTimes() => new List<SkillFixTime>
    {
        new() { SkillNumber = 2, Milliseconds = 279 }, // Meteorite
        new() { SkillNumber = 5, Milliseconds = 279 }, // Flame
        new() { SkillNumber = 8, Milliseconds = 279 }, // Twister
        new() { SkillNumber = 9, Milliseconds = 279 }, // Evil Spirit
        new() { SkillNumber = 10, Milliseconds = 673 }, // Hellfire
        new() { SkillNumber = 12, Milliseconds = 536 }, // Aqua Beam
        new() { SkillNumber = 13, Milliseconds = 279 }, // Cometfall
        new() { SkillNumber = 14, Milliseconds = 444 }, // Inferno
        new() { SkillNumber = 19, Milliseconds = 195 }, // Falling Slash
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
        new() { SkillNumber = 61, Milliseconds = 176 }, // Fire Burst
        new() { SkillNumber = 62, Milliseconds = 1467 }, // Earthshake
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

    // The play speed of the client action of a skill = base + factor x speed (client SetAttackSpeed, the speeds below
    // the fix). The actions are matched to the fix times above: the frames of each come out whole (e.g. 6 frames of the
    // DW spells at 279 ms). Not listed (fix time only): Rageful Blow and Earthshake (their speed doesn't depend on the
    // stats), Lance, Spiral Slash, Fire Slash, Mana Rays and Charge (the action can't be told from the fix time).
    private static ICollection<SkillSpeedCurve> CreateSpeedCurves() => new List<SkillSpeedCurve>
    {
        new() { SkillNumber = 2, Speed = SkillSpeedStat.MagicSpeed, PlaySpeedBase = 0.29f, SpeedFactor = 0.002f }, // Meteorite: PLAYER_SKILL_HAND
        new() { SkillNumber = 5, Speed = SkillSpeedStat.MagicSpeed, PlaySpeedBase = 0.29f, SpeedFactor = 0.002f }, // Flame: PLAYER_SKILL_HAND
        new() { SkillNumber = 8, Speed = SkillSpeedStat.MagicSpeed, PlaySpeedBase = 0.29f, SpeedFactor = 0.002f }, // Twister: PLAYER_SKILL_HAND
        new() { SkillNumber = 9, Speed = SkillSpeedStat.MagicSpeed, PlaySpeedBase = 0.29f, SpeedFactor = 0.002f }, // Evil Spirit: PLAYER_SKILL_HAND
        new() { SkillNumber = 10, Speed = SkillSpeedStat.MagicSpeed, PlaySpeedBase = 0.5f, SpeedFactor = 0.002f }, // Hellfire: PLAYER_SKILL_HELL
        new() { SkillNumber = 12, Speed = SkillSpeedStat.MagicSpeed, PlaySpeedBase = 0.4f, SpeedFactor = 0.002f }, // Aqua Beam: PLAYER_SKILL_FLASH
        new() { SkillNumber = 13, Speed = SkillSpeedStat.MagicSpeed, PlaySpeedBase = 0.29f, SpeedFactor = 0.002f }, // Cometfall: PLAYER_SKILL_HAND
        new() { SkillNumber = 14, Speed = SkillSpeedStat.MagicSpeed, PlaySpeedBase = 0.6f, SpeedFactor = 0.002f }, // Inferno: PLAYER_SKILL_INFERNO
        new() { SkillNumber = 19, Speed = SkillSpeedStat.AttackSpeed, PlaySpeedBase = 0.3f, SpeedFactor = 0.004f }, // Falling Slash: PLAYER_ATTACK_SKILL_SWORD
        new() { SkillNumber = 24, Speed = SkillSpeedStat.AttackSpeed, PlaySpeedBase = 0.3f, SpeedFactor = 0.004f }, // Triple Shot: PLAYER_ATTACK_BOW
        new() { SkillNumber = 38, Speed = SkillSpeedStat.MagicSpeed, PlaySpeedBase = 0.29f, SpeedFactor = 0.002f }, // Decay: PLAYER_SKILL_HAND
        new() { SkillNumber = 39, Speed = SkillSpeedStat.MagicSpeed, PlaySpeedBase = 0.29f, SpeedFactor = 0.002f }, // Ice Storm: PLAYER_SKILL_HAND
        new() { SkillNumber = 41, Speed = SkillSpeedStat.AttackSpeed, PlaySpeedBase = 0.24f, SpeedFactor = 0.004f }, // Twisting Slash: PLAYER_ATTACK_SKILL_WHEEL
        new() { SkillNumber = 43, Speed = SkillSpeedStat.AttackSpeed, PlaySpeedBase = 0.25f, SpeedFactor = 0.004f }, // Death Stab: PLAYER_ATTACK_DEATHSTAB
        new() { SkillNumber = 44, Speed = SkillSpeedStat.AttackSpeed, PlaySpeedBase = 0.3f, SpeedFactor = 0.004f }, // Crescent Moon Slash: PLAYER_ATTACK_SKILL_SWORD
        new() { SkillNumber = 46, Speed = SkillSpeedStat.AttackSpeed, PlaySpeedBase = 0.3f, SpeedFactor = 0.004f }, // Starfall: PLAYER_ATTACK_BOW
        new() { SkillNumber = 47, Speed = SkillSpeedStat.AttackSpeed, PlaySpeedBase = 0.3f, SpeedFactor = 0.004f }, // Impale: PLAYER_ATTACK_SKILL_SPEAR
        new() { SkillNumber = 51, Speed = SkillSpeedStat.AttackSpeed, PlaySpeedBase = 0.3f, SpeedFactor = 0.004f }, // Ice Arrow: PLAYER_ATTACK_BOW
        new() { SkillNumber = 52, Speed = SkillSpeedStat.AttackSpeed, PlaySpeedBase = 0.3f, SpeedFactor = 0.004f }, // Penetration: PLAYER_ATTACK_BOW
        new() { SkillNumber = 56, Speed = SkillSpeedStat.AttackSpeed, PlaySpeedBase = 0.25f, SpeedFactor = 0.004f }, // Power Slash: PLAYER_ATTACK_TWO_HAND_SWORD_TWO
        new() { SkillNumber = 61, Speed = SkillSpeedStat.AttackSpeed, PlaySpeedBase = 0.25f, SpeedFactor = 0.004f }, // Fire Burst: PLAYER_ATTACK_STRIKE
        new() { SkillNumber = 66, Speed = SkillSpeedStat.AttackSpeed, PlaySpeedBase = 0.25f, SpeedFactor = 0.004f }, // Force Wave: PLAYER_ATTACK_STRIKE
        new() { SkillNumber = 74, Speed = SkillSpeedStat.AttackSpeed, PlaySpeedBase = 0.25f, SpeedFactor = 0.004f }, // Fire Blast: PLAYER_ATTACK_STRIKE
        new() { SkillNumber = 78, Speed = SkillSpeedStat.AttackSpeed, PlaySpeedBase = 0.25f, SpeedFactor = 0.004f }, // Fire Scream: PLAYER_ATTACK_STRIKE
        new() { SkillNumber = 214, Speed = SkillSpeedStat.MagicSpeed, PlaySpeedBase = 0.25f, SpeedFactor = 0.002f }, // Drain Life: PLAYER_SKILL_DRAIN_LIFE
        new() { SkillNumber = 215, Speed = SkillSpeedStat.MagicSpeed, PlaySpeedBase = 0.25f, SpeedFactor = 0.002f }, // Chain Lightning: PLAYER_SKILL_CHAIN_LIGHTNING
        new() { SkillNumber = 223, Speed = SkillSpeedStat.MagicSpeed, PlaySpeedBase = 0.25f, SpeedFactor = 0.002f }, // Explosion: PLAYER_SKILL_SUMMON
        new() { SkillNumber = 224, Speed = SkillSpeedStat.MagicSpeed, PlaySpeedBase = 0.25f, SpeedFactor = 0.002f }, // Requiem: PLAYER_SKILL_SUMMON
        new() { SkillNumber = 225, Speed = SkillSpeedStat.MagicSpeed, PlaySpeedBase = 0.25f, SpeedFactor = 0.002f }, // Pollution: PLAYER_SKILL_SUMMON
        new() { SkillNumber = 230, Speed = SkillSpeedStat.MagicSpeed, PlaySpeedBase = 0.35f, SpeedFactor = 0.002f }, // Lightning Shock: PLAYER_SKILL_LIGHTNING_SHOCK
        new() { SkillNumber = 232, Speed = SkillSpeedStat.AttackSpeed, PlaySpeedBase = 0.3f, SpeedFactor = 0.004f }, // Strike of Destruction: PLAYER_SKILL_BLOW_OF_DESTRUCTION
        new() { SkillNumber = 235, Speed = SkillSpeedStat.AttackSpeed, PlaySpeedBase = 0.3f, SpeedFactor = 0.004f }, // Multi-Shot: PLAYER_ATTACK_BOW
        new() { SkillNumber = 236, Speed = SkillSpeedStat.MagicSpeed, PlaySpeedBase = 0.69f, SpeedFactor = 0.002f }, // Flame Strike: PLAYER_SKILL_FLAMESTRIKE
        new() { SkillNumber = 237, Speed = SkillSpeedStat.MagicSpeed, PlaySpeedBase = 0.55f, SpeedFactor = 0.004f }, // Gigantic Storm: PLAYER_SKILL_GIGANTICSTORM
        new() { SkillNumber = 238, Speed = SkillSpeedStat.AttackSpeed, PlaySpeedBase = 0.25f, SpeedFactor = 0.004f }, // Chaotic Diseier: PLAYER_ATTACK_STRIKE
        new() { SkillNumber = 260, Speed = SkillSpeedStat.AttackSpeed, PlaySpeedBase = 0.4f, SpeedFactor = 0.002f }, // Killing Blow: PLAYER_SKILL_THRUST (rage speed)
        new() { SkillNumber = 263, Speed = SkillSpeedStat.AttackSpeed, PlaySpeedBase = 0.3f, SpeedFactor = 0.002f }, // Dark Side: PLAYER_SKILL_DARKSIDE (rage speed)
        new() { SkillNumber = 264, Speed = SkillSpeedStat.AttackSpeed, PlaySpeedBase = 0.3f, SpeedFactor = 0.002f }, // Dragon Roar: PLAYER_SKILL_DRAGONLORE (rage speed)
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
