using HarmonyLib;

namespace BetterContinents;

public partial class BetterContinents
{
  public static void DynamicPatch()
  {
    PatchHeightmap();
    PatchGetBaseHeight();
    PatchGetBiomeHeight();
    PatchGetBiome();
    PatchAddRivers();
    PatchForestFactorPrefix();
    PatchForestFactorPostfix();
  }
  private static int HeightmapGetBiomePatched = 0;
  private static void PatchHeightmap()
  {
    var precision = Settings.EnabledForThisWorld ? Settings.BiomePrecision : 0;
    if (precision == HeightmapGetBiomePatched)
      return;
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
    foreach (var hm in Heightmap.Instances)
    {
      hm.m_buildData = null;
      hm.Regenerate();
    }
    ClutterSystem.instance?.ClearAll();

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
          if (Settings.HasHeightmap || Settings.BaseHeightNoise != null)
            patchVersion = 3;
          break;
      }
    }

    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetBaseHeight));
    var patch1 = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.GetBaseHeightPrefixV1));
    var patch2 = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.GetBaseHeightPrefixV2));
    var patch3 = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.GetBaseHeightPrefixV3));
    if (patchVersion == GetBaseHeightPatched)
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

  private static bool GetBiomeHeightPatched = false;
  private static void PatchGetBiomeHeight()
  {
    var toPatch = Settings.EnabledForThisWorld && (Settings.ShouldHeightmapOverrideAll || Settings.HasRoughmap);
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight));
    var patch = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.GetBiomeHeightPostfix));
    if (toPatch == GetBiomeHeightPatched)
      return;
    if (GetBiomeHeightPatched)
    {
      Log("Unpatching WorldGenerator.GetBiomeHeight");
      HarmonyInstance.Unpatch(method, patch);
      GetBiomeHeightPatched = false;
    }
    if (toPatch)
    {
      Log("Patching WorldGenerator.GetBiomeHeight");
      HarmonyInstance.Patch(method, postfix: new(patch));
      GetBiomeHeightPatched = true;
    }
  }
  private static bool GetBiomePatched = false;
  private static void PatchGetBiome()
  {
    var toPatch = Settings.EnabledForThisWorld && Settings.HasBiomemap;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetBiome), [typeof(float), typeof(float)]);
    var patch = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.GetBiomePrefix));
    if (toPatch == GetBiomePatched)
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
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.AddRivers));
    var patch = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.AddRiversPrefix));
    if (toPatch == AddRiversPAtched)
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
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetForestFactor));
    var patch = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.GetForestFactorPrefix));
    if (toPatch == ForestFactorPrefixPatched)
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
    var toPatch = Settings.EnabledForThisWorld && (Settings.HasForestmap || Settings.ForestAmountOffset != 0f);
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetForestFactor));
    var patch = AccessTools.Method(typeof(WorldGeneratorPatch), nameof(WorldGeneratorPatch.GetForestFactorPostfix));
    if (toPatch == ForestFactorPostfixPatched)
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
}