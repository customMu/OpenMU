// <copyright file="ResetExperienceRatePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Resets;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Multiplies the experience of a kill by a rate which depends on the reset count of the character,
/// so that every further reset takes longer. Works for solo kills and party kills.
/// The rates are configurable in the admin panel.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.ResetExperienceRatePlugIn_Name), Description = nameof(PlugInResources.ResetExperienceRatePlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("5D2FCF1C-27B0-46A0-BF8B-B81F09F1ADF7")]
public class ResetExperienceRatePlugIn : IExperienceCalculationPlugIn, ISupportCustomConfiguration<ResetExperienceRateConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    /// <inheritdoc />
    public ResetExperienceRateConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new ResetExperienceRateConfiguration();

    /// <inheritdoc />
    public ValueTask CalculateExperienceAsync(Player player, ExperienceCalculationArgs args)
    {
        var configuration = this.Configuration ??= (ResetExperienceRateConfiguration)this.CreateDefaultConfig();
        if (args.IsMasterExperience && !configuration.ApplyToMasterExperience)
        {
            return ValueTask.CompletedTask;
        }

        var resetCount = (int)(player.Attributes?[Stats.Resets] ?? 0f);
        args.Experience *= configuration.GetRate(resetCount);
        return ValueTask.CompletedTask;
    }
}
