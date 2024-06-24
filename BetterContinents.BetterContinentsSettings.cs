using System;
using System.Collections.Generic;
using System.IO;
using SixLabors.ImageSharp.PixelFormats;
using UnityEngine;

namespace BetterContinents;

public partial class BetterContinents
{
    // These are what are baked into the world when it is created
    public partial class BetterContinentsSettings
    {

        public bool EnabledForThisWorld;
        public int Version;
        public float GlobalScale;
        public float MountainsAmount;
        public float SeaLevelAdjustment;
        public float MaxRidgeHeight;
        public float RidgeScale;
        public float RidgeBlendSigmoidB;
        public float RidgeBlendSigmoidXOffset;
        public float HeightmapAmount = 1f;
        public float HeightmapBlend = 1f;
        public float HeightmapAdd;
        public bool OceanChannelsEnabled = true;
        public bool AshlandsGapEnabled = false;
        public bool DeepNorthGapEnabled = false;
        public bool RiversEnabled = true;
        public float ForestScale = 1f;
        public float ForestAmountOffset;
        public int BiomePrecision;
        public bool OverrideStartPosition;
        public float StartPositionX;
        public float StartPositionY;
        public float RoughmapBlend = 1f;
        public bool UseRoughInvertedAsFlat;
        public float FlatmapBlend;
        public float ForestmapMultiply = 1f;
        public float ForestmapAdd = 1f;
        public bool DisableMapEdgeDropoff;
        public bool MountainsAllowedAtCenter;
        public bool ForestFactorOverrideAllTrees;
        public bool HeightmapOverrideAll = true;
        public float HeightmapMask;
        public float HeatMapScale = 10f;
        public NoiseStackSettings BaseHeightNoise = new();

        // Non-serialized
        private ImageMapFloat? HeightMap;
        private ImageMapBiome? BiomeMap;
        private ImageMapTerrain? TerrainMap;
        private ImageMapPaint? PaintMap;
        private ImageMapFloat? LavaMap;
        private ImageMapFloat? MossMap;

        private ImageMapLocation? LocationMap;
        private ImageMapFloat? RoughMap;
        private ImageMapFloat? FlatMap;
        private ImageMapFloat? ForestMap;
        private ImageMapFloat? HeatMap;

        public bool HasHeightMap => HeightMap != null;
        public bool HasBiomeMap => BiomeMap != null;
        public bool HasLocationMap => LocationMap != null;
        public bool HasRoughMap => RoughMap != null;
        public bool HasFlatMap => FlatMap != null;
        public bool HasForestMap => ForestMap != null;
        public bool HasTerrainMap => TerrainMap != null;
        public bool HasPaintMap => PaintMap != null;
        public bool HasLavaMap => LavaMap != null;
        public bool HasMossMap => MossMap != null;
        public bool HasHeatMap => HeatMap != null;


        public bool AnyImageMap => HasHeightMap
                                   || HasRoughMap
                                   || HasFlatMap
                                   || HasBiomeMap
                                   || HasTerrainMap
                                   || HasLocationMap
                                   || HasForestMap
                                   || HasPaintMap
                                   || HasLavaMap
                                   || HasMossMap
                                   || HasHeatMap;
        public bool ShouldHeightMapOverrideAll => HasHeightMap && HeightmapOverrideAll;

        public static BetterContinentsSettings Create()
        {
            var settings = new BetterContinentsSettings();
            settings.InitSettings(ConfigEnabled.Value);
            return settings;
        }

        public static BetterContinentsSettings Disabled()
        {
            var settings = new BetterContinentsSettings();
            settings.InitSettings(false);
            return settings;
        }

        private static string GetPath(string projectDir, string projectDirFileName, string defaultFileName)
        {
            if (string.IsNullOrEmpty(projectDir))
                return CleanPath(defaultFileName);
            var path = Path.Combine(projectDir, CleanPath(projectDirFileName));
            if (File.Exists(path))
                return path;
            else
                return "";
        }

        private static readonly string HeightFile = "heightmap.png";
        private static readonly string BiomeFile = "biomemap.png";
        private static readonly string LocationFile = "locationmap.png";
        private static readonly string RoughFile = "roughmap.png";
        private static readonly string ForestFile = "forestmap.png";
        private static readonly string HeatFile = "heatmap.png";
        private static readonly string TerrainFile = "terrainmap.png";
        private static readonly string PaintFile = "paintmap.png";
        private static readonly string LavaFile = "lavamap.png";
        private static readonly string MossFile = "mossmmap.png";

