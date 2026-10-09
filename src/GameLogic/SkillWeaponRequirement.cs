// <copyright file="SkillWeaponRequirement.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

/// <summary>
/// The melee skills which need a weapon in hand (09.10.2026): Twisting Slash, Rageful Blow, Death Stab, Strike of
/// Destruction, Fire Slash, Power Slash, Spiral Slash, Flame Strike and their master versions. A staff or a shield
/// doesn't count, as in the original client. The client checks the same list (GameLogic/Combat/SkillWeaponRequirement.h).
/// </summary>
public static class SkillWeaponRequirement
{
    /// <summary>
    /// The numbers of the base skills which need a weapon.
    /// </summary>
    public static readonly IReadOnlySet<short> Skills = new HashSet<short> { 41, 42, 43, 55, 56, 57, 232, 236 };

    /// <summary>
    /// Determines whether the skill (or the skill which it replaces as a master skill) needs a weapon in hand.
    /// </summary>
    /// <param name="skill">The skill.</param>
    /// <returns><c>true</c>, if the skill needs a weapon.</returns>
    public static bool NeedsWeapon(Skill skill)
    {
        return Skills.Contains(skill.Number) || skill.GetBaseSkills().Any(s => Skills.Contains(s.Number));
    }

    /// <summary>
    /// Determines whether the player holds a weapon (swords, axes, maces, spears, bows; no staff, shield or ammunition).
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns><c>true</c>, if the player holds such a weapon.</returns>
    public static bool HasWeapon(Player player)
    {
        return player.Inventory?.EquippedItems.Any(item =>
                   (item.ItemSlot == InventoryConstants.LeftHandSlot || item.ItemSlot == InventoryConstants.RightHandSlot)
                   && item.Definition is { IsAmmunition: false, Group: <= 4 }) ?? false;
    }
}
