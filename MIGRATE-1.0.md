# Better Continents — Valheim 1.0.15 migration

Status as of 2026-09-22. **Working tree is uncommitted by request** — nothing has been
committed, branched or published. Version bumped 0.7.31 → **0.8.0**.

Target: Valheim **1.0.15** (client build 25390630, server 25390671, captured 2026-09-18).
Reference set: `libs-Tools/1.0/` and `libs-Tools/Decompiled_1.0.15/`.
Anything under `libs-Tools/OLD/` is 1.0.7 and must not be used — see
`libs-Tools/CURRENT-GAME-VERSION.md`.

## Verification

| Check | Result |
|---|---|
| Build | 0 errors, 0 warnings |
| ILRepack | merged; ImageSharp + FastNoiseLite present in the 3.95 MB DLL |
| `refcheck` vs 1.0.15 **client** | `RESULT: OK` — 5295 refs, 24 Harmony targets |
| `refcheck` vs 1.0.15 **dedicated server** | `RESULT: OK` |
| Live boot, real 1.0.15 Linux dedicated server | **BOOTED** — `Loading [Better Continents 0.8.0]`, `Zonesystem Awake`, **0** Harmony/MissingMethod/MissingField/TypeLoad/AmbiguousMatch errors |
| Client session | **DONE** — world created on the real 1.0.15 client; exposed and fixed the save-layout bug below |

Build: `export DOTNET_ROOT="$HOME/.dotnet" DOTNET_ROLL_FORWARD=LatestMajor PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"`
then `dotnet build BetterContinents.csproj -c Release`. Output → `dist/plugins/` (gitignored).

Boot test: `~/valheim-testbed/boot-check.sh fireandice 2567 420`.

## Build system