        private static string HeightPath(string defaultFilename, string projectDir) => GetPath(projectDir, HeightFile, defaultFilename);
        private static string BiomePath(string defaultFilename, string projectDir) => GetPath(projectDir, BiomeFile, defaultFilename);
        private static string LocationPath(string defaultFilename, string projectDir) => GetPath(projectDir, LocationFile, defaultFilename);
        private static string RoughPath(string defaultFilename, string projectDir) => GetPath(projectDir, RoughFile, defaultFilename);
        private static string ForestPath(string defaultFilename, string projectDir) => GetPath(projectDir, ForestFile, defaultFilename);
        private static string HeatPath(string defaultFilename, string projectDir) => GetPath(projectDir, HeatFile, defaultFilename);
        private static string TerrainPath(string defaultFilename, string projectDir) => GetPath(projectDir, TerrainFile, defaultFilename);
        private static string PaintPath(string defaultFilename, string projectDir) => GetPath(projectDir, PaintFile, defaultFilename);
        private static string LavaPath(string defaultFilename, string projectDir) => GetPath(projectDir, LavaFile, defaultFilename);
        private static string MossPath(string defaultFilename, string projectDir) => GetPath(projectDir, MossFile, defaultFilename);

        private static string HeightConfigPath => HeightPath(ConfigHeightFile.Value, ConfigMapSourceDir.Value);
        private static string BiomeConfigPath => BiomePath(ConfigBiomeFile.Value, ConfigMapSourceDir.Value);
        private static string LocationConfigPath => LocationPath(ConfigLocationFile.Value, ConfigMapSourceDir.Value);
        private static string RoughConfigPath => RoughPath(ConfigRoughFile.Value, ConfigMapSourceDir.Value);
        private static string ForestConfigPath => ForestPath(ConfigForestFile.Value, ConfigMapSourceDir.Value);
        private static string HeatConfigPath => HeatPath(ConfigHeatFile.Value, ConfigMapSourceDir.Value);
        private static string TerrainConfigPath => TerrainPath(ConfigTerrainFile.Value, ConfigMapSourceDir.Value);
        private static string PaintConfigPath => PaintPath(ConfigPaintFile.Value, ConfigMapSourceDir.Value);
        private static string LavaConfigPath => LavaPath(ConfigLavaFile.Value, ConfigMapSourceDir.Value);
        private static string MossConfigPath => MossPath(ConfigMossFile.Value, ConfigMapSourceDir.Value);


        private void InitSettings(bool enabled)
        {
            Log($"Init settings for new world");

            EnabledForThisWorld = enabled;

            if (EnabledForThisWorld)
            {
                ContinentSize = ConfigContinentSize.Value;
                SeaLevel = ConfigSeaLevelAdjustment.Value;

                HeightMap = ImageMapFloat.Create(HeightConfigPath);
                HeightmapAmount = ConfigHeightmapAmount.Value;
                HeightmapBlend = ConfigHeightmapBlend.Value;
                HeightmapAdd = ConfigHeightmapAdd.Value;
                HeightmapMask = ConfigHeightmapMask.Value;
                HeightmapOverrideAll = ConfigHeightmapOverrideAll.Value;

                BaseHeightNoise = new();

                BiomeMap = ImageMapBiome.Create(BiomeConfigPath);

                OceanChannelsEnabled = ConfigOceanChannelsEnabled.Value;
                AshlandsGapEnabled = ConfigAshlandsGapEnabled.Value;
                DeepNorthGapEnabled = ConfigDeepNorthGapEnabled.Value;
                RiversEnabled = ConfigRiversEnabled.Value;

                ForestScaleFactor = ConfigForestScale.Value;
                ForestAmount = ConfigForestAmount.Value;
                ForestFactorOverrideAllTrees = ConfigForestFactorOverrideAllTrees.Value;

                OverrideStartPosition = ConfigOverrideStartPosition.Value;
                StartPositionX = ConfigStartPositionX.Value;
                StartPositionY = ConfigStartPositionY.Value;

                LocationMap = ImageMapLocation.Create(LocationConfigPath);

                RoughMap = ImageMapFloat.Create(RoughConfigPath);
                RoughmapBlend = ConfigRoughmapBlend.Value;

                ForestMap = ImageMapFloat.Create(ForestConfigPath);
                ForestmapAdd = ConfigForestmapAdd.Value;
                ForestmapMultiply = ConfigForestmapMultiply.Value;
                MapEdgeDropoff = ConfigMapEdgeDropoff.Value;
                MountainsAllowedAtCenter = ConfigMountainsAllowedAtCenter.Value;
                BiomePrecision = ConfigBiomePrecision.Value;

                TerrainMap = ImageMapTerrain.Create(TerrainConfigPath);

                PaintMap = ImageMapPaint.Create(PaintConfigPath);
                LavaMap = ImageMapFloat.Create(LavaConfigPath);
                MossMap = ImageMapFloat.Create(MossConfigPath);

                HeatMap = ImageMapFloat.Create(HeatConfigPath);
                HeatMapScale = ConfigHeatScale.Value;
            }
            DynamicPatch();
        }

