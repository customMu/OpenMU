// <copyright file="VaultOptionText.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Web.AdminPanel.Services.Vault;

using System.Globalization;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.DataModel.Attributes;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Attributes;

/// <summary>
/// Describes item options with the texts of the game client, e.g. "Excellent Damage rate +10%"
/// instead of the formula of the power up ("+0.1 Excellent Damage Chance").
/// </summary>
internal static class VaultOptionText
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    /// <summary>
    /// Describes the options of an item, in the order of the item tooltip of the game.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>The texts.</returns>
    public static List<string> Describe(Item item)
    {
        var result = new List<string>();
        if (VaultItemDetails.DescribeRise(item) is { } rise)
        {
            result.Add(rise);
        }

        if (item.HasSkill)
        {
            var skill = item.Definition?.Skill?.Name.ToString();
            result.Add(string.IsNullOrEmpty(skill) ? "Skill" : $"{skill} skill");
        }

        if (item.ItemOptions.Any(o => o.ItemOption?.OptionType == ItemOptionTypes.Luck))
        {
            result.Add("Luck (success rate of Jewel of Soul +25%)");
            result.Add("Luck (critical damage rate +5%)");
        }

        foreach (var link in item.ItemOptions
                     .Where(o => o.ItemOption is not null && o.ItemOption.OptionType != ItemOptionTypes.Luck)
                     .OrderBy(o => Order(o.ItemOption!.OptionType))
                     .ThenBy(o => o.Index))
        {
            var option = link.ItemOption!;
            var level = option.LevelType == LevelType.ItemLevel ? item.Level : link.Level;
            var powerUp = option.LevelDependentOptions.FirstOrDefault(o => o.Level == level)?.PowerUpDefinition ?? option.PowerUpDefinition;
            if (Describe(option.OptionType, powerUp) is { } text)
            {
                result.Add(text);
            }
        }

        if (item.SocketCount > 0)
        {
            result.Add($"Sockets: {item.SocketCount}");
        }

        return result;
    }

    private static int Order(ItemOptionType? type)
        => type == ItemOptionTypes.Option ? 0
            : type == ItemOptionTypes.Excellent ? 1
            : type == ItemOptionTypes.Wing ? 2
            : type == ItemOptionTypes.AncientBonus ? 3
            : type == ItemOptionTypes.HarmonyOption ? 4
            : 5;

    private static string? Describe(ItemOptionType? type, PowerUpDefinition? powerUp)
    {
        if (powerUp?.TargetAttribute is not { } attribute || powerUp.Boost is not { } boost)
        {
            return null;
        }

        var value = boost.ConstantValue?.Value ?? 0;
        var aggregate = boost.ConstantValue?.AggregateType ?? AggregateType.AddRaw;
        if (boost.RelatedValues.FirstOrDefault() is { } related)
        {
            // e.g. "Increase Damage +level/20": the attribute grows with the level of the character.
            var divisor = related.InputOperand is > 0 and < 1 ? Math.Round(1 / related.InputOperand) : 0;
            var input = related.InputAttribute?.Id == Stats.TotalLevel.Id || related.InputAttribute?.Id == Stats.Level.Id ? "level" : related.InputAttribute?.Designation ?? "?";
            var amount = divisor > 0 ? $"{input}/{divisor.ToString(Culture)}" : $"{input}*{Number(related.InputOperand)}";
            var name = attribute.Id == Stats.PhysicalBaseDmg.Id ? "Increase Damage"
                : attribute.Id == Stats.WizardryBaseDmg.Id ? "Increase Wizardry Dmg"
                : attribute.Id == Stats.CurseBaseDmg.Id ? "Increase Curse Spell"
                : Name(attribute);
            return $"{name} +{amount}";
        }

        var id = attribute.Id;
        if (id == Stats.ManaAfterMonsterKillMultiplier.Id)
        {
            return $"Increases acquisition rate of Mana after hunting monsters +Mana/{Math.Round(1 / value).ToString(Culture)}";
        }

        if (id == Stats.HealthAfterMonsterKillMultiplier.Id)
        {
            return $"Increases acquisition rate of Life after hunting monsters +Life/{Math.Round(1 / value).ToString(Culture)}";
        }

        if (id == Stats.MoneyAmountRate.Id)
        {
            return $"Increases acquisition rate of Zen after hunting monsters +{Percent(aggregate, value)}";
        }

        if (type == ItemOptionTypes.Option && id == Stats.DefenseRatePvm.Id)
        {
            return $"Additional Defense rate +{Number(value)}";
        }

        var text = aggregate switch
        {
            AggregateType.Multiplicate => $"+{Percent(aggregate, value)}",
            _ when IsRate(id) => $"+{Percent(AggregateType.AddRaw, value)}",
            _ => $"+{Number(value)}",
        };
        return $"{Name(attribute)} {text}";
    }

    private static bool IsRate(Guid id)
        => id == Stats.ExcellentDamageChance.Id
           || id == Stats.CriticalDamageChance.Id
           || id == Stats.DamageReflection.Id
           || id == Stats.ArmorDamageDecrease.Id
           || id == Stats.HealthRecoveryMultiplier.Id;

    private static string Percent(AggregateType aggregate, float value)
    {
        var percent = aggregate == AggregateType.Multiplicate ? (value - 1) * 100 : value * 100;
        return Number((float)Math.Round(percent, 1)) + "%";
    }

    private static string Number(float value) => value.ToString("0.##", Culture);

    private static string Name(AttributeDefinition attribute)
    {
        var id = attribute.Id;
        return id == Stats.ExcellentDamageChance.Id ? "Excellent Damage rate"
            : id == Stats.PhysicalBaseDmgIncrease.Id ? "Increase Damage"
            : id == Stats.WizardryBaseDmgIncrease.Id ? "Increase Wizardry Dmg"
            : id == Stats.PhysicalBaseDmg.Id ? "Additional Damage"
            : id == Stats.WizardryBaseDmg.Id ? "Additional Wizardry Dmg"
            : id == Stats.CurseBaseDmg.Id ? "Additional Curse Spell"
            : id == Stats.AttackSpeedAny.Id ? "Increase Attacking(Wizardry)speed"
            : id == Stats.DefenseRatePvm.Id ? "Defense success rate"
            : id == Stats.DamageReflection.Id ? "Reflect damage"
            : id == Stats.ArmorDamageDecrease.Id ? "Damage Decrease"
            : id == Stats.MaximumMana.Id ? "Increase Max Mana"
            : id == Stats.MaximumHealth.Id ? "Increase Max HP"
            : id == Stats.DefenseBase.Id ? "Additional Defense"
            : id == Stats.HealthRecoveryMultiplier.Id ? "Automatic HP Recovery"
            : id == Stats.CriticalDamageChance.Id ? "Critical Damage rate"
            : attribute.Designation ?? "?";
    }
}
