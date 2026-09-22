using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace BetterContinents;

[BepInPlugin("BetterContinents", ModInfo.Name, ModInfo.Version)]
public partial class BetterContinents : BaseUnityPlugin
{
#nullable disable
    public static Harmony HarmonyInstance;
    // See the Awake function for the config descriptions
    public static ConfigEntry<int> NexusID;
    public static ConfigEntry<string> ConfigSelectedPreset;

    public static ConfigEntry<bool> ConfigEnabled;

    public static ConfigEntry<float> ConfigContinentSize;
    public static ConfigEntry<float> ConfigSeaLevelAdjustment;
    public static ConfigEntry<bool> ConfigOceanChannelsEnabled;
    public static ConfigEntry<bool> ConfigAshlandsGapEnabled;
    public static ConfigEntry<float> ConfigWorldSize;
    public static ConfigEntry<float> ConfigEdgeSize;
    public static ConfigEntry<bool> ConfigFixWaterColor;

    public static ConfigEntry<bool> ConfigDeepNorthGapEnabled;

    public static ConfigEntry<bool> ConfigRiversEnabled;
    public static ConfigEntry<bool> ConfigMapEdgeDropoff;
    public static ConfigEntry<bool> ConfigMountainsAllowedAtCenter;

    public static ConfigEntry<string> ConfigMapSourceDir;

    public static ConfigEntry<string> ConfigHeightFile;
    public static ConfigEntry<float> ConfigHeightmapAmount;
    public static ConfigEntry<float> ConfigHeightmapBlend;
    public static ConfigEntry<float> ConfigHeightmapAdd;
    public static ConfigEntry<float> ConfigHeightmapMask;
    public static ConfigEntry<bool> ConfigHeightmapOverrideAll;
    public static ConfigEntry<bool> ConfigHeightmapAlpha;

    public static ConfigEntry<int> ConfigBiomePrecision;
    public static ConfigEntry<string> ConfigBiomeFile;

    public static ConfigEntry<string> ConfigLocationFile;

    public static ConfigEntry<string> ConfigSpawnFile;

    public static ConfigEntry<string> ConfigTerrainFile;
    public static ConfigEntry<string> ConfigPaintFile;
    public static ConfigEntry<string> ConfigLavaFile;
    public static ConfigEntry<string> ConfigMossFile;
    public static ConfigEntry<string> ConfigVegetationFile;


    public static ConfigEntry<string> ConfigRoughFile;
    public static ConfigEntry<float> ConfigRoughmapBlend;

    public static ConfigEntry<float> ConfigForestScale;
    public static ConfigEntry<float> ConfigForestAmount;
    public static ConfigEntry<bool> ConfigForestFactorOverrideAllTrees;
    public static ConfigEntry<string> ConfigForestFile;
    public static ConfigEntry<float> ConfigForestmapMultiply;
    public static ConfigEntry<float> ConfigForestmapAdd;

    public static ConfigEntry<bool> ConfigOverrideStartPosition;
    public static ConfigEntry<float> ConfigStartPositionX;
    public static ConfigEntry<float> ConfigStartPositionY;

    public static ConfigEntry<bool> ConfigDebugModeEnabled;
    public static ConfigEntry<bool> ConfigSkipDefaultLocations;
    public static ConfigEntry<string> ConfigDebugResetCommand;
    public static ConfigEntry<string> ConfigOverrideVersion;

    public static ConfigEntry<float> ConfigHeatScale;
    public static ConfigEntry<string> ConfigHeatFile;

    public static BetterContinents instance;
#nullable enable
    public static void SetSize(float size, float edge)
    {
        Log($"Received world size {size} and edge size {edge}");
        TotalRadius = size + edge;
        TotalSize = TotalRadius * 2f;
        WorldRadius = size;
        WorldGeneratorPatch.ApplyNoiseSettings();
    }
    public static float TotalRadius = 10500f;
    public static float TotalSize = TotalRadius * 2f;
    public static float WorldRadius = 10000f;
    public const string ConfigFileExtension = ".BetterContinents";
    public static string GetBCFile(string path) => Path.ChangeExtension(path, ConfigFileExtension);
    public static string GetLegacyBCFile(string path) => Path.ChangeExtension(path, ".fwl" + ConfigFileExtension);