        #region Setters
        public float ContinentSize
        {
            set => GlobalScale = FeatureScaleCurve(value);
            get => InvFeatureScaleCurve(GlobalScale);
        }

        public float SeaLevel
        {
            set => SeaLevelAdjustment = Mathf.Lerp(1f, -1f, value);
            get => Mathf.InverseLerp(1f, -1f, SeaLevelAdjustment);
        }

        public bool MapEdgeDropoff
        {
            set => DisableMapEdgeDropoff = !value;
            get => !DisableMapEdgeDropoff;
        }

        public float ForestScaleFactor
        {
            set => ForestScale = FeatureScaleCurve(value);
            get => InvFeatureScaleCurve(ForestScale);
        }

        public float ForestAmount
        {
            set => ForestAmountOffset = Mathf.Lerp(1, -1, value);
            get => Mathf.InverseLerp(1, -1, ForestAmountOffset);
        }

        public void SetHeightPath(string path) => HeightMap = ImageMapFloat.Create(path);
        public string GetHeightPath() => HeightMap?.FilePath ?? string.Empty;

        public string ResolveHeightPath(string path) => ResolvePath(path, HeightFile);

        public void SetBiomePath(string path) => BiomeMap = ImageMapBiome.Create(path);
        public string GetBiomePath() => BiomeMap?.FilePath ?? string.Empty;
        public string ResolveBiomePath(string path) => ResolvePath(path, BiomeFile);

        public void SetTerrainPath(string path) => TerrainMap = ImageMapTerrain.Create(path);
        public string GetTerrainPath() => TerrainMap?.FilePath ?? string.Empty;
        public string ResolveTerrainPath(string path) => ResolvePath(path, TerrainFile);

        public void SetLocationPath(string path) => LocationMap = ImageMapLocation.Create(path);
        public string GetLocationPath() => LocationMap?.FilePath ?? string.Empty;
        public string ResolveLocationPath(string path) => ResolvePath(path, LocationFile);

        public void SetRoughPath(string path) => RoughMap = ImageMapFloat.Create(path);
        public string GetRoughPath() => RoughMap?.FilePath ?? string.Empty;
        public string ResolveRoughPath(string path) => ResolvePath(path, RoughFile);

        public void SetForestPath(string path) => ForestMap = ImageMapFloat.Create(path);
        public string GetForestPath() => ForestMap?.FilePath ?? string.Empty;
        public string ResolveForestPath(string path) => ResolvePath(path, ForestFile);

        public void SetHeatPath(string path) => HeatMap = ImageMapFloat.Create(path);
        public string GetHeatPath() => HeatMap?.FilePath ?? string.Empty;
        public string ResolveHeatPath(string path) => ResolvePath(path, HeatFile);

        public void SetPaintPath(string path) => PaintMap = ImageMapPaint.Create(path);
        public string GetPaintPath() => PaintMap?.FilePath ?? string.Empty;
        public string ResolvePaintPath(string path) => ResolvePath(path, PaintFile);

        public void SetLavaPath(string path) => LavaMap = ImageMapFloat.Create(path);
        public string GetLavaPath() => LavaMap?.FilePath ?? string.Empty;
        public string ResolveLavaPath(string path) => ResolvePath(path, LavaFile);

