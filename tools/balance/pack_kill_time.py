#!/usr/bin/env python3
"""Time to kill a pack of monsters, for every class at every reset step.

Model of the formulas of the server and the wiki (Formulas, Damage, Items):

* points: reset points (550 per reset, +1,000 x k at the resets 10, 20 ... 50) + 5 per level (7 for MG and DL);
  Agility by the wiki ladder (10,000 at 50 resets, the same share on every step), Vitality a share of the points,
  the rest into the damage stats of the class build (CLASSES[...]['build']);
* weapon: the best weapon of the highest rank the step allows (WEAPONS), with an enchant level (--enchant);
* one hit: random min..max (stats + weapon + skill damage, the skill 1.5x on max; spells: x (1 + rise)),
  minus the monster's defense, at least level / 10, x reset boost, x class multiplier x skill factor;
* hit chance: 1 - monster defense rate / attack rate, at least 5 %; a monster with more defense rate than the attack
  rate: 5 % and damage x 0.3;
* hits per target: Skill.NumberOfHitsPerAttack and its AreaSkillSettings from the database (or HITS_PER_TARGET);
* cast time: the plugin "Skill cast time" - fix time x (offset + speed at the fix) / (offset + speed), not below
  the fix time x (1 - harmony cut) (--cut);
* a pack: --pack monsters of one kind, the area skill hits all of them; time = casts until each is dead x cast time
  + --overhead seconds (running to the pack).

Monsters come from the live database (Level, Maximum Health, Base Defense, Defense Rate PvM):
  * default: `docker exec database psql -U postgres -d openmu` (the container of the PC),
  * --dsn "host=... dbname=openmu user=... password=..." with psycopg / psycopg2 installed,
  * --monsters-csv file with the columns name,level,hp,defense,defense_rate (no database needed).
The monsters of every step come from --steps (JSON, see pack_kill_steps.example.json): a list of monster names or a
level range per reset step.

The numbers are a model, not the game: the in-game check is the GM command `/chance` (CombatInfoChatCommandPlugIn),
which runs the real damage calculation of the server against a monster.

Usage (on the PC, server folder):
  python tools/balance/pack_kill_time.py --steps tools/balance/pack_kill_steps.json
  python tools/balance/pack_kill_time.py --steps ... --details          # damage, hit chance, cast time per class
  python tools/balance/pack_kill_time.py --steps ... --class DK --class DW
"""

import argparse
import csv
import json
import math
import shutil
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path

# ---------------------------------------------------------------------------------------------------------------------
# Settings of the server (keep in sync with the wiki / the database)
# ---------------------------------------------------------------------------------------------------------------------

# Attribute definitions (src/GameLogic/Attributes/Stats.cs)
STAT_LEVEL = '560931ad-0901-4342-b7f4-fd2e2fcc0563'
STAT_MAXIMUM_HEALTH = 'a6c39a5c-295f-415e-a314-5e9f9a748d27'
STAT_DEFENSE_BASE = 'eb098c46-60d4-4ca6-bbd4-5b6270a1407b'
STAT_DEFENSE_RATE_PVM = 'c520dd2d-1b06-4392-95ee-3c41f33e68da'

# Agility of every class by resets (wiki Formulas: the same share of the points on every step); interpolated
AGILITY_LADDER = {0: 280, 5: 890, 10: 1730, 20: 3420, 30: 5330, 50: 10000}

# The first reset of every weapon / armor rank (wiki Items)
RANK_FROM_RESETS = [0, 1, 3, 5, 10, 15, 20, 26]

# Accuracy of the weapon of a rank (wiki Equipment)
WEAPON_ACCURACY = [300, 340, 440, 560, 790, 990, 1340, 1580]

# Enchant: percent of the +0 value by level (wiki Formulas, armor enchanting), x (1 + 0.03 x (rank - 1))
ENCHANT_PERCENT = [0, 4, 8, 12, 16, 20, 24, 28, 32, 36, 42, 48, 56, 64, 74, 84]

# Plugin "Skill cast time" (SkillCastTimeConfiguration / client SkillCastTimeOptions.h)
ATTACK_SPEED_AT_FIX = 335
MAGIC_SPEED_AT_FIX = 285
ATTACK_SPEED_CURVE_OFFSET = 181
MAGIC_SPEED_CURVE_OFFSET = 145
WEAPON_SPEED = 35
MINIMUM_CAST_SECONDS = 0.12

# Reset boost: damage +0.3 % per reset
RESET_DAMAGE_BOOST = 0.003

