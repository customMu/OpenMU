// <copyright file="ZenAutoLootPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The zen of a kill goes straight into the inventory instead of dropping on the ground: to the owner of the drop
/// (the player or the party with the most damage), in a party split like a picked up pile (the shares of the kill,
/// members on the same map outside the safe zone). The client gets the same message as for a picked up pile;
/// the MU Helper fee is kept as well. If nobody can take it (maximum money), it drops as usual.
/// </summary>
[PlugIn]
[Display(Name = "Zen auto loot", Description = "The zen of a kill goes straight into the inventory of its owner (the player or party with the most damage) instead of dropping on the ground.")]
[Guid("D4A7C2E9-5B13-4F86-A0E2-8C6B3F9D1E57")]
public class ZenAutoLootPlugIn : IFeaturePlugIn, ISupportCustomConfiguration<ZenAutoLootConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc />
    public ZenAutoLootConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new ZenAutoLootConfiguration();

    /// <summary>
    /// Determines whether the zen of a kill of the drop owner is looted automatically.
    /// </summary>
    /// <param name="dropOwner">The owner of the drop.</param>
    /// <returns><c>true</c>, if the zen goes straight into the inventory.</returns>
    public bool IsActiveFor(Player dropOwner)
    {
        var configuration = this.Configuration ??= new ZenAutoLootConfiguration();
        return !configuration.OnlyForVip || (dropOwner.Attributes?[Stats.IsVip] ?? 0) > 0;
    }
}

/// <summary>
/// The configuration of the <see cref="ZenAutoLootPlugIn"/>.
/// </summary>
public class ZenAutoLootConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether only VIP accounts loot the zen automatically.
    /// </summary>
    [Display(Name = "Only for VIP", Description = "Only VIP accounts (premium) loot the zen automatically; in a party it's decided by the owner of the drop.")]
    public bool OnlyForVip { get; set; }
}