        public void SetMossPath(string path) => MossMap = ImageMapFloat.Create(path);
        public string GetMossPath() => MossMap?.FilePath ?? string.Empty;
        public string ResolveMossPath(string path) => ResolvePath(path, MossFile);

        private string ResolvePath(string path, string defaultName)
        {
            path = CleanPath(path);
            if (File.Exists(path))
                return path;
            if (string.IsNullOrEmpty(path))
            {
                if (File.Exists(Path.Combine(ConfigMapSourceDir.Value, defaultName)))
                    return Path.Combine(ConfigMapSourceDir.Value, defaultName);
                return "";
            }
            var name = Path.GetFileName(path);
            var directory = Path.GetDirectoryName(path);
            if (name != "" && File.Exists(Path.Combine(ConfigMapSourceDir.Value, name)))
                return Path.Combine(ConfigMapSourceDir.Value, name);
            if (directory != "" && File.Exists(Path.Combine(directory, defaultName)))
                return Path.Combine(directory, defaultName);
            return "";
        }
        #endregion

        private static float FeatureScaleCurve(float x) => ScaleRange(Gamma(x, 0.726965071031f), 0.2f, 3f);
        private static float InvFeatureScaleCurve(float y) => InvGamma(InvScaleRange(y, 0.2f, 3f), 0.726965071031f);

        private static float Gamma(float x, float h) => Mathf.Pow(x, Mathf.Pow(1 - h * 0.5f + 0.25f, 6f));
        private static float InvGamma(float g, float h) => Mathf.Pow(g, 1 / Mathf.Pow(1 - h * 0.5f + 0.25f, 6f));

        private static float ScaleRange(float x, float a, float b) => a + (b - a) * (1 - x);
        private static float InvScaleRange(float y, float a, float b) => 1f - (y - a) / (b - a);


        public void Dump(Action<string>? output = null)
        {
            output ??= Log;

            if (EnabledForThisWorld)
            {
                output($"Version {Version}");
                output($"Continent size {ContinentSize}");
                output($"Mountains amount {MountainsAmount}");
                output($"Sea level adjustment {SeaLevel}");
                output($"Ocean channels enabled {OceanChannelsEnabled}");
                output($"Ashlands gap enabled {AshlandsGapEnabled}");
                output($"Deep North gap enabled {DeepNorthGapEnabled}");
                output($"Rivers enabled {RiversEnabled}");

                output($"Map edge dropoff {MapEdgeDropoff}");
                output($"Mountains allowed at center {MountainsAllowedAtCenter}");

                if (HeightMap != null)
                {
                    output($"Heightmap file ({HeightMap.Size}) {HeightMap.FilePath}");
                    output($"Heightmap amount {HeightmapAmount}, blend {HeightmapBlend}, add {HeightmapAdd}, mask {HeightmapMask}");
                    if (HeightmapOverrideAll)
                    {
                        output($"Heightmap overrides ALL");
                    }
                }
                else output($"Heightmap disabled");

                if (Version < 7)
                {
                    if (UseRoughInvertedAsFlat)
                    {
                        output($"Using inverted Roughmap as Flatmap");
                    }
                    else
                    {
                        if (FlatMap != null)
                        {
                            output($"Flatmap file {FlatMap.FilePath}");
                            output($"Flatmap size {FlatMap.Size}x{FlatMap.Size}, blend {FlatmapBlend}");
                        }
                        else
                        {
                            output($"Flatmap disabled");
                        }
                    }
                }
                else
                {
                    output($"Base height noise stack:");
                    BaseHeightNoise.Dump(str => output($"    {str}"));
                }

                if (RoughMap != null)
                {
                    output($"Roughmap file ({RoughMap.Size}) {RoughMap.FilePath}");
                    output($"Roughmap blend {RoughmapBlend}");
                }
                else output($"Roughmap disabled");

                if (BiomeMap != null)
                {
                    output($"Biomemap file ({BiomeMap.Size}) {BiomeMap.FilePath}");
                    if (BiomePrecision > 0)
                        output($"Biome precision {BiomePrecision}");
                }
                else output($"Biomemap disabled");

                if (TerrainMap != null)
                {
                    output($"Terrainmap file ({TerrainMap.Size}) {TerrainMap.FilePath}");
                    output($"Terrain map colors {TerrainMap.SourceColors}");
                }
                else output($"Terrainmap disabled");

                output($"Forest scale {ForestScaleFactor}");
                output($"Forest amount {ForestAmount}");
                if (ForestMap != null)
                {
                    output($"Forestmap file ({ForestMap.Size}) {ForestMap.FilePath}");
                    output($"Forestmap multiply {ForestmapMultiply}, add {ForestmapAdd}");
                    if (ForestFactorOverrideAllTrees)
                        output($"Forest Factor overrides all trees");
                    else
                        output($"Forest Factor applies only to the same trees as vanilla");
                }
                else output($"Forestmap disabled");

                if (LocationMap != null)
                {
                    output($"Location file ({LocationMap.Size}) {LocationMap.FilePath}");
                    output($"Locationmap includes spawns for {LocationMap.RemainingAreas.Count} types");
                }
                else output($"Locationmap disabled");

                if (OverrideStartPosition) output($"StartPosition {StartPositionX}, {StartPositionY}");

                if (PaintMap != null)
                {
                    output($"Paintmap file ({PaintMap.Size}) {PaintMap.FilePath}");
                    output($"Paintmap colors {PaintMap.SourceColors}");
                }
                else output($"Paintmap disabled");

                if (LavaMap != null)
                    output($"Lavamap file ({LavaMap.Size}) {LavaMap.FilePath}");
                else output($"Lavamap disabled");

                if (MossMap != null)
                    output($"Mossmmap file ({MossMap.Size}) {MossMap.FilePath}");
                else output($"Mossmmap disabled");

                if (HeatMap != null)
                {
                    output($"Heatmap file {HeatMap.FilePath}");
                    output($"Heatmap size {HeatMap.Size}x{HeatMap.Size}");
                    output($"Heatmap scale {HeatMapScale}");
                }
                else output($"Heatmap disabled");
            }
            else
            {
                output($"DISABLED");
            }
        }



