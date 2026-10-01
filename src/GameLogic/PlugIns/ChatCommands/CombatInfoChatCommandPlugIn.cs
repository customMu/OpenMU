// <copyright file="CombatInfoChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A chat command plugin which shows the combat numbers between the player and the monster he fought last:
/// hit and dodge chances, damage per hit in both directions and hits to kill.
/// The numbers are not calculated by a separate formula: the real damage calculation of the server is run
/// a few hundred times (it has no side effects), so the command also tests the combat formulas.
/// The game client sends it on Shift + right click on a monster ('/chance (monster id)').
/// </summary>
[Guid("5C1E7A93-2B64-4D0F-A8E1-6F3B9D2C4E71")]
[PlugIn]
[Display(Name = nameof(PlugInResources.CombatInfoChatCommandPlugIn_Name), Description = nameof(PlugInResources.CombatInfoChatCommandPlugIn_Description), ResourceType = typeof(PlugInResources))]
[ChatCommandHelp(Command, typeof(CombatInfoChatCommandPlugIn.Arguments), MinimumStatus)]
public class CombatInfoChatCommandPlugIn : ChatCommandPlugInBase<CombatInfoChatCommandPlugIn.Arguments>
{
    private const string Command = "/chance";

    private const CharacterStatus MinimumStatus = CharacterStatus.Normal;

    private const int Simulations = 2000;

    private const int MaximumTargetDistance = 20;

    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(15);

    private static readonly TimeSpan TargetTimeout = TimeSpan.FromSeconds(60);

    private static readonly ConditionalWeakTable<Player, StrongBox<DateTime>> LastUse = new();

    /// <inheritdoc />
    public override string Key => Command;

    /// <inheritdoc />
    public override CharacterStatus MinCharacterStatusRequirement => MinimumStatus;

