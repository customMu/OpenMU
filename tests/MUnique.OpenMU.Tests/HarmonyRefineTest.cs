// <copyright file="HarmonyRefineTest.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

/// <summary>
/// Tests the harmony rework (09.10.2026): the Jewel of Harmony adds an option with 75 %, the refine stones raise it with
/// a chance by the level (2-3 always, then 70 % down to 10 % for level 10), on a fail the Lower Refine Stone sets the
/// option back to its base level and the Higher Refine Stone keeps it.
/// </summary>
[TestFixture]
public class HarmonyRefineTest
{
    /// <summary>
    /// The chance to reach a level with a refine stone.
    /// </summary>
    [TestCase(2, 1.0)]
    [TestCase(3, 1.0)]
    [TestCase(4, 0.7)]
    [TestCase(5, 0.6)]
    [TestCase(9, 0.2)]
    [TestCase(10, 0.1)]
    public void RefineChanceByLevel(int targetLevel, double chance)
    {
        Assert.That(new TestHigherRefineStone().ChanceOf(targetLevel), Is.EqualTo(chance).Within(1e-9));
        Assert.That(new TestLowerRefineStone().ChanceOf(targetLevel), Is.EqualTo(chance).Within(1e-9));
    }

    /// <summary>
    /// The Higher Refine Stone keeps the level on a fail, the Lower one sets it back.
    /// </summary>
    [Test]
    public void FailResults()
    {
        Assert.That(new HigherRefineStoneConsumeHandlerPlugIn().Configuration.FailResult, Is.EqualTo(ItemUpgradeConsumeHandlerPlugIn.ItemFailResult.None));
        Assert.That(new LowerRefineStoneConsumeHandlerPlugIn().Configuration.FailResult, Is.EqualTo(ItemUpgradeConsumeHandlerPlugIn.ItemFailResult.SetOptionToBaseLevel));
    }

    /// <summary>
    /// The Jewel of Harmony adds an option with 75 % and doesn't raise it.
    /// </summary>
    [Test]
    public void JewelOfHarmony()
    {
        var configuration = new HarmonyJewelConsumeHandlerPlugIn().Configuration;
        Assert.That(configuration.SuccessChance, Is.EqualTo(0.75));
        Assert.That(configuration.AddsOption, Is.True);
        Assert.That(configuration.IncreasesOption, Is.False);
    }

    private sealed class TestHigherRefineStone : HigherRefineStoneConsumeHandlerPlugIn
    {
        public double ChanceOf(int targetLevel) => this.GetUpgradeSuccessChance(targetLevel);
    }

    private sealed class TestLowerRefineStone : LowerRefineStoneConsumeHandlerPlugIn
    {
        public double ChanceOf(int targetLevel) => this.GetUpgradeSuccessChance(targetLevel);
    }
}
