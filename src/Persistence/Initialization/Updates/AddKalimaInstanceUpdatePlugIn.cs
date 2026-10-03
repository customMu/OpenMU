// <copyright file="AddKalimaInstanceUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Events;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// This update adds the Kalima instance (Kalima 1-7 as daily instance for a party or a single player)
/// and the Symbols of Kundun currency: the mini game definitions, the attributes of the daily entries and
/// of the symbol balance, the gatekeeper (Lugard) and the symbol shop (Delgado) in Lorencia.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("9C4E1A73-5B2F-4D86-A0E9-3F7B6C2D8E41")]
public class AddKalimaInstanceUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Add Kalima instance";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Adds the daily Kalima instance (mini games, Lugard in Lorencia) and the Symbols of Kundun currency (attribute, symbol shop at Delgado in Lorencia).";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddKalimaInstance;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override async ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        this.AddStatIfNotExists(context, gameConfiguration, Stats.KundunEssence);
        this.AddStatIfNotExists(context, gameConfiguration, Stats.KalimaInstanceEntryDay);
        this.AddStatIfNotExists(context, gameConfiguration, Stats.KalimaInstanceEntries);

        new KalimaInstanceInitializer(context, gameConfiguration).Initialize();

        // Kalima is entered through the gatekeeper only, so the warps into it are removed from the warp list (M window).
        var kalimaWarps = gameConfiguration.WarpList
            .Where(warp => warp.Gate?.Map is { } map && KalimaInstanceInitializer.KalimaMapNumbers.Contains(map.Number))
            .ToList();
        foreach (var warp in kalimaWarps)
        {
            gameConfiguration.WarpList.Remove(warp);
            await context.DeleteAsync(warp).ConfigureAwait(false);
        }
    }
}
