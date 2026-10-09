// <copyright file="SkillWeaponRequirementTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using Microsoft.Extensions.Logging.Abstractions;
using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.Persistence.InMemory;
using MUnique.OpenMU.Persistence.Initialization.VersionSeasonSix;

/// <summary>
/// Tests <see cref="SkillWeaponRequirement"/>: Twisting Slash, Death Stab ... (and their master skills) need a weapon in hand.
/// </summary>
[TestFixture]
public class SkillWeaponRequirementTest
{
    private GameConfiguration _gameConfiguration = null!;

    /// <summary>
    /// Creates the configuration of season 6.
    /// </summary>
    [OneTimeSetUp]
    public async ValueTask SetupAsync()
    {
        var contextProvider = new InMemoryPersistenceContextProvider();
        await new DataInitialization(contextProvider, new NullLoggerFactory()).CreateInitialDataAsync(3, true).ConfigureAwait(false);
        this._gameConfiguration = (await contextProvider.CreateNewConfigurationContext().GetAsync<GameConfiguration>().ConfigureAwait(false)).First();
    }

    /// <summary>
    /// The melee skills and their master versions need a weapon; the other skills don't.
    /// </summary>
    [TestCase(41, true)] // Twisting Slash
    [TestCase(330, true)] // Twisting Slash Strengthener (master skill of 41)
    [TestCase(332, true)] // Twisting Slash Mastery (master skill of 330)
    [TestCase(43, true)] // Death Stab
    [TestCase(232, true)] // Strike of Destruction
    [TestCase(55, true)] // Fire Slash
    [TestCase(19, false)] // Falling Slash: a skill of the weapon itself
    [TestCase(23, false)] // Slash: a skill of the weapon itself
    [TestCase(8, false)] // Twister
    [TestCase(62, false)] // Earthshake
    public void NeedsWeapon(int number, bool expected)
    {
        var skill = this._gameConfiguration.Skills.First(s => s.Number == number);
        Assert.That(SkillWeaponRequirement.NeedsWeapon(skill), Is.EqualTo(expected), skill.Name);
    }
}
