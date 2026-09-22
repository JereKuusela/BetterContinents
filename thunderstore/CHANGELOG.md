- v0.8.0
  - Updates the mod for Valheim 1.0.15.
  - Saves world settings in the new 1.0 chunked save format, inside the world's own folder.
  - Adds an upgrade path: a pre-1.0 world's settings are imported and moved into the world folder
    on its first save, and the old file is kept as "<world>.BetterContinents.pre10".
  - Carries the settings through Valheim's save operations (rename, copy, move), which filter by
    file extension and would otherwise drop or delete them.
  - Fixes the minimap drawing Swamp, Mountain, Plains and Mistlands as Black Forest, and restores
    the Ashlands and Mistlands map shading.
  - Fixes world generation hanging forever when the biome map omits a biome.

- v0.7.31
  - Fixes wrong default name for moss map image file (was mossmmap.png, should be mossmap.png).
  - Fixes manually reloading heat map using forest map file path.
  - Fixes flat map being disabled when rough map blend was zero (should be only disabled when flat map blend is zero).
  - Fixes None biome on biome maps not using the default biome generation.
  - Fixes location pin removing possibly removing a random pin from the map.
  - Minor optimizations.

- v0.7.30
  - Adds spawn map.
  - Adds vegetation map.
  - Changes the "Skip default location placement" config setting to be saved on the map settings.
  - Improves config settings layout.
  - Minor optimizations.
  - Fixes index out of range error when going out of map bounds.
  - Fixes "Disable map drop off" not working.

- v0.7.29
  - Fixes some worlds not working.

- v0.7.28
  - Adds fallback handling for lava when heat map is missing.

- v0.7.27
  - Fixed for the new game version.
