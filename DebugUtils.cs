using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using static BetterContinents.BetterContinents;
#nullable disable
namespace BetterContinents;

[HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
public class PlayerPatch
{
    private static void Postfix()
    {
        if (BetterContinents.AllowDebugActions)
        {
            if (!Terminal.m_cheat)
                Console.instance.TryRunCommand("devcommands");
        }
    }
}
[HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
public partial class DebugUtils
{
    static void Postfix()
    {
        new DebugUtils();
    }
    private static readonly string[] Bosses =
    [
            "StartTemple", "Eikthyrnir", "GDKing", "GoblinKing", "Bonemass", "Dragonqueen",
            "Vendor_BlackForest", "Mistlands_DvergrBossEntrance1"
        ];

    static DebugUtils()
    {
        new Terminal.ConsoleCommand("bc", "Root Better Continents command", args => RunConsoleCommand(args.FullLine.Trim()), true, false, true);
        rootCommand = new Command("bc", "Better Continents", "Better Continents command").Subcommands(bc =>
        {
            bc.AddCommand("info", "Dump Info", "Prints current settings to console", _ =>
            {
                BetterContinents.Settings.Dump(str =>
                    Console.instance.Print($"<size=15><color=#c0c0c0>{str}</color></size>"));
                Console.instance.Print(
                    $"<color=#ffa500>NOTE: these settings don't map exactly to console param function or the config file, as some of them are derived.</color>");
            });

            if (BetterContinents.Settings.AnyImageMap)
            {
                bc.AddGroup("reload", "Reload", "Reloads and reapplies one or more of the image maps", reload =>
                {
                    if (BetterContinents.Settings.HasHeightMap)
                        reload.AddCommand("hm", "Heightmap", "Reloads the heightmap",
                            HeightmapCommand(_ => BetterContinents.Settings.ReloadHeightMap()));
                    if (BetterContinents.Settings.HasRoughMap)
                        reload.AddCommand("rm", "Roughmap", "Reloads the roughmap",
                            HeightmapCommand(_ => BetterContinents.Settings.ReloadRoughMap()));
                    if (BetterContinents.Settings.HasFlatMap)
                        reload.AddCommand("fm", "Flatmap", "Reloads the flatmap",
                            HeightmapCommand(_ => BetterContinents.Settings.ReloadFlatMap()));
                    if (BetterContinents.Settings.HasBiomeMap)
                        reload.AddCommand("bm", "Biomemap", "Reloads the biomemap",
                            HeightmapCommand(_ => BetterContinents.Settings.ReloadBiomeMap()));
                    if (BetterContinents.Settings.HasTerrainMap)
                        reload.AddCommand("terrain", "Terrainmap", "Reloads the terrainmap",
                            HeightmapCommand(_ => BetterContinents.Settings.ReloadTerrainMap()));
                    if (BetterContinents.Settings.HasLocationMap)
                        reload.AddCommand("lm", "Locationmap", "Reloads the locationmap",
                            HeightmapCommand(_ => BetterContinents.Settings.ReloadLocationMap()));
                    if (BetterContinents.Settings.HasForestMap)
                        reload.AddCommand("fom", "Forestmap", "Reloads the forestmap",
                            HeightmapCommand(_ => BetterContinents.Settings.ReloadForestMap()));
                    if (BetterContinents.Settings.HasPaintMap)
                        reload.AddCommand("paint", "Paintmap", "Reloads the paintmap",
                            HeightmapCommand(_ => BetterContinents.Settings.ReloadPaintMap()));
                    if (BetterContinents.Settings.HasLavaMap)
                        reload.AddCommand("lava", "Lavamap", "Reloads the lavamap",
                            HeightmapCommand(_ => BetterContinents.Settings.ReloadLavaMap()));
                    if (BetterContinents.Settings.HasMossMap)
                        reload.AddCommand("moss", "Mossmap", "Reloads the mossmap",
                            HeightmapCommand(_ => BetterContinents.Settings.ReloadMossMap()));
                    if (BetterContinents.Settings.HasHeatMap)
                        reload.AddCommand("heat", "Heatmap", "Reloads the heatmap",
                            HeightmapCommand(_ => BetterContinents.Settings.ReloadHeatMap()));
                    if (BetterContinents.Settings.HasSpawnMap)
                        reload.AddCommand("spawn", "Spawnmap", "Reloads the spawnmap",
                            HeightmapCommand(_ => BetterContinents.Settings.ReloadSpawnMap()));
                    if (BetterContinents.Settings.AnyImageMap)
                    {
                        reload.AddCommand("all", "All", "Reloads all image maps", HeightmapCommand(_ =>
                        {
                            if (BetterContinents.Settings.HasHeightMap) BetterContinents.Settings.ReloadHeightMap();
                            if (BetterContinents.Settings.HasRoughMap) BetterContinents.Settings.ReloadRoughMap();
                            if (BetterContinents.Settings.HasFlatMap) BetterContinents.Settings.ReloadFlatMap();
                            if (BetterContinents.Settings.HasBiomeMap) BetterContinents.Settings.ReloadBiomeMap();
                            if (BetterContinents.Settings.HasTerrainMap) BetterContinents.Settings.ReloadTerrainMap();
                            if (BetterContinents.Settings.HasLocationMap) BetterContinents.Settings.ReloadLocationMap();
                            if (BetterContinents.Settings.HasForestMap) BetterContinents.Settings.ReloadForestMap();
                            if (BetterContinents.Settings.HasPaintMap) BetterContinents.Settings.ReloadPaintMap();
                            if (BetterContinents.Settings.HasLavaMap) BetterContinents.Settings.ReloadLavaMap();
                            if (BetterContinents.Settings.HasMossMap) BetterContinents.Settings.ReloadMossMap();
                            if (BetterContinents.Settings.HasHeatMap) BetterContinents.Settings.ReloadHeatMap();
                            if (BetterContinents.Settings.HasSpawnMap) BetterContinents.Settings.ReloadSpawnMap();
                        }));
                    }
                });
            }

            bc.AddCommand("locs", "Dump locations", "Prints all location spawn instance counts to the console", _ =>
            {
                var locationInstances = ZoneSystem.instance.m_locationInstances;

                var locationTypes = locationInstances.Values
                    .GroupBy(l => l.m_location.m_prefabName)
                    .ToDictionary(g => g.Key, g => g.ToList());
                foreach (var lg in locationTypes)
                {
                    Console.instance.Print($"Placed {lg.Value.Count} {lg.Key} locations");
                }

                foreach (var boss in Bosses)
                {
                    if (!locationTypes.ContainsKey(boss))
                    {
                        Console.instance.Print($"<color=#ffa500>WARNING: No {boss} generated</color>");
                    }
                }
            });
            bc.AddCommand("bosses", "Show bosses", "Shows pins for bosses, start temple and trader",
                _ => GameUtils.ShowOnMap(Bosses));
            bc.AddCommand("show", "Show locations", "Pins locations matching optional filter on the map", args =>
            {
                GameUtils.ShowOnMap((args ?? "")
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(f => f.Trim())
                    .ToArray());
            });
            bc.AddCommand("hide", "Hide locations", "Removes pins matching optional filter from the map", args =>
            {
                GameUtils.HideOnMap((args ?? "")
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(f => f.Trim())
                    .ToArray());
            });
            bc.AddCommand("clouds", "Clouds", "Toggles minimap clouds", args =>
            {
                if (GameUtils.MinimapCloudsEnabled)
                    GameUtils.DisableMinimapClouds();
                else
                    GameUtils.EnableMinimapClouds();
                GameUtils.HideOnMap((args ?? "")
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(f => f.Trim())
                    .ToArray());
            });
            bc.AddValue("mapds", "Minimap downscaling", "Sets minimap downscaling factor (for faster updates)",
                defaultValue: 2,
                list: [0, 1, 2, 3],
                getter: () => GameUtils.MinimapDownscalingPower,
                setter: val =>
                {
                    GameUtils.MinimapDownscalingPower = Mathf.Clamp(val, 0, 3);
                    GameUtils.FastMinimapRegen();
                });
            bc.AddCommand("reset", "reset",
                "Resets whole map (done automatically on every change)",
                _ => GameUtils.Reset());
            bc.AddCommand("scr", "Save map screenshot",
                "Saves the minimap to a png, optionally pass resolution, default is 2048", arg =>
                {
                    var filename = DateTime.Now.ToString("yyyy-dd-M-HH-mm-ss") + ".png";
                    var screenshotDir = Path.Combine(Utils.GetSaveDataPath(FileHelpers.FileSource.Local), "BetterContinents",
                        WorldGenerator.instance.m_world.m_name);
                    var path = Path.Combine(screenshotDir, filename);
                    int size = string.IsNullOrEmpty(arg) ? 2048 : int.Parse(arg);
                    GameUtils.SaveMinimap(path, size);
                    Console.instance.Print($"Map screenshot saved to {path}, size {size} x {size}");
                });
            bc.AddCommand("savepreset", "Save preset",
                "Saves current world settings as a preset, including a thumbnail, pass preset name as argument",
                arg =>
                {
                    arg ??= WorldGenerator.instance.m_world.m_name;
                    Presets.Save(BetterContinents.Settings, arg);
                });

            bc.AddGroup("g", "Global", "Global settings, get more info with 'bc g help'",
                group =>
                {
                    group.AddValue("worldsize", "World size", "World radius in meters",
                        defaultValue: 10000f, minValue: 0f, maxValue: 1000000f,
                        setter: SetHeightmapValue<float>(value => BetterContinents.Settings.WorldSize = value),
                        getter: () => BetterContinents.Settings.WorldSize);
                    group.AddValue("edgesize", "Edge size", "Edge size in meters",
                        defaultValue: 500f, minValue: 0f, maxValue: 1000000f,
                        setter: SetHeightmapValue<float>(value => BetterContinents.Settings.EdgeSize = value),
                        getter: () => BetterContinents.Settings.EdgeSize);
                    group.AddValue("cs", "Continent size adjustment", "Continent size adjustment",
                        defaultValue: 0.5f, minValue: 0, maxValue: 1,
                        setter: SetHeightmapValue<float>(value => BetterContinents.Settings.ContinentSize = value),
                        getter: () => BetterContinents.Settings.ContinentSize);
                    group.AddValue("sl", "Sea level adjustment", "Sea level adjustment",
                        defaultValue: 0.5f, minValue: 0, maxValue: 1,
                        setter: SetHeightmapValue<float>(value => BetterContinents.Settings.SeaLevel = value),
                        getter: () => BetterContinents.Settings.SeaLevel);
                    group.AddValue("r", "Rivers", "Whether rivers are enabled",
                        defaultValue: true,
                        setter: SetHeightmapValue<bool>(value => BetterContinents.Settings.RiversEnabled = value),
                        getter: () => BetterContinents.Settings.RiversEnabled);
                    group.AddValue("ag", "Ashlands Gap", "Whether The Ashlands ocean gap is enabled",
                        defaultValue: false,
                        setter: SetHeightmapValue<bool>(value => BetterContinents.Settings.AshlandsGapEnabled = value),
                        getter: () => BetterContinents.Settings.AshlandsGapEnabled);
                    group.AddValue("ng", "Deep North Gap", "Whether The Deep North ocean gap is enabled",
                        defaultValue: false,
                        setter: SetHeightmapValue<bool>(value => BetterContinents.Settings.DeepNorthGapEnabled = value),
                        getter: () => BetterContinents.Settings.DeepNorthGapEnabled);
                    group.AddValue("me", "Map edge drop off", "Whether the map drops away at the boundary",
                        defaultValue: true,
                        setter: SetHeightmapValue<bool>(value => BetterContinents.Settings.MapEdgeDropoff = value),
                        getter: () => BetterContinents.Settings.MapEdgeDropoff);
                    group.AddValue("mc", "Allow mountains in center",
                        "Whether the center of the map (usually the spawn area), is flattened",
                        defaultValue: false,
                        setter: SetHeightmapValue<bool>(value =>
                            BetterContinents.Settings.MountainsAllowedAtCenter = value),
                        getter: () => BetterContinents.Settings.MountainsAllowedAtCenter);
                });

            bc.AddGroup("h", "Heightmap", "Heightmap settings, get more info with 'bc param h help'",
                group =>
                {
                    group.AddValue("fn", "Heightmap filename",
                        "Set heightmap filename (full path, directory or file name)",
                        defaultValue: string.Empty,
                        setter: SetHeightmapValue<string>(path =>
                        {
                            var fullPath = BetterContinents.Settings.ResolveHeightPath(path);
                            BetterContinents.Settings.SetHeightPath(fullPath);
                            if (BetterContinents.Settings.HasHeightMap)
                                Console.instance.Print($"<color=#ffa500>Heightmap enabled!</color>");
                            else if (string.IsNullOrEmpty(path))
                                Console.instance.Print($"<color=#ff0000>Heightmap disabled!</color>");
                            else
                                Console.instance.Print($"<color=#ff0000>ERROR: Path {path} not found!</color>");
                        }),
                        getter: () => BetterContinents.Settings.GetHeightPath());
                    group.AddValue("ov", "Heightmap Override All",
                        "Causes the terrain to conform to the heightmap, ignoring biome specific variance",
                        defaultValue: false,
                        setter: SetHeightmapValue<bool>(value =>
                            BetterContinents.Settings.HeightmapOverrideAll = value),
                        getter: () => BetterContinents.Settings.HeightmapOverrideAll);
                    group.AddValue("am", "Heightmap Amount", "Heightmap amount",
                        defaultValue: 1f, minValue: 0, maxValue: 5,
                        setter: SetHeightmapValue<float>(value =>
                            BetterContinents.Settings.HeightmapAmount = value),
                        getter: () => BetterContinents.Settings.HeightmapAmount);
                    group.AddValue("bl", "Heightmap Blend", "Heightmap blend",
                        defaultValue: 1f, minValue: 0, maxValue: 1,
                        setter: SetHeightmapValue<float>(value => BetterContinents.Settings.HeightmapBlend = value),
                        getter: () => BetterContinents.Settings.HeightmapBlend);
                    group.AddValue("ad", "Heightmap Add", "Heightmap add",
                        defaultValue: 0f, minValue: -1, maxValue: 1,
                        setter: SetHeightmapValue<float>(value => BetterContinents.Settings.HeightmapAdd = value),
                        getter: () => BetterContinents.Settings.HeightmapAdd);
                    group.AddValue("ma", "Heightmap Mask", "Heightmap mask",
                        defaultValue: 0f, minValue: 0, maxValue: 1,
                        setter: SetHeightmapValue<float>(value => BetterContinents.Settings.HeightmapMask = value),
                        getter: () => BetterContinents.Settings.HeightmapMask);
                });

            bc.AddGroup("r", "Roughmap", "Roughmap settings, get more info with 'bc param r help'", group =>
            {
                group.AddValue("fn", "Roughmap Filename",
                    "Sets roughmap filename (full path, directory or file name)",
                    defaultValue: string.Empty,
                    setter: SetHeightmapValue<string>(path =>
                    {
                        var fullPath = BetterContinents.Settings.ResolveRoughPath(path);
                        BetterContinents.Settings.SetRoughPath(fullPath);
                        if (BetterContinents.Settings.HasRoughMap)
                            Console.instance.Print($"<color=#ffa500>Roughmap enabled!</color>");
                        else if (string.IsNullOrEmpty(path))
                            Console.instance.Print($"<color=#ff0000>Roughmap disabled!</color>");
                        else
                            Console.instance.Print($"<color=#ff0000>ERROR: Path {path} not found!</color>");
                    }),
                    getter: () => BetterContinents.Settings.GetRoughPath());
                group.AddValue("bl", "Roughmap Blend", "Roughmap blend",
                    defaultValue: 1f, minValue: 0, maxValue: 1,
                    setter: SetHeightmapValue<float>(value => BetterContinents.Settings.RoughmapBlend = value),
                    getter: () => BetterContinents.Settings.RoughmapBlend);
            });
            bc.AddGroup("b", "Biomemap", "Biomemap settings, get more info with 'bc param b help'", group =>
            {
                group.AddValue("fn", "Biomemap Filename",
                    "Sets biomemap filename (full path, directory or file name)",
                    defaultValue: string.Empty,
                    setter: SetHeightmapValue<string>(path =>
                    {
                        var fullPath = BetterContinents.Settings.ResolveBiomePath(path);
                        BetterContinents.Settings.SetBiomePath(fullPath);
                        if (BetterContinents.Settings.HasBiomeMap)
                            Console.instance.Print($"<color=#ffa500>Biomemap enabled!</color>");
                        else if (string.IsNullOrEmpty(path))
                            Console.instance.Print($"<color=#ff0000>Biomemap disabled!</color>");
                        else
                            Console.instance.Print($"<color=#ff0000>ERROR: Path {path} not found!</color>");
                    }),
                    getter: () => BetterContinents.Settings.GetBiomePath());

                group.AddValue("p", "Biome precision", "How precisely the terrain matches the biomemap",
                    defaultValue: 0, minValue: 0, maxValue: 5,
                    setter: SetHeightmapValue<int>(value => BetterContinents.Settings.BiomePrecision = value),
                    getter: () => BetterContinents.Settings.BiomePrecision);
            });
            bc.AddGroup("terrain", "Terrainmap", "Terrainmap settings, get more info with 'bc param terrain help'",
                group =>
                {
                    group.AddValue("fn", "Terrainmap Filename",
                        "Sets terrainmap filename (full path, directory or file name)",
                        defaultValue: string.Empty,
                        setter: SetHeightmapValue<string>(path =>
                        {
                            var fullPath = BetterContinents.Settings.ResolveTerrainPath(path);
                            BetterContinents.Settings.SetTerrainPath(fullPath);
                            if (BetterContinents.Settings.HasTerrainMap)
                                Console.instance.Print($"<color=#ffa500>Terrainmap enabled!</color>");
                            else if (string.IsNullOrEmpty(path))
                                Console.instance.Print($"<color=#ff0000>Terrainmap disabled!</color>");
                            else
                                Console.instance.Print($"<color=#ff0000>ERROR: Path {path} not found!</color>");
                        }),
                        getter: () => BetterContinents.Settings.GetTerrainPath());
                });
            bc.AddGroup("l", "Locationmap", "Locationmap settings, get more info with 'bc param s help'", group =>
            {
                group.AddValue("fn", "Locationmap Filename",
                    "Sets locationmap filename (full path, directory or file name)",
                    defaultValue: string.Empty,
                    setter: SetHeightmapValue<string>(path =>
                    {
                        var fullPath = BetterContinents.Settings.ResolveLocationPath(path);
                        BetterContinents.Settings.SetLocationPath(fullPath);
                        if (BetterContinents.Settings.HasLocationMap)
                            Console.instance.Print($"<color=#ffa500>Locationmap enabled!</color>");
                        else if (string.IsNullOrEmpty(path))
                            Console.instance.Print($"<color=#ff0000>Locationmap disabled!</color>");
                        else
                            Console.instance.Print($"<color=#ff0000>ERROR: Path {path} not found!</color>");
                    }),
                    getter: () => BetterContinents.Settings.GetLocationPath());
            });

            bc.AddGroup("paint", "Paintmap", "Paintmap settings, get more info with 'bc param s help'", group =>
            {
                group.AddValue("fn", "Paintmap Filename",
                    "Sets paintmap filename (full path, directory or file name)",
                    defaultValue: string.Empty,
                    setter: SetHeightmapValue<string>(path =>
                    {
                        var fullPath = BetterContinents.Settings.ResolvePaintPath(path);
                        BetterContinents.Settings.SetPaintPath(fullPath);
                        if (BetterContinents.Settings.HasPaintMap)
                            Console.instance.Print($"<color=#ffa500>Paintmap enabled!</color>");
                        else if (string.IsNullOrEmpty(path))
                            Console.instance.Print($"<color=#ff0000>Paintmap disabled!</color>");
                        else
                            Console.instance.Print($"<color=#ff0000>ERROR: Path {path} not found!</color>");
                    }),
                    getter: () => BetterContinents.Settings.GetPaintPath());
            });
            bc.AddGroup("lava", "Lavamap", "Lavamap settings, get more info with 'bc param s help'", group =>
            {
                group.AddValue("fn", "Lavamap Filename",
                    "Sets lavamap filename (full path, directory or file name)",
                    defaultValue: string.Empty,
                    setter: SetHeightmapValue<string>(path =>
                    {
                        var fullPath = BetterContinents.Settings.ResolveLavaPath(path);
                        BetterContinents.Settings.SetLavaPath(fullPath);
                        if (BetterContinents.Settings.HasLavaMap)
                            Console.instance.Print($"<color=#ffa500>Lavamap enabled!</color>");
                        else if (string.IsNullOrEmpty(path))
                            Console.instance.Print($"<color=#ff0000>Lavamap disabled!</color>");
                        else
                            Console.instance.Print($"<color=#ff0000>ERROR: Path {path} not found!</color>");
                    }),
                    getter: () => BetterContinents.Settings.GetLavaPath());
            });
            bc.AddGroup("moss", "Mossmap", "Mossmap settings, get more info with 'bc param s help'", group =>
            {
                group.AddValue("fn", "Mossmap Filename",
                    "Sets mossmap filename (full path, directory or file name)",
                    defaultValue: string.Empty,
                    setter: SetHeightmapValue<string>(path =>
                    {
                        var fullPath = BetterContinents.Settings.ResolveMossPath(path);
                        BetterContinents.Settings.SetMossPath(fullPath);
                        if (BetterContinents.Settings.HasMossMap)
                            Console.instance.Print($"<color=#ffa500>Mossmap enabled!</color>");
                        else if (string.IsNullOrEmpty(path))
                            Console.instance.Print($"<color=#ff0000>Mossmap disabled!</color>");
                        else
                            Console.instance.Print($"<color=#ff0000>ERROR: Path {path} not found!</color>");
                    }),
                    getter: () => BetterContinents.Settings.GetMossPath());
            });
            bc.AddGroup("heat", "Heatmap", "Heatmap settings, get more info with 'bc param s help'", group =>
            {
                group.AddValue("fn", "Heatmap Filename",
                    "Sets heatmap filename (full path, directory or file name)",
                    defaultValue: string.Empty,
                    setter: SetHeightmapValue<string>(path =>
                    {
                        var fullPath = BetterContinents.Settings.ResolveHeatPath(path);
                        BetterContinents.Settings.SetHeatPath(fullPath);
                        if (BetterContinents.Settings.HasHeatMap)
                            Console.instance.Print($"<color=#ffa500>Heatmap enabled!</color>");
                        else if (string.IsNullOrEmpty(path))
                            Console.instance.Print($"<color=#ff0000>Heatmap disabled!</color>");
                        else
                            Console.instance.Print($"<color=#ff0000>ERROR: Path {path} not found!</color>");
                    }),
                    getter: () => BetterContinents.Settings.GetHeatPath());

                group.AddValue("sc", "Heatmap Scale", "Heatmap scale",
                    defaultValue: 10f, minValue: 0f, maxValue: 100f,
                    setter: SetHeightmapValue<float>(value => BetterContinents.Settings.HeatMapScale = value),
                    getter: () => BetterContinents.Settings.HeatMapScale);
            });
            bc.AddGroup("fo", "Forest", "Forest settings, get more info with 'bc param fo help'", group =>
            {
                group.AddValue("sc", "Forest Scale", "Forest scale",
                    defaultValue: 0.5f, minValue: 0f, maxValue: 1f,
                    setter: SetHeightmapValue<float>(value => BetterContinents.Settings.ForestScaleFactor = value),
                    getter: () => BetterContinents.Settings.ForestScaleFactor);
                group.AddValue("am", "Forest Amount", "Forest amount",
                    defaultValue: 0.5f, minValue: 0f, maxValue: 1f,
                    setter: SetHeightmapValue<float>(value => BetterContinents.Settings.ForestAmount = value),
                    getter: () => BetterContinents.Settings.ForestAmount);
                group.AddValue("ffo", "Forest Factor Override All", "Forest factor override all trees",
                    setter: SetHeightmapValue<bool>(value =>
                    {
                        BetterContinents.Settings.ForestFactorOverrideAllTrees = value;
                        Console.instance.Print(
                            "<color=#ffa500>NOTE: You need to reload the world to apply this change to the forest factor override!</color>");
                    }),
                    getter: () => BetterContinents.Settings.ForestFactorOverrideAllTrees);
                group.AddValue("fn", "Forestmap Filename",
                    "Sets forestmap filename (full path, directory or file name)",
                    defaultValue: string.Empty,
                    setter: SetHeightmapValue<string>(path =>
                    {
                        var fullPath = BetterContinents.Settings.ResolveForestPath(path);
                        BetterContinents.Settings.SetForestPath(fullPath);
                        if (BetterContinents.Settings.HasForestMap)
                            Console.instance.Print($"<color=#ffa500>Forestmap enabled!</color>");
                        else if (string.IsNullOrEmpty(path))
                            Console.instance.Print($"<color=#ff0000>Forestmap disabled!</color>");
                        else
                            Console.instance.Print($"<color=#ff0000>ERROR: Path {path} not found!</color>");
                    }),
                    getter: () => BetterContinents.Settings.GetForestPath());
                group.AddValue("mu", "Forestmap Multiply", "Forestmap multiply",
                    defaultValue: 1f, minValue: 0f, maxValue: 1f,
                    setter: SetHeightmapValue<float>(value => BetterContinents.Settings.ForestmapMultiply = value),
                    getter: () => BetterContinents.Settings.ForestmapMultiply);
                group.AddValue("add", "Forestmap Add", "Forestmap add",
                    defaultValue: 0f, minValue: 0f, maxValue: 1f,
                    setter: SetHeightmapValue<float>(value => BetterContinents.Settings.ForestmapAdd = value),
                    getter: () => BetterContinents.Settings.ForestmapAdd);
            });
            // bc.AddGroup("ri", "ridge settings, get more info with 'bc param ri help'", 
            // subcmd =>
            // {
            //     AddHeightmapSubcommand(subcmd, "mh", "ridges max height", "(between 0 and 1)", args =>
            //     {
            //         BetterContinents.Settings.MaxRidgeHeight = float.Parse(args);
            //     });
            //     AddHeightmapSubcommand(subcmd, "si", "ridge size", "(between 0 and 1)", args => BetterContinents.Settings.RidgeSize = float.Parse(args));
            //     AddHeightmapSubcommand(subcmd, "bl", "ridge blend", "(between 0 and 1)", args => BetterContinents.Settings.RidgeBlend = float.Parse(args));
            //     AddHeightmapSubcommand(subcmd, "am", "ridge amount", "(between 0 and 1)", args => BetterContinents.Settings.RidgeAmount = float.Parse(args));
            // });
            bc.AddGroup("st", "Start Position", "Start position settings, get more info with 'bc param st help'",
                group =>
                {
                    group.AddValue("os", "Override Start Position", "Overrides the start position",
                        setter: SetHeightmapValue<bool>(value =>
                            BetterContinents.Settings.OverrideStartPosition = value),
                        getter: () => BetterContinents.Settings.OverrideStartPosition);
                    group.AddValue("x", "Start Position X", "Start position x",
                        defaultValue: 1f, minValue: 0f, maxValue: 1f,
                        setter: SetHeightmapValue<float>(value =>
                        {
                            BetterContinents.Settings.StartPositionX = value;
                        }),
                        getter: () => BetterContinents.Settings.StartPositionX);
                    group.AddValue("y", "Start Position Y", "Start position y",
                        defaultValue: 1f, minValue: 0f, maxValue: 1f,
                        setter: SetHeightmapValue<float>(value =>
                        {
                            BetterContinents.Settings.StartPositionY = value;
                        }),
                        getter: () => BetterContinents.Settings.StartPositionY);
                });

            void AddNoiseCommands(Command.SubcommandBuilder group, NoiseStackSettings.NoiseSettings settings,
                bool isWarp = false, bool isMask = false)
            {
                // Basic
                group.AddValue("nt", "Noise Type", "Noise type",
                    defaultValue: FastNoiseLite.NoiseType.OpenSimplex2,
                    setter: SetHeightmapValue<FastNoiseLite.NoiseType>(value => settings.NoiseType = value),
                    getter: () => settings.NoiseType);
                group.AddValue("fq", "Frequency X", "Frequency x",
                    defaultValue: 0.0005f,
                    setter: SetHeightmapValue<float>(value => settings.Frequency = value),
                    getter: () => settings.Frequency);
                group.AddValue("asp", "Aspect Ratio", "Scales y dimension relative to x",
                    defaultValue: 1,
                    setter: SetHeightmapValue<float>(value => settings.Aspect = value),
                    getter: () => settings.Aspect);

                // Fractal
                group.AddValue("ft", "Fractal Type", "Fractal type",
                    defaultValue: isWarp ? FastNoiseLite.FractalType.None : FastNoiseLite.FractalType.FBm,
                    list: isWarp
                        ? NoiseStackSettings.NoiseSettings.WarpFractalTypes
                        : NoiseStackSettings.NoiseSettings.NonFractalTypes,
                    setter: SetHeightmapValue<FastNoiseLite.FractalType>(value => settings.FractalType = value),
                    getter: () => settings.FractalType);

                if (settings.FractalType != FastNoiseLite.FractalType.None)
                {
                    group.AddValue<int>("fo", "Fractal Octaves", "Fractal octaves",
                        defaultValue: 4, minValue: 1, maxValue: 10,
                        setter: SetHeightmapValue<int>(value => settings.FractalOctaves = value),
                        getter: () => settings.FractalOctaves);
                    group.AddValue("fl", "Fractal Lacunarity", "Fractal lacunarity",
                        defaultValue: 2, minValue: 0, maxValue: 10,
                        setter: SetHeightmapValue<float>(value => settings.FractalLacunarity = value),
                        getter: () => settings.FractalLacunarity);
                    group.AddValue("fg", "Fractal Gain", "Fractal gain",
                        defaultValue: 0.5f, minValue: 0, maxValue: 2,
                        setter: SetHeightmapValue<float>(value => settings.FractalGain = value),
                        getter: () => settings.FractalGain);
                    group.AddValue("ws", "Weighted Strength", "Weighted strength",
                        defaultValue: 0, minValue: -2, maxValue: 2,
                        setter: SetHeightmapValue<float>(value => settings.FractalWeightedStrength = value),
                        getter: () => settings.FractalWeightedStrength);
                    if (settings.FractalType == FastNoiseLite.FractalType.PingPong)
                    {
                        group.AddValue("ps", "Ping-Pong Strength", "Ping-pong strength",
                            defaultValue: 2, minValue: 0, maxValue: 10,
                            setter: SetHeightmapValue<float>(value => settings.FractalPingPongStrength = value),
                            getter: () => settings.FractalPingPongStrength);
                    }
                }

                if (settings.NoiseType == FastNoiseLite.NoiseType.Cellular)
                {
                    // Cellular
                    group.AddValue("cf", "Cellular Distance Function", "Cellular distance function",
                        defaultValue: FastNoiseLite.CellularDistanceFunction.Euclidean,
                        setter: SetHeightmapValue<FastNoiseLite.CellularDistanceFunction>(value =>
                            settings.CellularDistanceFunction = value),
                        getter: () => settings.CellularDistanceFunction);
                    group.AddValue("ct", "Cellular Return Type", "Cellular return type",
                        defaultValue: FastNoiseLite.CellularReturnType.Distance2Div,
                        setter: SetHeightmapValue<FastNoiseLite.CellularReturnType>(value =>
                            settings.CellularReturnType = value),
                        getter: () => settings.CellularReturnType);
                    group.AddValue("cj", "Cellular Jitter", "Cellular jitter",
                        defaultValue: 1, minValue: 0, maxValue: 2,
                        setter: SetHeightmapValue<float>(value => settings.CellularJitter = value),
                        getter: () => settings.CellularJitter);
                }

                if (isWarp)
                {
                    // Warp
                    group.AddValue("dt", "Domain Warp Type", "Domain warp type",
                        defaultValue: FastNoiseLite.DomainWarpType.OpenSimplex2,
                        setter: SetHeightmapValue<FastNoiseLite.DomainWarpType>(value =>
                            settings.DomainWarpType = value),
                        getter: () => settings.DomainWarpType);
                    group.AddValue("da", "Domain Warp Amp", "Domain warp amp",
                        defaultValue: 50, minValue: 0, maxValue: 20000,
                        setter: SetHeightmapValue<float>(value => settings.DomainWarpAmp = value),
                        getter: () => settings.DomainWarpAmp);
                }

                // Filters
                group.AddValue("in", "Invert", "Invert",
                    setter: SetHeightmapValue<bool>(value => settings.Invert = value),
                    getter: () => settings.Invert);

                group.AddValue("ust", "Use Smooth Threshold", "Use smooth threshold",
                    setter: SetHeightmapValue<bool>(value => settings.UseSmoothThreshold = value),
                    getter: () => settings.UseSmoothThreshold);
                group.AddValue("sts", "Smooth Threshold Start", "Smooth threshold start",
                    defaultValue: 0, minValue: -1, maxValue: 1,
                    getter: () => settings.SmoothThresholdStart,
                    setter: SetHeightmapValue<float>(value => settings.SmoothThresholdStart = value));
                group.AddValue("ste", "Smooth Threshold End", "Smooth threshold end",
                    defaultValue: 1, minValue: -1, maxValue: 1,
                    getter: () => settings.SmoothThresholdEnd,
                    setter: SetHeightmapValue<float>(value => settings.SmoothThresholdEnd = value));

                group.AddValue("uth", "Use Threshold", "Use threshold",
                    setter: SetHeightmapValue<bool>(value => settings.UseThreshold = value),
                    getter: () => settings.UseThreshold);
                group.AddValue("th", "Threshold", "Threshold",
                    defaultValue: 0, minValue: 0, maxValue: 1,
                    getter: () => settings.Threshold,
                    setter: SetHeightmapValue<float>(value => settings.Threshold = value));

                group.AddValue("ura", "Use Range", "Use range",
                    setter: SetHeightmapValue<bool>(value => settings.UseRange = value),
                    getter: () => settings.UseRange);
                group.AddValue("ras", "Range Start", "Range start",
                    defaultValue: 0, minValue: -1, maxValue: 1,
                    getter: () => settings.RangeStart,
                    setter: SetHeightmapValue<float>(value => settings.RangeStart = value));
                group.AddValue("rae", "Range End", "Range end",
                    defaultValue: 1, minValue: -1, maxValue: 1,
                    getter: () => settings.RangeEnd,
                    setter: SetHeightmapValue<float>(value => settings.RangeEnd = value));

                group.AddValue("uop", "Use Opacity", "Use opacity",
                    setter: SetHeightmapValue<bool>(value => settings.UseOpacity = value),
                    getter: () => settings.UseOpacity);
                group.AddValue("op", "Opacity", "Opacity",
                    defaultValue: 1, minValue: 0, maxValue: 1,
                    getter: () => settings.Threshold,
                    setter: SetHeightmapValue<float>(value => settings.Threshold = value));

                group.AddValue("blm", "Blend Mode", "How to apply this layer to the previous one",
                    defaultValue: BlendOperations.BlendModeType.Overlay,
                    setter: SetHeightmapValue<BlendOperations.BlendModeType>(value =>
                        settings.BlendMode = value),
                    getter: () => settings.BlendMode);
            }

            bc.AddGroup("hl", "Height Layer Settings", "Height layer settings",
                hl =>
                {
                    var baseNoise = BetterContinents.Settings.BaseHeightNoise;
                    // hl.AddValue<int>("n", "Number of Layers", "set number of layers",
                    //         defaultValue: 1, minValue: 1, maxValue: 5,
                    //         getter: () => baseNoise.NoiseLayers.Count,
                    //         setter: SetHeightmapValue<int>(val => baseNoise.SetNoiseLayerCount(val)))
                    //     .CustomDrawer(cmd =>
                    //     {
                    //         GUILayout.BeginHorizontal();
                    //         if(baseNoise.NoiseLayers.Count > )
                    //         if (GUILayout.Button("-"))
                    //         {
                    //             
                    //         }
                    //         GUILayout.EndHorizontal();
                    //     });

                    hl.AddCommand("add", "Add Layer", "", HeightmapCommand(_ => baseNoise.AddNoiseLayer()));

                    for (int i = 0; i < baseNoise.NoiseLayers.Count; i++)
                    {
                        int index = i;
                        var noiseLayer = baseNoise.NoiseLayers[index];
                        hl.AddGroup(index.ToString(), $"layer {index}", $"layer {index} settings", l =>
                        {
                            l.AddGroup("npreset", "Apply Noise Preset", "", preset =>
                            {
                                preset.AddCommand("def", "Default", "General noise layer",
                                    HeightmapCommand(_ =>
                                        noiseLayer.noiseSettings = NoiseStackSettings.NoiseSettings.Default()));
                                preset.AddCommand("ri", "Ridges", "Ridged noise",
                                    HeightmapCommand(_ =>
                                        noiseLayer.noiseSettings = NoiseStackSettings.NoiseSettings.Ridged()));
                            });
                            l.AddGroup("n", "Noise", $"layer {index} noise settings", nm
                                    => AddNoiseCommands(nm, noiseLayer.noiseSettings))
                                .UIBackgroundColor(new Color32(0xCE, 0xB3, 0xAB, 0x7f));
                            l.AddGroup("nw", "Noise Warp", $"layer {index} noise warp settings", nm =>
                            {
                                nm.AddValue<bool>("on", "Enabled", $"layer {index} noise warp enabled",
                                    defaultValue: false,
                                    setter: SetHeightmapValue<bool>(value =>
                                    {
                                        if (value && noiseLayer.noiseWarpSettings == null)
                                            noiseLayer.noiseWarpSettings =
                                                NoiseStackSettings.NoiseSettings.DefaultWarp();
                                        else if (!value)
                                            noiseLayer.noiseWarpSettings = null;
                                    }),
                                    getter: () => noiseLayer.noiseWarpSettings != null);
                                if (noiseLayer.noiseWarpSettings != null)
                                {
                                    AddNoiseCommands(nm, noiseLayer.noiseWarpSettings, isWarp: true);
                                }
                            }).UIBackgroundColor(new Color32(0xCA, 0xAE, 0xA5, 0x7f));
                            l.AddValue<int>(null, $"Noise layer {index} preview", $"Noise layer {index} preview")
                                .CustomDrawer(_ => DrawNoisePreview(index));

                            if (index > 0)
                            {
                                l.AddGroup("mpreset", "Apply Mask Preset", "", preset =>
                                {
                                    preset.AddCommand("def", "Default", "General mask layer", HeightmapCommand(_ =>
                                    {
                                        noiseLayer.maskSettings = NoiseStackSettings.NoiseSettings.Default();
                                        noiseLayer.maskSettings.SmoothThresholdStart = 0.6f;
                                        noiseLayer.maskSettings.SmoothThresholdEnd = 0.75f;
                                    }));
                                    preset.AddCommand("25%", "25%", "About 25% coverage with smooth threshold",
                                        HeightmapCommand(_ =>
                                        {
                                            noiseLayer.maskSettings = NoiseStackSettings.NoiseSettings.Default();
                                            noiseLayer.maskSettings.SmoothThresholdStart = 0.6f;
                                            noiseLayer.maskSettings.SmoothThresholdEnd = 0.75f;
                                        }));
                                    preset.AddCommand("ri", "Ridges", "Warped with smooth threshold",
                                        HeightmapCommand(_ =>
                                        {
                                            noiseLayer.maskSettings = NoiseStackSettings.NoiseSettings.Default();
                                            noiseLayer.maskSettings.SmoothThresholdStart = 0.6f;
                                            noiseLayer.maskSettings.SmoothThresholdEnd = 0.75f;
                                            noiseLayer.maskWarpSettings =
                                                NoiseStackSettings.NoiseSettings.DefaultWarp();
                                        }));
                                });

                                l.AddGroup("m", "Mask", $"layer {index} mask settings", nm =>
                                {
                                    nm.AddValue<bool>("on", "Enabled", $"layer {index} mask enabled",
                                        defaultValue: false,
                                        setter: SetHeightmapValue<bool>(value =>
                                        {
                                            if (value && noiseLayer.maskSettings == null)
                                                noiseLayer.maskSettings =
                                                    NoiseStackSettings.NoiseSettings.Default();
                                            else if (!value)
                                                noiseLayer.maskSettings = noiseLayer.maskWarpSettings = null;
                                        }),
                                        getter: () => noiseLayer.maskSettings != null);
                                    if (noiseLayer.maskSettings != null)
                                    {
                                        AddNoiseCommands(nm, noiseLayer.maskSettings, isMask: true);
                                    }
                                }).UIBackgroundColor(new Color32(0xBA, 0xA5, 0xFF, 0x7f));
                                if (noiseLayer.maskSettings != null)
                                {
                                    l.AddGroup("mw", "Mask Warp", $"layer {index} mask warp settings", nm =>
                                    {
                                        nm.AddValue<bool>("on", "Enabled", $"layer {index} mask warp enabled",
                                            defaultValue: false,
                                            setter: SetHeightmapValue<bool>(value =>
                                            {
                                                if (value && noiseLayer.maskWarpSettings == null)
                                                    noiseLayer.maskWarpSettings =
                                                        NoiseStackSettings.NoiseSettings.DefaultWarp();
                                                else if (!value)
                                                    noiseLayer.maskWarpSettings = null;
                                            }),
                                            getter: () => noiseLayer.maskWarpSettings != null);
                                        if (noiseLayer.maskWarpSettings != null)
                                        {
                                            AddNoiseCommands(nm, noiseLayer.maskWarpSettings, isWarp: true);
                                        }
                                    }).UIBackgroundColor(new Color32(0xB1, 0x99, 0xFF, 0x7f));
                                    l.AddValue<int>(null, $"Mask layer {index} preview",
                                            $"Mask layer {index} preview")
                                        .CustomDrawer(_ => DrawMaskPreview(index));
                                }
                            }

                            l.AddCommand("delete", "Delete Layer", "",
                                    HeightmapCommand(_ => baseNoise.NoiseLayers.Remove(noiseLayer)))
                                .UIBackgroundColor(new Color(0.5f, 0.1f, 0.1f));
                            if (index > 0 && index < baseNoise.NoiseLayers.Count - 1)
                            {
                                l.AddCommand("down", "Move Down", "Swap this layer with the one below",
                                    HeightmapCommand(_ =>
                                    {
                                        var other = baseNoise.NoiseLayers[index + 1];
                                        baseNoise.NoiseLayers[index + 1] = noiseLayer;
                                        baseNoise.NoiseLayers[index] = other;
                                    }));
                            }
                        });
                    }

                    hl.AddValue<int>(null, $"Final preview", "preview of final heightmap")
                        .CustomDrawer(_ => DrawNoisePreview(baseNoise.NoiseLayers.Count));
                }).UIBackgroundColor(new Color32(0xB4, 0x9A, 0x67, 0x7f));

            // AddHeightmapSubcommand(command, "num", "height noise layer count", "(count from 0 to 4)", args => BetterContinents.Settings.BaseHeightNoise.SetNoiseLayerCount(int.Parse(args)));
            // for (int i = 0; i < 4; i++)
            // {
            //     int index = i;
            //     command.AddSubcommand(index.ToString(), $"height layer {index}", subcmdConfig: subcmdLayer =>
            //     {
            //         subcmdLayer.AddSubcommand("n", $"height layer {index} noise", 
            //             subcmdConfig: subcmdLayerPart => AddNoiseCommands(
            //                 (cmd, desc, args, action, getValue) => AddHeightmapSubcommand(subcmdLayerPart, cmd, desc, args, action, getValue),
            //                 () => BetterContinents.Settings.BaseHeightNoise.NoiseLayers[index].noiseSettings));
            //         subcmdLayer.AddSubcommand("nw", $"height layer {index} noise domain warp", 
            //             subcmdConfig: subcmdLayerPart =>
            //             {
            //                 AddHeightmapSubcommand(subcmdLayerPart, "on", $"enable height layer {index} noise domain warp", "", 
            //                     _ => BetterContinents.Settings.BaseHeightNoise.NoiseLayers[index].noiseWarpSettings ??= NoiseStackSettings.NoiseSettings.Default());
            //                 AddHeightmapSubcommand(subcmdLayerPart, "off", $"disable height layer {index} noise domain warp", "", 
            //                     _ => BetterContinents.Settings.BaseHeightNoise.NoiseLayers[index].noiseWarpSettings = null);
            //                 AddNoiseCommands(
            //                     (cmd, desc, args, action, getValue) =>
            //                         AddHeightmapSubcommand(subcmdLayerPart, cmd, desc, args, action,
            //                             getValue),
            //                     () => BetterContinents.Settings.BaseHeightNoise.NoiseLayers[index]
            //                         .noiseWarpSettings);
            //             });
            //         subcmdLayer.AddSubcommand("m", $"height layer {index} mask", 
            //             subcmdConfig: subcmdLayerPart =>
            //             {
            //                 AddHeightmapSubcommand(subcmdLayerPart, "on", $"enable height layer {index} mask", "", 
            //                     _ => BetterContinents.Settings.BaseHeightNoise.NoiseLayers[index].maskSettings ??= NoiseStackSettings.NoiseSettings.Default());
            //                 AddHeightmapSubcommand(subcmdLayerPart, "off", $"disable height layer {index} mask", "", 
            //                     _ =>
            //                     {
            //                         BetterContinents.Settings.BaseHeightNoise.NoiseLayers[index].maskSettings = null;
            //                         BetterContinents.Settings.BaseHeightNoise.NoiseLayers[index].maskWarpSettings = null;
            //                     });
            //                 AddNoiseCommands(
            //                     (cmd, desc, args, action, getValue) =>
            //                         AddHeightmapSubcommand(subcmdLayerPart, cmd, desc, args, action,
            //                             getValue),
            //                     () => BetterContinents.Settings.BaseHeightNoise.NoiseLayers[index].maskSettings);
            //             });
            //         subcmdLayer.AddSubcommand("mw", $"height layer {index} mask domain warp", 
            //             subcmdConfig: subcmdLayerPart =>
            //             {
            //                 AddHeightmapSubcommand(subcmdLayerPart, "on", $"enable height layer {index} mask domain warp", "", 
            //                     _ => BetterContinents.Settings.BaseHeightNoise.NoiseLayers[index].maskWarpSettings ??= NoiseStackSettings.NoiseSettings.Default());
            //                 AddHeightmapSubcommand(subcmdLayerPart, "off", $"disable height layer {index} mask domain warp", "", 
            //                     _ => BetterContinents.Settings.BaseHeightNoise.NoiseLayers[index].maskWarpSettings = null);
            //                 AddNoiseCommands(
            //                     (cmd, desc, args, action, getValue) =>
            //                         AddHeightmapSubcommand(subcmdLayerPart, cmd, desc, args, action,
            //                             getValue),
            //                     () => BetterContinents.Settings.BaseHeightNoise.NoiseLayers[index].maskWarpSettings);
            //             });
            //     });
            // }
        });
        CommandWrapper.Register("bc", args => GetAutoComplete());
    }

    private static List<string> GetAutoComplete()
    {
        var text = Console.instance.m_input.text;
        // Empty part kept on purpose to detect when going to the next part.
        var parts = text.Split(' ');
        var cmd = rootCommand;
        for (int i = 1; i < parts.Length - 1; i++)
        {
            var subcmd = cmd.GetSubcommands().FirstOrDefault(s => s.cmd == parts[i]);
            if (subcmd == null)
                break;
            cmd = subcmd;
        }
        if (cmd.GetSubcommands().Count == 0)
            return CommandWrapper.Info(cmd.desc);
        return cmd.GetSubcommands().Select(s => s.cmd).ToList();
    }

    public static void RunConsoleCommand(string text)
    {
        rootCommand.Run(text);
    }
    private static Action<string> HeightmapCommand(Action<string> command) =>
        value =>
        {
            command(value);
            WorldGeneratorPatch.ApplyNoiseSettings();
            noisePreviewTextures = null;
            maskPreviewTextures = null;
            GameUtils.Reset();
        };

    private static Action<T> SetHeightmapValue<T>(Action<T> setValue) =>
        value =>
        {
            setValue(value);
            WorldGeneratorPatch.ApplyNoiseSettings();
            DynamicPatch();
            noisePreviewTextures = null;
            maskPreviewTextures = null;
            GameUtils.Reset();
        };

    private static readonly Command rootCommand;
    private static List<Texture> noisePreviewTextures = null;
    private static List<Texture> maskPreviewTextures = null;
    private static readonly List<bool> noisePreviewExpanded = [];

    private const int NoisePreviewSize = 512;
    private static (Texture noise, Texture mask) GetPreviewTextures(int layerIndex)
    {
        if (noisePreviewTextures == null)
        {
            var noise = BetterContinents.WorldGeneratorPatch.BaseHeightNoise;
            noisePreviewTextures = [];
            maskPreviewTextures = [];
            for (int i = 0; i < noise.layers.Count; i++)
            {
                noisePreviewTextures.Add(CreateNoisePreview((x, y) => noise.layers[i].noise.GetNoise(x, y),
                    NoisePreviewSize));
                maskPreviewTextures.Add(noise.layers[i].mask != null
                    ? CreateNoisePreview((x, y) => noise.layers[i].mask.Value.GetNoise(x, y), NoisePreviewSize)
                    : null);
            }

            noisePreviewTextures.Add(CreateNoisePreview((x, y) => noise.Apply(x, y), NoisePreviewSize));
            maskPreviewTextures.Add(null);
        }

        return (noisePreviewTextures[layerIndex], maskPreviewTextures[layerIndex]);
    }

    private static Texture CreateNoisePreview(Func<float, float, float> noiseFn, int size = 128)
    {
        var tex = new Texture2D(size, size);
        var pixels = new Color32[size * size];
        GameUtils.SimpleParallelFor(4, 0, size, y =>
        {
            float yp = 2f * (y / (float)size - 0.5f) * BetterContinents.TotalRadius;
            for (int x = 0; x < size; ++x)
            {
                float xp = 2f * (x / (float)size - 0.5f) * BetterContinents.TotalRadius;
                byte val = (byte)Mathf.Clamp((int)(noiseFn(xp, yp) * 255f), 0, 255);
                pixels[y * size + x] = new Color32(val, val, val, byte.MaxValue);
            }
        });

        tex.SetPixels32(pixels);
        tex.Apply(false);
        return tex;
    }

    private static void DrawNoisePreview(int i)
    {
        var (noiseTexture, _) = GetPreviewTextures(i);

        noisePreviewExpanded.Resize(Mathf.Max(noisePreviewExpanded.Count, noisePreviewTextures.Count));
        noisePreviewExpanded[i] = GUILayout.Toggle(noisePreviewExpanded[i], i == noisePreviewTextures.Count - 1 ? "Preview Final" : $"Preview Layer {i} Noise");
        if (noisePreviewExpanded[i])
        {
            GUILayout.Box(noiseTexture);
        }
    }

    private static void DrawMaskPreview(int i)
    {
        var (_, maskTexture) = GetPreviewTextures(i);
        noisePreviewExpanded.Resize(Mathf.Max(noisePreviewExpanded.Count, maskPreviewTextures.Count));
        noisePreviewExpanded[i] = GUILayout.Toggle(noisePreviewExpanded[i], i == maskPreviewTextures.Count - 1 ? "Preview Final Mask" : $"Preview Layer {i} Mask");
        if (noisePreviewExpanded[i])
        {
            GUILayout.Box(maskTexture);
        }
    }
}

#nullable enable