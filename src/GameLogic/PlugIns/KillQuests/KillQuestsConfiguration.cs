// <copyright file="KillQuestsConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.KillQuests;

using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// The configuration of the <see cref="KillQuestsPlugIn"/>: one chain of quests "kill N monsters of a kind".
/// The first steps give the starter gear of the class (+0), the following ones stat points which stay after resets.
/// </summary>
public class KillQuestsConfiguration
{
    /// <summary>
    /// Gets or sets the quests in their order.
    /// </summary>
    [MemberOfAggregate]
    [Display(Name = "Quests", Description = "The chain of quests in order: kill 'Kills' monsters with the number 'Monster number'; reward: the gear step of the class (1-6: gloves, boots, helm, pants, armor, weapon) and/or stat points.")]
    public ICollection<KillQuest> Quests { get; set; } = CreateDefaultQuests();

    /// <summary>
    /// Gets or sets the starter gear by class and gear step.
    /// </summary>
    [MemberOfAggregate]
    [Display(Name = "Starter gear", Description = "The items of the gear steps by base class (0 DW, 4 DK, 8 Elf, 12 MG, 16 DL, 20 SUM, 24 RF), +0 without options. A class may get several items in one step (the bow with arrows).")]
    public ICollection<KillQuestGearItem> Gear { get; set; } = CreateDefaultGear();

    /// <summary>
    /// Gets or sets the share of the kills after which the progress is shown again (0.1 = every 10 %).
    /// </summary>
    [Display(Name = "Progress message every", Description = "Share of the kills of a quest after which the progress is shown (0.1 = every 10 %); the last 5 kills are always shown.")]
    public float ProgressMessageShare { get; set; } = 0.1f;

    /// <summary>
    /// Gets or sets the distance (fields) within which party members get the kill too.
    /// </summary>
    [Display(Name = "Party range", Description = "Party members on the same map within this distance (fields) of the killed monster count the kill too.")]
    public int PartyRange { get; set; } = 15;

    private static List<KillQuest> CreateDefaultQuests() =>
    [
        new() { MonsterNumber = 3, MonsterName = "Spider", Kills = 25, GearStep = 1 },
        new() { MonsterNumber = 3, MonsterName = "Spider", Kills = 75, GearStep = 2 },
        new() { MonsterNumber = 2, MonsterName = "Budge Dragon", Kills = 50, GearStep = 3 },
        new() { MonsterNumber = 2, MonsterName = "Budge Dragon", Kills = 100, GearStep = 4 },
        new() { MonsterNumber = 1, MonsterName = "Hound", Kills = 100, GearStep = 5 },
        new() { MonsterNumber = 1, MonsterName = "Hound", Kills = 200, GearStep = 6 },
    ];

    private static List<KillQuestGearItem> CreateDefaultGear()
    {
        var gear = new List<KillQuestGearItem>();

        void Set(int classNumber, short setNumber, (byte Group, short Number) weapon, bool helm = true, bool gloves = true)
        {
            // gloves, boots, helm, pants, armor, weapon; a class without helm or gloves gets a Jewel of Bless instead
            (int Step, byte Group, short Number)[] pieces =
            [
                (1, gloves ? (byte)10 : (byte)14, gloves ? setNumber : (short)13),
                (2, 11, setNumber),
                (3, helm ? (byte)7 : (byte)14, helm ? setNumber : (short)13),
                (4, 9, setNumber),
                (5, 8, setNumber),
                (6, weapon.Group, weapon.Number),
            ];
            gear.AddRange(pieces.Select(p => new KillQuestGearItem { ClassNumber = classNumber, Step = p.Step, ItemGroup = p.Group, ItemNumber = p.Number }));
        }

        Set(0, 2, (5, 0));               // Dark Wizard: Pad, Skull Staff
        Set(4, 5, (0, 1));               // Dark Knight: Leather, Short Sword
        Set(8, 10, (4, 0));              // Fairy Elf: Vine, Short Bow (+ Arrows)
        gear.Add(new KillQuestGearItem { ClassNumber = 8, Step = 6, ItemGroup = 4, ItemNumber = 15 });
        Set(12, 2, (0, 1), helm: false); // Magic Gladiator: Pad without helm, Short Sword
        Set(16, 5, (0, 1));              // Dark Lord: Leather, Short Sword
        Set(20, 39, (5, 0));             // Summoner: Mistery, Skull Staff
        Set(24, 5, (2, 0), gloves: false); // Rage Fighter: Leather without gloves, Mace
        return gear;
    }
}

/// <summary>
/// A quest of the chain: kill a number of monsters of a kind.
/// </summary>
public class KillQuest
{
    /// <summary>
    /// Gets or sets the number of the monster definition.
    /// </summary>
    [Display(Name = "Monster number")]
    public short MonsterNumber { get; set; }

    /// <summary>
    /// Gets or sets the name of the monster, as shown to the player.
    /// </summary>
    [Display(Name = "Monster name")]
    public string MonsterName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of kills.
    /// </summary>
    public int Kills { get; set; }

    /// <summary>
    /// Gets or sets the gear step (1-6) of the reward, 0 for none.
    /// </summary>
    [Display(Name = "Gear step", Description = "1 gloves, 2 boots, 3 helm, 4 pants, 5 armor, 6 weapon; 0 = no item.")]
    public int GearStep { get; set; }

    /// <summary>
    /// Gets or sets the stat points of the reward; they stay after resets.
    /// </summary>
    [Display(Name = "Stat points", Description = "Free stat points of the reward. They are kept by resets: a reset adds all stat points of the completed quests to the points of the resets.")]
    public int StatPoints { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"{this.Kills} x {this.MonsterName} ({this.MonsterNumber})";
}

/// <summary>
/// An item of the starter gear of a class.
/// </summary>
public class KillQuestGearItem
{
    /// <summary>
    /// Gets or sets the base class number (0 DW, 4 DK, 8 Elf, 12 MG, 16 DL, 20 SUM, 24 RF).
    /// </summary>
    [Display(Name = "Class number")]
    public int ClassNumber { get; set; }

    /// <summary>
    /// Gets or sets the gear step (1-6).
    /// </summary>
    public int Step { get; set; }

    /// <summary>
    /// Gets or sets the item group.
    /// </summary>
    [Display(Name = "Item group")]
    public byte ItemGroup { get; set; }

    /// <summary>
    /// Gets or sets the item number.
    /// </summary>
    [Display(Name = "Item number")]
    public short ItemNumber { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"class {this.ClassNumber}, step {this.Step}: {this.ItemGroup}/{this.ItemNumber}";
}
