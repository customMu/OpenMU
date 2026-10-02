// <copyright file="DropStatsChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Globalization;
using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A chat command plugin which shows the drop statistics (kills, items by rank, jewels) of the game master for balance tests.
/// </summary>
[Guid("2F6D8A41-7C9B-4E35-A1D8-5B3E7F9C2A16")]
[PlugIn]
[Display(Name = nameof(PlugInResources.DropStatsChatCommandPlugIn_Name), Description = nameof(PlugInResources.DropStatsChatCommandPlugIn_Description), ResourceType = typeof(PlugInResources))]
[ChatCommandHelp(Command, typeof(Arguments), MinimumStatus)]
public class DropStatsChatCommandPlugIn : ChatCommandPlugInBase<DropStatsChatCommandPlugIn.Arguments>, IDisabledByDefault
{
    private const string Command = "/dropstats";
    private const CharacterStatus MinimumStatus = CharacterStatus.GameMaster;

    /// <inheritdoc />
    public override string Key => Command;

    /// <inheritdoc />
    public override CharacterStatus MinCharacterStatusRequirement => MinimumStatus;

    /// <inheritdoc />
    protected override async ValueTask DoHandleCommandAsync(Player player, Arguments arguments)
    {
        if (player.SelectedCharacter?.Name is not { } characterName)
        {
            return;
        }

        if (string.Equals(arguments.Action, "reset", StringComparison.OrdinalIgnoreCase))
        {
            DropStatistics.Reset(characterName);
            await player.ShowBlueMessageAsync("Drop statistics cleared.").ConfigureAwait(false);
            return;
        }

        var snapshot = DropStatistics.GetSnapshot(characterName);
        var minutes = (DateTime.UtcNow - snapshot.Since).TotalMinutes;
        await player.ShowBlueMessageAsync(string.Format(CultureInfo.InvariantCulture, "Kills: {0} in {1:0} min", snapshot.Kills, minutes)).ConfigureAwait(false);
        if (snapshot.ItemsByRank.Count == 0 && snapshot.Jewels.Count == 0)
        {
            await player.ShowBlueMessageAsync("No items or jewels dropped.").ConfigureAwait(false);
            return;
        }

        foreach (var (rank, count) in snapshot.ItemsByRank.OrderBy(r => r.Key))
        {
            await player.ShowBlueMessageAsync(string.Format(CultureInfo.InvariantCulture, "Rank {0} items: {1} (1 per {2} kills)", rank, count, PerKills(snapshot.Kills, count))).ConfigureAwait(false);
        }

        foreach (var (name, count) in snapshot.Jewels.OrderBy(j => j.Key, StringComparer.Ordinal))
        {
            await player.ShowBlueMessageAsync(string.Format(CultureInfo.InvariantCulture, "{0}: {1} (1 per {2} kills)", name, count, PerKills(snapshot.Kills, count))).ConfigureAwait(false);
        }
    }

    private static string PerKills(long kills, long count)
    {
        return count > 0 ? ((double)kills / count).ToString("0.#", CultureInfo.InvariantCulture) : "-";
    }

    /// <summary>
    /// Arguments for the drop statistics chat command.
    /// </summary>
    public class Arguments : ArgumentsBase
    {
        /// <summary>
        /// Gets or sets the action: empty to show the statistics, "reset" to clear them.
        /// </summary>
        public string? Action { get; set; }
    }
}