        public static BetterContinentsSettings Load(ZPackage pkg)
        {
            var settings = new BetterContinentsSettings();
            settings.Deserialize(pkg);
            return settings;
        }

        public static BetterContinentsSettings Load(string path)
        {
            using BinaryReader binaryReader = new(File.OpenRead(path));
            int count = binaryReader.ReadInt32();
            return Load(new ZPackage(binaryReader.ReadBytes(count)));
        }

        public void Save(string path)
        {
            var zpackage = new ZPackage();
            Serialize(zpackage, false);

            byte[] binaryData = zpackage.GetArray();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using BinaryWriter binaryWriter = new(File.Create(path));
            binaryWriter.Write(binaryData.Length);
            binaryWriter.Write(binaryData);
        }

        public static BetterContinentsSettings LoadFromSource(string path, FileHelpers.FileSource fileSource)
        {
            FileReader fileReader;
            try
            {
                fileReader = new FileReader(path, fileSource);
            }
            catch
            {
                Log($"Couldn't find loaded settings for this world at {path}, mod is disabled.");
                return Disabled();
            }

            try
            {
                var binaryReader = (BinaryReader)fileReader;
                int count = binaryReader.ReadInt32();
                return Load(new ZPackage(binaryReader.ReadBytes(count)));
            }
            catch (Exception e)
            {
                LogError($"Failed to load settings from {path}: {e.Message}");
                return Disabled();
            }
            finally
            {
                fileReader.Dispose();
            }
        }

        public void SaveToSource(string path, FileHelpers.FileSource fileSource)
        {
            var zpackage = new ZPackage();
            Serialize(zpackage, false);

            byte[] binaryData = zpackage.GetArray();
            var fileWriter = new FileWriter(path, FileHelpers.FileHelperType.Binary, fileSource);
            fileWriter.m_binary.Write(binaryData.Length);
            fileWriter.m_binary.Write(binaryData);
            fileWriter.Finish();
        }

        public bool ApplyTerrainMap(float x, float y, ref Color color)
        {
            if (TerrainMap == null) return false;
            var normalized = WorldToNormalized(x, y);
            return TerrainMap.TryGetValue(normalized.x, normalized.y, out color);
        }

