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
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Every attack skill has a fix time: the client can't cast it faster, whatever the attack speed
/// (reached with 10 000 agility). Harmony options 11, 12, 13 of the rank 7-8 weapons lower the fix time of 3 skills
/// of the class (the option value is the share, e.g. 0.25 = -25 %). The server refuses casts which come earlier.
/// </summary>
[PlugIn]
[Display(Name = "Skill cast time", Description = "Refuses skill casts faster than the fix time of the skill; harmony options of rank 7-8 weapons lower it.")]
[Guid("8D2B6E14-7A39-4F05-B1C8-3E9D5A7F2C61")]
public class SkillCastTimePlugIn : IFeaturePlugIn, ISupportCustomConfiguration<SkillCastTimeConfiguration>, ISupportDefaultCustomConfiguration, ISkillCastTimeCheckPlugIn
{
    /// <summary>
    /// The harmony option numbers of the fix time options (skill 1, 2, 3).
    /// </summary>
    public static readonly int[] OptionNumbers = [11, 12, 13];

    private readonly ConditionalWeakTable<Player, State> _states = new();

    private Dictionary<short, int>? _fixTimes;

    private SkillCastTimeConfiguration? _configuration;

    /// <inheritdoc />
    public SkillCastTimeConfiguration? Configuration
    {
        get => this._configuration;
        set
        {
            this._configuration = value;
            this._fixTimes = null;
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

    /// <summary>
    /// Checks if the player may cast the skill now and registers the cast.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="skill">The skill.</param>
    /// <returns><c>true</c>, if the cast is allowed; <c>false</c>, if it comes too early and has to be ignored.</returns>
    public bool TryCast(Player player, Skill skill)
    {
        if (this.Configuration is not { } config || player is Offline.OfflinePlayer)
        {
            return true;
        }

        var interval = this.GetCastTime(player, skill);
        if (interval <= TimeSpan.Zero)
        {
            return true;
        }

        var now = DateTime.UtcNow;
        var state = this._states.GetValue(player, _ => new State());
        lock (state)
        {
            if (now + TimeSpan.FromMilliseconds(config.ToleranceMilliseconds) < state.NextAllowed)
            {
                player.Logger.LogDebug("Skill {skill} of {player} refused: {early} ms too early.", skill.Number, player.Name, (state.NextAllowed - now).TotalMilliseconds);
                return false;
            }

            state.NextAllowed = (now > state.NextAllowed ? now : state.NextAllowed) + interval;
            return true;
        }
    }

    /// <summary>
    /// Gets the minimum time until the next cast after a cast of the skill: the fix time, lowered by the options of the weapons.
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
                var cut = Math.Clamp(this.GetOptionCut(player, candidate.Number), 0, 0.9);
                return TimeSpan.FromMilliseconds(Math.Max(config.MinimumCastMilliseconds, fixTime * (1 - cut)));
            }
        }

        return TimeSpan.Zero;
    }

    /// <summary>
    /// Gets the share by which the harmony options of the equipped weapons lower the fix time of the skill.
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
        foreach (var slot in new[] { InventoryConstants.LeftHandSlot, InventoryConstants.RightHandSlot })
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

            foreach (var link in item.ItemOptions.Where(o => o.ItemOption?.OptionType == ItemOptionTypes.HarmonyOption))
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
                cut += value * (family == 8 ? config.ElfOptionFactor : 1);
            }
        }

        return cut;
    }

    private sealed class State
    {
        public DateTime NextAllowed { get; set; } = DateTime.MinValue;
    }
}