    // Valheim 1.0 stores every world as a directory - worlds_local/<name>/ holding _main.N.fwl2, .db2,
    // .chunks, .ok plus the chunk files - and that directory is the unit the save system works on:
    // SaveSystem.Delete, Copy and RenameDirectory all act on SaveFile.ChunkedDirectory. Writing our
    // settings inside it means they travel with the world through every copy, move, rename, backup and
    // delete with no save-system patching at all.
    //
    // The name is deliberately extension-less, matching the game's own cacheMinimapBiome / cacheMinimapHeight
    // / cacheMinimapMask / cacheMinimapMeta sitting in the same folder. SaveCollection.Reload scans every
    // file under worlds_local recursively, and SaveSystem.GetSaveInfo returns false as soon as a file has no
    // extension, so an extension-less file is simply invisible to the scanner.
    //
    // Two tempting alternatives are actively destructive and must not be used:
    //   - a name starting with "_main." joins the rolling-save group, and SaveCollection.KeepOnlyNewest
    //     deletes any such group that is not exactly four files. A fifth file makes it delete the world.
    //   - "<worldname>.BetterContinents" is seen as a second save sharing the world's name, and
    //     SaveWithBackups.EnsureSortedAndPrimaryFileDetermined resolves that by moving the older of the two
    //     aside - which renames the real world folder to <name>_backup_<timestamp> and makes the world
    //     vanish from the world list.
    public const string ConfigFileName = "BetterContinents";
    public static string GetWorldBCFile(string worldName, FileHelpers.FileSource fileSource) =>
      SaveSystem.GetWorldsSaveRootPath(fileSource) + "/" + worldName + "/" + ConfigFileName;
    private static readonly Vector2 Half = Vector2.one * 0.5f;
    private static float Normalize(float x) => Mathf.Clamp(x / TotalSize + 0.5f, 0f, 1f);
    private static Vector2 NormalizedToWorld(Vector2 p) => (p - Half) * TotalSize;

    public static void Log(string msg) => Debug.Log($"[BetterContinents] {msg}");
    public static void LogError(string msg) => Debug.LogError($"[BetterContinents] {msg}");
    public static void LogWarning(string msg) => Debug.LogWarning($"[BetterContinents] {msg}");

    public static bool AllowDebugActions => ZNet.instance
                                            && ZNet.instance.IsServer()
                                            && Settings.EnabledForThisWorld
                                            && ConfigDebugModeEnabled.Value;

    public static BetterContinentsSettings Settings = new();