# The best weapon of every rank per weapon kind (wiki Items, without chaos weapons): (min, max) damage or a rise in
# percent. A rank without an own weapon keeps the weapon of the rank before.
WEAPONS = {
    'dk_sword': [(21, 33), (46, 57), (106, 132), (170, 227), (340, 459), (605, 757), (938, 1124), (1025, 1134)],
    'bow': [(13, 19), (32, 43), (76, 93), (137, 165), (297, 351), (659, 805), (925, 1056), (1552, 1753)],
    'mg_sword': [(21, 33), (46, 57), (106, 132), (170, 227), (340, 459), (340, 459), (800, 960), (1094, 1336)],
    'scepter': [(14, 24), (30, 43), (68, 89), (97, 123), (268, 308), (492, 551), (566, 634), (1058, 1246)],
    'glove': [(13, 22), (29, 43), (85, 106), (85, 106), (298, 409), (443, 476), (543, 577), (780, 818)],
    'staff': [17.0, 23.1, 27.7, 34.0, 38.1, 50.6, 56.6, 65.9],
    'mg_staff': [17.0, 23.1, 27.7, 34.0, 38.1, 50.6, 54.0, 67.0],
    'stick': [17.0, 21.0, 21.0, 30.0, 37.0, 46.0, 55.0, 64.0],
}

# The classes: damage formula, multiplier, build, weapon and the main area skill of a pack (wiki Formulas, Classes).
# build: shares of the points left after Agility and Vitality. The skill: number, damage, factor, fix time (ms), the
# speed it follows (attack / magic).
CLASSES = {
    'DK': dict(name='Dark Knight', kind='physical', level_points=5, weapon='dk_sword', build={'S': 0.8, 'E': 0.2},
               skill=dict(number=41, name='Twisting Slash', damage=715, factor=0.79, fix=329, speed='attack')),
    'DW': dict(name='Dark Wizard', kind='magic', level_points=5, weapon='staff', build={'E': 1.0},
               skill=dict(number=9, name='Evil Spirit', damage=25, factor=1.36, fix=279, speed='magic')),
    'Elf': dict(name='Elf', kind='physical', level_points=5, weapon='bow', build={'A': 0.7, 'S': 0.3}, elf=True,
                skill=dict(number=235, name='Multi-Shot', damage=90, factor=0.46, fix=171, speed='attack')),
    'MG': dict(name='Magic Gladiator', kind='physical', level_points=7, weapon='mg_sword', build={'S': 0.75, 'E': 0.25},
               skill=dict(number=41, name='Twisting Slash', damage=110, factor=1.19, fix=329, speed='attack')),
    'MG-E': dict(name='Magic Gladiator (energy)', kind='magic', level_points=7, weapon='mg_staff', build={'E': 1.0},
                 skill=dict(number=9, name='Evil Spirit', damage=0, factor=0.91, fix=279, speed='magic')),
    'DL': dict(name='Dark Lord', kind='physical', level_points=7, weapon='scepter', build={'S': 0.6, 'E': 0.4},
               skill=dict(number=78, name='Fire Scream', damage=290, factor=0.65, fix=176, speed='attack')),
    'SUM': dict(name='Summoner', kind='magic', level_points=5, weapon='stick', build={'E': 1.0},
                skill=dict(number=230, name='Lightning Shock', damage=0, factor=1.30, fix=478, speed='magic')),
    'RF': dict(name='Rage Fighter', kind='physical', level_points=5, weapon='glove', build={'S': 0.6, 'E': 0.4},
               skill=dict(number=264, name='Dragon Roar', damage=955, factor=0.89, fix=412, speed='attack')),
}


def physical_damage(cls, stats):
    """Min and max damage from the stats (wiki Formulas, damage from stats)."""
    s, a, v, e = stats['S'], stats['A'], stats['V'], stats['E']
    return {
        'DK': (s / 6, s / 4),
        'Elf': (a / 7 + s / 14, a / 4 + s / 8),
        'MG': (s / 6 + e / 12, s / 4 + e / 8),
        'DL': (s / 7 + e / 14, s / 5 + e / 10),
        'RF': (s / 7 + v / 15, s / 5 + v / 12),
    }[cls]


def class_multiplier(cls, stats):
    """Class skill multiplier (wiki Formulas)."""
    e = stats['E']
    return {
        'DK': 1.25 + e / 1600,
        'DW': 1.3,
        'Elf': 2.2,
        'MG': 2.1,
        'MG-E': 2.1,
        'DL': 2.1 + e * 0.000525,
        'SUM': 2.8,
        'RF': 0.165 + e * 0.00033,
    }[cls]


# Hits per target and cast of a skill, if the database doesn't tell (or to override it): skill number -> hits
HITS_PER_TARGET = {}

