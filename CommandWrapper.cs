using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
namespace BetterContinents;
#nullable disable
public static class CommandWrapper
{
  public static Assembly ServerDevcommands = null;
  const string GUID = "server_devcommands";
  public static void Init()
  {
    if (Chainloader.PluginInfos.TryGetValue(GUID, out var info))
    {
      if (info.Metadata.Version.Major == 1 && info.Metadata.Version.Minor < 51)
      {
        BetterContinents.LogWarning($"Server devcommands v{info.Metadata.Version.Major}.{info.Metadata.Version.Minor} is outdated. Please update for better command instructions!");
      }
      else
      {
        ServerDevcommands = info.Instance.GetType().Assembly;
      }
    }
  }
  private static readonly BindingFlags PublicBinding = BindingFlags.Static | BindingFlags.Public;
  private static Type Type() => ServerDevcommands!.GetType("ServerDevcommands.AutoComplete");
  private static Type InfoType() => ServerDevcommands!.GetType("ServerDevcommands.ParameterInfo");
  // Fails soft: an optional integration must never take BC down if Server devcommands renames or
  // removes the member we're reflecting into (e.g. across a future incompatible release).
  private static MethodInfo GetMethod(Type type, string name, Type[] types)
  {
    if (type == null)
    {
      BetterContinents.LogWarning($"CommandWrapper: Server devcommands lookup type not found for member '{name}'; skipping autocomplete integration.");
      return null;
    }
    var method = type.GetMethod(name, PublicBinding, null, CallingConventions.Standard, types, null);
    if (method == null)
    {
      BetterContinents.LogWarning($"CommandWrapper: Server devcommands method {type.FullName}.{name}({string.Join(", ", types.Select(t => t.Name))}) not found; skipping autocomplete integration.");
    }
    return method;
  }
  public static void Register(string command, Func<int, int, List<string>> action)
  {
    if (ServerDevcommands == null) return;
    GetMethod(Type(), "Register", [typeof(string), typeof(Func<int, int, List<string>>)])?.Invoke(null, [command, action]);
  }
  public static void Register(string command, Func<int, List<string>> action)
  {
    if (ServerDevcommands == null) return;
    GetMethod(Type(), "Register", [typeof(string), typeof(Func<int, List<string>>)])?.Invoke(null, [command, action]);
  }
  public static void Register(string command, Func<int, List<string>> action, Dictionary<string, Func<int, List<string>>> named)
  {
    if (ServerDevcommands == null) return;
    GetMethod(Type(), "Register", [typeof(string), typeof(Func<int, List<string>>), typeof(Dictionary<string, Func<int, List<string>>>)])?.Invoke(null, [command, action, named]);
  }
  public static List<string> Info(string value)
  {
    if (ServerDevcommands == null) return [];
    return GetMethod(InfoType(), "Create", [typeof(string)])?.Invoke(null, [value]) as List<string> ?? [];
  }
}
