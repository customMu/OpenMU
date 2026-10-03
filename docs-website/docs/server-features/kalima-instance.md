---
title: Kalima instance, chamber of Kundun and Kundun Essence
sidebar_label: Kalima instance
sidebar_position: 2
description: Kalima 1-7 as a daily instance, the weekly chamber of Kundun, the Symbols of Kundun, the Kundun Essence and the drop modes of a party.
---

# Kalima instance, chamber of Kundun and Kundun Essence

Kalima 1-7 are no regular maps anymore. They are a **daily instance** for a party
or a single player, entered by talking to the gatekeeper in Lorencia. Once per
week, the **chamber of Kundun** on the same maps is the final fight; its entry fee
is a **Lost Map**. The currency of the instance is the **Kundun Essence**, which is
spent in the essence shop.

## For players

### Reset ranges

Every Kalima level (and chamber level) is for a **strict range of resets**. A
party can only enter, when every member is in the range of the same level;
otherwise the entry is refused with a message which names the members who don't
fit.

| Kalima | 1 | 2 | 3 | 4 | 5 | 6 | 7 |
|---|---|---|---|---|---|---|---|
| Resets | 5-9 | 10-14 | 15-21 | 22-29 | 30-37 | 38-44 | 45+ |

### Kalima instance (daily)

- Talk to **Lugard** in Lorencia to enter Kalima. Without a party you get your own
  instance, with a party everyone enters the instance of the party.
- **One entry per day.** The entries are reset every day at 06:00 (server time,
  or the configured time zone). Going back into the own instance, e.g. after a
  death, is free as long as it's running.
- An instance runs for **40 minutes**. Notices are shown 30, 10, 5 and 1 minute
  before it closes. An instance without players is closed after 5 minutes.
- **10 packs of 8 monsters** come along the way to the **Illusion of Kundun**.
- Rewards:
  - **Kundun Essence:** for every killed monster, each living player within 15
    tiles rolls 5 % for 1 × Kalima level; the Illusion gives 5 × Kalima level
    to everyone in the instance.
  - **Symbol of Kundun +N** (only in Kalima N): a regular monster drops one with
    1 %, the Illusion drops one per player in the instance.
  - **Lost Map +N:** the Illusion drops one with 3 %.

### Symbols of Kundun and Lost Map

- The symbols are items again: 5 symbols of the same level are combined to a
  **Lost Map** of this level.
- The Lost Map is the **entry fee of the chamber of Kundun** of its level. It can't
  be dropped on the ground and doesn't open a gate anymore.
- Dropped and bought lost maps can be traded and sold to other players. Lost maps
  of a donation are **bound** to the character (no trade, store, vault or npc sale).

### Kundun Essence and the essence shop

- The Kundun Essence is a counter of the character (no stack limit), so it can't
  be traded. The balances of the former symbol currency were taken over 1:1.
- `/essence` shows the balance; the client of this server shows it in a line
  below the zen in the inventory.
- **Delgado** in Lorencia sells for essence instead of zen, e.g. jewels, the boxes
  of Kundun and the **Lost Map +N for 25 × N** essence.

### Chamber of Kundun (weekly)

- Talk to **David** in Lorencia. Every character pays the entry with a **Lost Map +N**
  of the level of its reset range.
- **One entry per week**, two with an active chamber pass (donation). The week starts
  on monday at 06:00. Going back into the own chamber after a death is free.
- The chamber runs for **30 minutes**, without an enrage timer.
- **Kundun** appears right away. He has 3 times the health of the Illusion of
  Kundun of the same level and 150 % of its damage.
- The fight has **3 phases**. At 75 %, 50 % and 25 % health Kundun stops (the damage
  below the threshold is cut), calls Illusions of Kundun and is invulnerable until
  they are destroyed:

  | Phase | Illusions | Illusion health | Illusion damage | After the phase (per second after 20 free seconds) |
  |---|---|---|---|---|
  | 75 % | 1 | 0.5 × Illusion | 100 % | heals 0.10 % (max. 15 %) |
  | 50 % | 2 | 0.4 × Illusion | 110 % | heals 0.12 % (max. 20 %), defense +0.2 % (max. +40 %) |
  | 25 % | 3 | 0.35 × Illusion | 120 % | heals 0.15 % (max. 25 %), damage +0.3 % (max. +50 %) |

  Each phase happens once: when Kundun heals above a threshold again, its phase
  doesn't repeat. The defense and damage bonuses stay until the end of the fight.
