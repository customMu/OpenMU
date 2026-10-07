// <copyright file="SkillSpeedCurve.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>
/// The stat whose speed the client action of a skill follows (one curve for all, see
/// <see cref="SkillCastTimeConfiguration.AttackSpeedCurveOffset"/>).
/// </summary>
public class SkillSpeedCurve
{
    /// <summary>
    /// Gets or sets the skill number.
    /// </summary>
    public short SkillNumber { get; set; }

    /// <summary>
    /// Gets or sets the stat which sets the animation speed.
    /// </summary>
    public SkillSpeedStat Speed { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"{this.SkillNumber}: {this.Speed}";
}
