// <copyright file="VaultItemDetails.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services.Vault;

using System.Globalization;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// Describes the values of an item like the tooltip of the game client: damage or defense, attack speed,
/// durability and requirements (white lines), and the rise of staffs and scepters (the first blue line).
/// </summary>
internal static class VaultItemDetails
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    /// <summary>
    /// Gets the white lines of the tooltip.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The lines, e.g. "One-Handed Damage: 194 ~ 209".</returns>
    public static List<string> Describe(Item item)
    {
        var lines = new List<string>();
        var definition = item.Definition!;
        var minimumDamage = Value(item, Stats.MinimumPhysBaseDmgByWeapon);
        var maximumDamage = Value(item, Stats.MaximumPhysBaseDmgByWeapon);
        if (minimumDamage is not null || maximumDamage is not null)
        {
            var hands = Value(item, Stats.IsTwoHandedWeaponEquipped) is > 0 ? "Two-Handed" : "One-Handed";
            lines.Add($"{hands} Damage: {Number(minimumDamage ?? 0)} ~ {Number(maximumDamage ?? 0)}");
        }

        if (Value(item, Stats.DefenseBase) is { } defense)
        {
            lines.Add($"Defense: {Number(defense)}");
        }

        if (definition.Group == 6 && Value(item, Stats.DefenseRatePvm) is { } defenseRate)
        {
            lines.Add($"Defense rate: {Number(defenseRate)}");
        }

        if (Value(item, Stats.AttackSpeedAny) is { } attackSpeed && item.IsWearable())
        {
            lines.Add($"Attack speed: {Number(attackSpeed)}");
        }

        if (item.IsWearable() && definition.Durability > 0)
        {
            lines.Add($"Durability: [{(int)item.Durability}/{item.GetMaximumDurabilityOfOnePiece()}]");
        }

        foreach (var requirement in definition.Requirements.Where(r => r.Attribute is not null))
        {
            var (attribute, value) = item.GetRequirement(requirement);
            if (value > 0 && RequirementName(attribute) is { } name)
            {
                lines.Add($"{name} Requirement: {value.ToString(Culture)}");
            }
        }

        return lines;
    }

    /// <summary>
    /// Gets the line of the rise of a staff, a stick, a book or a scepter, e.g. "Wizardry Dmg 99% rise".
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The line, or <c>null</c>.</returns>
    public static string? DescribeRise(Item item)
    {
        if (Value(item, Stats.StaffRise) is { } staffRise and > 0)
        {
            return $"Wizardry Dmg {Number(staffRise)}% rise";
        }

        if (Value(item, Stats.ScepterRise) is { } scepterRise and > 0)
        {
            return $"Pet Attack Dmg {Number(scepterRise)}% rise";
        }

        return null;
    }

    private static string? RequirementName(AttributeDefinition attribute)
    {
        var id = attribute.Id;
        return id == Stats.TotalStrength.Id ? "Strength"
            : id == Stats.TotalAgility.Id ? "Agility"
            : id == Stats.TotalEnergy.Id ? "Energy"
            : id == Stats.TotalVitality.Id ? "Vitality"
            : id == Stats.TotalLeadership.Id ? "Command"
            : id == Stats.Level.Id || id == Stats.TotalLevel.Id ? "Level"
            : null;
    }

    private static float? Value(Item item, AttributeDefinition attribute)
    {
        var powerUp = item.Definition!.BasePowerUpAttributes.FirstOrDefault(p => p.TargetAttribute?.Id == attribute.Id);
        if (powerUp is null)
        {
            return null;
        }

        var bonus = powerUp.BonusPerLevelTable?.BonusPerLevel.FirstOrDefault(b => b.Level == item.Level)?.AdditionalValue ?? 0;
        return powerUp.BaseValue + bonus;
    }

    private static string Number(float value) => Math.Round(value).ToString(Culture);
}
