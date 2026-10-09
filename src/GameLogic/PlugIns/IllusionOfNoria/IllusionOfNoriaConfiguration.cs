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
    /// Gets or sets a value indicating whether the daily quest can be taken again right after the reward (for tests).
    /// </summary>
    [Display(Name = "Daily: repeatable (test)", Description = "The daily quest can be taken again right after the reward, without waiting for the next day. For tests.")]
    public bool DailyRepeatable { get; set; }

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
    /// Gets or sets the price of a Lesser Mirage Stone of the warden in Illusion Shards.
    /// </summary>
    [Display(Name = "Shop: Lesser Mirage Stone price", Description = "The Illusion Shards which a Lesser Mirage Stone of the warden costs (a daily quest gives 10). The Greater Mirage Stone is not sold: only the boss and the monsters drop it.")]
    public int LesserStonePrice { get; set; } = 5;

    /// <summary>
    /// Gets or sets the price of the Veil Ward of the warden in Illusion Shards.
    /// </summary>
    [Display(Name = "Shop: Veil Ward price", Description = "The Illusion Shards which the Veil Ward costs: the Golden Curse of the boss does no damage while it lasts.")]
    public int WardPrice { get; set; } = 5;

    /// <summary>
    /// Gets or sets the duration of the Veil Ward.
    /// </summary>
    [Display(Name = "Shop: Veil Ward duration", Description = "How long the Veil Ward lasts (it stays after death).")]
    public TimeSpan WardDuration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Gets or sets the number of the magic effect of the Veil Ward.
    /// </summary>
    [Display(Name = "Shop: Veil Ward effect", Description = "The number of the magic effect of the Veil Ward; the client shows its icon and timer.")]
    public short WardEffectNumber { get; set; } = 189;

    /// <summary>
    /// Gets or sets the price of the Blessing of the Veil of the warden in Illusion Shards.
    /// </summary>
    [Display(Name = "Shop: Blessing of the Veil price", Description = "The Illusion Shards which the Blessing of the Veil costs: more damage to the monsters of the illusion (and the boss).")]
    public int BlessingPrice { get; set; } = 5;

    /// <summary>
    /// Gets or sets the duration of the Blessing of the Veil.
    /// </summary>
    [Display(Name = "Shop: Blessing of the Veil duration", Description = "How long the Blessing of the Veil lasts; a new one renews the time. It works together with the Veil Ward.")]
    public TimeSpan BlessingDuration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets the additional damage of the Blessing of the Veil in percent.
    /// </summary>
    [Display(Name = "Shop: Blessing of the Veil damage (%)", Description = "The additional damage in percent to the monsters of the illusion and its boss.")]
    public int BlessingDamagePercent { get; set; } = 50;

    /// <summary>
    /// Gets or sets the number of the magic effect of the Blessing of the Veil.
    /// </summary>
    [Display(Name = "Shop: Blessing of the Veil effect", Description = "The number of the magic effect of the Blessing of the Veil; the client shows its icon and timer.")]
    public short BlessingEffectNumber { get; set; } = 185;




    /// <summary>
    /// Gets or sets the Illusion Shards of the mix which adds the skill fix option.
    /// </summary>
    [Display(Name = "Mix: shards to add the option", Description = "The Illusion Shards of the mix of the Chaos Goblin of the illusion which adds the skill fix option (the shards to remove it are in 'Reset: price rank 7/8').")]
    public int AddOptionShards { get; set; } = 10;

    /// <summary>
    /// Gets or sets the Jewels of Illusion of the mix which creates a random Echo.
    /// </summary>
    [Display(Name = "Mix: Echo - Jewels of Illusion", Description = "The Jewels of Illusion of the mix of the Chaos Goblin of the illusion which creates a random Echo (one of all 22).")]
    public int EchoJewels { get; set; } = 1;

    /// <summary>
    /// Gets or sets the Illusion Shards of the mix which creates a random Echo.
    /// </summary>
    [Display(Name = "Mix: Echo - shards", Description = "The Illusion Shards of the mix of the Chaos Goblin of the illusion which creates a random Echo.")]
    public int EchoShards { get; set; } = 20;

    /// <summary>
    /// Gets or sets the chance in percent that a killed monster of the illusion drops a Lesser Mirage Stone.
    /// </summary>
    [Display(Name = "Drop: Lesser Mirage Stone (%)", Description = "The chance in percent per killed monster of the illusion.")]
    public double MonsterLesserStoneChancePercent { get; set; } = 1;

    /// <summary>
    /// Gets or sets the chance in percent that a killed monster of the illusion drops a Greater Mirage Stone.
    /// </summary>
    [Display(Name = "Drop: Greater Mirage Stone (%)", Description = "The chance in percent per killed monster of the illusion.")]
    public double MonsterGreaterStoneChancePercent { get; set; } = 0.01;


    /// <summary>
    /// Gets or sets the chance in percent that a killed monster of the illusion drops a random Echo.
    /// </summary>
    [Display(Name = "Drop: random Echo (%)", Description = "The chance in percent per killed monster of the illusion (one of all Echoes).")]
    public double MonsterEchoChancePercent { get; set; } = 0.05;

    /// <summary>
    /// Gets or sets the chance in percent that a killed monster of the illusion drops a Jewel of Illusion.
    /// </summary>
    [Display(Name = "Drop: Jewel of Illusion (%)", Description = "The chance in percent per killed monster of the illusion. The jewel removes the skill fix option and creates an Echo (Chaos Goblin of the illusion).")]
    public double MonsterIllusionJewelChancePercent { get; set; } = 0.01;

    /// <summary>
    /// Gets or sets the chance in percent that a killed monster of the illusion drops a rank 7-8 weapon.
    /// </summary>
    [Display(Name = "Drop: rank 7-8 weapon (%)", Description = "The chance in percent per killed monster of the illusion: a random rank 7-8 weapon with the skill fix options, +10, with a random skill fix option of a random level (see 'Weapon: ...').")]
    public double MonsterWeaponChancePercent { get; set; } = 0.001;

    /// <summary>
    /// Gets or sets the chance in percent that the boss drops a rank 7-8 weapon.
    /// </summary>
    [Display(Name = "Boss: rank 7-8 weapon (%)", Description = "The chance in percent that the boss drops a rank 7-8 weapon (like the monsters).")]
    public double BossWeaponChancePercent { get; set; } = 1;

    /// <summary>
    /// Gets or sets the item level of a dropped rank 7-8 weapon.
    /// </summary>
    [Display(Name = "Weapon: level", Description = "The item level of a dropped rank 7-8 weapon.")]
    public byte WeaponLevel { get; set; } = 10;

    /// <summary>
    /// Gets or sets the largest level of the skill fix option of a dropped weapon (a random level from 1).
    /// </summary>
    [Display(Name = "Weapon: maximum option level", Description = "The skill fix option of a dropped weapon has a random level from 1 to this.")]
    public byte WeaponMaximumOptionLevel { get; set; } = 10;

    /// <summary>
    /// Gets or sets the chance in percent that a dropped weapon has luck.
    /// </summary>
    [Display(Name = "Weapon: luck (%)", Description = "The chance in percent that a dropped rank 7-8 weapon has luck.")]
    public double WeaponLuckChancePercent { get; set; } = 50;

    /// <summary>
    /// Gets or sets the chance in percent that a dropped weapon has its skill.
    /// </summary>
    [Display(Name = "Weapon: skill (%)", Description = "The chance in percent that a dropped rank 7-8 weapon has its skill. No excellent options.")]
    public double WeaponSkillChancePercent { get; set; } = 50;

    /// <summary>
    /// Gets or sets the Echoes: the jewels which add the skill fix option of one skill (item number : skill number).
    /// </summary>
    [Display(Name = "Echoes", Description = "The Echoes (group 14): item number:skill number, separated by commas. An Echo adds the skill fix option of its skill to a rank 7-8 weapon which has it (like the Jewel of Illusion, but not random).")]
    public string Echoes { get; set; } = "173:41,174:43,175:232,176:56,177:55,178:9,179:237,180:8,181:38,182:39,183:235,184:52,185:46,186:78,187:61,188:238,189:230,190:215,191:225,192:264,193:263,194:260";

    /// <summary>
    /// Gets the skill of an Echo.
    /// </summary>
    /// <param name="group">The item group.</param>
    /// <param name="number">The item number.</param>
    /// <returns>The skill number, or <c>null</c> if the item is no Echo.</returns>
    public short? GetEchoSkill(byte group, short number) => group == 14 && this.GetEchoes().TryGetValue(number, out var skill) ? skill : null;

    /// <summary>
    /// Gets the item numbers (group 14) of all Echoes.
    /// </summary>
    /// <returns>The item numbers.</returns>
    public IReadOnlyList<short> GetEchoItemNumbers() => this.GetEchoes().Keys.ToList();

    private Dictionary<short, short> GetEchoes()
    {
        var result = new Dictionary<short, short>();
        foreach (var entry in this.Echoes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (entry.Split(':') is [var item, var skill] && short.TryParse(item, out var itemNumber) && short.TryParse(skill, out var skillNumber))
            {
                result[itemNumber] = skillNumber;
            }
        }

        return result;
    }

    /// <summary>
    /// Gets or sets the distance from the killed monster in which the members of a party count the kill.
    /// </summary>
    [Display(Name = "Party range", Description = "The members of the party of the killer within this distance of the killed monster count the kill for their daily quest.")]
    public int PartyRange { get; set; } = 15;

    /// <summary>
    /// Gets or sets the rank 8 weapons (the others with the fix options are rank 7) for the price of the reset.
    /// </summary>
    [Display(Name = "Reset: rank 8 weapons", Description = "The rank 8 weapons (group:number, separated by commas); the reset of their skill fix option costs more. Other weapons with the option are rank 7.")]
    public string Rank8Weapons { get; set; } = "0:20,0:21,0:22,0:23,0:35,2:12,2:14,4:20,4:21,5:11,5:12,5:20";

    /// <summary>
    /// Gets or sets the price of the reset of the skill fix option of a rank 7 weapon.
    /// </summary>
    [MemberOfAggregate]
    [Display(Name = "Reset: price rank 7", Description = "The items which the reset of the skill fix option of a rank 7 weapon costs (the minimum amount counts).")]
    public ICollection<IllusionRewardItem> ResetPriceRank7 { get; set; } = new List<IllusionRewardItem>
    {
        new() { Name = "Jewel of Illusion", ItemGroup = 14, ItemNumber = 195, MinimumAmount = 1, MaximumAmount = 1 },
        new() { Name = "Lesser Mirage Stone", ItemGroup = 14, ItemNumber = 196, MinimumAmount = 4, MaximumAmount = 4 },
        new() { Name = "Greater Mirage Stone", ItemGroup = 14, ItemNumber = 197, MinimumAmount = 1, MaximumAmount = 1 },
        new() { Name = "Illusion Shard", ItemGroup = 14, ItemNumber = 172, MinimumAmount = 10, MaximumAmount = 10 },
    };

    /// <summary>
    /// Gets or sets the price of the reset of the skill fix option of a rank 8 weapon.
    /// </summary>
    [MemberOfAggregate]
    [Display(Name = "Reset: price rank 8", Description = "The items which the reset of the skill fix option of a rank 8 weapon costs (the minimum amount counts).")]
    public ICollection<IllusionRewardItem> ResetPriceRank8 { get; set; } = new List<IllusionRewardItem>
    {
        new() { Name = "Jewel of Illusion", ItemGroup = 14, ItemNumber = 195, MinimumAmount = 2, MaximumAmount = 2 },
        new() { Name = "Lesser Mirage Stone", ItemGroup = 14, ItemNumber = 196, MinimumAmount = 10, MaximumAmount = 10 },
        new() { Name = "Greater Mirage Stone", ItemGroup = 14, ItemNumber = 197, MinimumAmount = 2, MaximumAmount = 2 },
        new() { Name = "Illusion Shard", ItemGroup = 14, ItemNumber = 172, MinimumAmount = 20, MaximumAmount = 20 },
    };

    /// <summary>
    /// Gets or sets the number of the boss monster.
    /// </summary>
    [Display(Name = "Boss", Description = "The number of the boss monster (Gilded Colossus).")]
    public short BossNumber { get; set; } = 718;

    /// <summary>
    /// Gets or sets the kills on the map after which the boss awakens.
    /// </summary>
    [Display(Name = "Boss: kills", Description = "The boss awakens at a random place after this many monsters of the illusion were killed (by all players). The count stops while the boss lives and starts again after the cooldown.")]
    public int BossKills { get; set; } = 3000;

    /// <summary>
    /// Gets or sets a value indicating whether the boss awakens at the place of the last killed monster (for tests).
    /// </summary>
    [Display(Name = "Boss: at the last kill (test)", Description = "The boss awakens where the last monster was killed, instead of a random place. For tests.")]
    public bool BossAtLastKill { get; set; }

    /// <summary>
    /// Gets or sets the time after the death of the boss before the kills count again.
    /// </summary>
    [Display(Name = "Boss: cooldown", Description = "After the boss is defeated, the kills don't count for the next boss during this time.")]
    public TimeSpan BossCooldown { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets the message when the boss awakens ({0}: name of the boss).
    /// </summary>
    [Display(Name = "Boss: message", Description = "The golden message for the players in the illusion when the boss awakens; {0} is the name of the boss.")]
    public string BossAwakenedMessage { get; set; } = "{0} has awakened in the Illusion of Noria!";

    /// <summary>
    /// Gets or sets the message when the boss is defeated ({0}: name of the boss, {1}: name of the killer).
    /// </summary>
    [Display(Name = "Boss: defeated message", Description = "The golden message for the players in the illusion when the boss is defeated; {0} is the boss, {1} the killer ({2}: the minutes of the cooldown, if wanted).")]
    public string BossDefeatedMessage { get; set; } = "{0} was defeated by {1}!";

    /// <summary>
    /// Gets or sets the reward items which the boss drops.
    /// </summary>
    [MemberOfAggregate]
    [Display(Name = "Boss: drop", Description = "The items which the boss drops (with the chance, a random amount from minimum to maximum each); number -1 is a random Echo.")]
    public ICollection<IllusionRewardItem> BossDrops { get; set; } = new List<IllusionRewardItem>
    {
        new() { Name = "Random Echoes", ItemGroup = 14, ItemNumber = -1, MinimumAmount = 1, MaximumAmount = 3 },
        new() { Name = "Lesser Mirage Stone", ItemGroup = 14, ItemNumber = 196, MinimumAmount = 5, MaximumAmount = 10 },
        new() { Name = "Greater Mirage Stone", ItemGroup = 14, ItemNumber = 197, MinimumAmount = 0, MaximumAmount = 2 },
        new() { Name = "Jewel of Illusion", ItemGroup = 14, ItemNumber = 195, MinimumAmount = 0, MaximumAmount = 2, ChancePercent = 70 },
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
    /// Gets the price of the reset of the skill fix option of a weapon.
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
/// An item of a reward of the illusion (the boss).
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

    /// <summary>
    /// Gets or sets the chance in percent that the item drops at all (a drop of the boss).
    /// </summary>
    public double ChancePercent { get; set; } = 100;
}
