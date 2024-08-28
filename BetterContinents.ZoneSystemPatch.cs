using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace BetterContinents;

public partial class BetterContinents
{
  [HarmonyPatch(typeof(ZoneSystem))]
  private class ZoneSystemPatch
  {
    [HarmonyPostfix, HarmonyPatch(nameof(ZoneSystem.Load))]
    private static void LoadPostfix(ZoneSystem __instance)
    {
      if (Settings.EnabledForThisWorld)
      {
        if (!__instance.m_locationsGenerated && __instance.m_locationInstances.Count > 0)
        {
          LogWarning("Skipping automatic genloc, use the command manually if needed.");
          __instance.m_locationsGenerated = true;
        }
      }
    }
    // Changes to location type spawn placement (this is the functional part of the mod)
    [HarmonyPostfix, HarmonyPatch(nameof(ZoneSystem.ClearNonPlacedLocations), []), HarmonyPriority(Priority.Low)]
    private static void ClearNonPlacedLocationsPostfix(ZoneSystem __instance)
    {
      if (!Settings.EnabledForThisWorld) return;
      if (!Settings.HasLocationMap && !Settings.OverrideStartPosition) return;
      List<ZoneSystem.ZoneLocation> locs = [.. __instance.m_locations.Where(loc => loc.m_enable && loc.m_quantity != 0).OrderByDescending(x => x.m_prioritized)];
      if (Settings.HasLocationMap)
      {
        foreach (var loc in locs)
          HandleLocation(loc);
      }
      if (Settings.OverrideStartPosition)
      {
        var startLoc = locs.FirstOrDefault(loc => loc.m_prefabName == "StartTemple");
        if (startLoc != null)
        {
          var y = WorldGenerator.instance.GetHeight(Settings.StartPositionX, Settings.StartPositionY);
          Vector3 position = new(Settings.StartPositionX, y, Settings.StartPositionY);
          __instance.RegisterLocation(startLoc, position, false);
          Log($"Start position overriden: set to {position}");
        }
      }
    }

    private static void HandleLocation(ZoneSystem.ZoneLocation loc)
    {
      var groupName = string.IsNullOrEmpty(loc.m_group) ? "<unnamed>" : loc.m_group;
      Log($"Generating location of group {groupName}, required {loc.m_quantity}, unique {loc.m_unique}, name {loc.m_prefabName}");
      // Place all locations specified by the spawn map, ignoring counts specified in the prefab
      int placed = 0;
      foreach (var normalizedPosition in Settings.GetAllSpawns(loc.m_prefabName))
      {
        var worldPos = NormalizedToWorld(normalizedPosition);
        var position = new Vector3(
            worldPos.x,
            WorldGenerator.instance.GetHeight(worldPos.x, worldPos.y),
            worldPos.y
        );
        ZoneSystem.instance.RegisterLocation(loc, position, false);
        Log($"Position of {loc.m_prefabName} ({++placed}/{loc.m_quantity}) overriden: set to {position}");
      }
    }


    [HarmonyPrefix, HarmonyPatch(nameof(ZoneSystem.CountNrOfLocation))]
    private static bool CountNrOfLocation(ZoneSystem.ZoneLocation location, ref int __result)
    {
      if (!ConfigDebugSkipDefaultLocationPlacement.Value) return true;
      if (location.m_prefabName == "StartTemple") return true;
      __result = location.m_quantity;
      return false;
    }
  }
}