        public void ApplyPaintMap(float x, float y, Heightmap.Biome biome, ref Color mask)
        {
            var normalized = WorldToNormalized(x, y);
            if (PaintMap != null && PaintMap.TryGetValue(normalized.x, normalized.y, out var paint))
            {
                mask.r = paint.r;
                mask.g = paint.g;
                mask.b = paint.b;
                if (paint.a != 1f)
                    mask.a = paint.a;
            };
            if (LavaMap != null && biome == Heightmap.Biome.AshLands)
                mask.a = LavaMap.GetValue(normalized.x, normalized.y);
            else if (MossMap != null && biome == Heightmap.Biome.Mistlands)
                mask.a = MossMap.GetValue(normalized.x, normalized.y);

        }
        public float ApplyHeightmap(float x, float y, float height)
        {
            if (HeightMap == null || (HeightmapBlend == 0 && HeightmapAdd == 0 && HeightmapMask == 0))
            {
                return height;
            }

            float h = HeightMap.GetValue(x, y);
            float blendedHeight = Mathf.Lerp(height, h * HeightmapAmount, HeightmapBlend);
            return Mathf.Lerp(blendedHeight, blendedHeight * h, HeightmapMask) + h * HeightmapAdd;
        }

        public float ApplyRoughmap(float x, float y, float smoothHeight, float roughHeight)
        {
            if (RoughMap == null)
            {
                return roughHeight;
            }

            float r = RoughMap.GetValue(x, y);
            return Mathf.Lerp(smoothHeight, roughHeight, r * RoughmapBlend);
        }

        public float ApplyFlatmap(float x, float y, float flatHeight, float height)
        {
            if (Settings.ShouldHeightMapOverrideAll)
            {
                return flatHeight;
            }
            var image = UseRoughInvertedAsFlat ? RoughMap : FlatMap;
            if (image == null || RoughmapBlend == 0)
            {
                return height;
            }

            float f = UseRoughInvertedAsFlat ? 1 - image.GetValue(x, y) : image.GetValue(x, y);
            return Mathf.Lerp(height, flatHeight, f * FlatmapBlend);
        }
        public float ApplyHeatmap(float x, float y) => HeatMapScale * (HeatMap?.GetValue(x, y) ?? 0);

        public float ApplyForest(float x, float y, float forest)
        {
            float finalValue = forest;
            if (ForestMap != null)
            {
                // Map forest from weird vanilla range to 0 - 1
                float normalizedForestValue = Mathf.InverseLerp(1.850145f, 0.145071f, forest);
                float fmap = ForestMap.GetValue(x, y);
                float calculatedValue = Mathf.Lerp(normalizedForestValue, normalizedForestValue * fmap, ForestmapMultiply) + fmap * ForestmapAdd;
                // Map back to weird values
                finalValue = Mathf.Lerp(1.850145f, 0.145071f, calculatedValue);
            }

            // Clamp between the known good values (that vanilla generates)
            finalValue = Mathf.Clamp(finalValue + ForestAmountOffset, 0.145071f, 1.850145f);
            return finalValue;
        }

        public Heightmap.Biome GetBiomeOverride(float mapX, float mapY) => BiomeMap?.GetValue(mapX, mapY) ?? 0;

        public Vector2? FindSpawn(string spawn) => LocationMap?.FindSpawn(spawn);
        public IEnumerable<Vector2> GetAllSpawns(string spawn) => LocationMap?.GetAllSpawns(spawn) ?? [];

        public void ReloadHeightMap()
        {
            if (HeightMap == null) return;
            if (!HeightMap.LoadSourceImage())
            {
                if (!File.Exists(HeightConfigPath) || File.Exists(HeightMap.FilePath)) return;
                LogWarning($"Cannot find image {HeightMap.FilePath}: Using default path from config.");
                HeightMap.FilePath = HeightConfigPath;
                if (!HeightMap.LoadSourceImage()) return;
            }
            HeightMap.CreateMap();
        }

        public void ReloadBiomeMap()
        {
            if (BiomeMap == null) return;
            if (!BiomeMap.LoadSourceImage())
            {
                if (!File.Exists(BiomeConfigPath) || File.Exists(BiomeMap.FilePath)) return;
                LogWarning($"Cannot find image {BiomeMap.FilePath}: Using default path from config.");
                BiomeMap.FilePath = BiomeConfigPath;
                if (!BiomeMap.LoadSourceImage()) return;
            }
            BiomeMap.CreateMap();
        }