    public void Awake()
    {
        instance = this;

        // Cos why...
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        // Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);

        Console.SetConsoleEnabled(true);

        Config.Declare()
            .AddGroup("BetterContinents.Debug", groupBuilder =>
            {
                groupBuilder.AddValue("Enabled")
                    .Description("Whether this mod is enabled")
                    .Default(true).Bind(out ConfigEnabled);
                groupBuilder.AddValue("Debug Mode")
                    .Description("Automatically reveals the full map on respawn, enables cheat mode, and debug mode, for debugging purposes").Bind(out ConfigDebugModeEnabled);
                groupBuilder.AddValue("Debug Reset Command")
                    .Description("Upgrade World command to execute when reloading images.").Default("zones_reset start").Bind(out ConfigDebugResetCommand);
                groupBuilder.AddValue("Override version")
                    .Description("Override the save version")
                    .Default("").Bind(out ConfigOverrideVersion);
                groupBuilder.AddValue("Directory")
                    .Description("This directory will load automatically any existing map files matching the correct names, overriding specific files specified below. Filenames must match: heightmap.png, biomemap.png, locationmap.png, roughmap.png, forestmap.png, vegetationmap.png, spawnmap.png.")
                    .Default("").Bind(out ConfigMapSourceDir);
            })
            .AddGroup("BetterContinents.Global", groupBuilder =>
            {
                groupBuilder.AddValue("Skip Default Locations")
                  .Description("Skips the default location placement. Spawn temple and location map are still placed.").Bind(out ConfigSkipDefaultLocations);
                groupBuilder.AddValue("Continent Size")
                    .Description("Continent size")
                    .Default(0.5f).Range(0f, 1f).Bind(out ConfigContinentSize);
                groupBuilder.AddValue("World Size")
                    .Description("World radius in meter")
                    .Default(10000f).Bind(out ConfigWorldSize);
                groupBuilder.AddValue("Edge Size")
                    .Description("Edge size in meters")
                    .Default(500f).Bind(out ConfigEdgeSize);
                groupBuilder.AddValue("Fix Water Color")
                    .Description("Whether to fix the water color")
                    .Default(true).Bind(out ConfigFixWaterColor);
                groupBuilder.AddValue("Sea Level Adjustment")
                    .Description("Modify sea level, which changes the land:sea ratio")
                    .Default(0.5f).Range(0f, 1f).Bind(out ConfigSeaLevelAdjustment);
                groupBuilder.AddValue("Ocean Channels")
                    .Description("Whether ocean channels should be enabled or not (useful to disable when using height map for instance)")
                    .Default(true).Bind(out ConfigOceanChannelsEnabled);
                groupBuilder.AddValue("Ashlands Gap")
                    .Description("Whether to add the Ashlands ocean gap (usually custom maps don't need this)")
                    .Default(false).Bind(out ConfigAshlandsGapEnabled);
                groupBuilder.AddValue("Deep North Gap")
                    .Description("Whether to add the Deep North ocean gap (usually custom maps don't need this)")
                    .Default(false).Bind(out ConfigDeepNorthGapEnabled);
                groupBuilder.AddValue("Rivers")
                    .Description("Whether rivers should be enabled or not")
                    .Default(true).Bind(out ConfigRiversEnabled);
                groupBuilder.AddValue("Map Edge Drop-off")
                    .Description("Whether the map should drop off at the edges or not (consequences unknown!)")
                    .Default(true).Bind(out ConfigMapEdgeDropoff);
                groupBuilder.AddValue("Mountains Allowed At Center")
                    .Description("Whether the map should allow mountains to occur at the map center (if you have default spawn then you should keep this unchecked)")
                    .Default(false).Bind(out ConfigMountainsAllowedAtCenter);
            })
            .AddGroup("BetterContinents.Heightmap", groupBuilder =>
            {
                groupBuilder.AddValue("Heightmap File")
                    .Description("Path to a heightmap file to use. See the description on Nexusmods.com for the specifications (it will fail if they are not met)")
                    .Default("").Bind(out ConfigHeightFile);
                groupBuilder.AddValue("Heightmap Amount")
                    .Description("Multiplier of the height value from the heightmap file (more than 1 leads to higher max height than vanilla, good results are not guaranteed)")
                    .Default(1f).Range(0f, 5f).Bind(out ConfigHeightmapAmount);
                groupBuilder.AddValue("Heightmap Blend")
                    .Description("How strongly to blend the heightmap file into the final result")
                    .Default(1f).Range(0f, 1f).Bind(out ConfigHeightmapBlend);
                groupBuilder.AddValue("Heightmap Add")
                    .Description("How strongly to add the heightmap file to the final result (usually you want to blend it instead)")
                    .Default(0f).Range(-1f, 1f).Bind(out ConfigHeightmapAdd);
                groupBuilder.AddValue("Heightmap Mask")
                    .Description("How strongly to apply the heightmap as a mask on normal height generation (i.e. it limits maximum height to the height of the mask)")
                    .Default(0f).Range(0f, 1f).Bind(out ConfigHeightmapMask);
                groupBuilder.AddValue("Heightmap Override All")
                    .Description("All other aspects of the height calculation will be disabled, so the world will perfectly conform to your heightmap")
                    .Default(true).Bind(out ConfigHeightmapOverrideAll);
                groupBuilder.AddValue("Heightmap Alpha")
                    .Description("Enables alpha channel for the heightmap file to blend vanilla generation with the heightmap")
                    .Default(false).Bind(out ConfigHeightmapAlpha);
                groupBuilder.AddValue("Roughmap File")
                    .Description("Path to a roughmap file to use. See the description on Nexusmods.com for the specifications (it will fail if they are not met)")
                    .Default("").Bind(out ConfigRoughFile);
                groupBuilder.AddValue("Roughmap Blend")
                    .Description("How strongly to apply the roughmap file")
                    .Default(1f).Range(0f, 1f).Bind(out ConfigRoughmapBlend);
            })
            .AddGroup("BetterContinents.Biomemap", groupBuilder =>
            {
                groupBuilder.AddValue("Biomemap File")
                    .Description("Path to a biomemap file to use. See the description on Nexusmods.com for the specifications (it will fail if they are not met)")
                    .Default("").Bind(out ConfigBiomeFile);
                groupBuilder.AddValue("Biome precision")
                    .Description("Not working! Adjusts how precisely terrain is matched to the biomemap (0 = vanilla, 1 = 3x3, 2 = 5x5, etc.)")
                    .Default(0).Range(0, 5).Bind(out ConfigBiomePrecision);
                groupBuilder.AddValue("Terrainmap file")
                    .Description("Path to a terrainmap file to use. See thea description on Nexusmods.com for the specifications (it will fail if they are not met)")
                    .Default("").Bind(out ConfigTerrainFile);
            })
            .AddGroup("BetterContinents.Forest", groupBuilder =>
            {
                groupBuilder.AddValue("Forest Scale")
                    .Description("Scales forested/cleared area size")
                    .Default(1f).Range(0f, 10f).Bind(out ConfigForestScale);
                groupBuilder.AddValue("Forest Amount")
                    .Description("Adjusts how much forest there is, relative to clearings")
                    .Default(0.5f).Range(0f, 1f).Bind(out ConfigForestAmount);
                groupBuilder.AddValue("Forest Factor Overrides All Trees")
                    .Description("Trees in all biomes will be affected by forest factor (both procedural and from forestmap)")
                    .Default(false).Bind(out ConfigForestFactorOverrideAllTrees);
                groupBuilder.AddValue("Forestmap File")
                    .Description("Path to a forestmap file to use. See the description on Nexusmods.com for the specifications (it will fail if they are not met)")
                    .Default("").Bind(out ConfigForestFile);
                groupBuilder.AddValue("Forestmap Multiply")
                    .Description("How strongly to scale the vanilla forest factor by the forestmap")
                    .Default(1f).Range(0f, 1f).Bind(out ConfigForestmapMultiply);
                groupBuilder.AddValue("Forestmap Add")
                    .Description("How strongly to add the forestmap directly to the vanilla forest factor")
                    .Default(1f).Range(0f, 1f).Bind(out ConfigForestmapAdd);
            })
            .AddGroup("BetterContinents.StartPosition", groupBuilder =>
            {
                groupBuilder.AddValue("Override Start Position")
                    .Description("Whether to override the start position using the values provided (warning: will disable all validation of the position)")
                    .Default(false).Bind(out ConfigOverrideStartPosition);
                groupBuilder.AddValue("Start Position X")
                    .Description("Start position override X value, in ranges -10500 to 10500")
                    .Default(0f).Range(-10500f, 10500f).Bind(out ConfigStartPositionX);
                groupBuilder.AddValue("Start Position Y")
                    .Description("Start position override Y value, in ranges -10500 to 10500")
                    .Default(0f).Range(-10500f, 10500f).Bind(out ConfigStartPositionY);
            })
            .AddGroup("BetterContinents.Maps", groupBuilder =>
            {
                groupBuilder.AddValue("Locationmap File")
                    .Description("Path to a locationmap file to use. See the description on Nexusmods.com for the specifications (it will fail if they are not met)")
                    .Default("").Bind(out ConfigLocationFile);
                groupBuilder.AddValue("Spawnmap File")
                .Description("Legay path to a locationmap file to use. See the description on Nexusmods.com for the specifications (it will fail if they are not met)")
                .Default("").Bind(out ConfigSpawnFile);
                groupBuilder.AddValue("Vegetationmap File")
                      .Description("Path to a vegetationmap file to use.")
                      .Default("").Bind(out ConfigVegetationFile);
                groupBuilder.AddValue("Paintmap File")
                .Description("Path to a paintmap file to use.")
                .Default("").Bind(out ConfigPaintFile);
                groupBuilder.AddValue("Lavamap File")
                   .Description("Path to a lavamap file to use.")
                   .Default("").Bind(out ConfigLavaFile);
                groupBuilder.AddValue("Mossmap File")
                 .Description("Path to a mossmap file to use.")
                 .Default("").Bind(out ConfigMossFile);
                groupBuilder.AddValue("Heatmap File")
                  .Description("Path to a heatmap file to use.")
                  .Default("").Bind(out ConfigHeatFile);
                groupBuilder.AddValue("Heatmap Scale")
                    .Description("Multiplies the heatmap color value. Most heat effects cap at 1 value.")
                    .Default(10f).Range(0f, 100f).Bind(out ConfigHeatScale);
            })
            .AddGroup("BetterContinents.Misc", groupBuilder =>
            {
                groupBuilder.AddValue("NexusID")
                    .Hidden().Default(446).Bind(out NexusID);
                groupBuilder.AddValue("SelectedPreset")
                    .Hidden().Default("Vanilla").Bind(out ConfigSelectedPreset);
            });
        if (ConfigLocationFile.Value == "" && ConfigSpawnFile.Value != "")
        {
            ConfigLocationFile.Value = ConfigSpawnFile.Value.Replace("spawnmap", "locationmap");
            ConfigSpawnFile.Value = "";
            Config.Save();
        }
        HarmonyInstance = new Harmony("BetterContinents.Harmony");
        HarmonyInstance.PatchAll();
        Log("Awake");
        UI.Init();
    }

