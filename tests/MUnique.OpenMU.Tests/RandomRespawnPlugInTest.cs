// <copyright file="RandomRespawnPlugInTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>
/// Tests the <see cref="RandomRespawnPlugIn"/>: monsters with a short respawn delay come back after 6 ... 10 seconds, bosses keep theirs.
/// </summary>
[TestFixture]
public class RandomRespawnPlugInTest
{
    private readonly RandomRespawnConfiguration _configuration = new();

    /// <summary>
    /// The random value 0 gives the minimum, 1 the maximum, 0.5 the middle.
    /// </summary>
    /// <param name="random">The random value.</param>
    /// <param name="expectedSeconds">The expected delay in seconds.</param>
    [TestCase(0.0, 6)]
    [TestCase(0.5, 8)]
    [TestCase(1.0, 10)]
    public void ShortDelayIsRandom(double random, double expectedSeconds)
    {
        var delay = RandomRespawnPlugIn.GetDelay(this._configuration, TimeSpan.FromSeconds(10), random);
        Assert.That(delay, Is.EqualTo(TimeSpan.FromSeconds(expectedSeconds)));
    }

    /// <summary>
    /// A boss with a long respawn delay keeps it.
    /// </summary>
    [Test]
    public void LongDelayIsKept()
    {
        var delay = RandomRespawnPlugIn.GetDelay(this._configuration, TimeSpan.FromHours(3), 0.3);
        Assert.That(delay, Is.EqualTo(TimeSpan.FromHours(3)));
    }

    /// <summary>
    /// Without the plugin (no game context) the delay of the definition is used.
    /// </summary>
    [Test]
    public void WithoutPlugInTheDefinedDelayIsUsed()
    {
        Assert.That(RandomRespawnPlugIn.GetRespawnDelay(null, TimeSpan.FromSeconds(10)), Is.EqualTo(TimeSpan.FromSeconds(10)));
    }
}
