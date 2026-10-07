// <copyright file="SkillSpeedCurve.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>
/// The animation speed curve of a skill: the client plays its action at base + factor x speed.
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

    /// <summary>
    /// Gets or sets the play speed of the action at speed 0.
    /// </summary>
    public float PlaySpeedBase { get; set; }

    /// <summary>
    /// Gets or sets the play speed per point of speed.
    /// </summary>
    public float SpeedFactor { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"{this.SkillNumber}: {this.PlaySpeedBase} + {this.SpeedFactor} x {this.Speed}";
}
