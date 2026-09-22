using System.Collections;
using System.Reflection;
using HarmonyLib;

namespace BetterContinents;

public partial class BetterContinents
{
  public static void DynamicPatch()
  {
    PatchHeightmap();
    PatchBiomeColor();
    PatchGetBaseHeight();
    PatchGetBiomeHeight();
    PatchGetBiome();
    PatchAddRivers();
    PatchForestFactorPrefix();
    PatchForestFactorPostfix();
    PatchHeatPrefix();
    PatchWorldSize();
    PatchAshlandGap();
    PatchDeepNorthGap();
    PatchIsAshlands();
    PatchIsAshlandsFallback();
    PatchIsDeepnorth();
    PatchDeepNorthWaveFade();
    PatchGetAshlandsHeight();
    PatchColorTransition();
    PatchVegetationMap();
    PatchSpawnMap();
    // WorldGenerator caches GetBiome/GetBiomeArea results per grid cell for the lifetime of the
    // WorldGenerator instance (only cleared in its constructor). Any biome-affecting patch toggled
    // above (PatchGetBiome, PatchIsAshlands, PatchIsAshlandsFallback) can leave already-queried cells
    // returning their pre-patch answer for the rest of the session unless we clear the caches here.
    ClearWorldGeneratorBiomeCaches();
  }

  // AccessTools lookups return null instead of throwing when a member can't be found; Harmony then
  // throws when Patch()/Unpatch() is called with a null target, which would abort every remaining
  // Patch* call in DynamicPatch() for that cycle. Guard each lookup so a missing target logs a named
  // failure (and only disables that one feature) instead of silently killing everything after it.
  private static bool EnsurePatchTargetFound(object member, string description)
  {
    if (member == null)
    {
      LogError($"Could not find {description} - this BetterContinents feature will not work.");
      return false;
    }
    return true;
  }

  private static readonly FieldInfo CachedBiomeAreasField = AccessTools.Field(typeof(WorldGenerator), "s_cachedBiomeAreas");
  private static readonly FieldInfo CachedBiomesField = AccessTools.Field(typeof(WorldGenerator), "s_cachedBiomes");
  private static bool LoggedMissingBiomeCacheFields = false;

  private static void ClearWorldGeneratorBiomeCaches()
  {
    if (WorldGenerator.instance == null)
      return;
    if (CachedBiomeAreasField == null || CachedBiomesField == null)
    {
      if (!LoggedMissingBiomeCacheFields)
      {
        LogError("Could not find WorldGenerator.s_cachedBiomeAreas/s_cachedBiomes - biome caches may go stale after repatching.");
        LoggedMissingBiomeCacheFields = true;
      }
      return;
    }
    (CachedBiomeAreasField.GetValue(null) as IDictionary)?.Clear();
    (CachedBiomesField.GetValue(null) as IDictionary)?.Clear();
  }
  private static int HeightmapGetBiomePatched = 0;
  private static void PatchHeightmap()
  {
    var precision = Settings.EnabledForThisWorld ? Settings.BiomePrecision : 0;
    if (precision == HeightmapGetBiomePatched)
      return;
    Log($"Note: Biome precision feature doesn't work at the moment.");
    HeightmapGetBiomePatched = precision;
    return;
    /*
    var method1 = AccessTools.Method(typeof(Heightmap), nameof(Heightmap.GetBiome));
    var patch1 = AccessTools.Method(typeof(BetterContinents), nameof(GetBiomePatch));
    var method2 = AccessTools.Method(typeof(HeightmapBuilder), nameof(HeightmapBuilder.Build));
    var patch2 = AccessTools.Method(typeof(BetterContinents), nameof(BuildPatch));
    var method3 = AccessTools.Method(typeof(Heightmap), nameof(Heightmap.GetBiomeColor), [typeof(float), typeof(float)]);
    var patch3 = AccessTools.Method(typeof(BetterContinents), nameof(GetBiomeColorPatch));
    if (HeightmapGetBiomePatched > 0)
    {
      Log("Unpatching Heightmap.GetBiome");
      HarmonyInstance.Unpatch(method1, patch1);
      HarmonyInstance.Unpatch(method2, patch2);
      HarmonyInstance.Unpatch(method3, patch3);
      HeightmapGetBiomePatched = 0;
    }
    if (precision > 0)
    {
      Log($"Patching Heightmap.GetBiome with precision {precision}");
      HarmonyInstance.Patch(method1, prefix: new(patch1));
      HarmonyInstance.Patch(method2, prefix: new(patch2));
      HarmonyInstance.Patch(method3, prefix: new(patch3));
      HeightmapGetBiomePatched = precision;
    }
    foreach (Heightmap hm in Heightmap.Instances)
    {
      hm.m_buildData = null;
      hm.Regenerate();
    }
    ClutterSystem.instance?.ClearAll();
    */
  }