- Reward: **30 × N** Kundun Essence for everyone in the chamber and a **Box of Kundun
  +min(N, 5)** per player, distributed by the drop mode of the party.
- The kill time (from the appearance of Kundun) is saved as **ranking** for the website.

### Drop modes of a party

The party master chooses who may pick up the items which drop for kills of the party
(button in the party window of the game client of this server):

| Mode | Owner of an item |
|---|---|
| Free | every member (who comes first) |
| Random | a random member near the drop |
| In turn | the members near the drop in turn (in the order of the party list) |

An item which was assigned to a member belongs to it for 60 seconds, then everyone
can pick it up.

## The strength of the tiers

The difficulty is fixed - it doesn't depend on the number of players. It's made
for a party of 3-4 well equipped players. The strength of each tier comes from a
**reference map** of the reset ladder, as it's stored in the database (so changed
monster levels and health are taken over): the median of its regular monsters
times the factors of the tier.

| Kalima | Resets | Reference map | Level | Health | Damage | Defense |
|---|---|---|---|---|---|---|
| 1 | 5-9 | Kanturu Ruins (37) | ×1.1 | ×10 | ×2 | ×1.1 |
| 2 | 10-14 | Aida (33) | ×1.1 | ×10 | ×2 | ×1.1 |
| 3 | 15-21 | Karutan 2 (81) | ×1.1 | ×10 | ×2 | ×1.1 |
| 4 | 22-29 | Karutan 2 (81) | ×1.2 | ×10 | ×2.2 | ×1.1 |
| 5 | 30-37 | Raklion (57) | ×1.1 | ×10 | ×1.3 | ×1.1 |
| 6 | 38-44 | Raklion (57) | ×1.2 | ×10 | ×1.4 | ×1.1 |
| 7 | 45+ | Kanturu Relics (38) | ×1.2 | ×10 | ×2 | ×1.1 |

Each tier is at least 5 % stronger than the previous one, and the monster level
is limited to 400. The Illusion of Kundun has 50 times the health of a regular
monster. The experience is multiplied by 20, money drops like on a regular map,
and the penalty by resets doesn't apply in the instance.

## For admins

1. Apply the data updates (Admin panel → Updates):
   - **"Add Kalima instance"**: the mini games "Kalima Instance 1-7", the attributes
     of the daily entries and of the currency, Lugard and Delgado in Lorencia, the
     merchant store of Delgado; removes the warps into Kalima.
   - **"Symbols of Kundun only in Kalima"**: removes the regular symbol drops.
   - **"Add chamber of Kundun"**: the monsters Kundun 1-7 (numbers 700-706, copies of
     the Illusion of their Kalima map), the mini games "Chamber of Kundun 1-7",
     David in Lorencia, the attributes of the weekly entries and of the chamber pass,
     renames the currency to Kundun Essence, adds the lost maps to the essence shop
     and sets the reset ranges 38-44 and 45+ of the tiers 6 and 7.
2. The plugins **"Kalima instance"**, **"Chamber of Kundun"** and **"Kundun Essence
   currency"** are active by default. Their configuration (Admin panel → Plugins):
   - Kalima instance: gatekeeper, daily entries and reset, duration, packs,
     experience multiplier, essence chance and amounts, symbol and lost map drops,
     boss health factor, tiers (minimum resets, reference map, factors).
   - Chamber of Kundun: keeper, weekly entries (with and without a pass), weekly reset
     day and hour, duration, Kundun monster numbers, health and damage factors,
     free seconds and phases, rewards.
   - Kundun Essence currency: essence shop npc, prices, binding of bought lost maps.
3. A **chamber pass** is the character attribute "Kundun chamber pass until day":
   the last day on which the pass is active, as number of days since 2000-01-01 (UTC).
   The website can set it.
4. The **ranking** of the chamber is stored in the mini game ranking
   (`MiniGameRankingEntry`): one entry per player of a kill with the same game
   instance id, the kill time in seconds as score and the chamber level as game
   level of the mini game definition.

The game client of this server uses custom packets with the head code `FB`:
`C1 FB 01` essence balance, `C2 FB 02` prices of the essence shop and `C1 FB 03`
drop mode of the party (also sent by the client to change it). Other clients only
see the chat messages.
