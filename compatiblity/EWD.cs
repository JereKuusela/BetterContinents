using System;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace BetterContinents;

public class EWD
{
  public const string GUID = "expand_world_data";
  private static Assembly? Assembly;
  private static MethodInfo? SetSize;
  public static void Run()
  {
    if (!Chainloader.PluginInfos.TryGetValue(GUID, out var info)) return;
    Assembly = info.Instance.GetType().Assembly;
    var type = Assembly.GetType("ExpandWorldData.WorldInfo");
    if (type == null)
    {
      BetterContinents.LogWarning("EWD compatibility: type \"ExpandWorldData.WorldInfo\" not found; skipping (Expand World Data may have changed its API).");
      return;
    }
    // AccessTools.Method can itself throw AmbiguousMatchException when the name resolves to more
    // than one overload upstream; catch it so an optional integration can never take BC down.
    try
    {
      SetSize = AccessTools.Method(type, "Set");
    }
    catch (Exception ex)
    {
      SetSize = null;
      BetterContinents.LogWarning($"EWD compatibility: failed to resolve WorldInfo.Set ({ex.Message}); skipping.");
      return;
    }
    if (SetSize == null)
    {
      BetterContinents.LogWarning("EWD compatibility: method \"WorldInfo.Set\" not found; skipping (Expand World Data may have changed its API).");
      return;
    }
    BetterContinents.Log("\"Expand World Data\" detected. Applying compatibility.");
  }

  public static void RefreshSize(float worldRadius, float worldTotalRadius, float worldStretch, float biomeStretch)
  {
    if (SetSize == null) return;
    try
    {
      SetSize.Invoke(null, [worldRadius, worldTotalRadius, worldStretch, biomeStretch]);
    }
    catch (Exception ex)
    {
      BetterContinents.LogWarning($"EWD compatibility: WorldInfo.Set call failed ({ex.Message}); disabling further calls.");
      SetSize = null;
    }
  }
}
