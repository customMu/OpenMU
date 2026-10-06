// <copyright file="TicketPartsDropPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Composition;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The parts of the tickets of Blood Castle (Scroll of Archangel, Blood Bone) and Devil Square (Devil's Eye, Devil's Key)
/// drop from the monsters of the hunting maps by the monster level (up to 400, which the drop groups can't): the part +N
/// from the monsters of the levels of the event level N, the higher parts more rarely.
/// </summary>
[PlugIn]
[Display(Name = "Ticket parts drop", Description = "The parts of the tickets of Blood Castle and Devil Square drop from the monsters of the hunting maps by the monster level; the higher parts more rarely.")]
[Guid("7B3E9A51-4C2D-4F86-9E1B-6D8A2C5F3B97")]
public class TicketPartsDropPlugIn : IAdditionalItemDropPlugIn, ISupportCustomConfiguration<TicketPartsDropConfiguration>, ISupportDefaultCustomConfiguration
{
    /// <inheritdoc />
    public TicketPartsDropConfiguration? Configuration { get; set; }

    /// <inheritdoc />
    public object CreateDefaultConfig() => new TicketPartsDropConfiguration();

    /// <inheritdoc />
    public ValueTask AddItemDropsAsync(AdditionalItemDropArgs args)
    {
        var configuration = this.Configuration ??= (TicketPartsDropConfiguration)this.CreateDefaultConfig();
        if (args.KilledObject.IsSummonedMonster
            || args.Monster.ObjectKind != NpcObjectKind.Monster
            || args.Killer.CurrentMiniGame is not null)
        {
            return ValueTask.CompletedTask;
        }

        var level = (int)args.Monster[Stats.Level];
        var multiplier = MonsterDropMultiplier.Get(configuration.MonsterMultipliers, args.Monster.Number);
        if (multiplier <= 0)
        {
            return ValueTask.CompletedTask;
        }

        // the multiplier scales the chance of every part: the same as dividing the roll
        var roll = Rand.NextDouble() / multiplier;
        foreach (var part in configuration.Parts)
        {
            if (level < part.MinimumMonsterLevel || level > part.MaximumMonsterLevel)
            {
                continue;
            }

            var chance = part.ChancePercent / 100.0;
            if (roll < chance)
            {
                if (args.Killer.GameContext.Configuration.Items.FirstOrDefault(d => d.Group == part.ItemGroup && d.Number == part.ItemNumber) is { } definition)
                {
                    args.Items.Add(new TemporaryItem { Definition = definition, Level = part.ItemLevel, Durability = 1 });
                }

                break;
            }

            roll -= chance;
        }

        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// The configuration of the <see cref="TicketPartsDropPlugIn"/>.
/// </summary>
public class TicketPartsDropConfiguration
{
    /// <summary>
    /// Gets or sets the multipliers for specific monsters, e.g. bosses which take much longer to kill.
    /// </summary>
    [MemberOfAggregate]
    [ScaffoldColumn(true)]
    [Display(Name = "Monster multipliers", Description = "Multiplies the chances for specific monsters (bosses). Monsters which are not listed use 1. Use 0 to disable the drop of a monster.")]
    public ICollection<MonsterDropMultiplier> MonsterMultipliers { get; set; } = new List<MonsterDropMultiplier>();

    /// <summary>
    /// Gets or sets the parts.
    /// </summary>
    [DataModel.Composition.MemberOfAggregate]
    [Display(Name = "Parts", Description = "Each part drops with its chance per killed monster of its level range.")]
    public ICollection<TicketPart> Parts { get; set; } = CreateDefaultParts();

    private static List<TicketPart> CreateDefaultParts()
    {
        // The monster levels of the reset steps of the event levels (plugin 'Events by resets'): a level of the parts drops
        // from the monsters of the reset where its event level starts until the next one; in the window the chance grows:
        // first third x0.5, middle x1, last third x1.5 of the base chance (BC 8 for 50 resets: only level 400).
        (int Minimum, int Maximum)[] windows = [(1, 119), (120, 170), (171, 220), (221, 289), (290, 341), (342, 365), (366, 399), (400, 400)];
        float[] bloodCastle = [0.01f, 0.0068f, 0.0046f, 0.0032f, 0.0022f, 0.0015f, 0.001f, 0.001f];
        float[] devilSquare = [0.06f, 0.05f, 0.042f, 0.035f, 0.03f, 0.025f, 0.02f];
        var result = new List<TicketPart>();
        void Add(string name, byte group, short number, int level, float chance)
        {
            var (minimum, maximum) = windows[level - 1];
            if (minimum == maximum)
            {
                result.Add(new() { Name = $"{name} +{level}", ItemGroup = group, ItemNumber = number, ItemLevel = (byte)level, MinimumMonsterLevel = minimum, MaximumMonsterLevel = maximum, ChancePercent = chance });
                return;
            }

            var third = (maximum - minimum + 1) / 3;
            (int From, int To, float Factor)[] steps = [(minimum, minimum + third - 1, 0.5f), (minimum + third, maximum - third, 1f), (maximum - third + 1, maximum, 1.5f)];
            foreach (var (from, to, factor) in steps)
            {
                result.Add(new() { Name = $"{name} +{level}", ItemGroup = group, ItemNumber = number, ItemLevel = (byte)level, MinimumMonsterLevel = from, MaximumMonsterLevel = to, ChancePercent = chance * factor });
            }
        }

        for (var level = 1; level <= bloodCastle.Length; level++)
        {
            Add("Scroll of Archangel", 13, 16, level, bloodCastle[level - 1]);
            Add("Blood Bone", 13, 17, level, bloodCastle[level - 1]);
        }

        for (var level = 1; level <= devilSquare.Length; level++)
        {
            Add("Devil's Eye", 14, 17, level, devilSquare[level - 1]);
            Add("Devil's Key", 14, 18, level, devilSquare[level - 1]);
        }

        return result;
    }

    /// <summary>
    /// A part of a ticket.
    /// </summary>
    public class TicketPart
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
        /// Gets or sets the item level (the event level).
        /// </summary>
        [Display(Name = "Item level")]
        public byte ItemLevel { get; set; }

        /// <summary>
        /// Gets or sets the minimum monster level.
        /// </summary>
        [Display(Name = "Minimum monster level")]
        public int MinimumMonsterLevel { get; set; }

        /// <summary>
        /// Gets or sets the maximum monster level.
        /// </summary>
        [Display(Name = "Maximum monster level")]
        public int MaximumMonsterLevel { get; set; }

        /// <summary>
        /// Gets or sets the chance per killed monster in percent.
        /// </summary>
        [Display(Name = "Chance (%)")]
        public float ChancePercent { get; set; }

        /// <inheritdoc />
        public override string ToString() => $"{this.Name}: monster level {this.MinimumMonsterLevel}-{this.MaximumMonsterLevel}, {this.ChancePercent} %";
    }
}
