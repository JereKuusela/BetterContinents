- v0.7.18
  - Adds a new command "bc g v" which allows setting the save version.
  - Adds improved autocomplete when Server Devcommands mod is installed.
  - Fixes the new setting "Biome precision" not doing anything.
  - Fixes console output colors.
  - Fixes minimap not being cached.
  - Fixes main menu terrain getting messed up after exiting a world.

- v0.7.17
  - Fixes noise layer not being applied without a heightmap.

- v0.7.16
  - Adds new setting "Biome precision" to control how precisely the biome map is used.
  - Changes file format version to 9.
  - Fixes "Forest map add" sharing the same debug command as "Forest map multiply".
  - Fixes some Configuration Managers not working with Better Continents.
  - Fixed for the new game version.
  - Improves performance.
  - Removes settings "Multithreaded Heightmap Build" and "Parallel Chunks Build" as unused.

- v0.7.15
  - Fixes the log always showing the original amount of locations even when changed with Expand World Size.
  - Fixes the debug menu missing Continents Size setting.

- v0.7.14
  - Improves compatibility with mods that affect the minimap.
  - Fixes the world edge not working with Expand World Size.
  - Fixes float values in console commands using system locale (now always uses `.`).

- v0.7.13
  - Fixes preset worlds not working.

- v0.7.12
  - Adds support for remapping the biome and location colors.
  - Renames spawnmap to locationmap (old file is renamed automatically).
  - Changes file format version to 8.
  - Fixes server usage clearing file paths from the save file.
  - Removes heightmap being required. If missing, default height generation is used.

- v0.7.11
  - Fixed for the new patch.

- v0.7.10
  - Adds a new biome color #000000 for None. When None is returned, BC uses the original biome.
  - Fixes 16 bit heightmaps not working.

- v0.7.9
  - Fixes world modifier menu disabling Better Continents.

- v0.7.8
  - Adds color codes for Mistlands_DvergrBossEntrance1 (#9900ff) and Hildir_camp (#FF69B4).
  - Adds a new setting for the Upgrade World reset command.
  - Changes refresh command to reset command.
  - Fixes error and terrain issues when reloading images.
  - Removes the 4k image size limit.
  - Removes force parameter from the default Upgrade World command.
