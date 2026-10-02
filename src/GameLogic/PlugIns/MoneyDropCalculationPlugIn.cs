// <copyright file="MoneyDropCalculationPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// This plugin calculates the money drop of a killed monster independently of the experience
/// (and therefore independently of experience rates, happy hours etc.).
/// The parameters are configurable in the admin panel.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.MoneyDropCalculationPlugIn_Name), Description = nameof(PlugInResources.MoneyDropCalculationPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("0E140E6F-4BB1-49F6-98D6-1DF5D6C422BD")]
public class MoneyDropCalculationPlugIn : IMoneyDropCalculationPlugIn, ISupportCustomConfiguration<MoneyDropCalculationConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc />
    public MoneyDropCalculationConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new MoneyDropCalculationConfiguration();

    /// <inheritdoc />
    public ValueTask CalculateMoneyDropAsync(MoneyDropCalculationArgs args)
    {
        var configuration = this.Configuration ??= (MoneyDropCalculationConfiguration)this.CreateDefaultConfig();
        args.SplitEqually = configuration.SplitEquallyInParty;

        var killed = args.KilledObject;
        if (killed.IsSummonedMonster)
        {
            args.Amount = 0;
            return ValueTask.CompletedTask;
        }

        double level = killed.Attributes[Stats.Level];
        double maximumHealth = killed.Attributes[Stats.MaximumHealth];

        var money = (configuration.LevelFactor * level * (level + configuration.LevelOffset))
                    + (configuration.HealthFactor * maximumHealth);
        money = Math.Max(money, 0);

        if (configuration.ReduceForHighLevelKillers)
        {
            double killerLevel = args.Killer.Attributes?[Stats.TotalLevel] ?? 0f;
            if (killerLevel > level + 10)
            {
                money *= (level + 10) / killerLevel;
            }
        }

        money += configuration.MinimumMoney;
        money *= configuration.GlobalMultiplier;

        var mapNumber = args.Map.Definition.Number;
        foreach (var mapMultiplier in configuration.MapMultipliers)
        {
            if (mapMultiplier.MapNumber == mapNumber)
            {
                money *= mapMultiplier.Multiplier;
                break;
            }
        }

        var monsterNumber = args.Monster.Number;
        foreach (var monsterMultiplier in configuration.MonsterMultipliers)
        {
            if (monsterMultiplier.MonsterNumber == monsterNumber)
            {
                money *= monsterMultiplier.Multiplier;
                break;
            }
        }

        money *= GetOutleveledMultiplier(configuration, args, level);

        var variance = Math.Clamp(configuration.RandomVariance, 0f, 1f);
        if (variance > 0)
        {
            money *= 1.0 + (((Rand.NextDouble() * 2.0) - 1.0) * variance);
        }

        args.Amount = (uint)Math.Clamp(Math.Round(money), 0, int.MaxValue);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Monsters below the level of the reset tier of the strongest participant drop less money,
    /// so that farming masses of weak monsters doesn't pay more than the own spot.
    /// </summary>
    private static double GetOutleveledMultiplier(MoneyDropCalculationConfiguration configuration, MoneyDropCalculationArgs args, double monsterLevel)
    {
        if (configuration.ResetTiers.Count == 0)
        {
            return 1.0;
        }

        var resets = args.Participants.Count > 0
            ? args.Participants.Max(p => (int)(p.Attributes?[Stats.Resets] ?? 0))
            : (int)(args.Killer.Attributes?[Stats.Resets] ?? 0);
        MoneyDropCalculationConfiguration.ResetMonsterLevelTier? tier = null;
        foreach (var candidate in configuration.ResetTiers)
        {
            if (candidate.MinimumResetCount <= resets && (tier is null || candidate.MinimumResetCount > tier.MinimumResetCount))
            {
                tier = candidate;
            }
        }

        return tier is not null && monsterLevel < tier.MinimumMonsterLevel ? configuration.OutleveledMultiplier : 1.0;
    }
}
