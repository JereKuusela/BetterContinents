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
    if (type == null) return;
    SetSize = AccessTools.Method(type, "Set");
    if (SetSize == null) return;
    BetterContinents.Log("\"Expand World Data\" detected. Applying compatibility.");
  }

  public static void RefreshSize(float worldRadius, float worldTotalRadius, float worldStretch, float biomeStretch)
  {
    if (SetSize == null) return;
    SetSize.Invoke(null, [worldRadius, worldTotalRadius, worldStretch, biomeStretch]);
  }
}
