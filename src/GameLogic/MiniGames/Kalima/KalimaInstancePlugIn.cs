// <copyright file="KalimaInstancePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.MiniGames.Kalima;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.NPC;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The Kalima instance: the Kalima maps are no regular maps anymore, but a daily instance for a
/// party or a single player, which is entered by talking to the gatekeeper npc (Lugard in Lorencia).
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.KalimaInstancePlugIn_Name), Description = nameof(PlugInResources.KalimaInstancePlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("6F2B9D41-3A7C-4E85-9C1D-8B4E2F7A6C13")]
public class KalimaInstancePlugIn : IFeaturePlugIn, IPlayerTalkToNpcPlugIn, IPlayerStateChangedPlugIn, ISupportCustomConfiguration<KalimaInstanceConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <summary>
    /// Gets or sets the configuration.
    /// </summary>
    public KalimaInstanceConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new KalimaInstanceConfiguration();

    /// <summary>
    /// Gets the configuration, or the default one, if none is set.
    /// </summary>
    /// <returns>The configuration.</returns>
    public KalimaInstanceConfiguration GetConfiguration() => this.Configuration ??= new KalimaInstanceConfiguration();

    /// <summary>
    /// Determines whether the map is a map of the Kalima instance, which can't be entered as a regular map.
    /// </summary>
    /// <param name="map">The map definition.</param>
    /// <param name="gameConfiguration">The game configuration.</param>
    /// <returns><c>true</c>, if the map is a map of the Kalima instance.</returns>
    public bool IsInstanceMap(GameMapDefinition map, GameConfiguration gameConfiguration)
    {
        return gameConfiguration.MiniGameDefinitions.Any(definition =>
            definition.Type == MiniGameType.KalimaInstance
            && definition.Entrance?.Map is { } entranceMap
            && entranceMap.Number == map.Number);
    }

    /// <inheritdoc />
    public async ValueTask PlayerTalksToNpcAsync(Player player, NonPlayerCharacter npc, NpcTalkEventArgs eventArgs)
    {
        var configuration = this.GetConfiguration();
        if (npc.Definition.Number != configuration.GatekeeperNpcNumber)
        {
            return;
        }

        // The dialog is closed here already, because entering warps the player. Otherwise, the
        // talk action would set the state back to 'entered world' while the map is changing.
        eventArgs.HasBeenHandled = true;
        eventArgs.LeavesDialogOpen = true;
        player.OpenedNpc = null;
        await player.PlayerState.TryAdvanceToAsync(PlayerState.EnteredWorld).ConfigureAwait(false);

        await new KalimaInstanceEnterAction(configuration).TryEnterAsync(player).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public ValueTask PlayerStateChangedAsync(Player player, State previousState, State currentState)
    {
        if (currentState != PlayerState.EnteredWorld
            || player.CurrentMiniGame is not null
            || player.CurrentMap?.Definition is not { } map
            || !this.IsInstanceMap(map, player.GameContext.Configuration))
        {
            return ValueTask.CompletedTask;
        }

        // The character is on a regular Kalima map, e.g. it was there before the instance got introduced.
        // It's sent to the safezone, after it completely entered the map.
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                if (player.CurrentMiniGame is null && player.CurrentMap?.Definition == map)
                {
                    await player.WarpToSafezoneAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                player.Logger.LogError(ex, "Error when moving {player} out of the regular Kalima map.", player);
            }
        });

        return ValueTask.CompletedTask;
    }
}
