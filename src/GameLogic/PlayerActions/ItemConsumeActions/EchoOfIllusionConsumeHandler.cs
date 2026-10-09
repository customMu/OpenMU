// <copyright file="EchoOfIllusionConsumeHandler.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.ItemConsumeActions;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.GameLogic.PlugIns;

/// <summary>
/// An Echo of the Illusion of Noria (e.g. Echo of Twisting Slash): like the Jewel of Illusion (14/195,
/// 100 %), but it adds the skill fix option of its skill, not a random one. Only a rank 7-8 weapon of +10 and more whose
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
    /// <param name="weapons">The rank 7-8 weapons and the skills of their skill fix options.</param>
    public EchoOfIllusionConsumeHandler(short skill, int family, ICollection<WeaponFixSkills> weapons)
        : base(new ItemUpgradeConfiguration(ItemOptionTypes.SkillFixOption, true, false, 1.0, ItemFailResult.None))
    {
        this._skill = skill;
        this._family = family;
        this._weapons = weapons;
    }

    /// <inheritdoc />
    public override ItemIdentifier Key => new(173, 14);

    /// <summary>
    /// Gets the number of the skill fix option of the skill on the weapon, or <c>null</c> if the weapon has no such option.
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
    protected override IncreasableItemOption? SelectOption(Item item, IList<IncreasableItemOption> possibleOptions)
    {
        // 09.10.2026: a random one of the 4 illusion options of the skill of the Echo - Haste, Vampiric, Siphon or Fury
        if (this.GetOptionNumber(item) is not { } haste)
        {
            return null;
        }

        var skillIndex = haste - PlugIns.IllusionOfNoria.IllusionWeaponOptions.FirstOption;
        var numbers = Enum.GetValues<PlugIns.IllusionOfNoria.IllusionWeaponOptions.Effect>()
            .Select(effect => (int)PlugIns.IllusionOfNoria.IllusionWeaponOptions.GetOptionNumber(effect, skillIndex))
            .ToHashSet();
        var candidates = possibleOptions.Where(o => numbers.Contains(o.Number)).ToList();
        return candidates.Count == 0 ? null : candidates[Rand.NextInt(0, candidates.Count)];
    }
}
