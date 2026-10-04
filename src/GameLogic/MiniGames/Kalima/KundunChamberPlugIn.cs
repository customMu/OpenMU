// <copyright file="KundunChamberPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Kalima;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The chamber of Kundun: a weekly fight against Kundun on the Kalima map of the tier, for a party or a
/// single player. It's entered by talking to the keeper npc (David in Lorencia); each character pays with
/// a Lost Map of the level of the chamber.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.KundunChamberPlugIn_Name), Description = nameof(PlugInResources.KundunChamberPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("A4C71E92-5B38-4D06-9F2A-7E1B8C3D5F60")]
public class KundunChamberPlugIn : IFeaturePlugIn, IPlayerTalkToNpcPlugIn, ISupportCustomConfiguration<KundunChamberConfiguration>, ISupportDefaultCustomConfiguration
{
    private const int KeeperRange = 10;

    /// <summary>
    /// Gets or sets the configuration.
    /// </summary>
    public KundunChamberConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new KundunChamberConfiguration();

    /// <summary>
    /// Gets the configuration, or the default one, if none is set.
    /// </summary>
    /// <returns>The configuration.</returns>
    public KundunChamberConfiguration GetConfiguration() => this.Configuration ??= new KundunChamberConfiguration();

    /// <inheritdoc />
    public async ValueTask PlayerTalksToNpcAsync(Player player, NonPlayerCharacter npc, NpcTalkEventArgs eventArgs)
    {
        var configuration = this.GetConfiguration();
        if (npc.Definition.Number != configuration.KeeperNpcNumber)
        {
            return;
        }

        // The dialog is closed here already, because entering warps the player. Otherwise, the
        // talk action would set the state back to 'entered world' while the map is changing.
        eventArgs.HasBeenHandled = true;
        eventArgs.LeavesDialogOpen = true;
        player.OpenedNpc = null;
        await player.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);

        var kalimaConfiguration = player.GameContext.FeaturePlugIns.GetPlugIn<KalimaInstancePlugIn>()?.GetConfiguration() ?? new KalimaInstanceConfiguration();
        var action = new KundunChamberEnterAction(configuration, kalimaConfiguration);
        if (player.ViewPlugIns.GetPlugIn<Views.IKalimaInstanceViewPlugIn>() is { } view)
        {
            // The client shows the levels, the lost map and the entries left; it enters with a request (see EnterFromDialogAsync).
            await view.ShowChamberEntryDialogAsync(await action.GetInfoAsync(player).ConfigureAwait(false)).ConfigureAwait(false);
            return;
        }

        await action.TryEnterAsync(player).ConfigureAwait(false);
    }

    /// <summary>
    /// Enters the chamber after the player confirmed the entry dialog. The player has to be near the keeper.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <returns>The task.</returns>
    public async ValueTask EnterFromDialogAsync(Player player)
    {
        var configuration = this.GetConfiguration();
        if (player.CurrentMap is not { } map
            || !map.GetNpcsInRange(player.Position, KeeperRange).Any(npc => npc.Definition.Number == configuration.KeeperNpcNumber))
        {
            return;
        }

        var kalimaConfiguration = player.GameContext.FeaturePlugIns.GetPlugIn<KalimaInstancePlugIn>()?.GetConfiguration() ?? new KalimaInstanceConfiguration();
        await new KundunChamberEnterAction(configuration, kalimaConfiguration).TryEnterAsync(player).ConfigureAwait(false);
    }
}
