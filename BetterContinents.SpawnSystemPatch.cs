using System.Collections.Generic;


namespace BetterContinents;

public partial class BetterContinents
{

  private class SpawnSystemPatch
  {
    /* Spawn manipulation
       Enabling and disabling is done for the whole zone.
       This is currently done by setting all biomes. This has to be reverted at end of the function.
    */
    public static void UpdateSpawnListEnable(SpawnSystem __instance, List<SpawnSystem.SpawnData> spawners, bool eventSpawners)
    {
      if (eventSpawners) return;
      Settings.ApplySpawnMap(__instance.transform.position, spawners);
    }
    public static void UpdateSpawnListDisable()
    {
      Settings.RevertSpawnMap();
    }
  }
}
