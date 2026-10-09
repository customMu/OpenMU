// <copyright file="SkillFixStoneConsumeHandlerPlugIns.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The Jewel of Illusion (14/195, Illusion of Noria): since 09.10.2026 it removes the skill fix option in the Chaos
/// Machine of the illusion (IllusionRemoveSkillFixCrafting); it is not used from the inventory. The skill fix option is
/// separate from the harmony option.
/// </summary>
[Guid("2F8C6A41-9B37-4D52-A1E8-6C3D9F0B7E14")]
[PlugIn]
[Display(Name = "Jewel of Illusion consume handler", Description = "Not used from the inventory: the Jewel of Illusion removes the skill fix option in the Chaos Machine of the Illusion of Noria.")]
public class JewelOfIllusionConsumeHandlerPlugIn : ItemUpgradeConsumeHandlerPlugIn
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JewelOfIllusionConsumeHandlerPlugIn"/> class.
    /// </summary>
    public JewelOfIllusionConsumeHandlerPlugIn()
        : base(new ItemUpgradeConfiguration(ItemOptionTypes.SkillFixOption, true, false, 1.0, ItemFailResult.None))
    {
    }

    /// <summary>
    /// The jewel is not used from the inventory: it removes the option in the Chaos Machine of the Illusion of Noria
    /// (IllusionRemoveSkillFixCrafting, 09.10.2026).
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="item">The item.</param>
    /// <returns>Always <c>false</c>.</returns>
    protected override bool CheckPreconditions(Player player, Item item) => false;

    /// <inheritdoc />
    public override ItemIdentifier Key => new(195, 14);
}

/// <summary>
/// A Mirage Stone (Illusion of Noria): raises the skill fix option by one level up to 10, with a chance by the level
/// it goes to (2-3 always, then 70 % down to 10 % for level 10).
/// </summary>
public abstract class MirageStoneConsumeHandlerPlugIn : ItemUpgradeConsumeHandlerPlugIn
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MirageStoneConsumeHandlerPlugIn"/> class.
    /// </summary>
    /// <param name="failResult">What happens on a fail.</param>
    private protected MirageStoneConsumeHandlerPlugIn(ItemFailResult failResult)
        : base(new ItemUpgradeConfiguration(ItemOptionTypes.SkillFixOption, false, true, 1, failResult))
    {
    }

    /// <summary>
    /// Gets the chance in percent to reach the level (index) of the skill fix option; the client shows it in the tooltip
    /// (GameLogic/Combat/SkillCastTime.h RefineChance, tools/balance/skill_cast_time.py).
    /// </summary>
    internal static IReadOnlyList<int> ChancePercent { get; } = new[] { 0, 0, 100, 100, 70, 60, 50, 40, 30, 20, 10 };

    /// <inheritdoc />
    protected override double GetUpgradeSuccessChance(int targetLevel)
    {
        return targetLevel >= 0 && targetLevel < ChancePercent.Count ? ChancePercent[targetLevel] / 100.0 : 0;
    }
}

/// <summary>
/// The Lesser Mirage Stone (14/196): on a fail the skill fix option falls back to level 1.
/// </summary>
[Guid("8D4E1B72-3A65-4C9F-B0D2-5E7A1C8F3B96")]
[PlugIn]
[Display(Name = "Lesser Mirage Stone consume handler", Description = "Raises the skill fix option by one level; on a fail it falls back to level 1.")]
public class LesserMirageStoneConsumeHandlerPlugIn : MirageStoneConsumeHandlerPlugIn
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LesserMirageStoneConsumeHandlerPlugIn"/> class.
    /// </summary>
    public LesserMirageStoneConsumeHandlerPlugIn()
        : base(ItemFailResult.SetOptionToBaseLevel)
    {
    }

    /// <inheritdoc />
    public override ItemIdentifier Key => new(196, 14);
}

/// <summary>
/// The Greater Mirage Stone (14/197): on a fail the skill fix option keeps its level.
/// </summary>
[Guid("C6B39E25-7F14-4A8D-92E1-0D5C8B4A7F63")]
[PlugIn]
[Display(Name = "Greater Mirage Stone consume handler", Description = "Raises the skill fix option by one level; on a fail it keeps its level.")]
public class GreaterMirageStoneConsumeHandlerPlugIn : MirageStoneConsumeHandlerPlugIn
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GreaterMirageStoneConsumeHandlerPlugIn"/> class.
    /// </summary>
    public GreaterMirageStoneConsumeHandlerPlugIn()
        : base(ItemFailResult.None)
    {
    }

    /// <inheritdoc />
    public override ItemIdentifier Key => new(197, 14);
}
