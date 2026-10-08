// <copyright file="IllusionOfNoriaConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.IllusionOfNoria;

using MUnique.OpenMU.DataModel.Composition;

/// <summary>
/// The configuration of the <see cref="IllusionOfNoriaPlugIn"/>.
/// </summary>
public class IllusionOfNoriaConfiguration
{
    /// <summary>
    /// The first day of the day numbers, see <see cref="GetDayNumber"/>.
    /// </summary>
    private static readonly DateTime DayNumberEpoch = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

    /// <summary>
    /// Gets or sets the number of the map of the illusion.
    /// </summary>
    [Display(Name = "Illusion map", Description = "The number of the map Illusion of Noria.")]
    public short MapNumber { get; set; } = 82;

    /// <summary>
    /// Gets or sets the number of the map where the warden stands outside of the illusion (Noria).
    /// </summary>
    [Display(Name = "Home map", Description = "The number of the map where the warden stands outside of the illusion (Noria); 'Return' warps there.")]
    public short HomeMapNumber { get; set; } = 3;

    /// <summary>
    /// Gets or sets the number of the warden npc.
    /// </summary>
    [Display(Name = "Warden npc", Description = "The number of the warden npc (Noria and the town of the illusion).")]
    public short WardenNpcNumber { get; set; } = 719;

    /// <summary>
    /// Gets or sets the resets which are needed for the quest and the entry.
    /// </summary>
    [Display(Name = "Required resets", Description = "The resets which are needed for the quest of the whistle and the entry.")]
    public int RequiredResets { get; set; } = 20;

    /// <summary>
    /// Gets or sets the group of the whistle item.
    /// </summary>
    [Display(Name = "Whistle group", Description = "The item group of the Whistle of the Veil.")]
    public byte WhistleItemGroup { get; set; } = 14;

    /// <summary>
    /// Gets or sets the number of the whistle item.
    /// </summary>
    [Display(Name = "Whistle number", Description = "The item number of the Whistle of the Veil.")]
    public short WhistleItemNumber { get; set; } = 171;

    /// <summary>
    /// Gets or sets the monster which drops the whistle.
    /// </summary>
    [Display(Name = "Whistle monster", Description = "The monster which drops the whistle (Condra, the outcast).")]
    public short WhistleMonsterNumber { get; set; } = 575;

    /// <summary>
    /// Gets or sets the chance in percent that the whistle drops from the monster.
    /// </summary>
    [Display(Name = "Whistle chance (%)", Description = "The chance in percent per kill that the whistle drops for a player who took the quest (and has the resets).")]
    public double WhistleChancePercent { get; set; } = 0.005;

    /// <summary>
    /// Gets or sets the monsters of the illusion (the daily quest picks its targets from them).
    /// </summary>
    [Display(Name = "Illusion monsters", Description = "The numbers of the monsters of the illusion, separated by commas; the daily quest picks its targets from them.")]
    public string MonsterNumbers { get; set; } = "710,711,712,713,714,715,716,717";

    /// <summary>
    /// Gets or sets the number of monster kinds of the daily quest.
    /// </summary>
    [Display(Name = "Daily: kinds", Description = "The number of random monster kinds of the daily quest.")]
    public int DailyKinds { get; set; } = 3;

    /// <summary>
    /// Gets or sets the kills of each kind of the daily quest.
    /// </summary>
    [Display(Name = "Daily: kills", Description = "The kills of each monster kind of the daily quest.")]
    public int DailyKills { get; set; } = 100;

    /// <summary>
    /// Gets or sets the hour (server time) at which a new day of the daily quest starts.
    /// </summary>
    [Display(Name = "Daily: reset hour", Description = "The hour (server time) at which a new day of the daily quest starts.")]
    public int DailyResetHour { get; set; } = 6;

    /// <summary>
    /// Gets or sets the item group of the Illusion Shard.
    /// </summary>
    [Display(Name = "Shard group", Description = "The item group of the Illusion Shard, the currency of the warden (a stackable item which can be traded).")]
    public byte ShardItemGroup { get; set; } = 14;

    /// <summary>
    /// Gets or sets the item number of the Illusion Shard.
    /// </summary>
    [Display(Name = "Shard number", Description = "The item number of the Illusion Shard.")]
    public short ShardItemNumber { get; set; } = 172;

    /// <summary>
    /// Gets or sets the Illusion Shards of the reward of the daily quest.
    /// </summary>
    [Display(Name = "Daily: shards", Description = "The Illusion Shards (the currency of the warden) of the reward of the daily quest.")]
    public int DailyShards { get; set; } = 10;

    /// <summary>
    /// Gets or sets the chance in percent that a killed monster of the illusion drops an Illusion Shard.
    /// </summary>
    [Display(Name = "Shard chance (%)", Description = "The chance in percent per killed monster of the illusion that it drops an Illusion Shard.")]
    public double MonsterShardChancePercent { get; set; } = 0.01;

