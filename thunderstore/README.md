# Better Continents

Mod originally created by billw2012.

- [Full Documentation](https://jerekuusela.github.io/BetterContinents-Docs/introduction.html)
- [Installation Guide](https://jerekuusela.github.io/BetterContinents-Docs/setup-guide.html)
- [Discord](https://discord.gg/3XW8ZntYzN)

## Introduction

This mod provides the tools to both improve the standard terrain generation of the game, and override aspects of it with image maps, allowing precise control over height, biomes, location placement, and forest coverage.  
These generated worlds can be shared with others (who have this mod installed) by simply copying the usual world files, along with the extra `.BetterContinents` file.  

> See maps people have shared [here](https://www.nexusmods.com/valheim/mods/categories/13/).
> Check the links above for documentation and installation instructions.

### Recommended mods

- [Server Devcommands](https://valheim.thunderstore.io/package/JereKuusela/Server_devcommands/)
  - Edit the configuration to automatically enable flying or other features.
  - Better Continents enables devcommands for debug worlds, so that feature can be disabled from Server Devcommands.
- [Upgrade World](https://valheim.thunderstore.io/package/JereKuusela/Upgrade_World/)
  - This is needed to reset the world when reloading images or changing settings.
  - Better Continents calls the `zones_reset` command automatically when needed.
  - Other commands can be used for more specific resets.
  - Adds `location_register` command to make a location to spawn at the specific coordinates.

## Features

- Use image files as [base heightmap](https://jerekuusela.github.io/BetterContinents-Docs/settings/heightmap.html#heightmap-file) layer, [detail heightmap](https://jerekuusela.github.io/BetterContinents-Docs/settings/flatmap.html#flatmap-file) layer, and [biome specific heightmap](https://jerekuusela.github.io/BetterContinents-Docs/settings/roughmap.html#roughmap-file) layer, with blending options for each
- A comprehensive layer based noise system based on [FastNoiseLite](https://github.com/Auburn/FastNoise), including support for any number of layers of noise with separate settings, each with its own distortion and mask
- A preset system with a few inbuilt defaults, allowing easy selection and sharing of world configs (anyone can make and share their own presets)
- An in game UI for designing your world, with visualisation for the layer system, and fast updating of the terrain and map
- An extensive console command system matching the UI, for those who prefer it
- Use an [image file to specify biomes](https://jerekuusela.github.io/BetterContinents-Docs/settings/biomemap.html#biomemap-file)
- Use an [image file to specify spawning positions](https://jerekuusela.github.io/BetterContinents-Docs/settings/spawnmap.html#spawnmap-file) for locations, including player start, bosses, and trader
- Use an [image file to specify forest coverage](https://jerekuusela.github.io/BetterContinents-Docs/settings/forest.html#forestmap-file), including in [biomes that normally are totally forested](https://jerekuusela.github.io/BetterContinents-Docs/settings/forest.html#forest-factor-overrides-all-trees) (Swamp, Mistlands, Dark Forest, Mountains)
- Change [global scale](https://jerekuusela.github.io/BetterContinents-Docs/settings/global.html#continent-size) – changes continent sizes
- Adjust [sea level](https://jerekuusela.github.io/BetterContinents-Docs/settings/global.html#sea-level-adjustment)
- Adjust [forest scaling](https://jerekuusela.github.io/BetterContinents-Docs/settings/forest.html#forest-scale) (how big the contiguous areas of forest/clearings are), and amount
- Specify [starting position](https://jerekuusela.github.io/BetterContinents-Docs/settings/start-position.html) explicitly
- Still allows loading vanilla maps, without needing to adjust any settings or disable the mod
- Export full map to png at any resolution
