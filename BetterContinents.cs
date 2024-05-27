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

    public static ConfigEntry<int> ConfigBiomePrecision;
    public static ConfigEntry<string> ConfigBiomeFile;

    public static ConfigEntry<string> ConfigLocationFile;

    public static ConfigEntry<string> ConfigSpawnFile;
    public static ConfigEntry<string> ConfigPaintFile;

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
    public static ConfigEntry<bool> ConfigDebugSkipDefaultLocationPlacement;
    public static ConfigEntry<string> ConfigDebugResetCommand;

    public static BetterContinents instance;
#nullable enable
    public static float WorldSize = 10500f;
    public static float EdgeSize = 500f;
    public const string ConfigFileExtension = ".BetterContinents";
    public static string GetBCFile(string path) => path + ConfigFileExtension;
    private static readonly Vector2 Half = Vector2.one * 0.5f;
    private static float NormalizedX(float x) => x / (WorldSize * 2f) + 0.5f;
    private static float NormalizedY(float y) => y / (WorldSize * 2f) + 0.5f;
    private static Vector2 NormalizedToWorld(Vector2 p) => (p - Half) * WorldSize * 2f;
    private static Vector2 WorldToNormalized(float x, float y) => new(NormalizedX(x), NormalizedY(y));

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
            .AddGroup("BetterContinents.Global", groupBuilder =>
            {
                groupBuilder.AddValue("Enabled")
                    .Description("Whether this mod is enabled")
                    .Default(true).Bind(out ConfigEnabled);
                groupBuilder.AddValue("Continent Size")
                    .Description("Continent size")
                    .Default(0.5f).Range(0f, 1f).Bind(out ConfigContinentSize);
                groupBuilder.AddValue("Sea Level Adjustment")
                    .Description("Modify sea level, which changes the land:sea ratio")
                    .Default(0.5f).Range(0f, 1f).Bind(out ConfigSeaLevelAdjustment);
                groupBuilder.AddValue("Ocean Channels")
                    .Description("Whether ocean channels should be enabled or not (useful to disable when using height map for instance)")
                    .Default(true).Bind(out ConfigOceanChannelsEnabled);
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
            .AddGroup("BetterContinents.Project", groupBuilder =>
            {
                groupBuilder.AddValue("Directory")
                    .Description("This directory will load automatically any existing map files matching the correct names, overriding specific files specified below. Filenames must match: heightmap.png, biomemap.png, locationmap.png, roughmap.png, forestmap.png.")
                    .Default("").Bind(out ConfigMapSourceDir);
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
            })
            .AddGroup("BetterContinents.Roughmap", groupBuilder =>
            {
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
                    .Description("Adjusts how precisely terrain is matched to the biomemap (0 = vanilla, 1 = 3x3, 2 = 5x5, etc.)")
                    .Default(0).Range(0, 5).Bind(out ConfigBiomePrecision);
            })
            .AddGroup("BetterContinents.Forest", groupBuilder =>
            {
                groupBuilder.AddValue("Forest Scale")
                    .Description("Scales forested/cleared area size")
                    .Default(0.5f).Range(0f, 1f).Bind(out ConfigForestScale);
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
            .AddGroup("BetterContinents.Spawnmap", groupBuilder =>
            {
                groupBuilder.AddValue("Spawnmap File")
                    .Description("Legay path to a locationmap file to use. See the description on Nexusmods.com for the specifications (it will fail if they are not met)")
                    .Default("").Bind(out ConfigSpawnFile);
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
            .AddGroup("BetterContinents.Debug", groupBuilder =>
            {
                groupBuilder.AddValue("Debug Mode")
                    .Description("Automatically reveals the full map on respawn, enables cheat mode, and debug mode, for debugging purposes").Bind(out ConfigDebugModeEnabled);
                groupBuilder.AddValue("Skip Default Location Placement")
                    .Description("Skips default location placement during world gen (spawn temple and locationmap are still placed), for quickly testing the heightmap itself").Bind(out ConfigDebugSkipDefaultLocationPlacement);
                groupBuilder.AddValue("Debug Reset Command")
                    .Description("Upgrade World command to execute when reloading images.").Default("zones_reset start").Bind(out ConfigDebugResetCommand);
            })
            .AddGroup("BetterContinents.Misc", groupBuilder =>
            {
                groupBuilder.AddValue("NexusID")
                    .Hidden().Default(446).Bind(out NexusID);
                groupBuilder.AddValue("SelectedPreset")
                    .Hidden().Default("Vanilla").Bind(out ConfigSelectedPreset);
            })
            .AddGroup("BetterContinents.Locationmap", groupBuilder =>
            {
                groupBuilder.AddValue("Locationmap File")
                    .Description("Path to a locationmap file to use. See the description on Nexusmods.com for the specifications (it will fail if they are not met)")
                    .Default("").Bind(out ConfigLocationFile);
            })
            .AddGroup("BetterContinents.Paintmap", groupBuilder =>
            {
                groupBuilder.AddValue("Paintmap File")
                    .Description("Path to a paintmap file to use.")
                    .Default("").Bind(out ConfigPaintFile);
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

    public void OnGUI()
    {
        CommandWrapper.Init();
        UI.OnGUI();
    }

    // Debug mode helpers
    [HarmonyPatch(typeof(Minimap))]
    private class MinimapPatch
    {
        private static readonly Heightmap.Biome ForestableBiomes =
             Heightmap.Biome.Meadows |
             Heightmap.Biome.Mistlands |
             Heightmap.Biome.Mountain |
             Heightmap.Biome.Plains |
             Heightmap.Biome.Swamp |
             Heightmap.Biome.BlackForest
         ;

        [HarmonyPrefix, HarmonyPatch(nameof(Minimap.GetMaskColor))]
        private static bool GetMaskColorPrefix(float wx, float wy, Heightmap.Biome biome, ref Color __result, Color ___noForest, Color ___forest)
        {
            if (Settings.EnabledForThisWorld && Settings.ForestFactorOverrideAllTrees && (biome & ForestableBiomes) != 0)
            {
                float forestFactor = WorldGenerator.GetForestFactor(new Vector3(wx, 0f, wy));
                float limit = biome == Heightmap.Biome.Plains ? 0.8f : 1.15f;
                __result = forestFactor < limit ? ___forest : ___noForest;
                return false;
            }
            return true;
        }

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
            int halfSize = map.m_textureSize / 2;
            float halfSizeF = map.m_pixelSize / 2f;
            var mapPixels = new Color32[map.m_textureSize * map.m_textureSize];
            var forestPixels = new Color32[map.m_textureSize * map.m_textureSize];
            var heightPixels = new Color[map.m_textureSize * map.m_textureSize];
            var cachedTexture = new Color32[map.m_textureSize * map.m_textureSize];
            var half = 127.5f;
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
                        heightPixels[i * map.m_textureSize + j] = new Color(biomeHeight, 0f, 0f);

                        var num = Mathf.Clamp((int)(biomeHeight * half), 0, 65025);
                        var r = (byte)(num >> 8);
                        var g = (byte)(num & 255);
                        cachedTexture[i * map.m_textureSize + j] = new(r, g, 0, byte.MaxValue);
                        Interlocked.Increment(ref progress);
                    }
                });
            });

            try
            {
                UI.Add("GeneratingMinimap", () =>
                {
                    int percentProgress = (int)(100 * ((float)progress / (map.m_textureSize * map.m_textureSize)));
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
            Texture2D cached = new(map.m_textureSize, map.m_textureSize);
            cached.SetPixels32(cachedTexture);
            cached.Apply();

            Log($"Finished generating minimap textures multi-threaded ...");
            map.SaveMapTextureDataToDisk(map.m_forestMaskTexture, map.m_mapTexture, cached);
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
        static bool Prefix(Minimap __instance, ref bool __result)
        {
            __result = TryLoadMinimapTextureData(__instance);
            return false;
        }

        private static bool TryLoadMinimapTextureData(Minimap obj)
        {
            if (string.IsNullOrEmpty(obj.m_forestMaskTexturePath) || !File.Exists(obj.m_forestMaskTexturePath) || !File.Exists(obj.m_mapTexturePath) || !File.Exists(obj.m_heightTexturePath) || 33 != ZNet.World.m_worldVersion)
            {
                return false;
            }
            Stopwatch stopwatch = Stopwatch.StartNew();
            Texture2D texture2D = new Texture2D(obj.m_forestMaskTexture.width, obj.m_forestMaskTexture.height, TextureFormat.ARGB32, false);
            if (!texture2D.LoadImage(File.ReadAllBytes(obj.m_forestMaskTexturePath)))
                return false;
            if (obj.m_forestMaskTexture.width != texture2D.width || obj.m_forestMaskTexture.height != texture2D.height)
                return false;
            obj.m_forestMaskTexture.SetPixels(texture2D.GetPixels());
            obj.m_forestMaskTexture.Apply();
            if (!texture2D.LoadImage(File.ReadAllBytes(obj.m_mapTexturePath)))
                return false;
            if (obj.m_mapTexture.width != texture2D.width || obj.m_mapTexture.height != texture2D.height)
                return false;
            obj.m_mapTexture.SetPixels(texture2D.GetPixels());
            obj.m_mapTexture.Apply();
            if (!texture2D.LoadImage(File.ReadAllBytes(obj.m_heightTexturePath)))
                return false;
            if (obj.m_heightTexture.width != texture2D.width || obj.m_heightTexture.height != texture2D.height)
                return false;
            Color[] pixels = texture2D.GetPixels();
            for (int i = 0; i < obj.m_textureSize; i++)
            {
                for (int j = 0; j < obj.m_textureSize; j++)
                {
                    int num = i * obj.m_textureSize + j;
                    int num2 = (int)(pixels[num].r * 255f);
                    int num3 = (int)(pixels[num].g * 255f);
                    int num4 = (num2 << 8) + num3;
                    float num5 = 127.5f;
                    pixels[num].r = (float)num4 / num5;
                }
            }
            obj.m_heightTexture.SetPixels(pixels);
            obj.m_heightTexture.Apply();
            ZLog.Log("Loading minimap textures done [" + stopwatch.ElapsedMilliseconds.ToString() + "ms]");
            return true;
        }
    }
}

