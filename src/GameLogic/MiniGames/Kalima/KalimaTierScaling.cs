// <copyright file="KalimaTierScaling.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Kalima;

using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.NPC;

/// <summary>
/// Scales the monsters of a Kalima map to the strength of a tier. The regular monsters get the
/// average strength of the tier, the differences between the monster types stay. Other monsters
/// (e.g. the Illusion of Kundun) can get a target health relative to the tier.
/// </summary>
public sealed class KalimaTierScaling
{
    private readonly SimpleElement _damageMultiplier = new(1.0f, AggregateType.Multiplicate);
    private readonly SimpleElement _defenseMultiplier = new(1.0f, AggregateType.Multiplicate);
    private readonly SimpleElement _rateMultiplier = new(1.0f, AggregateType.Multiplicate);
    private readonly SimpleElement _levelIncrease = new(0f, AggregateType.AddRaw);
    private readonly SimpleElement _regularHealthMultiplier = new(1.0f, AggregateType.Multiplicate);

    /// <summary>
    /// Initializes a new instance of the <see cref="KalimaTierScaling"/> class.
    /// </summary>
    /// <param name="strength">The strength of the tier, or <c>null</c> to keep the strength of the monsters.</param>
    /// <param name="regularSpawns">The spawns of the regular monsters of the map.</param>
    public KalimaTierScaling(KalimaStrength? strength, IReadOnlyCollection<MonsterSpawnArea> regularSpawns)
    {
        this.Strength = strength;
        if (strength is null)
        {
            return;
        }

        var averageLevel = GetAverage(regularSpawns, Stats.Level);
        this._regularHealthMultiplier.Value = GetRatio(strength.Health, GetAverage(regularSpawns, Stats.MaximumHealth));
        this._damageMultiplier.Value = GetRatio(strength.Damage, GetAverage(regularSpawns, Stats.MaximumPhysBaseDmg));
        this._defenseMultiplier.Value = GetRatio(strength.Defense, GetAverage(regularSpawns, Stats.DefenseBase));
        this._rateMultiplier.Value = GetRatio(strength.Level, averageLevel);
        this._levelIncrease.Value = strength.Level > 0 && averageLevel > 0 ? strength.Level - averageLevel : 0;
    }

    /// <summary>
    /// Gets the strength of the regular monsters of the tier.
    /// </summary>
    public KalimaStrength? Strength { get; }

    /// <summary>
    /// Applies the strength of the tier to a regular monster.
    /// </summary>
    /// <param name="monster">The monster.</param>
    public void ApplyRegular(Monster monster)
    {
        this.Apply(monster, this._regularHealthMultiplier, this._damageMultiplier);
    }

    /// <summary>
    /// Applies the strength of the tier to a special monster (e.g. a boss) with the specified maximum health.
    /// The damage, defense and level are the ones of the tier, the damage multiplied with the damage factor.
    /// </summary>
    /// <param name="monster">The monster.</param>
    /// <param name="maximumHealth">The maximum health of the monster.</param>
    /// <param name="damageFactor">The factor of the damage relative to the regular monsters of the tier.</param>
    /// <returns>The damage element of the monster, which can be changed (e.g. for an enrage).</returns>
    public SimpleElement ApplySpecial(Monster monster, float maximumHealth, float damageFactor = 1.0f)
    {
        var baseHealth = KalimaStrengthCalculator.GetValue(monster.Definition, Stats.MaximumHealth);
        var healthMultiplier = this.Strength is not null && baseHealth > 0 && maximumHealth > 0 ? maximumHealth / baseHealth : 1.0f;
        var damageMultiplier = this._damageMultiplier.Value * damageFactor;
        var health = new SimpleElement(healthMultiplier, AggregateType.Multiplicate);
        var damage = new SimpleElement(damageMultiplier, AggregateType.Multiplicate);
        this.Apply(monster, health, damage);
        return damage;
    }

    /// <summary>
    /// Adds an additional defense element to the monster.
    /// </summary>
    /// <param name="monster">The monster.</param>
    /// <param name="element">The element, e.g. a multiplier which grows during a fight.</param>
    public void AddDefense(Monster monster, IElement element)
    {
        monster.Attributes.AddElement(element, Stats.DefenseBase);
    }

    private static float GetAverage(IReadOnlyCollection<MonsterSpawnArea> spawns, AttributeDefinition attribute)
    {
        return spawns.Count == 0 ? 0 : spawns.Average(area => KalimaStrengthCalculator.GetValue(area.MonsterDefinition!, attribute));
    }

    private static float GetRatio(float target, float average) => target > 0 && average > 0 ? target / average : 1.0f;

    private void Apply(Monster monster, IElement healthMultiplier, IElement damageMultiplier)
    {
        var attributes = monster.Attributes;
        if (this.Strength is not null)
        {
            attributes.AddElement(healthMultiplier, Stats.MaximumHealth);
            attributes.AddElement(damageMultiplier, Stats.MinimumPhysBaseDmg);
            attributes.AddElement(damageMultiplier, Stats.MaximumPhysBaseDmg);
            attributes.AddElement(this._defenseMultiplier, Stats.DefenseBase);
            attributes.AddElement(this._rateMultiplier, Stats.AttackRatePvm);
            attributes.AddElement(this._rateMultiplier, Stats.DefenseRatePvm);
            attributes.AddElement(this._levelIncrease, Stats.Level);
        }

        monster.Health = (int)attributes[Stats.MaximumHealth];
    }
}