    public void Start()
    {
        EWD.Run();
    }

    public void OnGUI()
    {
        CommandWrapper.Init();
        UI.OnGUI();
    }

    // Debug mode helpers
    [HarmonyPatch(typeof(Minimap))]
    private class MinimapPatch
    {
        // Minimap.GetMaskColor is deliberately NOT patched any more.
        //
        // The mask texture is not a simple "is there forest here" flag: 1.0 packs a different
        // meaning into each channel per biome. Red is the forest overlay (Meadows / Plains /
        // BlackForest), green carries the Mistlands mist density, blue carries the Ashlands
        // ocean gradient below the waterline and GetAshlandsHeight's mask.a above it, and
        // Swamp / Mountain / Deep North deliberately get no mask at all. Vanilla also returns
        // the ocean gradient for every pixel under 30 m before it ever looks at the biome.
        //
        // Better Continents already patches WorldGenerator.GetForestFactor, and vanilla's
        // InForest is just GetForestFactor(pos) < 1.15f, so vanilla's own GetMaskColor reads
        // Better Continents' forest values and paints the correct channel for free. The old
        // prefix collapsed six biomes onto the red forest channel, which drew Swamp, Mountain
        // and Mistlands as Black Forest and threw away the Ashlands and underwater gradients.

        // Some map mods may do stuff after generation which won't work with async.
        // So do one "fake" generate call to trigger those.
        static bool DoFakeGenerate = false;
        [HarmonyPrefix, HarmonyPatch(nameof(Minimap.GenerateWorldMap))]
        private static bool GenerateWorldMapPrefix(Minimap __instance)
        {
            if (DoFakeGenerate)
            {
                DoFakeGenerate = false;
                return false;
            }
            __instance.StartCoroutine(GenerateWorldMapMT(__instance));
            return false;
        }

