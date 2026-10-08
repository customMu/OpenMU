# Balance tools in the repository

The balance generators (`mob_layout.py`, `weapon_ranks.py`, `skill_cast_time.py` ...) live on the PC in
`mu\tools\balance` (not in git). This folder holds the tools which only read the live database.

## pack_kill_time.py

Time to kill a pack of monsters for every class at every reset step - a model of the formulas of the server and the
wiki (stats, weapon of the rank, skill damage and factor, class multiplier, defense, hit chance, the cast time curve of
the plugin "Skill cast time").

1. Copy `pack_kill_steps.example.json` to `pack_kill_steps.json` and put in the spot monsters of every reset step
   (names from the database, or a level range) - the example ranges are guesses.
2. With the database container running: `python tools/balance/pack_kill_time.py` (`--details` for the damage, hit
   chance and cast time of every class, `--help` for the build, level, enchant, pack size ...).

The classes, builds and skills are in the tables at the top of the script (from the wiki); keep them in sync after a
balance change. The in-game check of single numbers is the GM command `/chance` (Shift + right click on a monster).