- `csproj` repointed from the missing `..\Libs\` to `..\libs-Tools\`; fixed two
  Linux-case-sensitivity misses (`BepinEx.dll`, `SPlatform.dll`).
- Added `BepInEx.AssemblyPublicizer.MSBuild` and `<Publicize>true</Publicize>` on
  `assembly_valheim`/`assembly_utils`/`assembly_guiutils`; added `SoftReferenceableAssets`.
- Dropped the `BepInEx.Harmony` reference (no such DLL, unused).
- `CopyDLL` wrote the built DLL back into the *reference* folder; now writes `dist/plugins/`.
- `ILRepack.targets` merge inputs are existence-conditional, and `System.Text.Encoding.CodePages`
  was added to the merge.

### Target framework: net4.8 is forced, and this is not arbitrary

Unity 6's `UnityEngine.ImageConversionModule` is built against netstandard 2.1 and its
`Texture2D.LoadImage` now has `ReadOnlySpan<byte>` overloads. Under net4.8 that is an
unwinnable pair:

- no netstandard reference → `CS1705` (module needs 2.1, net4.8 supplies 2.0)
- add the game's netstandard 2.1 facade → `CS0518 Predefined type 'System.ReadOnlySpan\`1'
  is not defined`, because the facade's forwarder dead-ends and shadows the `System.Memory` shim

Tried and rejected: `System.Memory` 4.5.5 and 4.6.3, a `System.Runtime` facade, and the game's
own `System.Memory.dll`. Moving to `netstandard2.1` fixes Span but **removes
`System.Reflection.Emit.ILGenerator`, which every Harmony transpiler needs** — and this mod has
nine. So: net4.8 stays, the netstandard 2.1 facade is referenced, and the single `LoadImage`
call in `Presets.cs` binds its `byte[]` overload through a cached `MethodInfo`. The reasoning is
written into the code so nobody re-runs the experiment.

## Source changes

### Compile breaks (35, not 2)

The first build reported only 2 errors. Both were **attribute-level**, which makes Roslyn abort
before method-body binding — they were masking 33 more. Stub them and the real list appears.
Worth remembering when judging "is this file clean?" from a filtered build log.

| Area | Change |
|---|---|
| `HeightmapPatch`, `GameUtils`, `ImageMapBiome` | `Heightmap.m_cornerBiomes` is now `BiomeSector[4]`, not `Heightmap.Biome[4]`. `s_biomeToIndex`/`s_indexToBiome` deleted, replaced by `BiomeHelpers.ToIndex()` / `((Heightmap.BiomeIndex)i).ToBiome()`. `Heightmap.Poke(bool)` → `Poke(int delayed = 0, bool paintOnly = false)`. Minimap texture formats brought in line (`RGB24`, `RHalf` linear, vanilla's own `CreateMapTexture`). |
| `BetterContinents.cs` | Minimap cache path fields renamed (`m_mapTexturePath` → `m_cachedMinimapBiomeTexturePath`, etc.), plus a new `m_cachedMinimapMetaPath`. `SaveMapTextureDataToDisk` now takes `Color32[]`/`float[]`, not `Texture2D`. `Version.World` is a nested enum (`DeepNorth = 41`), and `World.m_worldVersion` is typed as it. |
| `WorldPatch` | `World.SaveWorldMetaData` → `World.SaveWorldFWLData(DateTime, out FileWriter)`. `FileHelpers.CheckMove` → `SaveSystem.CheckMove`. `World.RemoveWorld` gained a `FileHelpers.FileSource`; `GetMetaPath(string)` is gone (instance-only). |
| `SaveCollectionPatch`, `BetterContinentsSettings` | `SaveFile.FileName` → `SaveFile.Name`. |
| `FileHelpers` call sites | A `Splatform.CloudStorageFileGrouping` parameter was **inserted mid-signature**. Both affected lanes independently chose **`SameFolder`**, matching `SaveFileHelper.CreateFileForWriting`'s own rule (`IsWorldSaveFile(path) ? SameFolder : SameFileEnding`). `.BetterContinents` is not in `IsWorldSaveExtension`'s list, so BC must pass it explicitly or the sidecar lands in the wrong cloud bucket. |

### Latent runtime bugs found and fixed (none were compile errors)

1. **Stack overflow, uncatchable.** `IsAshlandsFallbackPrefix` called `WorldGenerator.GetBiome`,
   but `GetBiomePrefix` returns `true` (run vanilla) on a `None` pixel and vanilla `GetBiome`
   calls `IsAshlands` itself → unbounded recursion with identical arguments. Triggers on an
   ordinary setup: biome map, no heat map, any `None`/black region (which 0.7.31's changelog
   documents as "use default biome generation"). Both prefixes now read
   `Settings.GetBiomeOverride` directly and return `true` on `None`.
2. **Whole `MinimapPatch` class would have died.** BC injected `___noForest`/`___forest`; those
   are now `private static readonly s_forestColor`/`s_noForestColor`. Harmony throws on an
   unresolvable `___field` injection, taking every patch in the class with it.
3. **`ZoneSystemPatch` postfix parameter was named `result`, not `__result`.** Harmony only
   treats the literal `__result` specially; anything else is matched against the target's real
   parameter names and throws at `Patch()` time — for any world with a vegetation map.
4. **`FejdStartup.m_connectionFailedError` is now `TMP_Text`, was `UI.Text`.** The field-inject
   type mismatch would have hidden exactly the version-mismatch errors BC's own ZNet patches raise.
5. **`ToBiomeIndex()` throws `NotImplementedException`** on any biome outside the known ten, and
   `AltBiomeWorldData.GenerateBiomePoints` calls it for 2048×2048 points at world load — so one
   bad biome byte took the whole world down at load. Guarded at source in `ImageMapBiome`.
6. **Stale biome caches.** `WorldGenerator.s_cachedBiomeAreas`/`s_cachedBiomes` are cleared only
   in the `WorldGenerator` ctor; `Patcher.DynamicPatch()` now clears them after repatching.

### Deep North (new in this release)

1.0 made Deep North a real biome, but vanilla still decides *where it is* from a hardcoded
geographic test. BC had the Ashlands mirror of this and never gained the Deep North half.

| Vanilla | BC |
|---|---|
| `CreateDeepNorthGap` | `PatchDeepNorthGap` (pre-existing) |
| `IsDeepnorth` | **`PatchIsDeepnorth`** — new. Drives weather (`EnvMan.cs:605/693`), snow-vs-cultivate painting (`TerrainComp.cs:502`), vanilla's own biome fallback (`WorldGenerator.cs:804`), stream placement (`:545`) |
| `DeepNorthWaveFade` | **`PatchDeepNorthWaveFade`** — new. Wave height in `WaterVolume` and `Fish` |
| `GetDeepNorthHeight` | already covered — private, single caller, reached via BC's generic `GetBiomeHeight` postfix. (Ashlands needs its extra patch only because `GetAshlandsHeight` is public and also called from `Minimap.cs:2083`.) |

Both new patches gate on `EnabledForThisWorld && HasBiomeMap`, so vanilla behaviour and vanilla
performance are untouched without a biome map. `DeepNorthWaveFadePrefix` uses a single biome-map
sample (calm/normal) rather than rebuilding vanilla's 200 m ramp, because `GetWaterSurface` runs
per water sample per frame and every fish calls it too. Cost: a wave-height seam at the Deep
North shore instead of a short fade.

### Verified intact, no change needed

- **All 9 `WorldSizePatch` transpilers.** Verified against real 1.0.15 IL with match counts and
  stack-balance checks. They only swap `ldc.r4`/`ldc.r8` *operands*, which survives version churn.
- `Terminal.ConsoleCommand` arity trap did not bite — BC's one registration passes 6 positional
  args, all before the inserted `hideBehindDevCommands`. Converted to named args anyway.
- `ZoneLocation.m_prefab` becoming `SoftReference<GameObject>` does not affect BC — it works off
  `m_prefabName` and delegates to vanilla `RegisterLocation`.

### Deliberately not changed

- **Biome precision** stays disabled. Ported to `BiomeSector` underneath and all three Harmony
  targets resolve, but it has never been run. Reviving it should be a deliberate, tested decision.
- **Save format not bumped.** `Heightmap.Biome` numeric values are identical pre-1.0 → 1.0.15,
  and BC serialises a *bit index* rather than the enum value, so existing user maps decode
  correctly. Trap for reviewers: the new `Heightmap.BiomeIndex` enum uses *different* numbering
  (Ocean = 8 vs 0x100) — "simplifying" `ByteToBiome` to use it would silently rewrite every
  user's map.

### World generation hangs when the biome map omits a biome

**Found by play-testing; the second bug the first client run exposed.**

1.0 precomputes a list of map points per biome at world load (`AltBiomeWorldData`) and picks random
candidate zones from it while placing locations. `GetRandomPointByBiomeAboveSeaLevel` checks its own
list for emptiness, but the method it falls back to does not:

```csharp
return Biomes[biome].AllPoints[UnityEngine.Random.Range(0, Biomes[biome].AllPoints.Count)];
```

`Random.Range(0, 0)` returns `0`, so an empty list throws `ArgumentOutOfRangeException` instead of
returning nothing. Vanilla never trips this because every biome always exists somewhere. A Better
Continents biome map is under no such obligation - the Fire and Ice test map has no Mistlands, and it
threw on the first attempt to place a Mistlands location.

What makes it nasty is where it lands: `ZoneSystem.GenerateLocationsTimeSliced` is a **coroutine**.
Unity logs the exception and abandons the iterator, so location generation just stops. The player is
left on a world-creation progress bar that never completes, with nothing on screen to explain why. No
crash, no error dialog - the game simply hangs forever.

Fixed in `BetterContinents.AltBiomeWorldDataPatch.cs` with prefixes on `GetRandomPointByBiome` and
`GetRandomSectorByBiome` (same trap, same shape) that hand back a point from a biome that does exist.
That is not a fabricated placement: `GenerateLocationsTimeSliced` re-checks the biome of the point it
lands on (`if ((location.m_biome & biome) == 0) { errorBiome++; continue; }`), so locations needing a
missing biome are still rejected - they are counted and reported as "Failed to place all X" instead of
taking world generation down. One warning is logged per absent biome.

**Worth raising with Jere:** this is a vanilla guard gap, not a BC regression, but only BC can trigger
it. Any map that omits a biome hits it.

### Save format: settings now live inside the world folder

**This was a world-destroying bug, found by play-testing and fixed after the first client run.**

1.0 stores every world as a *directory* (`worlds_local/<name>/` holding `_main.N.fwl2`, `.db2`,
`.chunks`, `.ok` and the chunks). BC kept writing its settings to the pre-1.0 flat path
`worlds_local/<name>.BetterContinents`, because `World.GetMetaPath()` still returns the old
`<root>/<name>.fwl` formula. BC also registered `.BetterContinents` as a world save extension, so the
scanner saw *two* non-backup saves named `<name>`, and
`SaveWithBackups.EnsureSortedAndPrimaryFileDetermined` resolved the tie by moving the older aside -
renaming the real world folder to `<name>_backup_<timestamp>`. The world vanished from the world list
the moment it was created. Observed live: `There are two none-backup files for save ProximaMaxi`.

Settings are now written to **`worlds_local/<name>/BetterContinents`** - inside the folder, and
deliberately extension-less, matching the game's own `cacheMinimapBiome` / `cacheMinimapHeight` /
`cacheMinimapMask` / `cacheMinimapMeta` in the same directory.

Why that exact name, and not the two obvious alternatives:

| Candidate | What happens |
|---|---|
| `_main.N.BetterContinents` | `SaveCollection.KeepOnlyNewest` groups `_main.*` files by save number and **deletes any group that is not exactly 4 files**. A 5th file makes the game delete the player's world. |
| `<name>.BetterContinents` inside the folder | `GetSaveInfo` reports it as a save named `<name>` - the duplicate-save bug again, one level down. |
| `BetterContinents` (chosen) | `GetSaveInfo` returns `false` for any file with no extension, so the scanner ignores it entirely. |

`BetterContinents.SaveCollectionPatch.cs` was **deleted** - its `GetSaveInfo` and `IsWorldSaveExtension`
postfixes existed only to make an outside-the-world file a first-class save file. That registration is
precisely what let a stray sidecar masquerade as a world, so removing it also defuses any file left
behind by an earlier 0.8.0 build. That deletion was correct and stands.

#### Correction: the settings do NOT follow the world on their own

An earlier revision of this document claimed that because `SaveSystem.Delete`, `Copy` and
`RenameDirectory` all act on `SaveFile.ChunkedDirectory`, the settings would follow the world through
every copy, move, rename, backup and delete by themselves - and removed the `MoveSource` patch pair on
that basis. **That was wrong, and the removal was a regression.** Only local `Delete` behaves that way.

Valheim's save plumbing carries a fixed include-list and nothing else:

    SaveSystem.s_saveFileExtensions = { ".fwl2", ".db2", ".chunks", ".ok", ".chunk" }   (SaveSystem.cs:70)

`Path.GetExtension("BetterContinents")` is `""`, so:

| Routine | Behaviour on the settings file |
|---|---|
| `SaveSystem.RenameDirectory` (`SaveSystem.cs:441-463`) | after `Directory.Move`, walks the folder and **`File.Delete()`s** it |
| `SaveSystem.CopyDirectory` (`:371-378`) | passes the list to `FileHelpers.CopyDirectory` as an include-filter - **never copied** |
| `SaveSystem.MoveSource` (`:465-524`) | iterates `SaveFile.AllPaths`, which never contains it - **never moved** |
| `SaveSystem.Delete`, local (`:287`) | recursive `Directory.Delete` - correctly removed |

Vanilla is unharmed by the rename because its own extension-less files in that folder
(`cacheMinimapBiome` / `Height` / `Mask` / `Meta`, `Minimap.cs:2021-2024`) are regenerable. Ours is not.

Two of these are routine, not exotic. `ConsiderBackup` -> `Copy` -> `CopyDirectory` is the **auto-backup**
path, so every auto-backup of a BC world was being written without its maps. And `RestoreBackup`
(`:587-621`) combines both: it renames the live world aside (the rename deletes that copy) and then
copies the backup in (the copy never had one), leaving no copy of the world's maps anywhere on disk.

Restored and broadened as `BetterContinents.SaveSystemPatch.cs` - `RenameDirectory` (prefix captures the
bytes, postfix rewrites them into the renamed folder), `CopyDirectory` (postfix copies), and `MoveSource`
(prefix copies before the source is disposed of, postfix cleans up if the move failed). Harmony targets
21 -> 26.

The lesson generalises: **an extension-less name makes the file invisible to the save scanner, which is
exactly what we want, and invisible to the save plumbing, which we have to compensate for.**

Worlds written by the earlier 0.8.0 build are imported: if the in-folder file is missing, BC reads the
old flat path once, and the next save writes it into the folder.

#### The write path must not test `World.IsChunkedSave()`

The first fix branched on `IsChunkedSave()` - folder layout for a chunked world, flat path otherwise -
and that was wrong, in the one case that matters most. `World.IsChunkedSave()` returns `m_chunkedSave`,
a field set once from `SaveSystem.IsChunkedSave(pathPrimary, source)` when the save is *scanned*. It
records how the world was **read**, not how it is being **written**. A pre-1.0 world therefore reports
`false` for its entire session - including the very save that converts it into a folder.

`World.SaveWorldFWLData` never consults it. It writes unconditionally to `GetSaveFWLPath()`, which is
always `<root>/<name>/_main.<n>.fwl2`. So on an upgrade save the world moves into a directory while BC,
still seeing `false`, wrote its settings to the flat path beside it - and the settings were orphaned.

Observed exactly that on `Era02` (a real pre-1.0 BC world): after the session, `Era02/_main.1.fwl2`
existed, `Era02/BetterContinents` did **not**, and `Era02.BetterContinents` had just been rewritten at
the old path. The world upgraded; its map did not come with it.

BC now writes into the world folder unconditionally, matching `SaveWorldFWLData` itself. There is no
"pre-1.0 write" case, because 1.0.15 never writes a world in the old layout.

The old flat file is **renamed to `<name>.BetterContinents.pre10`**, not deleted - it holds the only
copy of that world's baked maps, and vanilla likewise keeps the pre-conversion `.db`/`.fwl` as
`<name>_backup_<timestamp>`. It only has to stop sharing a name with a live world; it does not have to
stop existing.

### The minimap mask is not a forest flag any more

`Minimap.GetMaskColor` is no longer patched, and the `GetMaskColorPrefix` prefix was **removed**.

In 1.0 that method packs a different meaning into each channel, per biome:

| Biome | Mask |
|---|---|
| anything under 30 m | `(0, 0, GetAshlandsOceanGradient, 0)` - checked **before** the biome switch |
| Meadows | red = forest, if `InForest(pos)` |
| Plains | red = forest, if `GetForestFactor(pos) < 0.8` |
| BlackForest | red = forest, always |
| Mistlands | **green** = `1 - SmoothStep(1.1, 1.3, forestFactor)`, the mist density |
| AshLands | **blue** = `GetAshlandsHeight(...).a` |
| Swamp, Mountain, Deep North, Ocean | no mask at all |

The old prefix collapsed six of those onto the red forest channel whenever
`ForestFactorOverrideAllTrees` was set. The result in game: Swamp, Mountain, Plains, Meadows and
Mistlands all drew as Black Forest, and the Ashlands gradients - above and below water - were thrown
away. Reported from a play session as *"swamp mistlands, everything reads as BF"*.

The patch was never needed. BC already patches `WorldGenerator.GetForestFactor` (prefix + postfix,
applying `ApplyForest`), and vanilla's `InForest(pos)` is just `GetForestFactor(pos) < 1.15f`, so
vanilla's own `GetMaskColor` reads BC's forest values and paints the correct channel for free. It was
written for an older Valheim where the forest colours were instance fields Harmony could inject; the
static `s_forestColor` / `s_noForestColor` in 1.0 prompted a rewrite that reimplemented - and lost -
the per-biome encoding. Harmony targets 22 -> 21.

`GenerateWorldMapMT` (BC's threaded replacement for `GenerateWorldMap`) already passed `biomeHeight`
into `GetMaskColor`, so the `height < 30f` branch works as soon as the prefix is gone. Its height
texture now also writes alpha 0, matching vanilla, which fills that array by assigning `.r` onto a
default `Color`.

## Known gaps worth raising with Jere

1. **A dedicated server cannot create a BC world from config.** `bWorldBeingCreated` is set only
   in `FejdStartupPatch.cs:43` (`OnNewWorldDone`), the client main-menu path, so
   `Presets.LoadActivePreset()` never runs headless. `SetServerPrefix` only *reads* an existing
   `.BetterContinents` sidecar. Consistent with the documented "author on client, upload to
   server" model, but it means BC worlds cannot be created or CI-tested headlessly.
2. **The Manage Saves screen bypasses BC.** `SaveSystem.Delete` and `SaveSystem.MoveSource` are
   called directly from `ManageSavesMenu`, so neither `World.RemoveWorld` nor `SaveWorldFWLData`
   fires. Moving a world between Local and Cloud there left the sidecar behind and the world
   arrived as vanilla. **Fixed in 0.8.0** by a `MoveSource` prefix/postfix pair; deletion still
   orphans the sidecar (harmless).
3. **The minimap cache does not hash BC's settings.** Edit a world's `.BetterContinents` outside
   the game and reload without `/bc reset` and the cache still validates. Pre-existing, not a 1.0
   regression.

## Test map and upgrade path (working notes, not part of the port)

The Fire and Ice test map and its generator live outside the repo in
`../BetterContinents-TestMaps/` (`generator/gen.py`, `stage-into-gale.sh`). Authoring rules that
cost a test world each are written up there and in the session memory - chiefly that the waterline
is `0.30 / HeightmapAmount`, that `Heightmap Amount` sets the altitude ceiling, and that vanilla
places locations radially from world centre.

Upgrade path from a pre-1.0 BC world: **half proven.** A save-version-11 `.BetterContinents`
sidecar from a flat pre-1.0 world (`Era02`) loads correctly under 0.8.0 on 1.0.15 - four embedded
1024px maps decoded, settings applied. Not yet proven: the write half. Vanilla only converts a
legacy world to the chunked folder on *save*, and only then does `SaveWorldFWLDataPostfix` write
the settings inside the folder and delete the stale flat file.

## Verified in play, and what is still unproven

A client session has been played end to end: a world created from a full image-map set, entered,
played, saved manually and exited cleanly; and separately a real pre-1.0 Better Continents world
(save version 11, a 4.85 MB sidecar carrying four embedded 1024px maps) loaded, upgraded and saved.

**Verified live:**

- World creation from config, with every image map loaded and all locations placed.
- The 1.0 save layout, on both a new world and an upgraded one. Each world is a directory with
  exactly one four-file `_main.N` group plus the settings, and no flat sidecar beside it.
- **The upgrade path.** A pre-1.0 flat world converted to the chunked layout and its settings moved
  into the world folder with all four embedded map blobs **byte-identical** to the original, the old
  file retired as `<name>.BetterContinents.pre10`, and no duplicate-save warning. The cross-root
  search in `ResolveWorldSettingsPath` is what makes this work: `SaveSystem.CheckMove` rewrites
  `m_fileSource` in place before the first save, so probing only the world's current root loses the
  settings entirely.
- Repeat saves. Both worlds show the `ReplaceOldFile` `.old` sibling, so the write path is
  idempotent rather than a one-shot.
- The minimap. Removing the `GetMaskColor` patch was driven by what the map actually drew in game.

**Not exercised at all - this is the honest gap:**

- **`BetterContinents.SaveSystemPatch.cs` has never run.** Rename, copy and move produced no log
  lines in any session. Auto-backup needs 1200 s of world time (`ZNet.ConsiderAutoBackup`) and the
  test sessions were 67-166 s, so `ConsiderBackup` bailed with *"Skipping backup. World session not
  long enough"* every time; and the backups vanilla did make were of the *flat* pre-conversion world,
  which takes `Rename`'s non-directory branch rather than `RenameDirectory`. The code is
  refcheck-clean on both client and server and desk-checked against the decompile, but it is the
  part that guards against a world-destroying deletion, and it has no runtime evidence behind it.
  The cheapest way to exercise two of the three patches is Manage Saves -> Move a world between
  Local and Cloud, which runs `MoveSource` and then `MoveToBackup` -> `Rename` -> `RenameDirectory`.
- **Deep North** (`PatchIsDeepnorth`, `PatchDeepNorthWaveFade`) has not been observed live - no
  weather, painting or wave-fade check yet.
- **Biome precision** is still disabled and still never run.
- Dedicated-server *world creation* remains impossible by design; see the known gap above.
