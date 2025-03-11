using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace BetterContinents;

public partial class BetterContinents
{
    // Changes to height, biome, forests, rivers etc. (this is the functional part of the mod)
    [HarmonyPatch(typeof(WorldGenerator))]
    public class WorldGeneratorPatch
    {
        private static readonly string[] TreePrefixes =
        [
                "FirTree",
                "Pinetree_01",
                "SwampTree2_darkland",
                "SwampTree1",
                "SwampTree2",
                "FirTree_small",
                "FirTree_small_dead",
                "HugeRoot1",
                "SwampTree2_log",
                "FirTree_oldLog",
                "vertical_web",
                "horizontal_web",
                "tunnel_web",
            ];

        private static int currentSeed;

        // Hardcoded gaps don't work well with the mod when often the whole world layout is changed.
        public static bool DisableGap(ref double __result)
        {
            __result = 1d;
            return false;
        }

        //private static Noise 
        [HarmonyPrefix, HarmonyPatch(nameof(WorldGenerator.Initialize))]
        private static void InitializePrefix(World world)
        {
            if (Settings.EnabledForThisWorld && !world.m_menu && Settings.ForestFactorOverrideAllTrees && ZoneSystem.instance != null)
            {
                foreach (var v in ZoneSystem.instance.m_vegetation)
                {
                    if (TreePrefixes.Contains(v.m_prefab.name))
                    {
                        v.m_inForest = true;
                        v.m_forestTresholdMin = 0f;
                        v.m_forestTresholdMax = 1.15f;
                    }
                }
            }

            if (Settings.EnabledForThisWorld)
            {
                currentSeed = world.m_seed;
                ApplyNoiseSettings();
            }
        }

        public static NoiseStack? BaseHeightNoise;

        public static void ApplyNoiseSettings()
        {
            BaseHeightNoise = new NoiseStack(TotalSize, currentSeed, Settings.BaseHeightNoise);
        }

        // wx, wy are [-10500, 10500]
        // __result should be [0, 1]
        public static bool GetBaseHeightPrefixV3(ref float wx, ref float wy, ref float __result, float ___m_minMountainDistance)
        {
            __result = GetBaseHeightV3(wx, wy, ___m_minMountainDistance);
            return false;
        }
        public static bool GetBaseHeightPrefixV2(ref float wx, ref float wy, ref float __result, float ___m_offset0, float ___m_offset1, float ___m_minMountainDistance)
        {
            __result = GetBaseHeightV2(wx, wy, ___m_offset0, ___m_offset1, ___m_minMountainDistance);
            return false;
        }
        public static bool GetBaseHeightPrefixV1(ref float wx, ref float wy, ref float __result, float ___m_offset0, float ___m_offset1, float ___m_minMountainDistance)
        {
            __result = GetBaseHeightV1(wx, wy, ___m_offset0, ___m_offset1, ___m_minMountainDistance);
            return false;
        }


#pragma warning disable IDE0060
        public static float GetBiomeHeightWithHeightPaint(float result, WorldGenerator __instance, Heightmap.Biome biome, ref Color mask, float wx, float wy)
        {
            Settings.ApplyPaintMap(wx, wy, biome, ref mask);
            return __instance.GetBaseHeight(wx, wy, false) * 200f;
        }
        public static float GetBiomeHeightWithHeight(float result, WorldGenerator __instance, float wx, float wy)
        {
            return __instance.GetBaseHeight(wx, wy, false) * 200f;
        }
#pragma warning restore IDE0060
        public static float GetBiomeHeightWithRoughPaint(float result, WorldGenerator __instance, Heightmap.Biome biome, ref Color mask, float wx, float wy)
        {
            var smoothHeight = __instance.GetBaseHeight(wx, wy, false) * 200f;
            Settings.ApplyPaintMap(wx, wy, biome, ref mask);
            return Settings.ApplyRoughmap(NormalizedX(wx), NormalizedY(wy), smoothHeight, result);
        }
        public static float GetBiomeHeightWithRough(float result, WorldGenerator __instance, ref Color mask, float wx, float wy)
        {
            var smoothHeight = __instance.GetBaseHeight(wx, wy, false) * 200f;
            return Settings.ApplyRoughmap(NormalizedX(wx), NormalizedY(wy), smoothHeight, result);
        }
        public static void GetBiomeHeightWithPaint(ref Color mask, Heightmap.Biome biome, float wx, float wy)
        {
            Settings.ApplyPaintMap(wx, wy, biome, ref mask);
        }

