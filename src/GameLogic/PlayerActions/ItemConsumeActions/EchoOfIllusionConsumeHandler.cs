// <copyright file="EchoOfIllusionConsumeHandler.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>
/// An Echo of the Illusion of Noria (e.g. Echo of Twisting Slash): like the Jewel of Illusion (the Jewel of Harmony,
/// 75 %), but it adds the harmony option of its skill, not a random one. Only a rank 7-8 weapon of +10 and more whose
/// options have the skill (for the class of the player, plugin "Skill cast time") takes it. Created per use by
/// <see cref="PlugIns.IllusionOfNoria.IllusionOfNoriaPlugIn.GetEchoHandler"/>.
/// </summary>
public class EchoOfIllusionConsumeHandler : ItemUpgradeConsumeHandlerPlugIn
{
    private static readonly short[] OptionNumbers = [11, 12, 13];

    private readonly short _skill;
    private readonly int _family;
    private readonly ICollection<WeaponFixSkills> _weapons;

    /// <summary>
    /// Initializes a new instance of the <see cref="EchoOfIllusionConsumeHandler"/> class.
    /// </summary>
    /// <param name="skill">The skill of the Echo.</param>
    /// <param name="family">The class family of the player (number of the 1st class).</param>
    /// <param name="weapons">The rank 7-8 weapons and the skills of their harmony options.</param>
    public EchoOfIllusionConsumeHandler(short skill, int family, ICollection<WeaponFixSkills> weapons)
        : base(new ItemUpgradeConfiguration(ItemOptionTypes.HarmonyOption, true, false, 0.75, ItemFailResult.None))
    {
        this._skill = skill;
        this._family = family;
        this._weapons = weapons;
    }

    /// <inheritdoc />
    public override ItemIdentifier Key => ItemConstants.JewelOfHarmony;

    /// <summary>
    /// Gets the number of the harmony option of the skill on the weapon, or <c>null</c> if the weapon has no such option.
    /// </summary>
    /// <param name="item">The weapon.</param>
    /// <returns>The option number.</returns>
    public short? GetOptionNumber(Item item)
    {
        if (item.Definition is not { } definition)
        {
            return null;
        }

        var weapon = this._weapons.FirstOrDefault(w => w.Group == definition.Group && w.Number == definition.Number && w.ClassFamily == this._family)
                     ?? this._weapons.FirstOrDefault(w => w.Group == definition.Group && w.Number == definition.Number && w.ClassFamily < 0);
        if (weapon is null)
        {
            return null;
        }

        short[] skills = [weapon.Skill1, weapon.Skill2, weapon.Skill3];
        var index = Array.IndexOf(skills, this._skill);
        return index < 0 ? null : OptionNumbers[index];
    }

    /// <inheritdoc />
    protected override bool ItemCanHaveOption(Item item)
    {
        return !item.IsAncient() && this.GetOptionNumber(item) is not null && base.ItemCanHaveOption(item);
    }

    /// <inheritdoc />
    protected override IncreasableItemOption? SelectHarmonyOption(Item item, IList<IncreasableItemOption> possibleOptions)
    {
        var number = this.GetOptionNumber(item);
        return possibleOptions.FirstOrDefault(o => o.Number == number);
    }
}
