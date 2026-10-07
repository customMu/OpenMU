// <copyright file="PetDropPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Composition;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The pets Guardian Angel and Imp and the Horn of Uniria (10 of them make a Dinorant in the Chaos Machine) drop from the
/// monsters of the hunting maps by the monster level: the chance grows linearly from the first to the last monster level.
/// </summary>
[PlugIn]
[Display(Name = "Pet drop", Description = "Guardian Angel, Imp and Horn of Uniria drop from the monsters of the hunting maps; the chance grows linearly with the monster level.")]
[Guid("45173DA8-440E-4AF7-BD83-32386EAFD934")]
public class PetDropPlugIn : IAdditionalItemDropPlugIn, ISupportCustomConfiguration<PetDropConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc />
    public PetDropConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new PetDropConfiguration();

    /// <summary>
    /// Gets the chance in percent of the pet for a monster of the level, without the monster multiplier.
    /// </summary>
    /// <param name="pet">The pet.</param>
    /// <param name="monsterLevel">The monster level.</param>
    /// <returns>The chance in percent; 0 outside of the monster levels of the pet.</returns>
    public static double GetChancePercent(PetDropConfiguration.PetDropEntry pet, int monsterLevel)
    {
        if (monsterLevel < pet.MinimumMonsterLevel || monsterLevel > pet.MaximumMonsterLevel)
        {
            return 0;
        }

        if (pet.MaximumMonsterLevel <= pet.MinimumMonsterLevel)
        {
            return pet.MaximumChancePercent;
        }

        var share = (double)(monsterLevel - pet.MinimumMonsterLevel) / (pet.MaximumMonsterLevel - pet.MinimumMonsterLevel);
        return pet.MinimumChancePercent + ((pet.MaximumChancePercent - pet.MinimumChancePercent) * share);
    }

    /// <inheritdoc />
    public ValueTask AddItemDropsAsync(AdditionalItemDropArgs args)
    {
        var configuration = this.Configuration ??= (PetDropConfiguration)this.CreateDefaultConfig();
        if (args.KilledObject.IsSummonedMonster
            || args.Monster.ObjectKind != NpcObjectKind.Monster
            || args.Killer.CurrentMiniGame is not null)
        {
            return ValueTask.CompletedTask;
        }

        var monsterLevel = (int)args.Monster[Stats.Level];
        var multiplier = MonsterDropMultiplier.Get(configuration.MonsterMultipliers, args.Monster.Number);
        if (multiplier <= 0)
        {
            return ValueTask.CompletedTask;
        }

        foreach (var pet in configuration.Pets)
        {
            var chance = GetChancePercent(pet, monsterLevel) * multiplier / 100.0;
            if (chance <= 0 || Rand.NextDouble() >= chance)
            {
                continue;
            }

            var definition = args.Killer.GameContext.Configuration.Items
                .FirstOrDefault(i => i.Group == pet.ItemGroup && i.Number == pet.ItemNumber);
            if (definition is null)
            {
                args.Killer.Logger.LogWarning("Pet drop: item {group}/{number} ({name}) not found in the configuration.", pet.ItemGroup, pet.ItemNumber, pet.Name);
                continue;
            }

            var item = new TemporaryItem { Definition = definition };
            item.Durability = item.GetMaximumDurabilityOfOnePiece();
            args.Items.Add(item);
        }

        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// The configuration of the <see cref="PetDropPlugIn"/>.
/// </summary>
public class PetDropConfiguration
{
    /// <summary>
    /// Gets or sets the multipliers for specific monsters, e.g. bosses which take much longer to kill.
    /// </summary>
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [Display(Name = "Monster multipliers", Description = "Multiplies the chances for specific monsters (bosses). Monsters which are not listed use 1. Use 0 to disable the drop of a monster.")]
    public ICollection<MonsterDropMultiplier> MonsterMultipliers { get; set; } = new List<MonsterDropMultiplier>();

    /// <summary>
    /// Gets or sets the pets.
    /// </summary>
    [MemberOfAggregate]
    [Display(Name = "Pets", Description = "Each pet drops with its own roll per killed monster of its level range; the chance grows linearly from the minimum to the maximum monster level.")]
    public ICollection<PetDropEntry> Pets { get; set; } = CreateDefaultPets();

    private static List<PetDropEntry> CreateDefaultPets() =>
    [
        new() { Name = "Guardian Angel", ItemGroup = 13, ItemNumber = 0 },
        new() { Name = "Imp", ItemGroup = 13, ItemNumber = 1 },
        new() { Name = "Horn of Uniria", ItemGroup = 13, ItemNumber = 2 },
    ];

    /// <summary>
    /// A pet which drops from the monsters.
    /// </summary>
    public class PetDropEntry
    {
        /// <summary>
        /// Gets or sets the name (only for the admin panel).
        /// </summary>
        public string Name { get; set; } = string.Empty;

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
        /// Gets or sets the minimum monster level.
        /// </summary>
        [Display(Name = "Minimum monster level")]
        public int MinimumMonsterLevel { get; set; } = 50;

        /// <summary>
        /// Gets or sets the maximum monster level.
        /// </summary>
        [Display(Name = "Maximum monster level")]
        public int MaximumMonsterLevel { get; set; } = 400;

        /// <summary>
        /// Gets or sets the chance per killed monster in percent at the minimum monster level.
        /// </summary>
        [Display(Name = "Chance at the minimum level (%)")]
        public float MinimumChancePercent { get; set; } = 0.001f;

        /// <summary>
        /// Gets or sets the chance per killed monster in percent at the maximum monster level.
        /// </summary>
        [Display(Name = "Chance at the maximum level (%)")]
        public float MaximumChancePercent { get; set; } = 0.05f;

        /// <inheritdoc />
        public override string ToString() => $"{this.Name}: monster level {this.MinimumMonsterLevel}-{this.MaximumMonsterLevel}, {this.MinimumChancePercent} % - {this.MaximumChancePercent} %";
    }
}
