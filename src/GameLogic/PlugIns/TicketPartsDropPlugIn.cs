// <copyright file="TicketPartsDropPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns;

using System.Runtime.InteropServices;
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
        var roll = Rand.NextDouble();
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
    /// Gets or sets the parts.
    /// </summary>
    [DataModel.Composition.MemberOfAggregate]
    [Display(Name = "Parts", Description = "Each part drops with its chance per killed monster of its level range.")]
    public ICollection<TicketPart> Parts { get; set; } = CreateDefaultParts();

    private static List<TicketPart> CreateDefaultParts()
    {
        // The level ranges follow the monster levels of the reset ladder of the event levels (plugin 'Events by resets').
        // Blood Castle and Devil Square have the same levels 1-7; Blood Castle 8 (50 resets) from the monsters of level 395+.
        (int Minimum, int Maximum, float Chance)[] devilSquare =
            [(1, 90, 0.06f), (91, 165, 0.05f), (166, 215, 0.042f), (216, 270, 0.035f), (271, 320, 0.03f), (321, 357, 0.025f), (358, 394, 0.02f)];
        // Blood Castle: 0.01 % (+1) ... 0.001 % (+7) per part, +8 like +7 (Devil Square 0.06 % ... 0.02 %).
        (int Minimum, int Maximum, float Chance)[] bloodCastle =
            [(1, 90, 0.01f), (91, 165, 0.0068f), (166, 215, 0.0046f), (216, 270, 0.0032f), (271, 320, 0.0022f), (321, 357, 0.0015f), (358, 394, 0.001f), (395, 400, 0.001f)];
        var result = new List<TicketPart>();
        for (var i = 0; i < bloodCastle.Length; i++)
        {
            var (minimum, maximum, chance) = bloodCastle[i];
            result.Add(new() { Name = $"Scroll of Archangel +{i + 1}", ItemGroup = 13, ItemNumber = 16, ItemLevel = (byte)(i + 1), MinimumMonsterLevel = minimum, MaximumMonsterLevel = maximum, ChancePercent = chance });
            result.Add(new() { Name = $"Blood Bone +{i + 1}", ItemGroup = 13, ItemNumber = 17, ItemLevel = (byte)(i + 1), MinimumMonsterLevel = minimum, MaximumMonsterLevel = maximum, ChancePercent = chance });
        }

        for (var i = 0; i < devilSquare.Length; i++)
        {
            var (minimum, maximum, chance) = devilSquare[i];
            result.Add(new() { Name = $"Devil's Eye +{i + 1}", ItemGroup = 14, ItemNumber = 17, ItemLevel = (byte)(i + 1), MinimumMonsterLevel = minimum, MaximumMonsterLevel = maximum, ChancePercent = chance });
            result.Add(new() { Name = $"Devil's Key +{i + 1}", ItemGroup = 14, ItemNumber = 18, ItemLevel = (byte)(i + 1), MinimumMonsterLevel = minimum, MaximumMonsterLevel = maximum, ChancePercent = chance });
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
