using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace BetterContinents;

public partial class BetterContinents
{
  // Valheim 1.0 builds a per-biome list of map points at world load (AltBiomeWorldData) and picks random
  // candidate zones from it while placing locations. Vanilla assumes every biome exists somewhere, so
  // while GetRandomPointByBiomeAboveSeaLevel does check its own list for emptiness, the method it falls
  // back to does not:
  //
  //     return Biomes[biome].AllPoints[UnityEngine.Random.Range(0, Biomes[biome].AllPoints.Count)];
  //
  // Random.Range(0, 0) returns 0, so an empty list throws ArgumentOutOfRangeException rather than
  // returning nothing. A Better Continents biome map is under no obligation to contain every biome - a
  // map with no Mistlands hits this immediately.
  //
  // The throw lands inside ZoneSystem.GenerateLocationsTimeSliced, which is a coroutine. Unity logs the
  // exception and abandons the iterator, so location generation simply stops: the player is left on a
  // world-creation progress bar that never finishes, with nothing on screen explaining why. That failure
  // mode is why this is worth patching rather than documenting.
  //
  // Handing back a point from a biome that does exist keeps generation moving without inventing a
  // placement: GenerateLocationsTimeSliced re-checks the biome of the point it ends up with
  // (`if ((location.m_biome & biome) == 0) { errorBiome++; continue; }`), so locations belonging to a
  // missing biome are still rejected - they just get counted and reported as "Failed to place all X"
  // instead of taking world generation down with them.
  [HarmonyPatch(typeof(AltBiomeWorldData))]
  private class AltBiomeWorldDataPatch
  {
    // Only used to keep the log to one line per absent biome; generation asks for these points thousands
    // of times and an unthrottled warning would bury everything else in the log.
    private static readonly HashSet<Heightmap.Biome> Reported = [];

    private static void ReportOnce(Heightmap.Biome biome, string what)
    {
      if (Reported.Add(biome))
        LogWarning($"The biome map has no {biome}, so no {what} exist for it. Locations that require {biome} will be skipped.");
    }

    [HarmonyPrefix, HarmonyPatch("GetRandomPointByBiome")]
    private static bool GetRandomPointByBiomePrefix(AltBiomeWorldData __instance, Heightmap.Biome biome,
        ref BiomePointCoordinate __result)
    {
      if (__instance.Biomes.TryGetValue(biome, out var info) && info.AllPoints.Count > 0)
        return true;
      // Prefer dry land: this stands in for a point the caller wanted above sea level.
      var fallback = __instance.Biomes.Values.FirstOrDefault(b => b.AllPointsAboveSeaLevel.Count > 0)
                     ?? __instance.Biomes.Values.FirstOrDefault(b => b.AllPoints.Count > 0);
      // A world with no points at all in any biome is broken in a way we should not paper over.
      if (fallback == null)
        return true;
      ReportOnce(biome, "map points");
      var points = fallback.AllPointsAboveSeaLevel.Count > 0 ? fallback.AllPointsAboveSeaLevel : fallback.AllPoints;
      __result = points[UnityEngine.Random.Range(0, points.Count)];
      return false;
    }

    // Identical shape, identical trap: Biomes[biome].Sectors is indexed without a count check.
    [HarmonyPrefix, HarmonyPatch("GetRandomSectorByBiome")]
    private static bool GetRandomSectorByBiomePrefix(AltBiomeWorldData __instance, Heightmap.Biome biome,
        ref BiomeSector __result)
    {
      if (__instance.Biomes.TryGetValue(biome, out var info) && info.Sectors.Count > 0)
        return true;
      var fallback = __instance.Biomes.Values.FirstOrDefault(b => b.Sectors.Count > 0);
      if (fallback == null)
        return true;
      ReportOnce(biome, "biome sectors");
      __result = fallback.Sectors[UnityEngine.Random.Range(0, fallback.Sectors.Count)];
      return false;
    }
  }
}