        private static IEnumerator GenerateWorldMapMT(Minimap map)
        {
            Log($"Generating minimap textures multi-threaded ...");
            // The vanilla GenerateWorldMap is skipped, so the cache has to be invalidated here instead.
            // Otherwise an interrupted generation leaves a cache that still passes the seed and version checks.
            Minimap.DeleteMapTextureData(ZNet.World.m_name);
            int halfSize = map.m_textureSize / 2;
            float halfSizeF = map.m_pixelSize / 2f;
            var mapPixels = new Color32[map.m_textureSize * map.m_textureSize];
            var forestPixels = new Color32[map.m_textureSize * map.m_textureSize];
            var heightPixels = new Color[map.m_textureSize * map.m_textureSize];
            var cachedHeights = new float[map.m_textureSize * map.m_textureSize];
            int progress = 0;
            var task = Task.Run(() =>
            {
                GameUtils.SimpleParallelFor(4, 0, map.m_textureSize, i =>
                {
                    for (int j = 0; j < map.m_textureSize; j++)
                    {
                        float wx = (j - halfSize) * map.m_pixelSize + halfSizeF;
                        float wy = (i - halfSize) * map.m_pixelSize + halfSizeF;
                        var biome = WorldGenerator.instance.GetBiome(wx, wy);
                        float biomeHeight = WorldGenerator.instance.GetBiomeHeight(biome, wx, wy, out _);
                        mapPixels[i * map.m_textureSize + j] = map.GetPixelColor(biome);
                        forestPixels[i * map.m_textureSize + j] = map.GetMaskColor(wx, wy, biomeHeight, biome);
                        // Alpha 0, not the 1 the three-argument Color constructor gives: vanilla
                        // fills this array by assigning .r onto a default Color, so its alpha is 0.
                        heightPixels[i * map.m_textureSize + j] = new Color(biomeHeight, 0f, 0f, 0f);
                        cachedHeights[i * map.m_textureSize + j] = biomeHeight;
                    }
                    // Updated every row, because every pixel is pointless for a percentage.
                    Interlocked.Increment(ref progress);
                });
            });

            try
            {
                UI.Add("GeneratingMinimap", () =>
                {
                    int percentProgress = (int)(100 * ((float)progress / map.m_textureSize));
                    UI.DisplayMessage($"Better Continents: generating minimap {percentProgress}% ...");
                });
                yield return new WaitUntil(() => task.IsCompleted);
            }
            finally
            {
                UI.Remove("GeneratingMinimap");
            }

            map.m_forestMaskTexture.SetPixels32(forestPixels);
            map.m_forestMaskTexture.Apply();
            map.m_mapTexture.SetPixels32(mapPixels);
            map.m_mapTexture.Apply();
            map.m_heightTexture.SetPixels(heightPixels);
            map.m_heightTexture.Apply();

            Log($"Finished generating minimap textures multi-threaded ...");
            if (FileHelpers.LocalStorageSupport == LocalStorageSupport.Supported)
            {
                // This also writes the meta file with the world seed and the cache version.
                map.SaveMapTextureDataToDisk(forestPixels, mapPixels, cachedHeights);
            }
            // Some map mods may do stuff after generation which won't work with async.
            // So do one "fake" generate call to trigger those.
            DoFakeGenerate = true;
            map.GenerateWorldMap();
        }
    }

