// <copyright file="IllusionWeaponOptions.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.IllusionOfNoria;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// The extra options of the skill fix slot of a rank 7-8 weapon (Illusion of Noria, 09.10.2026): an Echo gives a random
/// one of 4 options - the fix time of its skill (11-13) or HP steal (14), MP steal (15), Double damage (16). Like the fix
/// time cut, only the weapon in the left hand (slot 0) counts; they work against monsters and players. The values by
/// level come from the options (tools/balance/illusion_extra_options.py, Server/illusion-extra-options.sql).
/// </summary>
public static class IllusionWeaponOptions
{
    /// <summary>The option number of HP steal: a share of the dealt damage comes back as health.</summary>
    public const short HealthSteal = 14;

    /// <summary>The option number of MP steal: a share of the dealt damage comes back as mana.</summary>
    public const short ManaSteal = 15;

    /// <summary>The option number of Double damage: the chance of a double hit.</summary>
    public const short DoubleDamage = 16;

    /// <summary>
    /// Gets the option numbers which an Echo can give besides the fix time of its skill.
    /// </summary>
    public static IReadOnlyList<short> ExtraOptions { get; } = [HealthSteal, ManaSteal, DoubleDamage];

    /// <summary>
    /// Gets the value of an extra option of the weapon in the left hand of the player (e.g. 0.05 for 5 %), or 0.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="optionNumber">The option number, see <see cref="HealthSteal"/>.</param>
    /// <returns>The value.</returns>
    public static double GetValue(Player player, short optionNumber)
    {
        if (player.Inventory?.GetItem(InventoryConstants.LeftHandSlot) is not { Durability: > 0.0 } weapon)
        {
            return 0;
        }

        foreach (var link in weapon.ItemOptions)
        {
            if (link.ItemOption is { OptionType: { } type } option && type == ItemOptionTypes.SkillFixOption && option.Number == optionNumber)
            {
                return option.LevelDependentOptions.FirstOrDefault(l => l.Level == link.Level)?.PowerUpDefinition?.Boost?.ConstantValue?.Value
                       ?? option.PowerUpDefinition?.Boost?.ConstantValue?.Value
                       ?? 0;
            }
        }

        return 0;
    }

    /// <summary>
    /// Gives the attacker the HP and MP steal of its weapon after a hit.
    /// </summary>
    /// <param name="player">The attacking player.</param>
    /// <param name="hitInfo">The hit.</param>
    public static void ApplySteal(Player player, HitInfo hitInfo)
    {
        if (player.Attributes is not { } attributes)
        {
            return;
        }

        var damage = (double)hitInfo.HealthDamage + hitInfo.ShieldDamage;
        if (damage <= 0)
        {
            return;
        }

        if (GetValue(player, HealthSteal) is > 0 and var health)
        {
            attributes[Stats.CurrentHealth] = (float)Math.Min(attributes[Stats.MaximumHealth], attributes[Stats.CurrentHealth] + (damage * health));
        }

        if (GetValue(player, ManaSteal) is > 0 and var mana)
        {
            attributes[Stats.CurrentMana] = (float)Math.Min(attributes[Stats.MaximumMana], attributes[Stats.CurrentMana] + (damage * mana));
        }
    }
}
