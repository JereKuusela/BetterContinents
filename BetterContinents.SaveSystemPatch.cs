using System;
using System.IO;
using HarmonyLib;
using Splatform;

namespace BetterContinents;

public partial class BetterContinents
{
  // Valheim's save plumbing carries a fixed list of file extensions and nothing else:
  //
  //     SaveSystem.s_saveFileExtensions = { ".fwl2", ".db2", ".chunks", ".ok", ".chunk" }   (SaveSystem.cs:70)
  //
  // Our settings file inside the world folder is deliberately extension-less, because that is what makes
  // the save scanner ignore it (SaveSystem.GetSaveInfo returns false on an empty extension, so it can never
  // masquerade as a world the way the old flat sidecar did). The same property means every routine below
  // either skips it or deletes it, so the settings do NOT follow the world on their own:
  //
  //   SaveSystem.RenameDirectory (SaveSystem.cs:441-463) - after Directory.Move it walks the moved folder
  //       and File.Delete()s everything whose extension is not in that list. Vanilla is unharmed because
  //       its own extension-less files in there (cacheMinimapBiome / Height / Mask / Meta) are regenerable.
  //       Ours is not: it holds the world's baked maps. Reached from MoveToBackup -> Rename, and so from
  //       RestoreBackup, CheckMove, MoveSource, and SaveWithBackups' duplicate-name fixup.
  //   SaveSystem.CopyDirectory (SaveSystem.cs:371-378) - hands that same list to FileHelpers.CopyDirectory
  //       as an include-filter (assembly_utils.decompiled.cs:3950-3954), which is also not recursive. This
  //       is the ordinary auto-backup path (ConsiderBackup -> Copy -> CopyDirectory), so every auto-backup
  //       of a Better Continents world was being written without its maps.
  //   SaveSystem.MoveSource (SaveSystem.cs:465-524) - the "Manage Saves" Move button, which iterates
  //       SaveFile.AllPaths. Our file is never in there: FileHelpers.GetFiles only ever returns "_main.*"
  //       and "*.chunk" out of a world folder.
  //
  // Worst case without these patches is RestoreBackup (SaveSystem.cs:587-621): it renames the live world
  // aside (RenameDirectory deletes that copy) and then copies the backup into place (CopyDirectory never
  // put one there), leaving no copy of the world's maps anywhere on disk.
  [HarmonyPatch(typeof(SaveSystem))]
  private class SaveSystemPatch
  {
    private static string SettingsIn(string? directory) =>
        directory?.TrimEnd('/', '\\') + "/" + ConfigFileName;

    // Rename only routes to RenameDirectory for a chunked, NON-cloud save (SaveSystem.cs:382-385), so the
    // bytes can be read and written directly. Captured in a prefix because the original deletes the file.
    [HarmonyPrefix, HarmonyPatch("RenameDirectory", typeof(SaveFile), typeof(string))]
    private static void RenameDirectoryPrefix(SaveFile saveFile, out byte[]? __state)
    {
      __state = null;
      try
      {
        if (!saveFile.IsChunked) return;
        var settings = SettingsIn(saveFile.ChunkedDirectory);
        if (File.Exists(settings)) __state = File.ReadAllBytes(settings);
      }
      catch (Exception ex)
      {
        LogWarning($"Could not read settings before renaming {saveFile.Name}: {ex.Message}");
      }
    }

    [HarmonyPostfix, HarmonyPatch("RenameDirectory", typeof(SaveFile), typeof(string))]
    private static void RenameDirectoryPostfix(SaveFile saveFile, string newName, bool __result, byte[]? __state)
    {
      if (!__result || __state == null) return;
      try
      {
        // The same destination the original builds at SaveSystem.cs:443. SaveFile.m_chunkedDirectory is not
        // updated by the rename, so ChunkedDirectory still reads as the old path here, exactly as it does
        // inside the original after its own Directory.Move.
        var moved = Path.GetDirectoryName(saveFile.ChunkedDirectory) + "/" + newName;
        Directory.CreateDirectory(moved);
        File.WriteAllBytes(SettingsIn(moved), __state);
        Log($"Carried settings across the rename of {saveFile.Name} to {newName}");
      }
      catch (Exception ex)
      {
        LogError($"Failed to carry settings across the rename of {saveFile.Name}: {ex.Message}");
      }
    }

    // A copy leaves the source alone, so this only needs a postfix. `newName` is already the full
    // destination directory (built at SaveSystem.cs:332), and Auto resolves to the source's own location,
    // which is what FileHelpers.CopyDirectory does with it (assembly_utils.decompiled.cs:3939-3942).
    [HarmonyPostfix, HarmonyPatch("CopyDirectory", typeof(SaveFile), typeof(string), typeof(FileHelpers.FileSource))]
    private static void CopyDirectoryPostfix(SaveFile file, string newName, FileHelpers.FileSource destinationLocation,
        bool __result)
    {
      if (!__result || !file.IsChunked) return;
      try
      {
        var from = SettingsIn(file.ChunkedDirectory);
        if (!FileHelpers.Exists(from, file.m_source)) return;
        var to = SettingsIn(newName);
        var destination = destinationLocation == FileHelpers.FileSource.Auto ? file.m_source : destinationLocation;
        if (FileHelpers.Copy(from, file.m_source, to, CloudStorageFileGrouping.SameFolder, destination))
          Log($"Copied settings alongside {file.Name} to {newName}");
        else
          LogWarning($"Copy of {file.Name} did not take its settings with it");
      }
      catch (Exception ex)
      {
        LogError($"Failed to copy settings alongside {file.Name}: {ex.Message}");
      }
    }

    // MoveSource ends by disposing of the source - MoveToBackup for a non-cloud non-backup save, otherwise
    // Delete (SaveSystem.cs:515-522) - so the copy has to happen in a prefix, while the source still exists.
    // If the move then fails, the postfix takes the stray copy back out.
    [HarmonyPrefix, HarmonyPatch(nameof(SaveSystem.MoveSource))]
    private static void MoveSourcePrefix(SaveFile file, FileHelpers.FileSource destinationSource, out string? __state)
    {
      __state = null;
      try
      {
        if (!file.IsChunked) return;
        var from = SettingsIn(file.ChunkedDirectory);
        if (!FileHelpers.Exists(from, file.m_source)) return;
        var to = GetWorldBCFile(file.Name, destinationSource);
        if (destinationSource.IsNotCloud()) Directory.CreateDirectory(Path.GetDirectoryName(to));
        if (FileHelpers.Copy(from, file.m_source, to, CloudStorageFileGrouping.SameFolder, destinationSource))
          __state = to;
        else
          LogWarning($"Could not move the settings for {file.Name} to {destinationSource}");
      }
      catch (Exception ex)
      {
        LogWarning($"Could not move the settings for {file.Name}: {ex.Message}");
      }
    }

    [HarmonyPostfix, HarmonyPatch(nameof(SaveSystem.MoveSource))]
    private static void MoveSourcePostfix(SaveFile file, FileHelpers.FileSource destinationSource, bool __result,
        string? __state)
    {
      if (__state == null) return;
      if (__result)
      {
        Log($"Moved settings for {file.Name} to {destinationSource}");
        return;
      }
      try
      {
        FileHelpers.Delete(__state, destinationSource);
      }
      catch (Exception ex)
      {
        LogWarning($"Could not clean up {__state} after a failed move: {ex.Message}");
      }
    }
  }
}