    // Cache might have wrong map size so has to be fully reimplemented.
    // This could be transpiled too but more complex.
    [HarmonyPatch(typeof(Minimap), nameof(Minimap.TryLoadMinimapTextureData))]
    public class PatchTryLoadMinimapTextureData
    {
        static bool Prefix(Minimap __instance, int worldSeed, ref bool __result)
        {
            __result = TryLoadMinimapTextureData(__instance, worldSeed);
            return false;
        }

        private static bool TryLoadMinimapTextureData(Minimap obj, int worldSeed)
        {
            if (string.IsNullOrEmpty(obj.m_cachedMinimapMaskTexturePath)
                || !File.Exists(obj.m_cachedMinimapMaskTexturePath)
                || !File.Exists(obj.m_cachedMinimapBiomeTexturePath)
                || !File.Exists(obj.m_cachedMinimapHeightTexturePath)
                || !File.Exists(obj.m_cachedMinimapMetaPath)
                || Version.World.DeepNorth != ZNet.World.m_worldVersion)
            {
                return false;
            }
            try
            {
                var meta = File.ReadAllBytes(obj.m_cachedMinimapMetaPath);
                if (BitConverter.ToInt32(meta, 0) != worldSeed)
                {
                    Log("Cached minimap is for another seed, regenerating it.");
                    return false;
                }
                var cacheVersion = (Version.CachedMinimap)BitConverter.ToInt32(meta, 4);
                if (cacheVersion != Version.CachedMinimap.Original)
                {
                    Log($"Cached minimap version changed from {cacheVersion} to {Version.CachedMinimap.Original}, regenerating it.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogWarning($"Error loading the cached minimap meta data: {ex.Message}");
                return false;
            }
            Stopwatch stopwatch = Stopwatch.StartNew();
            int pixels = obj.m_textureSize * obj.m_textureSize;
            try
            {
                var forestPixels = Utils.CompressedBufferToColors(File.ReadAllBytes(obj.m_cachedMinimapMaskTexturePath));
                var mapPixels = Utils.CompressedBufferToColors(File.ReadAllBytes(obj.m_cachedMinimapBiomeTexturePath));
                var heightPixels = Utils.CompressedHalfBufferToRedChannel(File.ReadAllBytes(obj.m_cachedMinimapHeightTexturePath));
                // The meta data doesn't store the map size, so it has to be checked here.
                if (forestPixels.Length != pixels || mapPixels.Length != pixels || heightPixels.Length != pixels)
                {
                    Log("Cached minimap has a different size, regenerating it.");
                    return false;
                }
                obj.m_forestMaskTexture.SetPixels32(forestPixels);
                obj.m_forestMaskTexture.Apply();
                obj.m_mapTexture.SetPixels32(mapPixels);
                obj.m_mapTexture.Apply();
                obj.m_heightTexture.SetPixels(heightPixels);
                obj.m_heightTexture.Apply();
            }
            catch (Exception ex)
            {
                LogWarning($"Error loading the cached minimap textures: {ex.Message}");
                return false;
            }
            ZLog.Log("Loading minimap textures done [" + stopwatch.ElapsedMilliseconds.ToString() + "ms]");
            return true;
        }
    }
}
