// <copyright file="HappyHourConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.PeriodicTasks;

using System;

/// <summary>
/// The Happy Hour event configuration.
/// </summary>
public class HappyHourConfiguration : PeriodicTaskConfiguration
{
    /// <summary>
    /// Gets the default configuration.
    /// </summary>
    public static HappyHourConfiguration Default => new()
    {
        TaskDuration = TimeSpan.FromHours(1),
        PreStartMessageDelay = TimeSpan.FromSeconds(0),
        StartMessage = "Happy Hour event has been started!",
        EndMessage = "Happy Hour event has ended!",
        Timetable = GenerateTimeSequence(TimeSpan.FromHours(6), new TimeOnly(0, 5)).ToList(), // Every 6 hours,
        ExperienceMultiplier = 1.5f,
    };

    /// <summary>
    /// Gets or sets the experience multiplier.
    /// </summary>
    [Display(ResourceType = typeof(PlugInResources), Name = nameof(PlugInResources.HappyHourConfiguration_ExperienceMultiplier_Name))]
    public float ExperienceMultiplier { get; set; } = 1.5f;

    /// <summary>
    /// Gets or sets the days of the week on which the happy hour starts; empty = every day.
    /// </summary>
    [DataModel.Composition.MemberOfAggregate]
    [Display(Name = "Days", Description = "The days of the week on which the happy hour starts at the times of the timetable; empty = every day. E.g. Saturday and Sunday, the time 00:00 and a duration of 24 hours for the whole weekend.")]
    public IList<DayOfWeek> Days { get; set; } = new List<DayOfWeek>();

    /// <inheritdoc />
    protected override bool IsStartDay(DateTime localTime) => this.Days.Count == 0 || this.Days.Contains(localTime.DayOfWeek);

    /// <inheritdoc />
    public override bool IsItTimeToStart(TimeZoneInfo serverTimeZone)
    {
        if (this.Days.Count > 0 && !this.Days.Contains(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, serverTimeZone).DayOfWeek))
        {
            return false;
        }

        return base.IsItTimeToStart(serverTimeZone);
    }
}