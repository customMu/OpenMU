// <copyright file="SkillCastTimePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Every attack skill has a fix time: the client can't cast it faster, whatever the attack speed
/// (reached with 10 000 agility). Skill fix options 11, 12, 13 of the rank 7-8 weapons lower the fix time of 3 skills
/// of the class (the option value is the share, e.g. 0.25 = -25 %). The server refuses casts which come earlier.
/// </summary>
[PlugIn]
[Display(Name = "Skill cast time", Description = "Refuses skill casts faster than the fix time of the skill; skill fix options of rank 7-8 weapons lower it.")]
[Guid("8D2B6E14-7A39-4F05-B1C8-3E9D5A7F2C61")]
public class SkillCastTimePlugIn : IFeaturePlugIn, ISupportCustomConfiguration<SkillCastTimeConfiguration>, ISupportDefaultCustomConfiguration, ISkillCastTimeCheckPlugIn
{
    /// <summary>
    /// The skill fix option numbers of the fix time options (skill 1, 2, 3).
    /// </summary>
    public static readonly int[] OptionNumbers = [11, 12, 13];

    private readonly ConditionalWeakTable<Player, State> _states = new();

    private Dictionary<short, int>? _fixTimes;

    private Dictionary<short, SkillSpeedCurve>? _speedCurves;

    private SkillCastTimeConfiguration? _configuration;

    /// <inheritdoc />
    public SkillCastTimeConfiguration? Configuration
    {
        get => this._configuration;
        set
        {
            this._configuration = value;
            this._fixTimes = null;
            this._speedCurves = null;
        }
    }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new SkillCastTimeConfiguration();

    /// <inheritdoc />
    public void CheckCast(Player player, Skill skill, CancelEventArgs eventArgs)
    {
        if (!this.TryCast(player, skill))
        {
            eventArgs.Cancel = true;
        }
    }

    /// <inheritdoc />
    public void CheckNormalAttack(Player player, CancelEventArgs eventArgs)
    {
        if (!this.TryNormalAttack(player))
        {
            eventArgs.Cancel = true;
        }
    }

    /// <summary>
    /// Checks if the player may do a normal attack now and registers it.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns><c>true</c>, if the attack is allowed; <c>false</c>, if it comes too early and has to be ignored.</returns>
    public bool TryNormalAttack(Player player)
    {
        var milliseconds = this.Configuration?.NormalAttackMilliseconds ?? 0;
        return this.TryAct(player, TimeSpan.FromMilliseconds(milliseconds), "Normal attack");
    }

    /// <summary>
    /// Checks if the player may cast the skill now and registers the cast.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="skill">The skill.</param>
    /// <returns><c>true</c>, if the cast is allowed; <c>false</c>, if it comes too early and has to be ignored.</returns>
    public bool TryCast(Player player, Skill skill)
    {
        return this.TryAct(player, this.GetCastTime(player, skill), $"Skill {skill.Number}");
    }

    /// <summary>
    /// Gets the minimum time until the next cast after a cast of the skill: the fix time, lowered by the options of the
    /// weapons; below the fix speed at least the animation time at the speed of the player.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="skill">The skill.</param>
    /// <returns>The cast time, or zero if the skill isn't checked.</returns>
    public TimeSpan GetCastTime(Player player, Skill skill)
    {
        if (this.Configuration is not { } config)
        {
            return TimeSpan.Zero;
        }

        this._fixTimes ??= config.FixTimes.GroupBy(f => f.SkillNumber).ToDictionary(g => g.Key, g => g.First().Milliseconds);

        // a master skill (e.g. the strengthened Twisting Slash) has the fix time and the options of its base skill
        foreach (var candidate in skill.GetBaseSkills().Prepend(skill))
        {
            if (this._fixTimes.TryGetValue(candidate.Number, out var fixTime) && fixTime > 0)
            {
                // the skill fix option cuts the cast time of its skill directly (09.10.2026): the fix time and, below the
                // fix speed, the animation - no speed above the fix is needed for it
                var cut = Math.Clamp(this.GetOptionCut(player, candidate.Number), 0, 0.9);
                var milliseconds = Math.Max(config.MinimumCastMilliseconds, fixTime * (1 - cut));
                milliseconds = Math.Max(milliseconds, this.GetAnimationMilliseconds(player, candidate.Number, fixTime) * (1 - cut) * config.SpeedCheckShare);
                return TimeSpan.FromMilliseconds(milliseconds);
            }
        }

        return TimeSpan.Zero;
    }

