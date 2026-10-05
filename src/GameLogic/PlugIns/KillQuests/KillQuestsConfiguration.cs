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
        new() { MonsterNumber = 3, MonsterName = "Spider", Kills = 25, GearStep = 1, Variants = StartVariants((26, "Goblin"), (418, "Strange Rabbit")) },
        new() { MonsterNumber = 3, MonsterName = "Spider", Kills = 75, GearStep = 2, Variants = StartVariants((26, "Goblin"), (418, "Strange Rabbit")) },
        new() { MonsterNumber = 2, MonsterName = "Budge Dragon", Kills = 50, GearStep = 3, Variants = StartVariants((26, "Goblin"), (418, "Strange Rabbit")) },
        new() { MonsterNumber = 2, MonsterName = "Budge Dragon", Kills = 100, GearStep = 4, Variants = StartVariants((26, "Goblin"), (418, "Strange Rabbit")) },
        new() { MonsterNumber = 1, MonsterName = "Hound", Kills = 100, GearStep = 5, Variants = StartVariants((28, "Beetle Monster"), (419, "Polluted Butterfly")) },
        new() { MonsterNumber = 1, MonsterName = "Hound", Kills = 200, GearStep = 6, Variants = StartVariants((28, "Beetle Monster"), (419, "Polluted Butterfly")) },
    ];

    /// <summary>
    /// The variants of a starter quest for the classes which don't start in Lorencia: the elf (Noria) and the summoner (Elvenland).
    /// </summary>
    private static List<KillQuestVariant> StartVariants((short Number, string Name) noria, (short Number, string Name) elvenland) =>
    [
        new() { HomeMapNumber = 3, MonsterNumber = noria.Number, MonsterName = noria.Name },
        new() { HomeMapNumber = 51, MonsterNumber = elvenland.Number, MonsterName = elvenland.Name },
    ];

    /// <summary>
    /// The zen for a gear piece which the class can't wear (helm of the Magic Gladiator, gloves of the Rage Fighter).
    /// </summary>
    private const int MissingPieceMoney = 5000;

    private static List<KillQuestGearItem> CreateDefaultGear()
    {
        var gear = new List<KillQuestGearItem>();

        void Set(int classNumber, short setNumber, bool helm = true, bool gloves = true)
        {
            // gloves, boots, helm, pants, armor, weapon; a class without helm or gloves gets zen instead
            (int Step, byte Group, short Number, bool Has)[] pieces =
            [
                (1, 10, setNumber, gloves),
                (2, 11, setNumber, true),
                (3, 7, setNumber, helm),
                (4, 9, setNumber, true),
                (5, 8, setNumber, true),
            ];
            gear.AddRange(pieces.Select(p => new KillQuestGearItem { ClassNumber = classNumber, Step = p.Step, ItemGroup = p.Group, ItemNumber = p.Number, Money = p.Has ? 0 : MissingPieceMoney }));
        }

        void Weapon(int classNumber, byte group, short number)
            => gear.Add(new KillQuestGearItem { ClassNumber = classNumber, Step = 6, ItemGroup = group, ItemNumber = number, Skill = true, Luck = true });

        Set(0, 2);                 // Dark Wizard: Pad
        Set(4, 5);                 // Dark Knight: Leather
        Set(8, 10);                // Fairy Elf: Vine
        Set(12, 5, helm: false);   // Magic Gladiator: Leather without helm
        Set(16, 5);                // Dark Lord: Leather
        Set(20, 39);               // Summoner: Mistery
        Set(24, 5, gloves: false); // Rage Fighter: Leather without gloves

        // the weapon (+luck): one weapon for all classes which can use it, the weakest one with a skill where rank 1 has one:
        // Morning Star (Falling Slash) for DK, MG, DL and RF, Golden Crossbow (Triple Shot) with bolts for the elf;
        // Skull Staff for DW and Mistery Stick for the summoner (the staffs and sticks of rank 1 have no skill)
        foreach (var classNumber in new[] { 4, 12, 16, 24 })
        {
            Weapon(classNumber, 2, 1);
        }

        Weapon(8, 4, 9);
        Weapon(0, 5, 0);
        Weapon(20, 5, 14);
        gear.Add(new KillQuestGearItem { ClassNumber = 8, Step = 6, ItemGroup = 4, ItemNumber = 7 });
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

    /// <summary>
    /// Gets or sets the zen of the reward.
    /// </summary>
    [Display(Name = "Zen", Description = "Zen of the reward (about 2 hours of farming at the monster).")]
    public int Money { get; set; }

    /// <summary>
    /// Gets or sets the map where the monster lives, shown with the monster name ("Hound (Lorencia)").
    /// </summary>
    [Display(Name = "Location", Description = "The map where the monster lives; shown with the monster name.")]
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the variants of the quest for characters whose class starts on another map than Lorencia.
    /// </summary>
    [MemberOfAggregate]
    [Display(Name = "Variants by home map", Description = "Another monster for the characters whose class starts on the map (home map number: 3 Noria, 51 Elvenland). The monster of the quest counts for them too.")]
    public ICollection<KillQuestVariant> Variants { get; set; } = new List<KillQuestVariant>();

    /// <inheritdoc />
    public override string ToString() => $"{this.Kills} x {this.MonsterName} ({this.MonsterNumber})";
}

/// <summary>
/// A variant of a quest for the characters whose class starts on another map: another monster of that map.
/// </summary>
public class KillQuestVariant
{
    /// <summary>
    /// Gets or sets the number of the home map of the character class (3 Noria, 51 Elvenland).
    /// </summary>
    [Display(Name = "Home map number")]
    public short HomeMapNumber { get; set; }

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
    /// Gets or sets the map where the monster lives.
    /// </summary>
    [Display(Name = "Location")]
    public string Location { get; set; } = string.Empty;

    /// <inheritdoc />
    public override string ToString() => $"map {this.HomeMapNumber}: {this.MonsterName} ({this.MonsterNumber})";
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

    /// <summary>
    /// Gets or sets a value indicating whether the item comes with its skill (only items which have a skill).
    /// </summary>
    public bool Skill { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the item comes with luck (only items which can have luck).
    /// </summary>
    public bool Luck { get; set; }

    /// <summary>
    /// Gets or sets the zen given instead of an item (for a class without this piece, e.g. no helm); 0 = the item.
    /// </summary>
    public int Money { get; set; }

    /// <inheritdoc />
    public override string ToString() => $"class {this.ClassNumber}, step {this.Step}: {this.ItemGroup}/{this.ItemNumber}{(this.Skill ? " +skill" : string.Empty)}{(this.Luck ? " +luck" : string.Empty)}{(this.Money > 0 ? $" ({this.Money} zen instead)" : string.Empty)}";
}
