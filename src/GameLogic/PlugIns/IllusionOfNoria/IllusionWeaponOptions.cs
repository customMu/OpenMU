// <copyright file="IllusionWeaponOptions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.IllusionOfNoria;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// The illusion options of a rank 7-8 weapon (Illusion of Noria, option type "Skill Fix Option"): each belongs to one of
/// the 3 skills of the weapon (for the class) and has one effect - Haste (11-13: the cast time of the skill), Vampiric
/// (14-16: health from the damage of the skill), Siphon (17-19: mana from the damage of the skill) or Fury (20-22: the
/// chance of a double hit of the skill); option number = 11 + 3 x effect + the index of the skill. An Echo gives a random
/// one of the 4 effects for its skill. Only the weapon in the left hand (slot 0) counts; they work against monsters and
/// players. The values by level come from the options (tools/balance/illusion_extra_options.py).
/// </summary>
public static class IllusionWeaponOptions
{
    /// <summary>The first option number (Haste of the 1st skill).</summary>
    public const short FirstOption = 11;

    /// <summary>
    /// The effects of the illusion options.
    /// </summary>
    public enum Effect
    {
        /// <summary>Haste: the cast time of the skill is cut (plugin "Skill cast time").</summary>
        Haste = 0,

        /// <summary>Vampiric: a share of the damage of the skill comes back as health.</summary>
        Vampiric = 1,

        /// <summary>Siphon: a share of the damage of the skill comes back as mana.</summary>
        Siphon = 2,

        /// <summary>Fury: the chance of a double hit of the skill.</summary>
        Fury = 3,
    }

    /// <summary>
    /// Gets the option number of an effect for the skill with the index (0-2) on the weapon.
    /// </summary>
    /// <param name="effect">The effect.</param>
    /// <param name="skillIndex">The index of the skill on the weapon (0-2).</param>
    /// <returns>The option number.</returns>
    public static short GetOptionNumber(Effect effect, int skillIndex) => (short)(FirstOption + (3 * (int)effect) + skillIndex);

    /// <summary>
    /// Splits an option number into its effect and the index of the skill, or <c>null</c> if it's no illusion option.
    /// </summary>
    /// <param name="optionNumber">The option number.</param>
    /// <returns>The effect and the index of the skill.</returns>
    public static (Effect Effect, int SkillIndex)? Split(int optionNumber)
    {
        var offset = optionNumber - FirstOption;
        return offset is >= 0 and < 12 ? ((Effect)(offset / 3), offset % 3) : null;
    }

    /// <summary>
    /// Gets the value of an effect of the weapon in the left hand of the player for the skill (e.g. 0.05 for 5 %), or 0.
    /// A master skill counts as its base skill.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="effect">The effect.</param>
    /// <param name="skill">The skill of the hit, or <c>null</c> for a normal attack (no effect).</param>
    /// <returns>The value.</returns>
    public static double GetValue(Player player, Effect effect, Skill? skill)
    {
        return player.GameContext.FeaturePlugIns.GetPlugIn<SkillCastTimePlugIn>()?.Configuration is { } config
            ? GetValue(player, effect, skill, config.Weapons)
            : 0;
    }

    /// <summary>
    /// Gets the value of an effect for the skill, see <see cref="GetValue(Player, Effect, Skill?)"/>.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="effect">The effect.</param>
    /// <param name="skill">The skill of the hit.</param>
    /// <param name="weapons">The rank 7-8 weapons and their skills (plugin "Skill cast time").</param>
    /// <returns>The value.</returns>
    internal static double GetValue(Player player, Effect effect, Skill? skill, ICollection<WeaponFixSkills> weapons)
    {
        if (skill is null
            || player.Inventory?.GetItem(InventoryConstants.LeftHandSlot) is not { Durability: > 0.0, Definition: { } definition } weapon)
        {
            return 0;
        }

        var family = (player.SelectedCharacter?.CharacterClass?.Number ?? 0) / 4 * 4;
        var skills = weapons.FirstOrDefault(w => w.Group == definition.Group && w.Number == definition.Number && w.ClassFamily == family)
                     ?? weapons.FirstOrDefault(w => w.Group == definition.Group && w.Number == definition.Number && w.ClassFamily < 0);
        if (skills is null)
        {
            return 0;
        }

        var hitSkills = skill.GetBaseSkills().Prepend(skill).Select(s => s.Number).ToHashSet();
        foreach (var link in weapon.ItemOptions)
        {
            if (link.ItemOption is not { OptionType: { } type } option || type != ItemOptionTypes.SkillFixOption
                || Split(option.Number) is not { } split || split.Effect != effect)
            {
                continue;
            }

            var optionSkill = split.SkillIndex switch { 0 => skills.Skill1, 1 => skills.Skill2, _ => skills.Skill3 };
            if (!hitSkills.Contains(optionSkill))
            {
                continue;
            }

            return option.LevelDependentOptions.FirstOrDefault(l => l.Level == link.Level)?.PowerUpDefinition?.Boost?.ConstantValue?.Value
                   ?? option.PowerUpDefinition?.Boost?.ConstantValue?.Value
                   ?? 0;
        }

        return 0;
    }

    /// <summary>
    /// Gives the attacker the Vampiric and Siphon of its weapon after a hit of the skill.
    /// </summary>
    /// <param name="player">The attacking player.</param>
    /// <param name="hitInfo">The hit.</param>
    /// <param name="skill">The skill of the hit.</param>
    public static void ApplySteal(Player player, HitInfo hitInfo, Skill? skill)
    {
        if (player.Attributes is not { } attributes || skill is null)
        {
            return;
        }

        var damage = (double)hitInfo.HealthDamage + hitInfo.ShieldDamage;
        if (damage <= 0)
        {
            return;
        }

        if (GetValue(player, Effect.Vampiric, skill) is > 0 and var health)
        {
            attributes[Stats.CurrentHealth] = (float)Math.Min(attributes[Stats.MaximumHealth], attributes[Stats.CurrentHealth] + (damage * health));
        }

        if (GetValue(player, Effect.Siphon, skill) is > 0 and var mana)
        {
            attributes[Stats.CurrentMana] = (float)Math.Min(attributes[Stats.MaximumMana], attributes[Stats.CurrentMana] + (damage * mana));
        }
    }
}
