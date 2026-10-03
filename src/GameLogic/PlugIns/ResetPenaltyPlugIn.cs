// <copyright file="ResetPenaltyPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Replaces the regular experience penalty for too weak monsters by a penalty which depends on the
/// reset count: every character gets the experience without the level penalty, except for monster
/// tiers it has outgrown (configured reset count reached) - there the regular level penalty applies
/// and the drop is reduced. Master experience keeps the regular formula.
/// For parties, the "Party experience" plugin should be active, so that every member is calculated separately.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.ResetPenaltyPlugIn_Name), Description = nameof(PlugInResources.ResetPenaltyPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("DE5DAA99-EFA1-4CF9-8F35-969A1A7142A5")]
public class ResetPenaltyPlugIn : IFeaturePlugIn, ISupportCustomConfiguration<ResetPenaltyConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    /// <inheritdoc />
    public ResetPenaltyConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new ResetPenaltyConfiguration();

    /// <summary>
    /// Calculates the base experience of a kill for the specified player, with the level penalty
    /// only if the player has outgrown the tier of the killed monster.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="killedObject">The killed object.</param>
    /// <returns>The base experience (before rates).</returns>
    public double CalculateBaseExperience(Player player, IAttackable killedObject)
    {
        var configuration = this.GetConfiguration();
        if (player.Attributes is not { } attributes || !this.IsPenalized(player, killedObject))
        {
            return killedObject.CalculateBaseExperience(0);
        }

        var level = configuration.UseLevelWithoutMasterLevel ? attributes[Stats.Level] : attributes[Stats.TotalLevel];
        return killedObject.CalculateBaseExperience(level);
    }

    /// <summary>
    /// Determines whether the player has outgrown the tier of the killed object.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="killedObject">The killed object.</param>
    /// <returns><c>true</c>, if the penalty applies.</returns>
    public bool IsPenalized(Player player, IAttackable killedObject)
    {
        if (player.CurrentMiniGame is { IgnoresResetPenalty: true })
        {
            return false;
        }

        var resetCount = (int)(player.Attributes?[Stats.Resets] ?? 0f);
        var monsterLevel = (int)killedObject.Attributes[Stats.Level];
        return this.GetConfiguration().IsPenalized(resetCount, monsterLevel);
    }

    /// <summary>
    /// Determines whether the drop of a kill is suppressed, because a receiver has outgrown the tier of the killed object.
    /// </summary>
    /// <param name="killedObject">The killed object.</param>
    /// <param name="receivers">The receivers of the drop (the drop group).</param>
    /// <returns><c>true</c>, if nothing should be dropped.</returns>
    public bool ShouldSuppressDrop(IAttackable killedObject, IEnumerable<Player> receivers)
    {
        if (!receivers.Any(receiver => this.IsPenalized(receiver, killedObject)))
        {
            return false;
        }

        var chance = Math.Clamp(this.GetConfiguration().DropChanceWhenPenalized, 0f, 1f);
        return !Rand.NextRandomBool((double)chance);
    }

    private ResetPenaltyConfiguration GetConfiguration()
    {
        return this.Configuration ??= (ResetPenaltyConfiguration)this.CreateDefaultConfig();
    }
}
