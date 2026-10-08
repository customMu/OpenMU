// <copyright file="SpeedHackDetectConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.ComponentModel;

/// <summary>
/// Configuration for the speedhack detection anti-cheat system.
/// </summary>
public class SpeedHackDetectConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether to auto-ban players that are cheating with speedhacks.
    /// Off by default: the checks warn and at most disconnect, a ban is up to a game master who reads the log.
    /// </summary>
    [DefaultValue(false)]
    public bool AutoBan { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to disconnect players that are cheating with speedhacks.
    /// </summary>
    [DefaultValue(true)]
    public bool DisconnectOnViolation { get; set; } = true;

    /// <summary>
    /// Gets or sets the threshold of warnings a player receives before being banned/disconnected.
    /// </summary>
    [DefaultValue(5)]
    public int MaxWarnings { get; set; } = 5;

    /// <summary>
    /// Gets or sets the warning alert debounce period in seconds.
    /// Consecutive warnings within this period are ignored to avoid spamming/jitter.
    /// </summary>
    [DefaultValue(5)]
    public int AlertDebounceSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets the warning history expiration period in hours.
    /// Warnings older than this time are cleared from the player's history.
    /// </summary>
    [DefaultValue(1)]
    public int WarningHistoryHours { get; set; } = 1;

    /// <summary>
    /// Gets or sets the walk speed check tolerance threshold in milliseconds.
    /// Represents the maximum deficits allowed compared to expectations before a violation is flagged.
    /// </summary>
    [DefaultValue(900)]
    public int WalkSpeedToleranceMs { get; set; } = 900;

    /// <summary>
    /// Gets or sets the maximum distance offset between client walk start position and server position
    /// before resynchronizing (rubberbanding) the client.
    /// </summary>
    [DefaultValue(5)]
    public int MaxAllowedWalkStartOffset { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum tokens for the token bucket attack check.
    /// </summary>
    [DefaultValue(5.0)]
    public double MaxAttackTokens { get; set; } = 5.0;

    /// <summary>
    /// Gets or sets the base delay in milliseconds used in attack speed calculation.
    /// </summary>
    [DefaultValue(450.0)]
    public double AttackSpeedBaseDelayMs { get; set; } = 450.0;

    /// <summary>
    /// Gets or sets the scaling factor applied to attack speed attribute in delay calculation.
    /// </summary>
    [DefaultValue(1.2)]
    public double AttackSpeedScalingFactor { get; set; } = 1.2;

    /// <summary>
    /// Gets or sets the minimum delay floor in milliseconds between attacks.
    /// </summary>
    [DefaultValue(60.0)]
    public double AttackSpeedMinIntervalMs { get; set; } = 60.0;

    /// <summary>
    /// Gets or sets the share of the time which the character's own stats allow between two actions (plugin "Skill cast
    /// time": the fix time of the skill, its animation at the character's speed, the harmony option; 150 ms for a normal
    /// attack) below which an action counts against the token bucket. 0.75 = a quarter of margin for the network; a
    /// character of 0 resets who acts like one with 500 attack speed runs out of tokens, a 50 reset one at the fix doesn't.
    /// Without the plugin "Skill cast time" the old formula (base delay - attack speed x factor) is used.
    /// </summary>
    [DefaultValue(0.75)]
    public double ActionIntervalShare { get; set; } = 0.75;

    /// <summary>
    /// Gets or sets a value indicating whether the stat points of a character are checked: the points invested in the
    /// stats plus the free points can't be more than the resets (Reset Feature tiers), the levels since the last reset and
    /// the kill quests give. More points mean edited or duped stats (e.g. 500 attack speed at 0 resets).
    /// Game masters aren't checked (they change their stats with commands).
    /// </summary>
    [DefaultValue(true)]
    public bool CheckStatPoints { get; set; } = true;

    /// <summary>
    /// Gets or sets the stat points a character may have above the computed maximum (stat fruits, quest rewards which the
    /// check doesn't know). The allowed excess is this plus <see cref="StatPointsMarginShare"/> of the maximum.
    /// </summary>
    [DefaultValue(1000)]
    public int StatPointsMargin { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the share of the computed maximum of stat points a character may have above it.
    /// </summary>
    [DefaultValue(0.05)]
    public double StatPointsMarginShare { get; set; } = 0.05;

    /// <summary>
    /// Gets or sets how often (seconds) the stat points of an attacking character are checked.
    /// </summary>
    [DefaultValue(60)]
    public int StatCheckIntervalSeconds { get; set; } = 60;
}
