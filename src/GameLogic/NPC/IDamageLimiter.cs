// <copyright file="IDamageLimiter.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.NPC;

/// <summary>
/// Limits the damage which an <see cref="AttackableNpcBase"/> takes, e.g. for a boss which is
/// invulnerable during a phase, or whose health can't drop below the threshold of the next phase.
/// </summary>
public interface IDamageLimiter
{
    /// <summary>
    /// Limits the damage of a hit. It's called while the health of the npc is locked for other hits.
    /// </summary>
    /// <param name="npc">The npc which is hit.</param>
    /// <param name="damage">The damage of the hit.</param>
    /// <returns>The damage which the npc takes.</returns>
    uint LimitDamage(AttackableNpcBase npc, uint damage);
}
