using System;
using System.IO;
using HarmonyLib;
using Splatform;

namespace BetterContinents;

public partial class BetterContinents
{
  // Saving and removing of worlds
  [HarmonyPatch(typeof(World))]
  private class WorldPatch
  {
    // New saving logic:
    //  SaveSystem.CheckMove() -- checks for source=legacy, changes source directly to cloud or local, moves file passed in to backup
    //  ZNet.SaveWorldThread() -- saves async during gameplay
    //      Called in:
    //          Auto-save
    //          Explicit save
    //          Exit save
    //          ... world must already be created at these points, i.e. NOT called during world creation.
    //      Calls:
    //          SaveSystem.CheckMove()
    //          World.SaveWorldFWLData()
    //      PREFIX to backup the bc file
    // World.SaveWorldFWLData() saves the world metadata specifically (as opposed to the db)
    //      Called in:
    //          FejdStartup.OnNewWorldDone() -- world creation on client
    //          World.GetCreateWorld() -- world creation on server (only in the world create branch)
    //          World.GetDevWorld() -- same
    //          ZNet.SaveWorldThread() -- during live game
    //      Calls:
    //          SaveSystem.CheckMove()
    // FejdStartup.OnNewWorldDone()
    //      PREFIX to set bWorldBeingCreated flag
    //      POSTFIX to clear bWorldBeingCreated flag

    // An explicit flag that indicates that the next SaveWorldMetaData call relates to a newly created world, 
    // and that we should generate / save a new BC config from the appropriate source (preset / config etc.).
    public static bool bWorldBeingCreated = false;

    // This isn't needed, it is replaced by bWorldBeingCreated
    // [HarmonyPrefix, HarmonyPatch(nameof(World.SaveWorldMetaData))]
    // private static void SaveWorldMetaDataPrefix(World __instance, out bool __state)
    // {
    //     // We need to record whether this is a first time save (during world creation),
    //     // so we know if we need to apply a template config or not.
    //     // However we can't write the config here, because if this is an *upgrade* save where
    //     // the path is changing (e.g. legacy to local or cloud), then GetMetaPath is going
    //     // to point to the old location currently, and we need to save to the new location.
    //     if (__instance.m_fileSource == FileHelpers.FileSource.Legacy)
    //     {
    //         Log($"[Saving][{__instance.m_name}] Updating from legacy save");
    //         string bcConfigFile = __instance.GetMetaPath() + BetterContinents.ConfigFileExtension;
    //         Log($"[Saving][{__instance.m_name}] Backing up {bcConfigFile}");
    //         FileHelpers.MoveToBackup(__instance.GetMetaPath() + BetterContinents.ConfigFileExtension);
    //         // Legacy save by definition already has the metadata file, as we can't create a new legacy save
    //         __state = true;
    //     }
    //     else
    //     {
    //         // Non-legacy save, we check for existence of the bc metadata file in the original save location. 
    //         // Except it won't exist if the save occurred during the gameplay, as SaveWorldThread will have already moved it...
    //         __state = File.Exists(__instance.GetMetaPath());
    //     }
    //     Log($"[Saving][{__instance.m_name}] Meta path = {__instance.GetMetaPath()}, exists = {__state}");
    // }