    /// <inheritdoc />
    protected override async ValueTask DoHandleCommandAsync(Player player, Arguments arguments)
    {
        var lastUse = LastUse.GetOrCreateValue(player);
        var wait = lastUse.Value + Cooldown - DateTime.UtcNow;
        if (wait > TimeSpan.Zero)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.CombatInfoCooldownFormat), (int)Math.Ceiling(wait.TotalSeconds)).ConfigureAwait(false);
            return;
        }

        // the monster clicked in the client (Shift + right click), otherwise the monster fought last
        var monster = arguments.TargetId != 0 && player.GetObject(arguments.TargetId) is Monster clicked && player.GetDistanceTo(clicked) <= MaximumTargetDistance
            ? clicked
            : DateTime.UtcNow - player.LastCombatTime <= TargetTimeout ? player.LastCombatTarget as Monster : null;
        if (player.Attributes is not { } playerAttributes || monster is null)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.CombatInfoNoTarget)).ConfigureAwait(false);
            return;
        }

        lastUse.Value = DateTime.UtcNow;
        var attributes = monster.Attributes;
        await player.ShowLocalizedBlueMessageAsync(
            nameof(PlayerMessage.CombatInfoTargetFormat),
            monster.Definition.Designation.GetTranslation(player.Culture) ?? string.Empty,
            (int)attributes[Stats.Level],
            (int)attributes[Stats.MaximumHealth],
            (int)attributes[Stats.AttackRatePvm],
            (int)monster.GetDefenseRatePvm(),
            (int)attributes[Stats.DefensePvm]).ConfigureAwait(false);

        // monster -> player
        var received = await SimulateAsync(monster, player, null).ConfigureAwait(false);
        var attackDelay = monster.Definition.AttackDelay.TotalSeconds;
        var damagePerSecond = attackDelay > 0 ? received.HitChance * received.AverageDamage / attackDelay : 0;
        var maximumHealth = Math.Max(1, playerAttributes[Stats.MaximumHealth]);
        await player.ShowLocalizedBlueMessageAsync(
            nameof(PlayerMessage.CombatInfoDefenseFormat),
            Percent(received.HitChance),
            Percent(1 - received.HitChance),
            (int)received.AverageDamage,
            Percent(received.AverageDamage / maximumHealth),
            (int)damagePerSecond,
            (int)received.MaximumDamage,
            (int)maximumHealth).ConfigureAwait(false);

        // player -> monster: normal attack and the best learned attack skill
        var monsterHealth = Math.Max(1, attributes[Stats.MaximumHealth]);
        var normal = await SimulateAsync(player, monster, null).ConfigureAwait(false);
        await this.ShowAttackAsync(player, player.GetLocalizedMessage(nameof(PlayerMessage.CombatInfoNormalAttack)), normal, 1, monsterHealth).ConfigureAwait(false);

        // the skill selected in the client (no line for buffs and other skills without damage);
        // typed by hand without a skill number: the strongest learned attack skill
        if (arguments.SkillNumber != 0)
        {
            if (player.SkillList?.GetSkill(arguments.SkillNumber) is { Skill: { } activeSkill } activeEntry
                && IsAttackSkill(activeSkill))
            {
                var active = await SimulateAsync(player, monster, activeEntry).ConfigureAwait(false);
                await this.ShowAttackAsync(player, activeSkill.Name.GetTranslation(player.Culture) ?? string.Empty, active, Math.Max((int)activeSkill.NumberOfHitsPerAttack, 1), monsterHealth).ConfigureAwait(false);
            }

            return;
        }

        SkillEntry? bestSkill = null;
        var best = default(Result);
        var bestHits = 1;
        foreach (var skillEntry in player.SkillList?.Skills ?? [])
        {
            if (skillEntry.Skill is not { } skill || !IsAttackSkill(skill))
            {
                continue;
            }

            var result = await SimulateAsync(player, monster, skillEntry).ConfigureAwait(false);
            var hits = Math.Max((int)skill.NumberOfHitsPerAttack, 1);
            if (result.HitChance * result.AverageDamage * hits > best.HitChance * best.AverageDamage * bestHits)
            {
                (bestSkill, best, bestHits) = (skillEntry, result, hits);
            }
        }

        if (bestSkill?.Skill is { } bestSkillDefinition)
        {
            await this.ShowAttackAsync(player, bestSkillDefinition.Name.GetTranslation(player.Culture) ?? string.Empty, best, bestHits, monsterHealth).ConfigureAwait(false);
        }
    }

    private static bool IsAttackSkill(Skill skill)
    {
        return skill.DamageType is DamageType.Physical or DamageType.Wizardry or DamageType.Curse
               && skill.SkillType is SkillType.DirectHit or SkillType.AreaSkillAutomaticHits or SkillType.AreaSkillExplicitHits or SkillType.AreaSkillExplicitTarget;
    }

    private static async ValueTask<Result> SimulateAsync(IAttacker attacker, IAttackable defender, SkillEntry? skill)
    {
        var landed = 0;
        long damage = 0;
        uint maximum = 0;
        int critical = 0, excellent = 0, doubled = 0;
        for (var i = 0; i < Simulations; i++)
        {
            var hit = await attacker.CalculateDamageAsync(defender, skill, false).ConfigureAwait(false);
            var total = hit.HealthDamage + hit.ShieldDamage;
            if (total > 0)
            {
                landed++;
                damage += total;
                maximum = Math.Max(maximum, total);
                critical += hit.Attributes.HasFlag(DamageAttributes.Critical) ? 1 : 0;
                excellent += hit.Attributes.HasFlag(DamageAttributes.Excellent) ? 1 : 0;
                doubled += hit.Attributes.HasFlag(DamageAttributes.Double) ? 1 : 0;
            }
        }

        double Share(int count) => landed > 0 ? (double)count / landed : 0;
        return new Result((double)landed / Simulations, landed > 0 ? (double)damage / landed : 0, maximum, Share(critical), Share(excellent), Share(doubled));
    }

    private static int Percent(double value) => (int)Math.Round(value * 100);

    private static string SpecialHits(Result result)
    {
        var parts = new List<string>(3);
        if (result.CriticalShare > 0)
        {
            parts.Add($"crit {Percent(result.CriticalShare)}%");
        }

        if (result.ExcellentShare > 0)
        {
            parts.Add($"exc {Percent(result.ExcellentShare)}%");
        }

        if (result.DoubleShare > 0)
        {
            parts.Add($"double {Percent(result.DoubleShare)}%");
        }

        return parts.Count > 0 ? $" ({string.Join(", ", parts)})" : string.Empty;
    }

    private async ValueTask ShowAttackAsync(Player player, string attackName, Result result, int hitsPerAttack, double monsterHealth)
    {
        var damagePerAttack = result.HitChance * result.AverageDamage * hitsPerAttack;
        var attacksToKill = damagePerAttack > 0 ? (int)Math.Ceiling(monsterHealth / damagePerAttack) : 0;
        await player.ShowLocalizedBlueMessageAsync(
            nameof(PlayerMessage.CombatInfoAttackFormat),
            attackName,
            Percent(result.HitChance),
            (int)result.AverageDamage,
            hitsPerAttack > 1 ? $" x{hitsPerAttack}" : string.Empty,
            attacksToKill,
            (int)result.MaximumDamage,
            SpecialHits(result)).ConfigureAwait(false);
    }

    /// <summary>
    /// The result of a simulation.
    /// </summary>
    /// <param name="HitChance">The share of attacks which hit.</param>
    /// <param name="AverageDamage">The average damage of a hit which landed (health and shield), including critical, excellent and double damage at their chances.</param>
    /// <param name="MaximumDamage">The highest damage of a single hit in the simulation.</param>
    /// <param name="CriticalShare">The share of landed hits which were critical.</param>
    /// <param name="ExcellentShare">The share of landed hits which were excellent.</param>
    /// <param name="DoubleShare">The share of landed hits with double damage.</param>
    private readonly record struct Result(double HitChance, double AverageDamage, uint MaximumDamage, double CriticalShare, double ExcellentShare, double DoubleShare);

    /// <summary>
    /// Arguments for this command.
    /// </summary>
    public class Arguments : ArgumentsBase
    {
        /// <summary>
        /// Gets or sets the id of the monster (optional; the game client sends it on Shift + right click).
        /// </summary>
        [Argument("id", false)]
        public ushort TargetId { get; set; }

        /// <summary>
        /// Gets or sets the number of the skill which is selected in the client (optional).
        /// </summary>
        [Argument("skill", false)]
        public ushort SkillNumber { get; set; }
    }
}
