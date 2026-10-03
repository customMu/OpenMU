// <copyright file="KundunSymbolsOnlyInKalimaUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Events;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// This update removes the regular drops of the Symbols of Kundun from all maps and monsters.
/// The symbols are given personally to the players of the Kalima instance only.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("5E8B2C14-7A3D-4F69-B1C0-9D4E6A2F8B57")]
public class KundunSymbolsOnlyInKalimaUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Symbols of Kundun only in Kalima";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Removes the regular drops of the Symbols of Kundun; they are given personally to the players of the Kalima instance only.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.KundunSymbolsOnlyInKalima;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 10, 3, 18, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        KalimaInstanceInitializer.RemoveSymbolDrops(gameConfiguration);
        return ValueTask.CompletedTask;
    }
}