        public static void GetAshlandsHeight(ref Color mask, float wx, float wy)
        {
            Settings.ApplyPaintMap(wx, wy, Heightmap.Biome.AshLands, ref mask);
        }
        private static float GetBaseHeightV1(float wx, float wy, float ___m_offset0, float ___m_offset1, float ___m_minMountainDistance)
        {
            float distance = Utils.Length(wx, wy);

            // The base map x, y coordinates in 0..1 range
            float mapX = NormalizedX(wx);
            float mapY = NormalizedY(wy);

            wx *= Settings.GlobalScale;
            wy *= Settings.GlobalScale;

            float WarpScale = 0.001f * Settings.RidgeScale;

            float warpX = (Mathf.PerlinNoise(wx * WarpScale, wy * WarpScale) - 0.5f) * TotalRadius;
            float warpY = (Mathf.PerlinNoise(wx * WarpScale + 2f, wy * WarpScale + 3f) - 0.5f) * TotalRadius;

            wx += 100000f + ___m_offset0;
            wy += 100000f + ___m_offset1;

            float bigFeatureNoiseHeight = Mathf.PerlinNoise(wx * 0.002f * 0.5f, wy * 0.002f * 0.5f) * Mathf.PerlinNoise(wx * 0.003f * 0.5f, wy * 0.003f * 0.5f) * 1f;
            float bigFeatureHeight = Settings.ApplyHeightmap(mapX, mapY, bigFeatureNoiseHeight);
            float ridgeHeight = Mathf.PerlinNoise(warpX * 0.002f * 0.5f, warpY * 0.002f * 0.5f) * Mathf.PerlinNoise(warpX * 0.003f * 0.5f, warpY * 0.003f * 0.5f) * Settings.MaxRidgeHeight;

            // https://www.desmos.com/calculator/uq8wmu6dy7
            float SigmoidActivation(float x, float a, float b) => 1 / (1 + Mathf.Exp(a + b * x));
            float lerp = Settings.ShouldHeightMapOverrideAll
                ? 0
                : Mathf.Clamp01(SigmoidActivation(Mathf.PerlinNoise(wx * 0.005f - 10000, wy * 0.005f - 5000) - Settings.RidgeBlendSigmoidXOffset, 0, Settings.RidgeBlendSigmoidB));

            float finalHeight = 0f;

            float bigFeature = Mathf.Clamp01(Mathf.Lerp(bigFeatureHeight, ridgeHeight, lerp));
            const float SeaLevel = 0.05f;
            float ApplyMountains(float x, float n) => x * (1 - Mathf.Pow(1 - x, 1.2f + n * 0.8f)) + x * (1 - x);

            finalHeight += ApplyMountains(bigFeature - SeaLevel, Settings.MountainsAmount) + SeaLevel;

            finalHeight += Mathf.PerlinNoise(wx * 0.002f * 1f, wy * 0.002f * 1f) * Mathf.PerlinNoise(wx * 0.003f * 1f, wy * 0.003f * 1f) * finalHeight * 0.9f;

            finalHeight += Mathf.PerlinNoise(wx * 0.005f * 1f, wy * 0.005f * 1f) * Mathf.PerlinNoise(wx * 0.01f * 1f, wy * 0.01f * 1f) * 0.5f * finalHeight;

            finalHeight -= 0.07f;

            finalHeight += Settings.SeaLevelAdjustment;

            if (Settings.OceanChannelsEnabled && !Settings.ShouldHeightMapOverrideAll)
            {
                float v = Mathf.Abs(
                    Mathf.PerlinNoise(wx * 0.002f * 0.25f + 0.123f, wy * 0.002f * 0.25f + 0.15123f) -
                    Mathf.PerlinNoise(wx * 0.002f * 0.25f + 0.321f, wy * 0.002f * 0.25f + 0.231f));
                finalHeight *= 1f - (1f - Utils.LerpStep(0.02f, 0.12f, v)) *
                    Utils.SmoothStep(744f, 1000f, distance);
            }

            // Edge of the world
            if (distance > WorldRadius)
            {
                float t = Utils.LerpStep(WorldRadius, TotalRadius, distance);
                finalHeight = Mathf.Lerp(finalHeight, -0.2f, t);
                var edge = TotalRadius - 10;
                if (distance > edge)
                {
                    float t2 = Utils.LerpStep(edge, TotalRadius, distance);
                    finalHeight = Mathf.Lerp(finalHeight, -2f, t2);
                }
            }
            if (distance < ___m_minMountainDistance && finalHeight > 0.28f && !Settings.ShouldHeightMapOverrideAll)
            {
                float t3 = Mathf.Clamp01((finalHeight - 0.28f) / 0.099999994f);
                finalHeight = Mathf.Lerp(Mathf.Lerp(0.28f, 0.38f, t3), finalHeight, Utils.LerpStep(___m_minMountainDistance - 400f, ___m_minMountainDistance, distance));
            }
            return finalHeight;
        }

