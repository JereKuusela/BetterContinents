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
