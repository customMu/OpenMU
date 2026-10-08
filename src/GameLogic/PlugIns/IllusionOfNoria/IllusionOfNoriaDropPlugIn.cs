// <copyright file="IllusionOfNoriaDropPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.IllusionOfNoria;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The kills for the <see cref="IllusionOfNoriaPlugIn"/>: counts the kills in the illusion (daily quest, boss), drops the
/// Whistle of the Veil from Condra for the players of the quest and the reward of the boss.
/// </summary>
[PlugIn]
[Display(Name = "Illusion of Noria: kills and drops", Description = "Counts the kills in the Illusion of Noria (daily quest, boss), drops the whistle of the quest from Condra and the reward of the boss.")]
[Guid("B4D17E62-9A3C-4F58-8E21-5C6A9F3D7B04")]
public class IllusionOfNoriaDropPlugIn : IAdditionalItemDropPlugIn
{
    /// <inheritdoc />
    public async ValueTask AddItemDropsAsync(AdditionalItemDropArgs args)
    {
        if (args.KilledObject.IsSummonedMonster
            || args.Monster.ObjectKind != NpcObjectKind.Monster
            || args.Killer.GameContext.FeaturePlugIns.GetPlugIn<IllusionOfNoriaPlugIn>() is not { } illusion)
        {
            return;
        }

        var configuration = illusion.GetConfiguration();
        if (args.Monster.Number == configuration.WhistleMonsterNumber
            && args.Killer.CurrentMiniGame is null
            && illusion.CanGetWhistle(args.Killer)
            && Rand.NextRandomBool(configuration.WhistleChancePercent / 100.0)
            && args.Killer.GameContext.Configuration.Items.FirstOrDefault(d => d.Group == configuration.WhistleItemGroup && d.Number == configuration.WhistleItemNumber) is { } whistle)
        {
            // the whistle is bound to the character: only the killer can pick it up
            args.Items.Add(new TemporaryItem { Definition = whistle, Durability = 1 });
        }

        if (args.Map.Definition.Number != configuration.MapNumber)
        {
            return;
        }

        if (args.Monster.Number == configuration.BossNumber)
        {
            args.Items.AddRange(IllusionOfNoriaPlugIn.CreateRewardItems(args.Killer.GameContext, configuration.BossDrops));
            return;
        }

        await illusion.CountKillAsync(args.Killer, args.Monster, args.Map, args.KilledObject.Position).ConfigureAwait(false);
        if (Rand.NextRandomBool(configuration.MonsterShardChancePercent / 100.0))
        {
            args.Items.AddRange(illusion.CreateShards(args.Killer.GameContext, 1));
        }
    }
}