# Distance of the targets for the hit chance per distance of the area skills (AreaSkillSettings)
TARGET_DISTANCE = 2


# ---------------------------------------------------------------------------------------------------------------------
# Data
# ---------------------------------------------------------------------------------------------------------------------

@dataclass
class Monster:
    name: str
    level: float
    hp: float
    defense: float
    defense_rate: float


MONSTER_SQL = f'''
SELECT md."Designation",
       MAX(CASE WHEN ma."AttributeDefinitionId" = '{STAT_LEVEL}' THEN ma."Value" END),
       MAX(CASE WHEN ma."AttributeDefinitionId" = '{STAT_MAXIMUM_HEALTH}' THEN ma."Value" END),
       COALESCE(MAX(CASE WHEN ma."AttributeDefinitionId" = '{STAT_DEFENSE_BASE}' THEN ma."Value" END), 0),
       COALESCE(MAX(CASE WHEN ma."AttributeDefinitionId" = '{STAT_DEFENSE_RATE_PVM}' THEN ma."Value" END), 0)
FROM config."MonsterDefinition" md
JOIN config."MonsterAttribute" ma ON ma."MonsterDefinitionId" = md."Id"
GROUP BY md."Id", md."Designation"
HAVING MAX(CASE WHEN ma."AttributeDefinitionId" = '{STAT_MAXIMUM_HEALTH}' THEN ma."Value" END) > 0
'''

SKILL_SQL = '''
SELECT s."Number", s."NumberOfHitsPerAttack",
       a."MinimumNumberOfHitsPerTarget", a."MaximumNumberOfHitsPerTarget", a."HitChancePerDistanceMultiplier",
       a."MaximumNumberOfHitsPerAttack", a."ProjectileCount", a."UseFrustumFilter", s."AttackDamage"
FROM config."Skill" s
LEFT JOIN config."AreaSkillSettings" a ON a."Id" = s."AreaSkillSettingsId"
'''


def query(args, sql):
    """Rows of a query against the live database: psycopg with --dsn, else psql in the docker container."""
    if args.dsn:
        try:
            import psycopg  # type: ignore
        except ImportError:
            import psycopg2 as psycopg  # type: ignore
        with psycopg.connect(args.dsn) as connection:
            with connection.cursor() as cursor:
                cursor.execute(sql)
                return [list(row) for row in cursor.fetchall()]

    if shutil.which('docker') is None:
        sys.exit('No database: install docker (container "database") or pass --dsn or --monsters-csv.')
    command = ['docker', 'exec', args.container, 'psql', '-U', args.db_user, '-d', args.db_name, '-At', '-F', '\t', '-c', sql]
    output = subprocess.run(command, check=True, capture_output=True, text=True).stdout
    return [line.split('\t') for line in output.splitlines() if line]


def load_monsters(args):
    if args.monsters_csv:
        with open(args.monsters_csv, newline='', encoding='utf-8') as file:
            return {row['name']: Monster(row['name'], float(row['level']), float(row['hp']), float(row['defense']),
                                         float(row['defense_rate'])) for row in csv.DictReader(file)}

    monsters = {}
    for name, level, hp, defense, defense_rate in query(args, MONSTER_SQL):
        if level in (None, ''):
            continue
        monster = Monster(name, float(level), float(hp), float(defense), float(defense_rate))
        # several definitions with one name (e.g. event copies): keep the one with the most health
        if name not in monsters or monster.hp > monsters[name].hp:
            monsters[name] = monster
    return monsters


def load_skill_hits(args):
    """Expected hits per target and cast of every skill (number of hits per attack x area settings)."""
    if args.monsters_csv:
        return {}

    def number(value, default=0.0):
        return default if value in (None, '') else float(value)

    hits = {}
    for row in query(args, SKILL_SQL):
        skill, per_attack, minimum, maximum, chance, max_per_attack, projectiles, frustum, _ = row
        per_target = 1.0
        if maximum not in (None, '') and not (str(frustum).lower() in ('t', 'true') and number(projectiles) > 1):
            minimum, maximum, chance = number(minimum), number(maximum, 1), number(chance, 1)
            per_target = minimum + max(0.0, maximum - minimum) * chance ** TARGET_DISTANCE
            if number(max_per_attack) > 0:
                per_target = min(per_target, number(max_per_attack) / max(1, args.pack))
        hits[int(skill)] = max(1.0, number(per_attack, 1)) * max(per_target, 0.0)
    return hits