    /// <summary>
    /// The default price of an Echo in Illusion Shards.
    /// </summary>
    private const int EchoPrice = 15;

    /// <summary>
    /// Gets or sets the shop of the warden (opened after the quest of the whistle): the items for Illusion Shards.
    /// </summary>
    [MemberOfAggregate]
    [Display(Name = "Shop", Description = "The items which the warden sells for Illusion Shards after the quest (one piece per purchase; the minimum amount is the price). An Echo is shown only to the characters who know its skill.")]
    public ICollection<IllusionRewardItem> Shop { get; set; } = new List<IllusionRewardItem>
    {
        new() { Name = "Jewel of Illusion", ItemGroup = 14, ItemNumber = 42, MinimumAmount = 5, MaximumAmount = 5 },
        new() { Name = "Lesser Mirage Stone", ItemGroup = 14, ItemNumber = 43, MinimumAmount = 1, MaximumAmount = 1 },
        new() { Name = "Greater Mirage Stone", ItemGroup = 14, ItemNumber = 44, MinimumAmount = 20, MaximumAmount = 20 },
        new() { Name = "Echo of Twisting Slash", ItemGroup = 14, ItemNumber = 173, MinimumAmount = EchoPrice, MaximumAmount = EchoPrice },
        new() { Name = "Echo of Death Stab", ItemGroup = 14, ItemNumber = 174, MinimumAmount = EchoPrice, MaximumAmount = EchoPrice },
        new() { Name = "Echo of Strike of Destruction", ItemGroup = 14, ItemNumber = 175, MinimumAmount = EchoPrice, MaximumAmount = EchoPrice },
        new() { Name = "Echo of Power Slash", ItemGroup = 14, ItemNumber = 176, MinimumAmount = EchoPrice, MaximumAmount = EchoPrice },
        new() { Name = "Echo of Fire Slash", ItemGroup = 14, ItemNumber = 177, MinimumAmount = EchoPrice, MaximumAmount = EchoPrice },
        new() { Name = "Echo of Evil Spirit", ItemGroup = 14, ItemNumber = 178, MinimumAmount = EchoPrice, MaximumAmount = EchoPrice },
        new() { Name = "Echo of Gigantic Storm", ItemGroup = 14, ItemNumber = 179, MinimumAmount = EchoPrice, MaximumAmount = EchoPrice },
        new() { Name = "Echo of Twister", ItemGroup = 14, ItemNumber = 180, MinimumAmount = EchoPrice, MaximumAmount = EchoPrice },
        new() { Name = "Echo of Decay", ItemGroup = 14, ItemNumber = 181, MinimumAmount = EchoPrice, MaximumAmount = EchoPrice },
        new() { Name = "Echo of Ice Storm", ItemGroup = 14, ItemNumber = 182, MinimumAmount = EchoPrice, MaximumAmount = EchoPrice },
        new() { Name = "Echo of Multi-Shot", ItemGroup = 14, ItemNumber = 183, MinimumAmount = EchoPrice, MaximumAmount = EchoPrice },
        new() { Name = "Echo of Penetration", ItemGroup = 14, ItemNumber = 184, MinimumAmount = EchoPrice, MaximumAmount = EchoPrice },
        new() { Name = "Echo of Starfall", ItemGroup = 14, ItemNumber = 185, MinimumAmount = EchoPrice, MaximumAmount = EchoPrice },
        new() { Name = "Echo of Fire Scream", ItemGroup = 14, ItemNumber = 186, MinimumAmount = EchoPrice, MaximumAmount = EchoPrice },
        new() { Name = "Echo of Fire Burst", ItemGroup = 14, ItemNumber = 187, MinimumAmount = EchoPrice, MaximumAmount = EchoPrice },
        new() { Name = "Echo of Chaotic Diseier", ItemGroup = 14, ItemNumber = 188, MinimumAmount = EchoPrice, MaximumAmount = EchoPrice },
        new() { Name = "Echo of Lightning Shock", ItemGroup = 14, ItemNumber = 189, MinimumAmount = EchoPrice, MaximumAmount = EchoPrice },
        new() { Name = "Echo of Chain Lightning", ItemGroup = 14, ItemNumber = 190, MinimumAmount = EchoPrice, MaximumAmount = EchoPrice },
        new() { Name = "Echo of Pollution", ItemGroup = 14, ItemNumber = 191, MinimumAmount = EchoPrice, MaximumAmount = EchoPrice },
        new() { Name = "Echo of Dragon Roar", ItemGroup = 14, ItemNumber = 192, MinimumAmount = EchoPrice, MaximumAmount = EchoPrice },
        new() { Name = "Echo of Dark Side", ItemGroup = 14, ItemNumber = 193, MinimumAmount = EchoPrice, MaximumAmount = EchoPrice },
        new() { Name = "Echo of Killing Blow", ItemGroup = 14, ItemNumber = 194, MinimumAmount = EchoPrice, MaximumAmount = EchoPrice },
    };