        public void ReloadLocationMap()
        {
            if (LocationMap == null) return;
            if (!LocationMap.LoadSourceImage())
            {
                if (!File.Exists(LocationConfigPath) || File.Exists(LocationMap.FilePath)) return;
                LogWarning($"Cannot find image {LocationMap.FilePath}: Using default path from config.");
                LocationMap.FilePath = LocationConfigPath;
                if (!LocationMap.LoadSourceImage()) return;
            }
            LocationMap.CreateMap();
        }

        public void ReloadRoughMap()
        {
            if (RoughMap == null) return;
            if (!RoughMap.LoadSourceImage())
            {
                if (!File.Exists(RoughConfigPath) || File.Exists(RoughMap.FilePath)) return;
                LogWarning($"Cannot find image {RoughMap.FilePath}: Using default path from config.");
                RoughMap.FilePath = RoughConfigPath;
                if (!RoughMap.LoadSourceImage()) return;
            }
            RoughMap.CreateMap();
        }

        public void ReloadFlatMap()
        {
            if (UseRoughInvertedAsFlat)
            {
                ReloadRoughMap();
                return;
            }
            if (FlatMap == null) return;
            if (!FlatMap.LoadSourceImage())
            {
                if (!File.Exists(RoughConfigPath) || File.Exists(FlatMap.FilePath)) return;
                LogWarning($"Cannot find image {FlatMap.FilePath}: Using default path from config.");
                FlatMap.FilePath = RoughConfigPath;
                if (!FlatMap.LoadSourceImage()) return;
            }
            FlatMap.CreateMap();
        }

        public void ReloadForestMap()
        {
            if (ForestMap == null) return;
            if (!ForestMap.LoadSourceImage())
            {
                if (!File.Exists(ForestConfigPath) || File.Exists(ForestMap.FilePath)) return;
                LogWarning($"Cannot find image {ForestMap.FilePath}: Using default path from config.");
                ForestMap.FilePath = ForestConfigPath;
                if (!ForestMap.LoadSourceImage()) return;
            }
            ForestMap.CreateMap();
        }
        public void ReloadHeatMap()
        {
            if (HeatMap == null) return;
            if (!HeatMap.LoadSourceImage())
            {
                if (!File.Exists(HeatConfigPath) || File.Exists(HeatMap.FilePath)) return;
                LogWarning($"Cannot find image {HeatMap.FilePath}: Using default path from config.");
                HeatMap.FilePath = ForestConfigPath;
                if (!HeatMap.LoadSourceImage()) return;
            }
            HeatMap.CreateMap();
        }

        public void ReloadTerrainMap()
        {
            if (TerrainMap == null) return;
            if (!TerrainMap.LoadSourceImage())
            {
                if (!File.Exists(TerrainConfigPath) || File.Exists(TerrainMap.FilePath)) return;
                LogWarning($"Cannot find image {TerrainMap.FilePath}: Using default path from config.");
                TerrainMap.FilePath = TerrainConfigPath;
                if (!TerrainMap.LoadSourceImage()) return;
            }
            TerrainMap.CreateMap();
        }

        public void ReloadPaintMap()
        {
            if (PaintMap == null) return;
            if (!PaintMap.LoadSourceImage())
            {
                if (!File.Exists(PaintConfigPath) || File.Exists(PaintMap.FilePath)) return;
                LogWarning($"Cannot find image {PaintMap.FilePath}: Using default path from config.");
                PaintMap.FilePath = PaintConfigPath;
                if (!PaintMap.LoadSourceImage()) return;
            }
            PaintMap.CreateMap();
        }

        public void ReloadLavaMap()
        {
            if (LavaMap == null) return;
            if (!LavaMap.LoadSourceImage())
            {
                if (!File.Exists(LavaConfigPath) || File.Exists(LavaMap.FilePath)) return;
                LogWarning($"Cannot find image {LavaMap.FilePath}: Using default path from config.");
                LavaMap.FilePath = LavaConfigPath;
                if (!LavaMap.LoadSourceImage()) return;
            }
            LavaMap.CreateMap();
        }

        public void ReloadMossMap()
        {
            if (MossMap == null) return;
            if (!MossMap.LoadSourceImage())
            {
                if (!File.Exists(MossConfigPath) || File.Exists(MossMap.FilePath)) return;
                LogWarning($"Cannot find image {MossMap.FilePath}: Using default path from config.");
                MossMap.FilePath = MossConfigPath;
                if (!MossMap.LoadSourceImage()) return;
            }
            MossMap.CreateMap();
        }
    }
}
