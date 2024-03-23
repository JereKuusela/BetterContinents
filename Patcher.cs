using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace BetterContinents;

public partial class BetterContinents
{
  private static int HeightmapGetBiomePatched = 0;
  public static void DynamicPatch()
  {
    PatchHeightmap();
  }
  private static void PatchHeightmap()
  {
    var method = AccessTools.Method(typeof(Heightmap), nameof(Heightmap.GetBiome));
    var patch = AccessTools.Method(typeof(BetterContinents), nameof(GetBiomePatch));
    if (HeightmapGetBiomePatched == Settings.BiomePrecision)
      return;
    if (HeightmapGetBiomePatched > 0)
    {
      Log("Unpatching Heightmap.GetBiome");
      HarmonyInstance.Unpatch(method, patch);
      HeightmapGetBiomePatched = 0;
    }
    if (Settings.BiomePrecision > 0)
    {
      Log($"Patching Heightmap.GetBiome with precision {Settings.BiomePrecision}");
      HarmonyInstance.Patch(method, transpiler: new(patch));
      HeightmapGetBiomePatched = Settings.BiomePrecision;
    }
  }
}