    /// <summary>
    /// Gets or sets the Echoes: the jewels which add the harmony option of one skill (item number : skill number).
    /// </summary>
    [Display(Name = "Echoes", Description = "The Echoes (group 14): item number:skill number, separated by commas. An Echo adds the harmony option of its skill to a rank 7-8 weapon which has it (like the Jewel of Illusion, but not random).")]
    public string Echoes { get; set; } = "173:41,174:43,175:232,176:56,177:55,178:9,179:237,180:8,181:38,182:39,183:235,184:52,185:46,186:78,187:61,188:238,189:230,190:215,191:225,192:264,193:263,194:260";

    /// <summary>
    /// Gets the skill of an Echo.
    /// </summary>
    /// <param name="group">The item group.</param>
    /// <param name="number">The item number.</param>
    /// <returns>The skill number, or <c>null</c> if the item is no Echo.</returns>
    public short? GetEchoSkill(byte group, short number)
    {
        if (group != 14)
        {
            return null;
        }

        foreach (var entry in this.Echoes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (entry.Split(':') is [var item, var skill] && short.TryParse(item, out var itemNumber) && itemNumber == number && short.TryParse(skill, out var skillNumber))
            {
                return skillNumber;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets or sets the distance from the killed monster in which the members of a party count the kill.
    /// </summary>
    [Display(Name = "Party range", Description = "The members of the party of the killer within this distance of the killed monster count the kill for their daily quest.")]
    public int PartyRange { get; set; } = 15;

    /// <summary>
    /// Gets or sets the rank 8 weapons (the others with the fix options are rank 7) for the price of the reset.
    /// </summary>
    [Display(Name = "Reset: rank 8 weapons", Description = "The rank 8 weapons (group:number, separated by commas); the reset of their harmony option costs more. Other weapons with the option are rank 7.")]
    public string Rank8Weapons { get; set; } = "0:20,0:21,0:22,0:23,0:35,2:12,2:14,4:20,4:21,5:11,5:12,5:20";

    /// <summary>
    /// Gets or sets the price of the reset of the harmony option of a rank 7 weapon.
    /// </summary>
    [MemberOfAggregate]
    [Display(Name = "Reset: price rank 7", Description = "The items which the reset of the harmony option of a rank 7 weapon costs (the minimum amount counts).")]
    public ICollection<IllusionRewardItem> ResetPriceRank7 { get; set; } = new List<IllusionRewardItem>
    {
        new() { Name = "Jewel of Illusion", ItemGroup = 14, ItemNumber = 42, MinimumAmount = 2, MaximumAmount = 2 },
        new() { Name = "Lesser Mirage Stone", ItemGroup = 14, ItemNumber = 43, MinimumAmount = 2, MaximumAmount = 2 },
    };

    /// <summary>
    /// Gets or sets the price of the reset of the harmony option of a rank 8 weapon.
    /// </summary>
    [MemberOfAggregate]
    [Display(Name = "Reset: price rank 8", Description = "The items which the reset of the harmony option of a rank 8 weapon costs (the minimum amount counts).")]
    public ICollection<IllusionRewardItem> ResetPriceRank8 { get; set; } = new List<IllusionRewardItem>
    {
        new() { Name = "Jewel of Illusion", ItemGroup = 14, ItemNumber = 42, MinimumAmount = 3, MaximumAmount = 3 },
        new() { Name = "Lesser Mirage Stone", ItemGroup = 14, ItemNumber = 43, MinimumAmount = 3, MaximumAmount = 3 },
        new() { Name = "Greater Mirage Stone", ItemGroup = 14, ItemNumber = 44, MinimumAmount = 1, MaximumAmount = 1 },
    };

    /// <summary>
    /// Gets or sets the number of the boss monster.
    /// </summary>
    [Display(Name = "Boss", Description = "The number of the boss monster (Gilded Colossus).")]
    public short BossNumber { get; set; } = 718;

    /// <summary>
    /// Gets or sets the kills on the map after which the boss awakens.
    /// </summary>
    [Display(Name = "Boss: kills", Description = "The boss awakens at a random place after this many monsters of the illusion were killed (by all players).")]
    public int BossKills { get; set; } = 3000;

    /// <summary>
    /// Gets or sets the message when the boss awakens ({0}: name of the boss).
    /// </summary>
    [Display(Name = "Boss: message", Description = "The golden message for the players in the illusion when the boss awakens; {0} is the name of the boss.")]
    public string BossAwakenedMessage { get; set; } = "{0} has awakened in the Illusion of Noria!";

    /// <summary>
    /// Gets or sets the message when the boss is defeated ({0}: name of the boss, {1}: name of the killer).
    /// </summary>
    [Display(Name = "Boss: defeated message", Description = "The golden message for the players in the illusion when the boss is defeated; {0} is the boss, {1} the killer.")]
    public string BossDefeatedMessage { get; set; } = "{0} was defeated by {1}!";

    /// <summary>
    /// Gets or sets the reward items which the boss drops.
    /// </summary>
    [MemberOfAggregate]
    [Display(Name = "Boss: drop", Description = "The items which the boss drops (a random amount from minimum to maximum each).")]
    public ICollection<IllusionRewardItem> BossDrops { get; set; } = new List<IllusionRewardItem>
    {
        new() { Name = "Jewel of Illusion", ItemGroup = 14, ItemNumber = 42, MinimumAmount = 1, MaximumAmount = 3 },
        new() { Name = "Lesser Mirage Stone", ItemGroup = 14, ItemNumber = 43, MinimumAmount = 5, MaximumAmount = 10 },
        new() { Name = "Greater Mirage Stone", ItemGroup = 14, ItemNumber = 44, MinimumAmount = 0, MaximumAmount = 2 },
    };

    /// <summary>
    /// Gets or sets the number of the magic effect of the curse of the boss.
    /// </summary>
    [Display(Name = "Curse: effect", Description = "The number of the magic effect of the curse of the boss (Golden Curse); the client shows its icon.")]
    public short CurseEffectNumber { get; set; } = 188;

    /// <summary>
    /// Gets or sets the distance around the boss in which the players get the curse.
    /// </summary>
    [Display(Name = "Curse: range", Description = "Players within this distance of the boss (about a screen) get the curse; it's renewed every second while they stay.")]
    public int CurseRange { get; set; } = 12;

    /// <summary>
    /// Gets or sets the damage of the curse per second.
    /// </summary>
    [Display(Name = "Curse: damage per second", Description = "The health which the curse takes every second.")]
    public int CurseDamagePerSecond { get; set; } = 300;

    /// <summary>
    /// Gets or sets the duration of the curse.
    /// </summary>
    [Display(Name = "Curse: duration", Description = "How long the curse lasts after the player left the range of the boss.")]
    public TimeSpan CurseDuration { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Gets the monster numbers of the illusion.
    /// </summary>
    /// <returns>The monster numbers.</returns>
    public IReadOnlyList<short> GetMonsterNumbers()
    {
        return this.MonsterNumbers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(n => short.TryParse(n, out var number) ? number : (short)-1)
            .Where(n => n >= 0)
            .ToList();
    }

    /// <summary>
    /// Gets the price of the reset of the harmony option of a weapon.
    /// </summary>
    /// <param name="group">The item group.</param>
    /// <param name="number">The item number.</param>
    /// <returns>The rank (7 or 8) and the price.</returns>
    public (int Rank, ICollection<IllusionRewardItem> Price) GetResetPrice(byte group, short number)
    {
        var rank8 = this.Rank8Weapons.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(entry => entry.Split(':') is [var g, var n] && byte.TryParse(g, out var gg) && short.TryParse(n, out var nn) && gg == group && nn == number);
        return rank8 ? (8, this.ResetPriceRank8) : (7, this.ResetPriceRank7);
    }

    /// <summary>
    /// Gets the number of the day of the time, considering the <see cref="DailyResetHour"/>.
    /// </summary>
    /// <param name="now">The current local time.</param>
    /// <returns>The number of the day.</returns>
    public int GetDayNumber(DateTime now)
    {
        var shifted = now.AddHours(-Math.Clamp(this.DailyResetHour, 0, 23));
        return (int)(shifted.Date - DayNumberEpoch).TotalDays;
    }

    /// <summary>
    /// Gets the time until the next day of the daily quest.
    /// </summary>
    /// <param name="now">The current local time.</param>
    /// <returns>The time until the next day.</returns>
    public TimeSpan GetTimeUntilNextDay(DateTime now)
    {
        var resetToday = now.Date.AddHours(Math.Clamp(this.DailyResetHour, 0, 23));
        return (now < resetToday ? resetToday : resetToday.AddDays(1)) - now;
    }
}

/// <summary>
/// An item of a reward of the illusion (the daily quest, the boss).
/// </summary>
public class IllusionRewardItem
{
    /// <summary>
    /// Gets or sets the name (only for the configuration).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the item group.
    /// </summary>
    public byte ItemGroup { get; set; }

    /// <summary>
    /// Gets or sets the item number.
    /// </summary>
    public short ItemNumber { get; set; }

    /// <summary>
    /// Gets or sets the smallest amount.
    /// </summary>
    public int MinimumAmount { get; set; }

    /// <summary>
    /// Gets or sets the largest amount.
    /// </summary>
    public int MaximumAmount { get; set; }
}