    /// <summary>
    /// Gets the animation time of the skill at the attack / magic speed of the player, as the client plays it: the fix
    /// time x (offset + fix speed) / (offset + speed) - longer below the fix speed, shorter above it.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="skillNumber">The (base) skill number.</param>
    /// <param name="fixTime">The fix time of the skill in milliseconds.</param>
    /// <returns>The animation time in milliseconds, or 0 if the skill has no speed curve.</returns>
    public double GetAnimationMilliseconds(Player player, short skillNumber, int fixTime)
    {
        if (this.Configuration is not { } config || player.Attributes is not { } attributes)
        {
            return 0;
        }

        this._speedCurves ??= config.SpeedCurves.GroupBy(c => c.SkillNumber).ToDictionary(g => g.Key, g => g.First());
        if (!this._speedCurves.TryGetValue(skillNumber, out var curve))
        {
            return 0;
        }

        var (speed, fixSpeed, offset) = curve.Speed == SkillSpeedStat.MagicSpeed
            ? (attributes[Stats.MagicSpeed], config.MagicSpeedAtFix, config.MagicSpeedCurveOffset)
            : (attributes[Stats.AttackSpeed], config.AttackSpeedAtFix, config.AttackSpeedCurveOffset);
        if (offset + fixSpeed <= 0)
        {
            return 0;
        }

        // the fix time must be the animation time at the fix speed - not the floor of 0.15 s (no curve for such skills).
        // Above the fix speed the animation gets shorter than the fix time, but the cast never (the fix time is the
        // minimum); the skill fix options cut both (as in the client).
        return fixTime * (offset + fixSpeed) / (offset + Math.Max(0, speed));
    }

    /// <summary>
    /// Gets the share by which the skill fix options of the equipped weapons lower the fix time of the skill.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="skillNumber">The skill number.</param>
    /// <returns>The share, e.g. 0.25 for -25 %.</returns>
    public double GetOptionCut(Player player, short skillNumber)
    {
        if (this.Configuration is not { } config || player.Inventory is not { } inventory)
        {
            return 0;
        }

        var family = (player.SelectedCharacter?.CharacterClass?.Number ?? 0) / 4 * 4;
        double cut = 0;
        // only the weapon in the left hand (slot 0, the left weapon slot of the inventory window) counts
        foreach (var slot in new[] { InventoryConstants.LeftHandSlot })
        {
            if (inventory.GetItem(slot) is not { Durability: > 0.0, Definition: { } definition } item)
            {
                continue;
            }

            var weapon = config.Weapons.FirstOrDefault(w => w.Group == definition.Group && w.Number == definition.Number && w.ClassFamily == family)
                         ?? config.Weapons.FirstOrDefault(w => w.Group == definition.Group && w.Number == definition.Number && w.ClassFamily < 0);
            if (weapon is null)
            {
                continue;
            }

            foreach (var link in item.ItemOptions.Where(o => o.ItemOption?.OptionType == ItemOptionTypes.SkillFixOption))
            {
                var index = Array.IndexOf(OptionNumbers, link.ItemOption!.Number);
                var optionSkill = index switch { 0 => weapon.Skill1, 1 => weapon.Skill2, 2 => weapon.Skill3, _ => (short)-1 };
                if (optionSkill != skillNumber)
                {
                    continue;
                }

                var value = link.ItemOption.LevelDependentOptions.FirstOrDefault(l => l.Level == link.Level)?.PowerUpDefinition?.Boost?.ConstantValue?.Value
                            ?? link.ItemOption.PowerUpDefinition?.Boost?.ConstantValue?.Value
                            ?? 0;
                // two weapons with the option of the same skill (e.g. a DK with two swords): the better one counts, they don't add up
                cut = Math.Max(cut, value * (family == 8 ? config.ElfOptionFactor : 1));
            }
        }

        return cut;
    }

    private bool TryAct(Player player, TimeSpan interval, string action)
    {
        if (this.Configuration is not { } config || player is Offline.OfflinePlayer || interval <= TimeSpan.Zero)
        {
            return true;
        }

        var now = DateTime.UtcNow;
        var state = this._states.GetValue(player, _ => new State());
        lock (state)
        {
            if (now + TimeSpan.FromMilliseconds(config.ToleranceMilliseconds) < state.NextAllowed)
            {
                player.Logger.LogDebug("{action} of {player} refused: {early} ms too early.", action, player.Name, (state.NextAllowed - now).TotalMilliseconds);
                return false;
            }

            state.NextAllowed = (now > state.NextAllowed ? now : state.NextAllowed) + interval;
            return true;
        }
    }

    private sealed class State
    {
        public DateTime NextAllowed { get; set; } = DateTime.MinValue;
    }
}