  private static bool BiomeColorPatched = false;
  private static void PatchBiomeColor()
  {
    var toPatch = Settings.EnabledForThisWorld && Settings.HasTerrainMap;
    if (toPatch == BiomeColorPatched)
      return;
    var method = AccessTools.Method(typeof(Heightmap), nameof(Heightmap.GetBiomeColor), [typeof(float), typeof(float)]);
    var patch = AccessTools.Method(typeof(BetterContinents), nameof(GetBiomeColorPatch));
    if (!EnsurePatchTargetFound(method, "Heightmap.GetBiomeColor(float,float)"))
      return;
    if (BiomeColorPatched)
    {
      Log("Unpatching Heightmap.GetBiomeColor");
      HarmonyInstance.Unpatch(method, patch);
      BiomeColorPatched = false;
    }
    if (toPatch)
    {
      Log("Patching Heightmap.GetBiomeColor");
      HarmonyInstance.Patch(method, prefix: new(patch));
      BiomeColorPatched = true;
    }
  }

  private static int GetBaseHeightPatched = 0;
  private static void PatchGetBaseHeight()
  {
    var patchVersion = 0;
    if (Settings.EnabledForThisWorld)
    {
      switch (Settings.Version)
      {
        case 1:
        case 2:
          patchVersion = 1;
          break;
        case 3:
        case 4:
        case 5:
        case 6:
          patchVersion = 2;
          break;
        default:
          // GetBaseHeightV3 doesn't work at all without heightmap which makes testing more difficult.
          if (Settings.HasHeightMap || Settings.BaseHeightNoise.NoiseLayers.Count > 0)
            patchVersion = 3;
          break;
      }
    }

    if (patchVersion == GetBaseHeightPatched)
      return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetBaseHeight));
    var patch1 = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.GetBaseHeightPrefixV1));
    var patch2 = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.GetBaseHeightPrefixV2));
    var patch3 = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.GetBaseHeightPrefixV3));
    if (!EnsurePatchTargetFound(method, "WorldGenerator.GetBaseHeight"))
      return;
    if (GetBaseHeightPatched == 1)
    {
      Log("Unpatching WorldGenerator.GetBaseHeight V1");
      HarmonyInstance.Unpatch(method, patch1);
      GetBaseHeightPatched = 0;
    }
    if (GetBaseHeightPatched == 2)
    {
      Log("Unpatching WorldGenerator.GetBaseHeight V2");
      HarmonyInstance.Unpatch(method, patch2);
      GetBaseHeightPatched = 0;
    }
    if (GetBaseHeightPatched == 3)
    {
      Log("Unpatching WorldGenerator.GetBaseHeight");
      HarmonyInstance.Unpatch(method, patch3);
      GetBaseHeightPatched = 0;
    }
    if (patchVersion == 1)
    {
      Log($"Patching WorldGenerator.GetBaseHeight V1");
      HarmonyInstance.Patch(method, prefix: new(patch1));
      GetBaseHeightPatched = 1;
    }
    if (patchVersion == 2)
    {
      Log($"Patching WorldGenerator.GetBaseHeight V2");
      HarmonyInstance.Patch(method, prefix: new(patch2));
      GetBaseHeightPatched = 2;
    }
    if (patchVersion == 3)
    {
      Log($"Patching WorldGenerator.GetBaseHeight");
      HarmonyInstance.Patch(method, prefix: new(patch3));
      GetBaseHeightPatched = 3;
    }
  }

  // Three different patches for different cases.
  private static bool GetBiomeHeightWithRoughPatched = false;
  private static bool GetBiomeHeightWithRoughPaintPatched = false;
  private static bool GetBiomeHeightWithHeightPatched = false;
  private static bool GetBiomeHeightWithHeightPaintPatched = false;
  private static bool GetBiomeHeightWithPaintPatched = false;


  private static void PatchGetBiomeHeight()
  {
    var toHeightPaintPatch = Settings.EnabledForThisWorld;
    var toHeightPatch = Settings.EnabledForThisWorld;
    var toRoughPaintPatch = Settings.EnabledForThisWorld;
    var toRoughPatch = Settings.EnabledForThisWorld;
    var toPaintPatch = Settings.EnabledForThisWorld;
    if (Settings.HasPaintMap || Settings.HasLavaMap || Settings.HasMossMap || Settings.HasVegetationMap)
    {
      toHeightPatch = false;
      toRoughPatch = false;
    }
    else
    {
      toHeightPaintPatch = false;
      toRoughPaintPatch = false;
      toPaintPatch = false;
    }
    if (Settings.ShouldHeightMapOverrideAll)
    {
      toRoughPaintPatch = false;
      toRoughPatch = false;
    }
    else
    {
      toHeightPaintPatch = false;
      toHeightPatch = false;
    }
    if (!Settings.HasRoughMap)
    {
      toRoughPaintPatch = false;
      toRoughPatch = false;
    }

    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight));
    // 5 different patches depending what is needed.
    var patchRough = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.GetBiomeHeightWithRough));
    var patchRoughPaint = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.GetBiomeHeightWithRoughPaint));
    var patchHeight = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.GetBiomeHeightWithHeight));
    var patchHeightPaint = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.GetBiomeHeightWithHeightPaint));
    var patchPeint = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.GetBiomeHeightWithPaint));

    if (!EnsurePatchTargetFound(method, "WorldGenerator.GetBiomeHeight"))
      return;

    if (toRoughPatch != GetBiomeHeightWithRoughPatched)
    {
      if (GetBiomeHeightWithRoughPatched)
      {
        Log("Unpatching WorldGenerator.GetBiomeHeight with rough");
        HarmonyInstance.Unpatch(method, patchRough);
        GetBiomeHeightWithRoughPatched = false;
      }
      if (toRoughPatch)
      {
        Log("Patching WorldGenerator.GetBiomeHeight with rough");
        HarmonyInstance.Patch(method, postfix: new(patchRough));
        GetBiomeHeightWithRoughPatched = true;
      }
    }
    if (toRoughPaintPatch != GetBiomeHeightWithRoughPaintPatched)
    {
      if (GetBiomeHeightWithRoughPaintPatched)
      {
        Log("Unpatching WorldGenerator.GetBiomeHeight with rough and paint");
        HarmonyInstance.Unpatch(method, patchRoughPaint);
        GetBiomeHeightWithRoughPaintPatched = false;
      }
      if (toRoughPaintPatch)
      {
        Log("Patching WorldGenerator.GetBiomeHeight with rough and paint");
        HarmonyInstance.Patch(method, postfix: new(patchRoughPaint));
        GetBiomeHeightWithRoughPaintPatched = true;
      }
    }
    if (toHeightPatch != GetBiomeHeightWithHeightPatched)
    {
      if (GetBiomeHeightWithHeightPatched)
      {
        Log("Unpatching WorldGenerator.GetBiomeHeight with height");
        HarmonyInstance.Unpatch(method, patchHeight);
        GetBiomeHeightWithHeightPatched = false;
      }
      if (toHeightPatch)
      {
        Log("Patching WorldGenerator.GetBiomeHeight with height");
        HarmonyInstance.Patch(method, postfix: new(patchHeight));
        GetBiomeHeightWithHeightPatched = true;
      }
    }
    if (toHeightPaintPatch != GetBiomeHeightWithHeightPaintPatched)
    {
      if (GetBiomeHeightWithHeightPaintPatched)
      {
        Log("Unpatching WorldGenerator.GetBiomeHeight with height and paint");
        HarmonyInstance.Unpatch(method, patchHeightPaint);
        GetBiomeHeightWithHeightPaintPatched = false;
      }
      if (toHeightPaintPatch)
      {
        Log("Patching WorldGenerator.GetBiomeHeight with height and paint");
        HarmonyInstance.Patch(method, postfix: new(patchHeightPaint));
        GetBiomeHeightWithHeightPaintPatched = true;
      }
    }
    if (toPaintPatch != GetBiomeHeightWithPaintPatched)
    {
      if (GetBiomeHeightWithPaintPatched)
      {
        Log("Unpatching WorldGenerator.GetBiomeHeight with paint");
        HarmonyInstance.Unpatch(method, patchPeint);
        GetBiomeHeightWithPaintPatched = false;
      }
      if (toPaintPatch)
      {
        Log("Patching WorldGenerator.GetBiomeHeight with paint");
        HarmonyInstance.Patch(method, postfix: new(patchPeint));
        GetBiomeHeightWithPaintPatched = true;
      }
    }

  }

  private static bool GetAshlandsHeightPatched = false;

  private static void PatchGetAshlandsHeight()
  {
    // Mossmap is not needed as it doesn't apply to Ashlands.
    var toPatch = Settings.EnabledForThisWorld && (Settings.HasPaintMap || Settings.HasLavaMap);
    if (toPatch == GetAshlandsHeightPatched)
      return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetAshlandsHeight));
    var patch = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.GetAshlandsHeight));
    if (!EnsurePatchTargetFound(method, "WorldGenerator.GetAshlandsHeight"))
      return;
    if (GetAshlandsHeightPatched)
    {
      Log("Unpatching WorldGenerator.GetAshlandsHeight");
      HarmonyInstance.Unpatch(method, patch);
      GetAshlandsHeightPatched = false;
    }
    if (toPatch)
    {
      Log("Patching WorldGenerator.GetAshlandsHeight");
      HarmonyInstance.Patch(method, postfix: new(patch));
      GetAshlandsHeightPatched = true;
    }
  }

  private static void PatchWorldSize()
  {
    if (!Settings.EnabledForThisWorld)
      WorldSizeHelper.PatchEdgeChecks(HarmonyInstance, 10000f, 500f);
    else if (Settings.DisableMapEdgeDropoff)
      // Easiest to just apply very large values to disable the feature.
      WorldSizeHelper.PatchEdgeChecks(HarmonyInstance, 1E30f, 500f);
    else
      WorldSizeHelper.PatchEdgeChecks(HarmonyInstance, Settings.WorldSize, Settings.EdgeSize);

    if (!Settings.EnabledForThisWorld)
      WorldSizeHelper.PatchWorldSize(HarmonyInstance, 10000f, 500f);
    else
      WorldSizeHelper.PatchWorldSize(HarmonyInstance, Settings.WorldSize, Settings.EdgeSize);
  }

  private static bool GetBiomePatched = false;
  private static void PatchGetBiome()
  {
    var toPatch = Settings.EnabledForThisWorld && Settings.HasBiomeMap;
    if (toPatch == GetBiomePatched)
      return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetBiome), [typeof(float), typeof(float), typeof(float), typeof(bool)]);
    var patch = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.GetBiomePrefix));
    if (!EnsurePatchTargetFound(method, "WorldGenerator.GetBiome(float,float,float,bool)"))
      return;
    if (GetBiomePatched)
    {
      Log("Unpatching WorldGenerator.GetBiome");
      HarmonyInstance.Unpatch(method, patch);
      GetBiomePatched = false;
    }
    if (toPatch)
    {
      Log("Patching WorldGenerator.GetBiome");
      HarmonyInstance.Patch(method, prefix: new(patch));
      GetBiomePatched = true;
    }
  }
  private static bool AddRiversPAtched = false;
  private static void PatchAddRivers()
  {
    var toPatch = Settings.EnabledForThisWorld && !Settings.RiversEnabled;
    if (toPatch == AddRiversPAtched)
      return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.AddRivers));
    var patch = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.AddRiversPrefix));
    if (!EnsurePatchTargetFound(method, "WorldGenerator.AddRivers"))
      return;
    if (AddRiversPAtched)
    {
      Log("Unpatching WorldGenerator.AddRivers");
      HarmonyInstance.Unpatch(method, patch);
      AddRiversPAtched = false;
    }
    if (toPatch)
    {
      Log("Patching WorldGenerator.AddRivers");
      HarmonyInstance.Patch(method, prefix: new(patch));
      AddRiversPAtched = true;
    }
  }
  private static bool ForestFactorPrefixPatched = false;
  private static void PatchForestFactorPrefix()
  {
    var toPatch = Settings.EnabledForThisWorld && Settings.ForestScale != 1f;
    if (toPatch == ForestFactorPrefixPatched)
      return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetForestFactor));
    var patch = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.GetForestFactorPrefix));
    if (!EnsurePatchTargetFound(method, "WorldGenerator.GetForestFactor"))
      return;
    if (ForestFactorPrefixPatched)
    {
      Log("Unpatching WorldGenerator.GetForestFactor prefix");
      HarmonyInstance.Unpatch(method, patch);
      ForestFactorPrefixPatched = false;
    }
    if (toPatch)
    {
      Log("Patching WorldGenerator.GetForestFactor prefix");
      HarmonyInstance.Patch(method, prefix: new(patch));
      ForestFactorPrefixPatched = true;
    }
  }
  private static bool ForestFactorPostfixPatched = false;
  private static void PatchForestFactorPostfix()
  {
    var toPatch = Settings.EnabledForThisWorld && (Settings.HasForestMap || Settings.ForestAmountOffset != 0f);
    if (toPatch == ForestFactorPostfixPatched)
      return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetForestFactor));
    var patch = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.GetForestFactorPostfix));
    if (!EnsurePatchTargetFound(method, "WorldGenerator.GetForestFactor"))
      return;
    if (ForestFactorPostfixPatched)
    {
      Log("Unpatching WorldGenerator.GetForestFactor postfix");
      HarmonyInstance.Unpatch(method, patch);
      ForestFactorPostfixPatched = false;
    }
    if (toPatch)
    {
      Log("Patching WorldGenerator.GetForestFactor postfix");
      HarmonyInstance.Patch(method, postfix: new(patch));
      ForestFactorPostfixPatched = true;
    }
  }

  private static bool HeatPrefixPatched = false;
  private static void PatchHeatPrefix()
  {
    var toPatch = Settings.EnabledForThisWorld && Settings.HasHeatMap && Settings.HeatMapScale > 0f;
    if (toPatch == HeatPrefixPatched)
      return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetAshlandsOceanGradient), [typeof(float), typeof(float)]);
    var patch = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.GetAshlandsOceanGradientPrefix));
    if (!EnsurePatchTargetFound(method, "WorldGenerator.GetAshlandsOceanGradient(float,float)"))
      return;
    if (HeatPrefixPatched)
    {
      Log("Unpatching WorldGenerator.GetAshlandsOceanGradient prefix");
      HarmonyInstance.Unpatch(method, patch);
      HeatPrefixPatched = false;
    }
    if (toPatch)
    {
      Log("Patching WorldGenerator.GetAshlandsOceanGradient prefix");
      HarmonyInstance.Patch(method, prefix: new(patch));
      HeatPrefixPatched = true;
    }
  }
  private static bool AshlandsGapPatched = false;

  private static void PatchAshlandGap()
  {
    var toPatch = Settings.EnabledForThisWorld && !Settings.AshlandsGapEnabled;
    if (toPatch == AshlandsGapPatched)
      return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.CreateAshlandsGap));
    var patch = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.DisableGap));
    if (!EnsurePatchTargetFound(method, "WorldGenerator.CreateAshlandsGap"))
      return;
    if (AshlandsGapPatched)
    {
      Log("Unpatching WorldGenerator.CreateAshlandsGap");
      HarmonyInstance.Unpatch(method, patch);
      AshlandsGapPatched = false;
    }
    if (toPatch)
    {
      Log("Patching WorldGenerator.CreateAshlandsGap");
      HarmonyInstance.Patch(method, prefix: new(patch));
      AshlandsGapPatched = true;
    }
  }

  private static bool DeepNorthGapPatched = false;
  private static void PatchDeepNorthGap()
  {
    var toPatch = Settings.EnabledForThisWorld && !Settings.DeepNorthGapEnabled;
    if (toPatch == DeepNorthGapPatched)
      return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.CreateDeepNorthGap));
    var patch = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.DisableGap));
    if (!EnsurePatchTargetFound(method, "WorldGenerator.CreateDeepNorthGap"))
      return;
    if (DeepNorthGapPatched)
    {
      Log("Unpatching WorldGenerator.CreateDeepNorthGap");
      HarmonyInstance.Unpatch(method, patch);
      DeepNorthGapPatched = false;
    }
    if (toPatch)
    {
      Log("Patching WorldGenerator.CreateDeepNorthGap");
      HarmonyInstance.Patch(method, prefix: new(patch));
      DeepNorthGapPatched = true;
    }
  }

  private static bool IsAshlandsPatched = false;
  private static void PatchIsAshlands()
  {
    var toPatch = Settings.EnabledForThisWorld && Settings.HasHeatMap && Settings.HeatMapScale > 0f;
    if (toPatch == IsAshlandsPatched)
      return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.IsAshlands));
    var patch = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.IsAshlandsPrefix));
    if (!EnsurePatchTargetFound(method, "WorldGenerator.IsAshlands"))
      return;
    if (IsAshlandsPatched)
    {
      Log("Unpatching WorldGenerator.IsAshlands");
      HarmonyInstance.Unpatch(method, patch);
      IsAshlandsPatched = false;
    }
    if (toPatch)
    {
      Log("Patching WorldGenerator.IsAshlands");
      HarmonyInstance.Patch(method, prefix: new(patch, Priority.VeryHigh));
      IsAshlandsPatched = true;
    }
  }
  private static bool IsAshlandsFallbackPatched = false;
  private static void PatchIsAshlandsFallback()
  {
    var toPatch = Settings.EnabledForThisWorld && Settings.HasBiomeMap && (!Settings.HasHeatMap || Settings.HeatMapScale == 0f);
    if (toPatch == IsAshlandsFallbackPatched)
      return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.IsAshlands));
    var patch = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.IsAshlandsFallbackPrefix));
    if (!EnsurePatchTargetFound(method, "WorldGenerator.IsAshlands"))
      return;
    if (IsAshlandsFallbackPatched)
    {
      Log("Unpatching WorldGenerator.IsAshlands (no heat map)");
      HarmonyInstance.Unpatch(method, patch);
      IsAshlandsFallbackPatched = false;
    }
    if (toPatch)
    {
      Log("Patching WorldGenerator.IsAshlands (no heat map)");
      HarmonyInstance.Patch(method, prefix: new(patch, Priority.VeryHigh));
      IsAshlandsFallbackPatched = true;
    }
  }
  private static bool IsDeepnorthPatched = false;
  // Valheim 1.0 made Deep North a real biome with its own terrain, weather and snow behaviour, but vanilla
  // still decides where it IS from a hardcoded geographic test (WorldGenerator.IsDeepnorth). That test feeds
  // EnvMan's weather selection, TerrainComp's snow-vs-cultivate painting, stream placement and vanilla's own
  // GetBiome fallback - none of which consult the biome map. Without this, a biome map that moves Deep North
  // produces Deep North terrain that still has the wrong weather and paints the wrong ground when cultivated.
  // This mirrors PatchIsAshlandsFallback; there is no heat-map condition because heat is Ashlands-only.
  private static void PatchIsDeepnorth()
  {
    var toPatch = Settings.EnabledForThisWorld && Settings.HasBiomeMap;
    if (toPatch == IsDeepnorthPatched)
      return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.IsDeepnorth));
    var patch = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.IsDeepnorthPrefix));
    if (!EnsurePatchTargetFound(method, "WorldGenerator.IsDeepnorth"))
      return;
    if (IsDeepnorthPatched)
    {
      Log("Unpatching WorldGenerator.IsDeepnorth");
      HarmonyInstance.Unpatch(method, patch);
      IsDeepnorthPatched = false;
    }
    if (toPatch)
    {
      Log("Patching WorldGenerator.IsDeepnorth");
      HarmonyInstance.Patch(method, prefix: new(patch, Priority.VeryHigh));
      IsDeepnorthPatched = true;
    }
  }
  private static bool DeepNorthWaveFadePatched = false;
  // Follows PatchIsDeepnorth: the wave fade uses the same hardcoded Deep North circle, so a biome map that
  // moves the biome has to move the calm water with it, or the sea stays flat over open ocean and choppy in
  // the new Deep North.
  private static void PatchDeepNorthWaveFade()
  {
    var toPatch = Settings.EnabledForThisWorld && Settings.HasBiomeMap;
    if (toPatch == DeepNorthWaveFadePatched)
      return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.DeepNorthWaveFade));
    var patch = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.DeepNorthWaveFadePrefix));
    if (!EnsurePatchTargetFound(method, "WorldGenerator.DeepNorthWaveFade"))
      return;
    if (DeepNorthWaveFadePatched)
    {
      Log("Unpatching WorldGenerator.DeepNorthWaveFade");
      HarmonyInstance.Unpatch(method, patch);
      DeepNorthWaveFadePatched = false;
    }
    if (toPatch)
    {
      Log("Patching WorldGenerator.DeepNorthWaveFade");
      HarmonyInstance.Patch(method, prefix: new(patch, Priority.VeryHigh));
      DeepNorthWaveFadePatched = true;
    }
  }
  private static bool IsVegetationMapPatched = false;
  private static void PatchVegetationMap()
  {
    var toPatch = Settings.EnabledForThisWorld && Settings.HasVegetationMap;
    if (toPatch == IsVegetationMapPatched)
      return;
    var method = AccessTools.Method(typeof(ZoneSystem), nameof(ZoneSystem.PlaceVegetation));
    var prefixPatch = AccessTools.Method(typeof(ZoneSystemPatch), nameof(ZoneSystemPatch.PlaceVegetationEnable));
    var postfixPatch = AccessTools.Method(typeof(ZoneSystemPatch), nameof(ZoneSystemPatch.PlaceVegetationRestore));
    var transpilerPatch = AccessTools.Method(typeof(ZoneSystemPatch), nameof(ZoneSystemPatch.PlaceVegetationSaveCurrent));
    var clearAreaMethod = AccessTools.Method(typeof(ZoneSystem), nameof(ZoneSystem.InsideClearArea));
    var clearAreaPatch = AccessTools.Method(typeof(ZoneSystemPatch), nameof(ZoneSystemPatch.CheckVegetationMapClearArea));
    if (!EnsurePatchTargetFound(method, "ZoneSystem.PlaceVegetation") || !EnsurePatchTargetFound(clearAreaMethod, "ZoneSystem.InsideClearArea"))
      return;
    if (IsVegetationMapPatched)
    {
      Log("Unpatching ZoneSystem.PlaceVegetation");
      HarmonyInstance.Unpatch(method, prefixPatch);
      HarmonyInstance.Unpatch(method, postfixPatch);
      HarmonyInstance.Unpatch(method, transpilerPatch);
      HarmonyInstance.Unpatch(clearAreaMethod, clearAreaPatch);
      IsVegetationMapPatched = false;
    }
    if (toPatch)
    {
      Log("Patching ZoneSystem.PlaceVegetation");
      HarmonyInstance.Patch(method, prefix: new(prefixPatch), postfix: new(postfixPatch), transpiler: new(transpilerPatch));
      HarmonyInstance.Patch(clearAreaMethod, postfix: new(clearAreaPatch));
      IsVegetationMapPatched = true;
    }
  }
  private static void PatchColorTransition()
  {
    /*
  private static bool ColorTransitionPatched = false;
    var toPatch = Settings.EnabledForThisWorld && Settings.FixWaterColor;
    if (toPatch == ColorTransitionPatched)
      return;
    var methodStart = AccessTools.Method(typeof(Player), nameof(Player.AddKnownBiome));
    var methodEnd = AccessTools.Method(typeof(Player), nameof(Player.OnSpawned));
    var patchStart = AccessTools.Method(typeof(WaterColor), nameof(WaterColor.StartColorTransition));
    var patchEnd = AccessTools.Method(typeof(WaterColor), nameof(WaterColor.ResetColorTransition));
    if (ColorTransitionPatched)
    {
      Log("Unpatching WorldGenerator.GetAshlandsOceanGradient prefix");
      HarmonyInstance.Unpatch(methodStart, patchStart);
      HarmonyInstance.Unpatch(methodEnd, patchEnd);
      ColorTransitionPatched = false;
    }
    if (toPatch)
    {
      Log("Patching WorldGenerator.GetAshlandsOceanGradient prefix");
      HarmonyInstance.Patch(methodStart, postfix: new(patchStart));
      HarmonyInstance.Patch(methodEnd, postfix: new(patchEnd));
      ColorTransitionPatched = true;
    }
    */
  }

  private static bool IsSpawnMapPatched = false;
  private static void PatchSpawnMap()
  {
    var toPatch = Settings.EnabledForThisWorld && Settings.HasSpawnMap;
    if (toPatch == IsSpawnMapPatched)
      return;
    var method = AccessTools.Method(typeof(SpawnSystem), nameof(SpawnSystem.UpdateSpawnList));
    var prefixPatch = AccessTools.Method(typeof(SpawnSystemPatch), nameof(SpawnSystemPatch.UpdateSpawnListEnable));
    var postfixPatch = AccessTools.Method(typeof(SpawnSystemPatch), nameof(SpawnSystemPatch.UpdateSpawnListDisable));
    if (!EnsurePatchTargetFound(method, "SpawnSystem.UpdateSpawnList"))
      return;
    if (IsSpawnMapPatched)
    {
      Log("Unpatching SpawnSystem.UpdateSpawnList");
      HarmonyInstance.Unpatch(method, prefixPatch);
      HarmonyInstance.Unpatch(method, postfixPatch);
      IsSpawnMapPatched = false;
    }
    if (toPatch)
    {
      Log("Patching SpawnSystem.UpdateSpawnList");
      HarmonyInstance.Patch(method, prefix: new(prefixPatch), postfix: new(postfixPatch));
      IsSpawnMapPatched = true;
    }
  }
}