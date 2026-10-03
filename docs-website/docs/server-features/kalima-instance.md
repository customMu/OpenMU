---
title: Kalima instance and Symbols of Kundun
sidebar_label: Kalima instance
sidebar_position: 2
description: Kalima 1-7 as a daily instance for a party or a single player, and the Symbols of Kundun as a currency.
---

# Kalima instance and Symbols of Kundun

Kalima 1-7 are no regular maps anymore. They are a **daily instance** for a party
or a single player, entered by talking to the gatekeeper in Lorencia. The
Symbols of Kundun are a **currency** which is spent in the symbol shop.

## For players

- Talk to **Lugard** in Lorencia to enter Kalima. Without a party you get your own
  instance, with a party everyone enters the instance of the party.
- The Kalima level depends on the resets. A new instance is opened for the
  highest level which **every online party member** reached:

  | Kalima | 1 | 2 | 3 | 4 | 5 | 6 | 7 |
  |---|---|---|---|---|---|---|---|
  | Resets | 5 | 10 | 15 | 22 | 30 | 40 | 50 |

- **One entry per day.** The entries are reset every day at 06:00 (server time,
  or the configured time zone). Going back into the own instance, e.g. after a
  death, is free as long as it's running.
- An instance runs for **40 minutes**. Notices are shown 30, 10, 5 and 1 minute
  before it closes. After a death you respawn in Lorencia and can talk to Lugard
  again to go back; the killed packs stay killed. An instance without players
  is closed after 5 minutes.
- **Symbols of Kundun** only come from the Kalima instance and go into a counter
  of the character (no stack limit), so they can't be traded or sold.
  `/symbols` shows the balance; the client shows it when the mouse is over the
  zen in the inventory.
- **Delgado** in Lorencia sells for symbols instead of zen.
- The Lost Map doesn't open a gate anymore, and symbols aren't combined to a
  Lost Map.

## The run: packs and the boss

There is no respawn. The monsters come in **10 packs of 8** along the way from
the entrance to the Illusion of Kundun (the spawn points of the regular Kalima
map, ordered by their walking distance from the entrance). The next pack
appears when the previous one is killed, the **Illusion of Kundun** after the
last pack.

The difficulty is fixed - it doesn't depend on the number of players. It's made
for a party of 3-4 well equipped players: about the monsters of the reset ladder
5 resets above the tier, with ten times their health. A single player can
survive, but hardly reaches the boss in time.

| Kalima | Resets | Monster level | Health | Damage | Defense | Boss health |
|---|---|---|---|---|---|---|
| 1 | 5 | 100 | 400 000 | 1 200 | 450 | 20 M |
| 2 | 10 | 110 | 700 000 | 1 500 | 500 | 35 M |
| 3 | 15 | 117 | 900 000 | 1 650 | 620 | 45 M |
| 4 | 22 | 122 | 1 000 000 | 1 800 | 680 | 50 M |
| 5 | 30 | 130 | 1 000 000 | 2 800 | 650 | 50 M |
| 6 | 40 | 140 | 2 400 000 | 3 100 | 820 | 120 M |
| 7 | 50 | 145 | 2 600 000 | 3 300 | 900 | 130 M |

The values are the averages of the regular monsters; the differences between
the monster types of the map stay. The boss has 50 times the health of a
regular monster. The level counts for the experience and the attack and
defense rates. The damage of Kalima 5-7 is lower than double of the reference
maps, because the monsters of Raklion already hit very hard; the defense is not
multiplied, because it's subtracted from the damage and would make weaker
players useless.

The experience is multiplied by 20, money drops like on a regular map, and the
penalty by resets doesn't apply in the instance.

## Symbols per run

- For every killed monster, each living player within 15 tiles rolls **5 %** for
  **1 × Kalima level** symbols.
- The Illusion of Kundun gives **5 × Kalima level** symbols to everyone in the
  instance (the daily boss reward).

| Kalima | 1 | 4 | 7 |
|---|---|---|---|
| Expected from the 80 monsters | ~4 | ~16 | ~28 |
| Boss | 5 | 20 | 35 |
| Per day | ~9 | ~36 | ~63 |

The total health of a run shows the damage which is needed: at Kalima 1 about
52 M (80 × 400 000 + 20 M), at Kalima 7 about 340 M. For 30 minutes of fighting
this is about 29 000 damage per second for the party at Kalima 1, and about
190 000 at Kalima 7. The values of the tiers are a starting point and should be
adjusted to the real damage of the characters of the server.

## For admins

1. Apply the data update **"Add Kalima instance"** (Admin panel → Updates). It
   adds the mini game definitions "Kalima Instance 1-7", the attributes of the
   daily entries and of the symbol balance, Lugard and Delgado in Lorencia,
   the merchant store of Delgado, removes the warps into Kalima from the
   warp list and the regular drops of the symbols. The update "Symbols of Kundun
   only in Kalima" removes the regular symbol drops for a server which already
   applied the first one.
2. The plugins **"Kalima instance"** and **"Symbols of Kundun currency"** are
   active by default. Their configuration (Admin panel → Plugins):
   - Kalima instance: gatekeeper npc, entries per day, daily reset hour, time
     zone, duration, packs, monsters per pack, experience multiplier, symbol
     chance and amounts, boss health factor, tiers (minimum resets, monster
     level, health, damage, defense, drop multiplier per Kalima level).
   - Symbols of Kundun: symbol shop npc and the prices (by item group, number and
     level). The items of the shop are edited at the merchant store of the npc.
3. The playing time is the duration of the plugin configuration; the game
   duration of the mini game definitions has to be longer.

The game client of this server shows the symbol balance and the symbol prices
of the shop with two custom packets (`C1 FB 01` balance, `C2 FB 02` shop
prices); other clients only see the chat messages.
