// <copyright file="SpeedHackDetectPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.Resets;
using MUnique.OpenMU.Pathfinding;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A feature plugin that provides configuration and acts as a trigger control for speedhack anti-cheat checks.
/// </summary>
[PlugIn]
[Display(Name = "Speedhack Anti-Cheat", Description = "Detects and prevents player walk and attack speedhacking.")]
[Guid("A95A8D2F-A0C3-442E-995C-005B5C1B42D2")]
public class SpeedHackDetectPlugIn : IFeaturePlugIn, ISupportCustomConfiguration<SpeedHackDetectConfiguration>, ISupportDefaultCustomConfiguration, ISpeedHackCheatCheckPlugIn
{
    private const int NormalStepDelayMs = 300;
    private readonly ConditionalWeakTable<Player, SpeedHackState> _playerStates = new();

    /// <inheritdoc/>
    public SpeedHackDetectConfiguration? Configuration { get; set; }

    /// <inheritdoc/>
    public object CreateDefaultConfig() => new SpeedHackDetectConfiguration();

    /// <inheritdoc/>
    public async ValueTask WalkCheatCheckAsync(Player player, Memory<WalkingStep> steps, SpeedHackCheckEventArgs eventArgs)
    {
        if (steps.IsEmpty || IsServerControlled(player))
        {
            return;
        }

        var config = this.Configuration;
        if (config is null)
        {
            return;
        }

        bool isSafezone = player.IsAtSafezone();
        bool shouldRecordViolation = false;
        var startPoint = steps.Span[0].From;
        var state = this.GetState(player);

        // A walk which starts far from the server position is a desync (e.g. MU Helper pulls the character back to its
        // spot): PlayerMovement resynchronizes the client. Counting its tiles would flag a speedhack that isn't one.
        if (startPoint.EuclideanDistanceTo(player.Position) > config.MaxAllowedWalkStartOffset)
        {
            lock (state.Lock)
            {
                state.RecentWalks.Clear();
                state.LastWalkStartTime = DateTime.MinValue;
            }

            return;
        }

        lock (state.Lock)
        {
            if (isSafezone)
            {
                state.RecentWalks.Clear();
                state.LastWalkStartTime = DateTime.MinValue;
            }
            else
            {
                var now = DateTime.UtcNow;
                if (state.LastWalkStartTime > DateTime.MinValue)
                {
                    if (now - state.LastWalkStartTime > TimeSpan.FromSeconds(2))
                    {
                        state.RecentWalks.Clear();
                    }
                }

                state.RecentWalks.Enqueue(new WalkHistoryEntry { Time = now, StartPoint = startPoint });
                state.LastWalkStartTime = now;

                while (state.RecentWalks.Count > 5)
                {
                    state.RecentWalks.Dequeue();
                }

                if (state.RecentWalks.Count >= 3)
                {
                    var first = state.RecentWalks.Peek();
                    var elapsed = now - first.Time;

                    // Compute cumulative Chebyshev path length between consecutive start positions.
                    var cumulativeTiles = 0;
                    Point? prevPoint = null;
                    foreach (var entry in state.RecentWalks)
                    {
                        if (prevPoint.HasValue)
                        {
                            cumulativeTiles += Math.Max(
                                Math.Abs((int)entry.StartPoint.X - prevPoint.Value.X),
                                Math.Abs((int)entry.StartPoint.Y - prevPoint.Value.Y));
                        }

                        prevPoint = entry.StartPoint;
                    }

                    if (cumulativeTiles > 0)
                    {
                        double stepDelayMs = player.StepDelay.TotalMilliseconds;
                        double scalingFactor = stepDelayMs / NormalStepDelayMs;

                        const double BaseStepDelayMarginMs = 50.0;
                        const double MinStepDelayMs = 50.0;
                        double stepDelayMarginMs = BaseStepDelayMarginMs * scalingFactor;
                        double checkStepDelayMs = Math.Max(Math.Min(MinStepDelayMs, stepDelayMs), stepDelayMs - stepDelayMarginMs);
                        var expectedTime = TimeSpan.FromMilliseconds(cumulativeTiles * checkStepDelayMs);
                        var deficit = expectedTime - elapsed;
                        var tolerance = TimeSpan.FromMilliseconds(config.WalkSpeedToleranceMs * scalingFactor);

                        if (deficit > tolerance)
                        {
                            player.Logger.LogWarning(
                                "Speedhack detected on walk for player {0}: traveled {1} tiles in {2}ms (expected at least {3}ms). Deficit: {4}ms.",
                                player.Name,
                                cumulativeTiles,
                                elapsed.TotalMilliseconds,
                                expectedTime.TotalMilliseconds,
                                deficit.TotalMilliseconds);
                            shouldRecordViolation = true;
                            state.RecentWalks.Clear(); // Clear to avoid double triggers
                            state.LastWalkStartTime = DateTime.MinValue; // Reset tracker
                        }
                    }
                }
            }
        }

        if (shouldRecordViolation)
        {
            eventArgs.IsCheatDetected = true;
            await this.RecordViolationAsync(player, state, config).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async ValueTask AttackCheatCheckAsync(Player player, SpeedHackCheckEventArgs eventArgs)
    {
        if (player.Attributes is not { } attributes || IsServerControlled(player))
        {
            return;
        }

        var config = this.Configuration;
        if (config is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var minIntervalMs = GetMinimumIntervalMs(player, attributes, eventArgs.Skill, config);
        var state = this.GetState(player);
        bool shouldRecordViolation = false;

        // game masters change their stats with commands: no stat check for them (the timing checks stay)
        if (config.CheckStatPoints
            && player.SelectedCharacter?.CharacterStatus != CharacterStatus.GameMaster
            && now - state.LastStatCheckTime > TimeSpan.FromSeconds(config.StatCheckIntervalSeconds))
        {
            state.LastStatCheckTime = now;
            var owned = StatPointsCalculator.GetOwnedPoints(player);
            var allowed = StatPointsCalculator.GetPointsOfResetsAndLevel(player);
            if (owned > (allowed * (1 + config.StatPointsMarginShare)) + config.StatPointsMargin)
            {
                player.Logger.LogError(
                    "Stat check failed for player {0}: {1} stat points (invested + free), but resets {2}, level {3} and the kill quests give {4}. Attack speed {5}.",
                    player.Name,
                    owned,
                    (int)attributes[Stats.Resets],
                    (int)attributes[Stats.Level],
                    allowed,
                    attributes[Stats.AttackSpeed]);
                await this.RecordViolationAsync(player, state, config, "stat check").ConfigureAwait(false);
            }
        }

        lock (state.Lock)
        {
            if (state.LastAttackTokenUpdateTime == DateTime.MinValue)
            {
                state.LastAttackTokenUpdateTime = now;
                state.AttackTokens = config.MaxAttackTokens - 1.0;
                return;
            }

            var elapsedMs = (now - state.LastAttackTokenUpdateTime).TotalMilliseconds;
            state.LastAttackTokenUpdateTime = now;

            var regen = elapsedMs / minIntervalMs;
            state.AttackTokens = Math.Min(config.MaxAttackTokens, state.AttackTokens + regen);

            if (state.AttackTokens >= 1.0)
            {
                state.AttackTokens -= 1.0;
                return;
            }

            shouldRecordViolation = true;
        }

        if (shouldRecordViolation)
        {
            player.Logger.LogWarning(
                "Speedhack detected on attack for player {0}: {1} faster than {2:0} ms (attack speed {3}).",
                player.Name,
                eventArgs.Skill?.Name.ToString() ?? "normal attack",
                minIntervalMs,
                attributes[Stats.AttackSpeed]);
            eventArgs.IsCheatDetected = true;
            await this.RecordViolationAsync(player, state, config).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public ValueTask ResetMovementStateAsync(Player player)
    {
        var state = this.GetState(player);
        lock (state.Lock)
        {
            state.LastWalkStartTime = DateTime.MinValue;
            state.RecentWalks.Clear();
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Gets the warning count for the player (used in diagnostics/testing).
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The warning count.</returns>
    public int GetWarningCount(Player player)
    {
        var state = this.GetState(player);
        lock (state.Lock)
        {
            return state.AlertTimes.Count;
        }
    }

    /// <summary>
    /// Sets the last alert time for the player (used in diagnostics/testing).
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="time">The time to set.</param>
    public void SetLastAlertTime(Player player, DateTime time)
    {
        var state = this.GetState(player);
        lock (state.Lock)
        {
            state.LastAlertTime = time;
        }
    }

    /// <summary>
    /// Determines whether the player's actions originate on the server instead of a game client.
    /// These checks validate what a client claims about its own timing, so there is nothing to
    /// validate for an offline player: the server itself paces its walks and attacks. Its MU Helper
    /// tick performs the same work cycle as the original client helper (recover, then attack), which
    /// draws two attack tokens from one 500 ms tick and eventually empties the bucket - so leaving
    /// these players in would only ever produce false positives, and with the default configuration
    /// those are answered with a persisted account ban.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns><c>true</c> if the player is controlled by the server; otherwise, <c>false</c>.</returns>
    private static bool IsServerControlled(Player player) => player is Offline.OfflinePlayer;

    /// <summary>
    /// The time between two actions below which an action draws on the token bucket: the share of what the character's
    /// own stats allow (plugin "Skill cast time": fix time, animation at its speed, harmony option; normal attacks), or
    /// the old formula by the attack speed without that plugin.
    /// </summary>
    private static double GetMinimumIntervalMs(Player player, MUnique.OpenMU.AttributeSystem.IAttributeSystem attributes, Skill? skill, SpeedHackDetectConfiguration config)
    {
        if (player.GameContext.FeaturePlugIns.GetPlugIn<SkillCastTimePlugIn>() is { Configuration: { } castConfig } castTime)
        {
            var allowedMs = skill is not null ? castTime.GetCastTime(player, skill).TotalMilliseconds : castConfig.NormalAttackMilliseconds;
            if (allowedMs > 0)
            {
                return allowedMs * config.ActionIntervalShare;
            }
        }

        return Math.Max(config.AttackSpeedMinIntervalMs, config.AttackSpeedBaseDelayMs - (attributes[Stats.AttackSpeed] * config.AttackSpeedScalingFactor));
    }

    private SpeedHackState GetState(Player player)
    {
        return this._playerStates.GetValue(player, p => new SpeedHackState(this.Configuration?.MaxAttackTokens ?? 5.0));
    }

    private async ValueTask RecordViolationAsync(Player player, SpeedHackState state, SpeedHackDetectConfiguration config, string check = "speed check")
    {
        var now = DateTime.UtcNow;
        bool shouldBan = false;
        bool shouldWarn = false;
        bool shouldDisconnect = false;

        lock (state.Lock)
        {
            if (now - state.LastAlertTime < TimeSpan.FromSeconds(config.AlertDebounceSeconds))
            {
                return;
            }

            state.LastAlertTime = now;
            state.AlertTimes.Enqueue(now);

            while (state.AlertTimes.Count > 0 && now - state.AlertTimes.Peek() > TimeSpan.FromHours(config.WarningHistoryHours))
            {
                state.AlertTimes.Dequeue();
            }

            player.Logger.LogWarning("Anti-cheat warning ({0}) issued for player {1}. Total warnings in last hour: {2}", check, player.Name, state.AlertTimes.Count);

            if (state.AlertTimes.Count > config.MaxWarnings)
            {
                if (config.AutoBan)
                {
                    player.Logger.LogError("Player {0} exceeded speedhack warning limit. Banning account {1} and disconnecting.", player.Name, player.Account?.LoginName);
                    if (player.Account is { } account)
                    {
                        account.State = AccountState.Banned;
                        shouldBan = true;
                    }
                }

                if (config.DisconnectOnViolation)
                {
                    shouldDisconnect = true;
                }

                if (!shouldBan && !shouldDisconnect)
                {
                    shouldWarn = true;
                }
            }
            else
            {
                shouldWarn = true;
            }
        }

        if (shouldBan)
        {
            await player.SaveProgressAsync().ConfigureAwait(false);
        }

        if (shouldBan || shouldDisconnect)
        {
            await player.DisconnectAsync().ConfigureAwait(false);
        }
        else if (shouldWarn)
        {
            await player.ShowBlueMessageAsync($"Warning: unusual activity detected ({check}).").ConfigureAwait(false);
        }
        else
        {
            // Do nothing if no actions are required.
        }
    }

    private readonly record struct WalkHistoryEntry
    {
        public DateTime Time { get; init; }

        public Point StartPoint { get; init; }
    }

    private class SpeedHackState
    {
        public SpeedHackState(double maxAttackTokens)
        {
            this.AttackTokens = maxAttackTokens;
        }

        public object Lock { get; } = new();

        public double AttackTokens { get; set; }

        public DateTime LastAttackTokenUpdateTime { get; set; } = DateTime.MinValue;

        public DateTime LastAlertTime { get; set; } = DateTime.MinValue;

        public Queue<DateTime> AlertTimes { get; } = new();

        public Queue<WalkHistoryEntry> RecentWalks { get; } = new();

        public DateTime LastWalkStartTime { get; set; } = DateTime.MinValue;

        public DateTime LastStatCheckTime { get; set; } = DateTime.MinValue;
    }
}
