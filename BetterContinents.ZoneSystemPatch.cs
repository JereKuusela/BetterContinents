using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
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

    /* Vegetation manipulation
       Enabling is done for the whole zone. More precise solution would require entirely new implementation.
       Enabling is currently done by setting all biomes. This has to be reverted at end of the function.
       Disabling uses the clear area system so it's very precise. However transpiler is needed to keep track of the current vegetation.
       This technically should allow precise manipulation with enable + disable combo.
    */
    public static void PlaceVegetationEnable(ZoneSystem __instance, Vector3 zoneCenterPos)
    {
      Settings.ApplyVegetationMap(zoneCenterPos, __instance.m_vegetation);
    }
    public static void PlaceVegetationRestore()
    {
      Settings.RevertVegetationMap();
    }
    private static ZoneSystem.ZoneVegetation? CurrentVegetation;
    private static ZoneSystem.ZoneVegetation SetCurrentVegetation(ZoneSystem.ZoneVegetation vegetation)
    {
      CurrentVegetation = vegetation;
      return vegetation;
    }
    public static IEnumerable<CodeInstruction> PlaceVegetationSaveCurrent(IEnumerable<CodeInstruction> instructions) =>
      new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(ZoneSystem.ZoneVegetation), nameof(ZoneSystem.ZoneVegetation.m_enable))))
      .Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ZoneSystemPatch), nameof(SetCurrentVegetation))))
      .InstructionEnumeration();


    public static bool CheckVegetationMapClearArea(bool result, Vector3 point)
    {
      if (result || CurrentVegetation == null) return result;
      return Settings.CheckVegetationMap(point, CurrentVegetation);
    }
  }
}