def load_steps(path, monsters):
    """Reset step -> monsters of the step (names or a level range)."""
    with open(path, encoding='utf-8') as file:
        data = json.load(file)

    steps = {}
    for key, value in data.items():
        if key.startswith('_'):
            continue
        step = int(key)
        if isinstance(value, dict) and 'levels' in value:
            low, high = value['levels']
            chosen = [m for m in monsters.values() if low <= m.level <= high]
        else:
            names = value['monsters'] if isinstance(value, dict) else value
            missing = [n for n in names if n not in monsters]
            if missing:
                print(f'Step {step}: unknown monsters {", ".join(missing)}', file=sys.stderr)
            chosen = [monsters[n] for n in names if n in monsters]
        if not chosen:
            print(f'Step {step}: no monsters', file=sys.stderr)
            continue
        steps[step] = chosen
    return dict(sorted(steps.items()))


# ---------------------------------------------------------------------------------------------------------------------
# Model
# ---------------------------------------------------------------------------------------------------------------------

def reset_points(resets):
    return 550 * resets + sum(1000 * (milestone // 10) for milestone in (10, 20, 30, 40, 50) if milestone <= resets)


def interpolate(table, x):
    keys = sorted(table)
    if x <= keys[0]:
        return table[keys[0]]
    for low, high in zip(keys, keys[1:]):
        if x <= high:
            return table[low] + (table[high] - table[low]) * (x - low) / (high - low)
    return table[keys[-1]] * x / keys[-1]


def rank_of(resets):
    return max(i for i, first in enumerate(RANK_FROM_RESETS) if first <= resets)


def stats_of(cls, resets, args):
    config = CLASSES[cls]
    points = reset_points(resets) + (args.level - 10) * config['level_points'] + args.base_points
    agility = interpolate(AGILITY_LADDER, resets)
    vitality = points * args.vitality
    rest = max(0.0, points - agility - vitality)
    stats = {'S': 0.0, 'A': agility, 'V': vitality, 'E': 0.0}
    for stat, share in config['build'].items():
        stats[stat] += rest * share
    return stats


def enchant_factor(rank, enchant):
    return ENCHANT_PERCENT[min(enchant, 15)] / 100 * (1 + 0.03 * rank)


def hit_range(cls, stats, rank, args):
    """Min and max of one hit before the defense of the target, and the factor after it."""
    config = CLASSES[cls]
    skill = config['skill']
    weapon = WEAPONS[config['weapon']][rank]
    if config['kind'] == 'magic':
        rise = weapon / 100 * (1 + enchant_factor(rank, args.enchant))
        low = (stats['E'] / 9 + skill['damage']) * (1 + rise)
        high = (stats['E'] / 4 + skill['damage'] * 1.5) * (1 + rise)
    else:
        stat_low, stat_high = physical_damage(cls, stats)
        bonus = 3 * args.enchant if rank == 0 else None
        weapon_low = weapon[0] + (bonus if bonus is not None else weapon[0] * enchant_factor(rank, args.enchant))
        weapon_high = weapon[1] + (bonus if bonus is not None else weapon[1] * enchant_factor(rank, args.enchant))
        low = stat_low + weapon_low + skill['damage']
        high = stat_high + weapon_high + skill['damage'] * 1.5
    return low, high, class_multiplier(cls, stats) * skill['factor']


def average_damage(low, high, defense, factor, level, penalty):
    """Average damage of one landed hit: uniform roll, minus defense, at least level / 10."""
    samples = 41
    total = 0.0
    for i in range(samples):
        roll = low + (high - low) * i / (samples - 1)
        total += max(level / 10, (roll - defense) * penalty)
    return total / samples * factor


def cast_seconds(cls, stats, args):
    config = CLASSES[cls]
    skill = config['skill']
    elf = config.get('elf', False)
    if skill['speed'] == 'magic':
        speed = stats['A'] * (0.015 if elf else 0.025) + WEAPON_SPEED
        offset, at_fix = MAGIC_SPEED_CURVE_OFFSET, MAGIC_SPEED_AT_FIX
    else:
        speed = stats['A'] * (0.018 if elf else 0.03) + WEAPON_SPEED
        offset, at_fix = ATTACK_SPEED_CURVE_OFFSET, ATTACK_SPEED_AT_FIX
    fix = skill['fix'] / 1000
    animation = fix * (offset + at_fix) / (offset + speed)
    return max(animation, fix * (1 - args.cut), MINIMUM_CAST_SECONDS)


@dataclass
class Result:
    seconds: float
    casts: float
    cast: float
    hit_chance: float
    damage: float


def pack_time(cls, resets, monster, hits_per_target, args):
    config = CLASSES[cls]
    stats = stats_of(cls, resets, args)
    rank = rank_of(resets)
    accuracy = 5 * args.level + stats['A'] * (1.0 if config.get('elf') else 4 / 3) + WEAPON_ACCURACY[rank]
    penalty = 1.0
    if monster.defense_rate > accuracy:
        hit_chance, penalty = 0.05, 0.3
    else:
        hit_chance = max(0.05, 1 - monster.defense_rate / max(accuracy, 1))

    low, high, factor = hit_range(cls, stats, rank, args)
    damage = average_damage(low, high, monster.defense, factor * (1 + RESET_DAMAGE_BOOST * resets), args.level, penalty)
    per_cast = damage * hit_chance * hits_per_target
    casts = math.ceil(monster.hp / per_cast) if per_cast > 0 else math.inf
    cast = cast_seconds(cls, stats, args)
    return Result(casts * cast + args.overhead, casts, cast, hit_chance, damage)


# ---------------------------------------------------------------------------------------------------------------------
# Report
# ---------------------------------------------------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--steps', default=str(Path(__file__).with_name('pack_kill_steps.json')),
                        help='JSON: reset step -> monster names or {"levels": [min, max]}')
    parser.add_argument('--monsters-csv', help='monsters without a database: name,level,hp,defense,defense_rate')
    parser.add_argument('--dsn', help='PostgreSQL connection string for psycopg (else docker exec)')
    parser.add_argument('--container', default='database')
    parser.add_argument('--db-user', default='postgres')
    parser.add_argument('--db-name', default='openmu')
    parser.add_argument('--class', dest='classes', action='append', choices=list(CLASSES), help='only these classes')
    parser.add_argument('--level', type=int, default=250, help='character level (default 250, as the wiki hit chances)')
    parser.add_argument('--base-points', type=int, default=100, help='stat points of a new character')
    parser.add_argument('--vitality', type=float, default=0.2, help='share of the points in Vitality')
    parser.add_argument('--enchant', type=int, default=9, help='enchant level of the weapon')
    parser.add_argument('--cut', type=float, default=0.0, help='harmony cut of the fix time (0.25 = -25 %%)')
    parser.add_argument('--pack', type=int, default=5, help='monsters in a pack')
    parser.add_argument('--overhead', type=float, default=1.5, help='seconds per pack to reach it')
    parser.add_argument('--details', action='store_true', help='damage, hit chance and cast time per class and step')
    args = parser.parse_args()

    if not Path(args.steps).exists():
        example = Path(__file__).with_name('pack_kill_steps.example.json')
        sys.exit(f'No {args.steps}: copy {example.name} to {Path(args.steps).name} and fill in the monsters of the steps.')

    monsters = load_monsters(args)
    hits = load_skill_hits(args)
    hits.update(HITS_PER_TARGET)
    steps = load_steps(args.steps, monsters)
    classes = args.classes or list(CLASSES)

    print(f'Pack of {args.pack}, level {args.level}, weapon +{args.enchant}, cut {args.cut:.0%}, '
          f'{args.overhead:g} s to reach a pack. Seconds per pack (relative to the mean of the step):')
    header = f'{"resets":>6} ' + ''.join(f'{cls:>14}' for cls in classes)
    print(header)
    for resets, chosen in steps.items():
        cells = []
        for cls in classes:
            skill = CLASSES[cls]['skill']['number']
            results = [pack_time(cls, resets, m, hits.get(skill, 1.0), args) for m in chosen]
            cells.append(sum(r.seconds for r in results) / len(results))
        mean = sum(cells) / len(cells)
        print(f'{resets:>6} ' + ''.join(f'{t:>8.1f} ({t / mean:4.2f})' for t in cells))

    if args.details:
        for resets, chosen in steps.items():
            names = ', '.join(sorted({m.name for m in chosen}))
            print(f'\n{resets} resets - {names}')
            for cls in classes:
                skill = CLASSES[cls]['skill']
                stats = stats_of(cls, resets, args)
                results = [pack_time(cls, resets, m, hits.get(skill['number'], 1.0), args) for m in chosen]
                count = len(results)
                print(f'  {cls:5} {skill["name"]:16} S {stats["S"]:6.0f} A {stats["A"]:6.0f} E {stats["E"]:6.0f}'
                      f' | hit {sum(r.damage for r in results) / count:8.0f} x {hits.get(skill["number"], 1.0):.2f}'
                      f' | chance {sum(r.hit_chance for r in results) / count:4.0%}'
                      f' | cast {sum(r.cast for r in results) / count:4.2f} s'
                      f' | casts {sum(r.casts for r in results) / count:5.1f}'
                      f' | {sum(r.seconds for r in results) / count:6.1f} s')


if __name__ == '__main__':
    main()