        private static float GetBaseHeightV2(float wx, float wy, float ___m_offset0, float ___m_offset1, float ___m_minMountainDistance)
        {
            float distance = Utils.Length(wx, wy);

            // The base map x, y coordinates in 0..1 range
            float mapX = NormalizedX(wx);
            float mapY = NormalizedY(wy);

            wx *= Settings.GlobalScale;
            wy *= Settings.GlobalScale;

            float WarpScale = 0.001f * Settings.RidgeScale;

            float warpX = (Mathf.PerlinNoise(wx * WarpScale, wy * WarpScale) - 0.5f) * TotalRadius;
            float warpY = (Mathf.PerlinNoise(wx * WarpScale + 2f, wy * WarpScale + 3f) - 0.5f) * TotalRadius;

            wx += 100000f + ___m_offset0;
            wy += 100000f + ___m_offset1;

            float bigFeatureNoiseHeight = Mathf.PerlinNoise(wx * 0.002f * 0.5f, wy * 0.002f * 0.5f) * Mathf.PerlinNoise(wx * 0.003f * 0.5f, wy * 0.003f * 0.5f) * 1f;
            float bigFeatureHeight = Settings.ApplyHeightmap(mapX, mapY, bigFeatureNoiseHeight);
            float ridgeHeight = (Mathf.PerlinNoise(warpX * 0.002f * 0.5f, warpY * 0.002f * 0.5f) * Mathf.PerlinNoise(warpX * 0.003f * 0.5f, warpY * 0.003f * 0.5f)) * Settings.MaxRidgeHeight;

            // https://www.desmos.com/calculator/uq8wmu6dy7
            float SigmoidActivation(float x, float a, float b) => 1 / (1 + Mathf.Exp(a + b * x));
            float lerp = Mathf.Clamp01(SigmoidActivation(Mathf.PerlinNoise(wx * 0.005f - 10000, wy * 0.005f - 5000) - Settings.RidgeBlendSigmoidXOffset, 0, Settings.RidgeBlendSigmoidB));

            float bigFeature = Mathf.Clamp01(bigFeatureHeight + ridgeHeight * lerp);

            const float SeaLevel = 0.05f;
            float ApplyMountains(float x, float n) => x * (1 - Mathf.Pow(1 - x, 1.2f + n * 0.8f)) + x * (1 - x);

            float detailedFinalHeight = ApplyMountains(bigFeature - SeaLevel, Settings.MountainsAmount) + SeaLevel;

            // Finer height variation
            detailedFinalHeight += Mathf.PerlinNoise(wx * 0.002f * 1f, wy * 0.002f * 1f) * Mathf.PerlinNoise(wx * 0.003f * 1f, wy * 0.003f * 1f) * detailedFinalHeight * 0.9f;
            detailedFinalHeight += Mathf.PerlinNoise(wx * 0.005f * 1f, wy * 0.005f * 1f) * Mathf.PerlinNoise(wx * 0.01f * 1f, wy * 0.01f * 1f) * 0.5f * detailedFinalHeight;

            float finalHeight = Settings.ApplyFlatmap(mapX, mapY, bigFeatureHeight, detailedFinalHeight);

            finalHeight -= 0.07f;

            finalHeight += Settings.SeaLevelAdjustment;

            if (Settings.OceanChannelsEnabled)
            {
                float v = Mathf.Abs(
                    Mathf.PerlinNoise(wx * 0.002f * 0.25f + 0.123f, wy * 0.002f * 0.25f + 0.15123f) -
                    Mathf.PerlinNoise(wx * 0.002f * 0.25f + 0.321f, wy * 0.002f * 0.25f + 0.231f));
                finalHeight *= 1f - (1f - Utils.LerpStep(0.02f, 0.12f, v)) *
                    Utils.SmoothStep(744f, 1000f, distance);
            }

            // Edge of the world
            if (!Settings.DisableMapEdgeDropoff && distance > WorldRadius)
            {
                float t = Utils.LerpStep(WorldRadius, TotalRadius, distance);
                finalHeight = Mathf.Lerp(finalHeight, -0.2f, t);
                var edge = TotalRadius - 10;
                if (distance > edge)
                {
                    float t2 = Utils.LerpStep(edge, TotalRadius, distance);
                    finalHeight = Mathf.Lerp(finalHeight, -2f, t2);
                }
            }

            // Avoid mountains in the center
            if (!Settings.MountainsAllowedAtCenter && distance < ___m_minMountainDistance && finalHeight > 0.28f)
            {
                float t3 = Mathf.Clamp01((finalHeight - 0.28f) / 0.099999994f);
                finalHeight = Mathf.Lerp(Mathf.Lerp(0.28f, 0.38f, t3), finalHeight, Utils.LerpStep(___m_minMountainDistance - 400f, ___m_minMountainDistance, distance));
            }
            return finalHeight;
        }

