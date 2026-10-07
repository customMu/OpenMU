// <copyright file="QuestRewardBoxesUpdatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Persistence.Initialization.Updates;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// This update makes the Box of Luck give one excellent ring and the Box of Heaven one excellent pendant:
/// the rewards of the last kill quests.
/// </summary>
[PlugIn]
[Display(Name = PlugInName, Description = PlugInDescription)]
[Guid("58ACD499-8AA1-4400-8C9F-D29A260560B8")]
public class QuestRewardBoxesUpdatePlugIn : UpdatePlugInBase
{
    /// <summary>
    /// The plug in name.
    /// </summary>
    internal const string PlugInName = "Quest reward boxes";

    /// <summary>
    /// The plug in description.
    /// </summary>
    internal const string PlugInDescription = "The Box of Luck gives one excellent ring, the Box of Heaven (Box of Luck +7) one excellent pendant: the rewards of the last kill quests.";

    /// <inheritdoc />
    public override string Name => PlugInName;

    /// <inheritdoc />
    public override string Description => PlugInDescription;

    /// <inheritdoc />
    public override UpdateVersion Version => UpdateVersion.QuestRewardBoxes;

    /// <inheritdoc />
    public override string DataInitializationKey => VersionSeasonSix.DataInitialization.Id;

    /// <inheritdoc />
    public override bool IsMandatory => false;

    /// <inheritdoc />
    public override DateTime CreatedAt => new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

    /// <inheritdoc />
    protected override ValueTask ApplyAsync(IContext context, GameConfiguration gameConfiguration)
    {
        QuestRewardBoxes.Configure(context, gameConfiguration);
        return ValueTask.CompletedTask;
    }
}
