// <copyright file="RandomRespawnPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Every killed monster comes back after its own random time (by default 6 ... 10 seconds), so that a spot doesn't
/// respawn all at once. Only monsters with a short respawn delay (up to 10 seconds) - bosses keep their delay.
/// </summary>
[PlugIn]
[Display(Name = "Random respawn", Description = "Every killed monster with a short respawn delay comes back after its own random time (6 ... 10 seconds by default); bosses keep their delay.")]
[Guid("4F7A2C91-6D3E-4B58-A0C7-9E1B5D8F3A26")]
public class RandomRespawnPlugIn : IFeaturePlugIn, ISupportCustomConfiguration<RandomRespawnConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <summary>
    /// Gets or sets the configuration.
    /// </summary>
    public RandomRespawnConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new RandomRespawnConfiguration();

    /// <summary>
    /// Gets the respawn delay of a killed monster.
    /// </summary>
    /// <param name="gameContext">The game context, if known.</param>
    /// <param name="definedDelay">The respawn delay of the monster definition.</param>
    /// <returns>The delay until the monster comes back.</returns>
    public static TimeSpan GetRespawnDelay(IGameContext? gameContext, TimeSpan definedDelay)
    {
        if (gameContext?.FeaturePlugIns.GetPlugIn<RandomRespawnPlugIn>() is not { } plugIn)
        {
            return definedDelay;
        }

        var configuration = plugIn.Configuration ??= new RandomRespawnConfiguration();
        return GetDelay(configuration, definedDelay, Rand.NextDouble());
    }

    /// <summary>
    /// Gets the delay for a random value between 0 and 1.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="definedDelay">The respawn delay of the monster definition.</param>
    /// <param name="random">A random value between 0 and 1.</param>
    /// <returns>The delay.</returns>
    internal static TimeSpan GetDelay(RandomRespawnConfiguration configuration, TimeSpan definedDelay, double random)
    {
        if (definedDelay <= TimeSpan.Zero || definedDelay > configuration.AppliesUpTo)
        {
            return definedDelay;
        }

        var minimum = configuration.Minimum;
        var maximum = configuration.Maximum < minimum ? minimum : configuration.Maximum;
        return minimum + TimeSpan.FromTicks((long)((maximum - minimum).Ticks * Math.Clamp(random, 0, 1)));
    }
}

/// <summary>
/// The configuration of the <see cref="RandomRespawnPlugIn"/>.
/// </summary>
public class RandomRespawnConfiguration
{
    /// <summary>
    /// Gets or sets the shortest respawn time.
    /// </summary>
    [Display(Name = "Minimum", Description = "The shortest time until a killed monster comes back.")]
    public TimeSpan Minimum { get; set; } = TimeSpan.FromSeconds(6);

    /// <summary>
    /// Gets or sets the longest respawn time.
    /// </summary>
    [Display(Name = "Maximum", Description = "The longest time until a killed monster comes back.")]
    public TimeSpan Maximum { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the longest respawn delay of a monster definition which is replaced by the random time.
    /// </summary>
    [Display(Name = "Applies up to", Description = "Only monsters whose respawn delay is at most this long get the random time (bosses with minutes or hours keep theirs).")]
    public TimeSpan AppliesUpTo { get; set; } = TimeSpan.FromSeconds(10);
}