        private static float GetBaseHeightV3(float wx, float wy, float ___m_minMountainDistance)
        {
            float distance = Utils.Length(wx, wy);

            // The base map x, y coordinates in 0..1 range
            float mapX = NormalizedX(wx);
            float mapY = NormalizedY(wy);

            float baseHeight = Settings.ApplyHeightmap(mapX, mapY, 0f);
            float finalHeight = BaseHeightNoise?.Apply(wx, wy, baseHeight) ?? 0;
            finalHeight -= 0.15f; // Resulting in about 30% water coverage by default
            finalHeight += Settings.SeaLevelAdjustment;

            // Edge of the world
            if (!Settings.DisableMapEdgeDropoff && distance > WorldRadius)
            {
                float t = Utils.LerpStep(WorldRadius, TotalRadius, distance);
                finalHeight = Mathf.Lerp(finalHeight, -0.2f, t);
                var edge = TotalRadius - 10;
                if (distance > edge)
                {
                    float t2 = Utils.LerpStep(edge, TotalRadius, distance);
                    finalHeight = Mathf.Lerp(finalHeight, -2f, t2);
                }
            }

            // Avoid mountains in the center
            if (!Settings.MountainsAllowedAtCenter && distance < ___m_minMountainDistance && finalHeight > 0.28f)
            {
                float t3 = Mathf.Clamp01((finalHeight - 0.28f) / 0.099999994f);
                finalHeight = Mathf.Lerp(Mathf.Lerp(0.28f, 0.38f, t3), finalHeight, Utils.LerpStep(___m_minMountainDistance - 400f, ___m_minMountainDistance, distance));
            }
            return finalHeight;
        }

        public static bool GetBiomePrefix(float wx, float wy, ref Heightmap.Biome __result, World ___m_world)
        {
            var normalized = WorldToNormalized(wx, wy);
            var result = Settings.GetBiomeOverride(normalized.x, normalized.y);
            if (result != Heightmap.Biome.None)
                __result = result;
            return false;
        }

        public static bool AddRiversPrefix(ref float __result, float h)
        {
            __result = h;
            return false;
        }

        public static void GetForestFactorPrefix(ref Vector3 pos)
        {
            pos *= Settings.ForestScale;
        }

        // Range: 0.145071 1.850145
        public static void GetForestFactorPostfix(Vector3 pos, ref float __result)
        {
            if (Settings.ForestScale != 1f)
                pos /= Settings.ForestScale;
            __result = Settings.ApplyForest(NormalizedX(pos.x), NormalizedY(pos.z), __result);
        }

        public static bool GetAshlandsOceanGradientPrefix(float x, float y, ref float __result)
        {
            // Ships take damage even with 0 heat, so the small subtraction is needed to turn zero to slightly negative.
            __result = Settings.ApplyHeatmap(NormalizedX(x), NormalizedY(y)) - 0.00001f;
            return false;
        }

        public static bool IsAshlandsPrefix(float x, float y, ref bool __result)
        {
            var heat = Settings.ApplyHeatmap(NormalizedX(x), NormalizedY(y));
            __result = heat > 0f;
            return false;
        }
        // Usually lava requires heat so this is a fallback solution when people are using biome map but no heat map.
        public static bool IsAshlandsFallbackPrefix(WorldGenerator __instance, float x, float y, ref bool __result)
        {
            __result = __instance.GetBiome(x, y) == Heightmap.Biome.AshLands;
            return false;
        }
    }
}
