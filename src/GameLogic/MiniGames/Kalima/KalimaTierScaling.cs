// <copyright file="KalimaTierScaling.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Kalima;

using System.Collections.Concurrent;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.NPC;

/// <summary>
/// Scales the monsters of a Kalima map to the strength of a tier. Each regular monster type gets the
/// strength of the tier (health, damage, defense, level), so all of them are equally strong and all
/// Kalima maps are the same, just stronger. Other monsters (e.g. the Illusion of Kundun) can get a
/// target health relative to the tier.
/// </summary>
public sealed class KalimaTierScaling
{
    private readonly SimpleElement _damageMultiplier = new(1.0f, AggregateType.Multiplicate);
    private readonly SimpleElement _defenseMultiplier = new(1.0f, AggregateType.Multiplicate);
    private readonly SimpleElement _rateMultiplier = new(1.0f, AggregateType.Multiplicate);
    private readonly SimpleElement _levelIncrease = new(0f, AggregateType.AddRaw);
    private readonly SimpleElement _regularHealthMultiplier = new(1.0f, AggregateType.Multiplicate);
    private readonly ConcurrentDictionary<MonsterDefinition, RegularElements> _regularElements = new();

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
    /// <param name="packMultiplier">The additional multiplier of the health and damage, e.g. of the pack.</param>
    public void ApplyRegular(Monster monster, IElement? packMultiplier = null)
    {
        var attributes = monster.Attributes;
        if (packMultiplier is not null)
        {
            attributes.AddElement(packMultiplier, Stats.MaximumHealth);
            attributes.AddElement(packMultiplier, Stats.MinimumPhysBaseDmg);
            attributes.AddElement(packMultiplier, Stats.MaximumPhysBaseDmg);
        }

        if (this.Strength is not { } strength)
        {
            this.Apply(monster, this._regularHealthMultiplier, this._damageMultiplier);
            return;
        }

        var elements = this._regularElements.GetOrAdd(monster.Definition, definition => CreateRegularElements(definition, strength));
        attributes.AddElement(elements.Health, Stats.MaximumHealth);
        attributes.AddElement(elements.Damage, Stats.MinimumPhysBaseDmg);
        attributes.AddElement(elements.Damage, Stats.MaximumPhysBaseDmg);
        attributes.AddElement(elements.Defense, Stats.DefenseBase);
        attributes.AddElement(elements.Rate, Stats.AttackRatePvm);
        attributes.AddElement(elements.Rate, Stats.DefenseRatePvm);
        attributes.AddElement(elements.Level, Stats.Level);
        monster.Health = (int)attributes[Stats.MaximumHealth];
    }

    /// <summary>
    /// Applies the strength of the tier relative to the average of the spawns: the average monster gets the strength
    /// of the tier, the differences between the monster types (e.g. the bosses of an event) stay.
    /// </summary>
    /// <param name="monster">The monster.</param>
    public void ApplyRelative(Monster monster)
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

    private static RegularElements CreateRegularElements(MonsterDefinition definition, KalimaStrength strength)
    {
        var level = KalimaStrengthCalculator.GetValue(definition, Stats.Level);
        return new RegularElements(
            new SimpleElement(GetRatio(strength.Health, KalimaStrengthCalculator.GetValue(definition, Stats.MaximumHealth)), AggregateType.Multiplicate),
            new SimpleElement(GetRatio(strength.Damage, KalimaStrengthCalculator.GetValue(definition, Stats.MaximumPhysBaseDmg)), AggregateType.Multiplicate),
            new SimpleElement(GetRatio(strength.Defense, KalimaStrengthCalculator.GetValue(definition, Stats.DefenseBase)), AggregateType.Multiplicate),
            new SimpleElement(GetRatio(strength.Level, level), AggregateType.Multiplicate),
            new SimpleElement(strength.Level > 0 && level > 0 ? strength.Level - level : 0, AggregateType.AddRaw));
    }

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

    /// <summary>
    /// The elements which scale a regular monster type to the strength of the tier.
    /// </summary>
    private sealed record RegularElements(SimpleElement Health, SimpleElement Damage, SimpleElement Defense, SimpleElement Rate, SimpleElement Level);
}
