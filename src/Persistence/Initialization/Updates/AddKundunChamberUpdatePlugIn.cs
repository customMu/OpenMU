// <copyright file="AddKundunChamberUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.MiniGames.Kalima;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Events;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// This update adds the chamber of Kundun (the monsters Kundun 1-7, the mini game definitions, the keeper
/// David in Lorencia and the attributes of the weekly entries), renames the currency of the Kalima instance
/// to Kundun Essence, adds the lost maps to the essence shop and sets the strict reset ranges of the tiers.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("3B8F6D21-9C47-4E5A-B0D3-71A2E5C9F846")]
public class AddKundunChamberUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Add chamber of Kundun";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "Adds the chamber of Kundun (Kundun 1-7, mini games, David in Lorencia), renames the currency to Kundun Essence, adds the lost maps to the essence shop and sets the reset ranges 5-9 ... 38-44, 45+ of the Kalima tiers.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.AddKundunChamber;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 10, 3, 22, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        this.AddStatIfNotExists(context, gameConfiguration, Stats.KundunEssence);
        this.AddStatIfNotExists(context, gameConfiguration, Stats.KundunChamberEntryWeek);
        this.AddStatIfNotExists(context, gameConfiguration, Stats.KundunChamberEntries);
        this.AddStatIfNotExists(context, gameConfiguration, Stats.KundunChamberPassUntilDay);
        if (gameConfiguration.Attributes.FirstOrDefault(a => a.Id == Stats.KundunEssence.Id) is { } essence)
        {
            essence.Designation = Stats.KundunEssence.Designation;
            essence.Description = Stats.KundunEssence.Description;
        }

        // Adds the missing items (the lost maps) to the essence shop, too.
        new KalimaInstanceInitializer(context, gameConfiguration).Initialize();
        new KundunChamberInitializer(context, gameConfiguration).Initialize();
        UpdateTierResets(gameConfiguration);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Sets the new minimum resets of the tiers 6 and 7 (38 and 45 instead of 40 and 50),
    /// if they still have the old default values.
    /// </summary>
    /// <param name="gameConfiguration">The game configuration.</param>
    private static void UpdateTierResets(GameConfiguration gameConfiguration)
    {
        var plugInConfiguration = gameConfiguration.PlugInConfigurations.FirstOrDefault(c => c.TypeId == typeof(KalimaInstancePlugIn).GUID);
        if (plugInConfiguration?.GetConfiguration<KalimaInstanceConfiguration>(NoReferencesHandler.Instance) is not { } configuration)
        {
            return;
        }

        var changed = false;
        foreach (var (level, oldResets, newResets) in new[] { (6, 40, 38), (7, 50, 45) })
        {
            if (configuration.GetTier(level) is { } tier && tier.MinimumResets == oldResets)
            {
                tier.MinimumResets = newResets;
                changed = true;
            }
        }

        if (changed)
        {
            plugInConfiguration.SetConfiguration(configuration, NoReferencesHandler.Instance);
        }
    }

    /// <summary>
    /// Reads and writes plug-in configurations in the format of the admin panel (empty <c>$id</c> and <c>$values</c>),
    /// for configurations which don't reference objects of the database.
    /// </summary>
    private sealed class NoReferencesHandler : ReferenceHandler
    {
        /// <summary>
        /// Gets the instance.
        /// </summary>
        public static NoReferencesHandler Instance { get; } = new();

        /// <inheritdoc />
        public override ReferenceResolver CreateResolver() => new Resolver();

        private sealed class Resolver : ReferenceResolver
        {
            public override void AddReference(string referenceId, object value)
            {
                // The ids are empty, there is nothing to resolve later.
            }

            public override string GetReference(object value, out bool alreadyExists)
            {
                alreadyExists = false;
                return string.Empty;
            }

            public override object ResolveReference(string referenceId) => throw new KeyNotFoundException($"Reference with id '{referenceId}' not found.");
        }
    }
}
