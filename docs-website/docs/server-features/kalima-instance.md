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
- An instance runs for **60 minutes**. Notices are shown 30, 10, 5 and 1 minute
  before it closes. After a death you respawn in Lorencia and can talk to Lugard
  again to go back.
- The monsters are much stronger than on a regular map, and they get stronger
  with each player in the instance - but the drop grows faster than the monsters.
- **Symbols of Kundun** aren't items anymore. A picked up symbol goes into a
  counter of the character (no stack limit), so it can't be traded or sold.
  `/symbols` shows the balance; the client shows it when the mouse is over the
  zen in the inventory.
- **Delgado** in Lorencia sells for symbols instead of zen.
- The Lost Map doesn't open a gate anymore, and symbols aren't combined to a
  Lost Map.

## Scaling by players

For each player in the instance (up to a full party) the monsters and the drop
are multiplied with:

```
factor = 1 + (per player - 1) * (players - 1)
```

| Per player (default) | 1 player | 2 players | 3 players | 5 players |
|---|---|---|---|---|
| Health ×2.0 | ×1 | ×2 | ×3 | ×5 |
| Defense ×2.0 | ×1 | ×2 | ×3 | ×5 |
| Damage ×1.6 | ×1 | ×1.6 | ×2.2 | ×3.4 |
| Drop ×2.1 | ×1 | ×2.1 | ×3.2 | ×5.4 |

The factors are multiplied with the base multipliers of the Kalima level (the
monsters of Kalima 1 are made for characters without resets, so they get the
highest base multipliers). The number of monsters stays the same.

A drop multiplier of 2.5 means two full drop rolls and a 50 % chance for a third
one; every roll includes the regular drop groups and the additional drops
(jewels, items by rank). Money drops like on a regular map. The penalty by
resets doesn't apply in the instance.

When a player enters or leaves, the multipliers are updated at once; the health
of the living monsters is adapted, so their health bar keeps its percentage.

## For admins

1. Apply the data update **"Add Kalima instance"** (Admin panel → Updates). It
   adds the mini game definitions "Kalima Instance 1-7", the attributes of the
   daily entries and of the symbol balance, Lugard and Delgado in Lorencia,
   the merchant store of Delgado, and removes the warps into Kalima from the
   warp list.
2. The plugins **"Kalima instance"** and **"Symbols of Kundun currency"** are
   active by default. Their configuration (Admin panel → Plugins):
   - Kalima instance: gatekeeper npc, entries per day, daily reset hour, time
     zone, multipliers per player, tiers (minimum resets and base multipliers per
     Kalima level).
   - Symbols of Kundun: symbol shop npc and the prices (by item group, number and
     level). The items of the shop are edited at the merchant store of the npc.
3. The playing time is the game duration of the mini game definition plus one
   minute (the entrance phase).

The game client of this server shows the symbol balance and the symbol prices
of the shop with two custom packets (`C1 FB 01` balance, `C2 FB 02` shop
prices); other clients only see the chat messages.