    // When the world metadata is saved we write an extra file next to it for our own config
    // Do this as a Postfix because the targeted directory (and thus the GetMetaPath() result) might change
    // if the world is being "upgraded" to the new save location.
    // 1.0.15: World.SaveWorldMetaData was renamed to World.SaveWorldFWLData, which now has two overloads -
    // SaveWorldFWLData(DateTime) just forwards to SaveWorldFWLData(DateTime, out FileWriter), so patching
    // that (DateTime, out FileWriter) overload still catches every caller (world creation and live saves).
    [HarmonyPostfix, HarmonyPatch(nameof(World.SaveWorldFWLData),
         new[] { typeof(DateTime), typeof(FileWriter) },
         new[] { ArgumentType.Normal, ArgumentType.Out })]
    private static void SaveWorldFWLDataPostfix(World __instance)
    {
      // World modifiers being set, nothing to be done.
      if (!bWorldBeingCreated && __instance != WorldGenerator.instance?.m_world)
        return;
      Log($"[Saving][{__instance.m_name}] Saving settings for {__instance.m_name}");

      BetterContinentsSettings settingsToSave;

      // This flag is set explicitly in the OnNewWorldDonePrefix function only
      if (bWorldBeingCreated)
      {
        // World is being created, so bake our settings from the preset
        Log($"[Saving][{__instance.m_name}] bWorldBeingCreated flag set, first time save of {__instance.m_name}, applying selected preset '{ConfigSelectedPreset.Value}'");
        settingsToSave = Presets.LoadActivePreset();
        bWorldBeingCreated = false;
      }
      else
      {
        Log($"[Saving][{__instance.m_name}] bWorldBeingCreated flag NOT set, saving active world settings");
        settingsToSave = Settings;
      }
      if (!settingsToSave.EnabledForThisWorld)
      {
        Log($"[Saving][{__instance.m_name}] BC is disabled for this world, skipping save");
        return;
      }
      settingsToSave.Dump();

      // The settings always go inside the world's own folder, with no test on the world's current
      // shape. World.SaveWorldFWLData writes its metadata to GetSaveFWLPath(), which is
      // unconditionally "<saves root>/<world name>/_main.<n>.fwl2"; it never consults
      // IsChunkedSave(). That flag only records how the world was *read* at scan time, so a
      // pre-1.0 world reports false for its entire session - including the very save that
      // converts it into a folder. Branching on it left the settings sitting beside a world that
      // had just moved into a directory, which is how an upgraded world lost its map.
      string bcConfigFile = GetWorldBCFile(__instance.m_worldName, __instance.m_fileSource);
      // On that upgrade save the folder is created by SaveWorldFWLData itself, but a cheap guard
      // here means we are never the ones to fail on a missing directory.
      var bcConfigDir = Path.GetDirectoryName(bcConfigFile);
      if (__instance.m_fileSource != FileHelpers.FileSource.Cloud
          && !string.IsNullOrEmpty(bcConfigDir) && !Directory.Exists(bcConfigDir))
        Directory.CreateDirectory(bcConfigDir);
      string newName = bcConfigFile + ".new";
      string oldName = bcConfigFile + ".old";
      settingsToSave.SaveToSource(newName, __instance.m_fileSource);
      // 1.0.15: ReplaceOldFile gained a CloudStorageFileGrouping parameter (inserted before the FileSource
      // one). SameFolder is what World.SaveWorldFWLData() itself uses for the .fwl2, and it is what we want
      // here too so the settings stay in the same Steam Cloud bucket as the world files they configure.
      FileHelpers.ReplaceOldFile(bcConfigFile, newName, oldName, CloudStorageFileGrouping.SameFolder, __instance.m_fileSource);

      // Retire any settings file still sitting at the old flat path beside the world folder. That
      // covers both a pre-1.0 world that this save has just converted and a world created by an
      // early 0.8.0 build, whose stray sidecar was what made the game see a duplicate save and
      // back the real world up. It is renamed rather than deleted: the settings hold the only
      // copy of the world's baked maps, vanilla likewise keeps the pre-conversion .db/.fwl as a
      // backup, and the file only has to stop sharing a name with a live world - it does not have
      // to stop existing. This runs after the in-folder copy is safely in place.
      try
      {
        var stale = GetBCFile(__instance.GetMetaPath());
        if (File.Exists(stale) && File.Exists(bcConfigFile))
        {
          var retired = stale + ".pre10";
          File.Delete(retired);
          File.Move(stale, retired);
          Log($"[Saving][{__instance.m_name}] Settings migrated into the world folder; kept the old file as {retired}");
        }
      }
      catch (Exception ex)
      {
        LogWarning($"Could not retire the old settings file: {ex.Message}");
      }
    }

    [HarmonyPostfix, HarmonyPatch(nameof(World.RemoveWorld))]
    private static void RemoveWorldPostfix(string name, FileHelpers.FileSource fileSource)
    {
      try
      {
        // 1.0.15: RemoveWorld gained a fileSource parameter (World.cs), and World.GetMetaPath is now an
        // instance method with no static string-name overload any more, so we rebuild its ".fwl" path formula
        // here (World.GetMetaPath(FileSource), World.cs) from the name/fileSource RemoveWorld gives us.
        var path = SaveSystem.GetWorldsSaveRootPath(fileSource) + "/" + name + ".fwl";
        File.Delete(GetBCFile(path));
        File.Delete(GetLegacyBCFile(path));
        Log($"Deleted saved settings for {name}");
      }
      catch
      {
        LogError($"Failed to delete saved settings for {name}");
      }
    }
  }

  // Note: Valheim 1.0's "Manage Saves" screen moves, copies and deletes a world through SaveSystem, which
  // never calls World.SaveWorldFWLData or World.RemoveWorld. Better Continents used to need its own patches
  // to chase the settings file around because that file sat outside the world. It now lives inside the world
  // folder, and SaveSystem.Copy, RenameDirectory and Delete all operate on the whole directory
  // (SaveFile.ChunkedDirectory), so the settings follow the world on their own and no patch is needed.
}