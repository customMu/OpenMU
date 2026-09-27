// <copyright file="ResetBoostPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Resets;

using System.Runtime.InteropServices;
using MUnique.OpenMU.AttributeSystem;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A passive bonus for every reset: maximum health and mana, dealt damage and defense are multiplied
/// by (1 + resets * percentage / 100). The bonus follows the reset count immediately, e.g. after a reset.
/// Changed percentages apply when a character enters the game the next time.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.ResetBoostPlugIn_Name), Description = nameof(PlugInResources.ResetBoostPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("5C398B06-D89C-4301-818D-E9CFC14073BF")]
public class ResetBoostPlugIn : IFeaturePlugIn, ISupportCustomConfiguration<ResetBoostConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    /// <inheritdoc />
    public ResetBoostConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new ResetBoostConfiguration();

    /// <summary>
    /// Adds the reset boost to the attributes of the player. Called once, when the character got selected.
    /// </summary>
    /// <param name="player">The player.</param>
    public void ApplyTo(Player player)
    {
        if (player.Attributes is not { } attributes)
        {
            return;
        }

        var configuration = this.Configuration ??= (ResetBoostConfiguration)this.CreateDefaultConfig();
        var resets = attributes.GetOrCreateAttribute(Stats.Resets);

        attributes.AddElement(new ResetBoostElement(resets, configuration.HealthAndManaPercentPerReset), Stats.MaximumHealth);
        attributes.AddElement(new ResetBoostElement(resets, configuration.HealthAndManaPercentPerReset), Stats.MaximumMana);
        attributes.AddElement(new ResetBoostElement(resets, configuration.DamageAndDefensePercentPerReset), Stats.AttackDamageIncrease);
        attributes.AddElement(new ResetBoostElement(resets, configuration.DamageAndDefensePercentPerReset), Stats.DefenseFinal);
    }

    /// <summary>
    /// A multiplier of (1 + resets * percentage / 100), which follows the reset count.
    /// </summary>
    private sealed class ResetBoostElement : IElement
    {
        private readonly IElement _resets;
        private readonly float _percentPerReset;

        public ResetBoostElement(IElement resets, float percentPerReset)
        {
            this._resets = resets;
            this._percentPerReset = Math.Max(percentPerReset, 0f);
            this._resets.ValueChanged += this.OnResetsChanged;
        }

        /// <inheritdoc />
        public event EventHandler? ValueChanged;

        /// <inheritdoc />
        public float Value => 1f + (Math.Max(this._resets.Value, 0f) * this._percentPerReset / 100f);

        /// <inheritdoc />
        public AggregateType AggregateType => AggregateType.Multiplicate;

        private void OnResetsChanged(object? sender, EventArgs e)
        {
            this.ValueChanged?.Invoke(this, e);
        }
    }
}
