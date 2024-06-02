using System;
using System.Collections.Generic;
using System.IO;
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
        private ImageMapFloat? Heightmap;
        private ImageMapBiome? Biomemap;
        private ImageMapColor? Paintmap;

        private ImageMapLocation? Locationmap;
        private ImageMapFloat? Roughmap;
        private ImageMapFloat? Flatmap;
        private ImageMapFloat? Forestmap;
        private ImageMapFloat? Heatmap;

        public bool HasHeightmap => Heightmap != null;
        public bool HasBiomemap => Biomemap != null;
        public bool HasLocationmap => Locationmap != null;
        public bool HasRoughmap => Roughmap != null;
        public bool HasFlatmap => Flatmap != null;
        public bool HasForestmap => Forestmap != null;
        public bool HasPaintmap => Paintmap != null;
        public bool HasHeatmap => Heatmap != null;


        public bool AnyImageMap => HasHeightmap
                                   || HasRoughmap
                                   || HasFlatmap
                                   || HasBiomemap
                                   || HasLocationmap
                                   || HasForestmap
                                   || HasPaintmap
                                   || HasHeatmap;
        public bool ShouldHeightmapOverrideAll => HasHeightmap && HeightmapOverrideAll;

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

        private static readonly string HeightFile = "Heightmap.png";
        private static readonly string BiomeFile = "Biomemap.png";
        private static readonly string LocationFile = "Locationmap.png";
        private static readonly string RoughFile = "Roughmap.png";
        private static readonly string ForestFile = "Forestmap.png";
        private static readonly string HeatFile = "Heatmap.png";
        private static readonly string PaintFile = "Paintmap.png";

        private static string HeightPath(string defaultFilename, string projectDir) => GetPath(projectDir, HeightFile, defaultFilename);
        private static string BiomePath(string defaultFilename, string projectDir) => GetPath(projectDir, BiomeFile, defaultFilename);
        private static string LocationPath(string defaultFilename, string projectDir) => GetPath(projectDir, LocationFile, defaultFilename);
        private static string RoughPath(string defaultFilename, string projectDir) => GetPath(projectDir, RoughFile, defaultFilename);
        private static string ForestPath(string defaultFilename, string projectDir) => GetPath(projectDir, ForestFile, defaultFilename);
        private static string HeatPath(string defaultFilename, string projectDir) => GetPath(projectDir, HeatFile, defaultFilename);
        private static string PaintPath(string defaultFilename, string projectDir) => GetPath(projectDir, PaintFile, defaultFilename);

        private static string HeightConfigPath => HeightPath(ConfigHeightFile.Value, ConfigMapSourceDir.Value);
        private static string BiomeConfigPath => BiomePath(ConfigBiomeFile.Value, ConfigMapSourceDir.Value);
        private static string LocationConfigPath => LocationPath(ConfigLocationFile.Value, ConfigMapSourceDir.Value);
        private static string RoughConfigPath => RoughPath(ConfigRoughFile.Value, ConfigMapSourceDir.Value);
        private static string ForestConfigPath => ForestPath(ConfigForestFile.Value, ConfigMapSourceDir.Value);
        private static string HeatConfigPath => HeatPath(ConfigHeatFile.Value, ConfigMapSourceDir.Value);
        private static string PaintConfigPath => PaintPath(ConfigPaintFile.Value, ConfigMapSourceDir.Value);


        private void InitSettings(bool enabled)
        {
            Log($"Init settings for new world");

            EnabledForThisWorld = enabled;

            if (EnabledForThisWorld)
            {
                ContinentSize = ConfigContinentSize.Value;
                SeaLevel = ConfigSeaLevelAdjustment.Value;

                Heightmap = ImageMapFloat.Create(HeightConfigPath);
                HeightmapAmount = ConfigHeightmapAmount.Value;
                HeightmapBlend = ConfigHeightmapBlend.Value;
                HeightmapAdd = ConfigHeightmapAdd.Value;
                HeightmapMask = ConfigHeightmapMask.Value;
                HeightmapOverrideAll = ConfigHeightmapOverrideAll.Value;

                BaseHeightNoise = new();

                Biomemap = ImageMapBiome.Create(BiomeConfigPath);

                OceanChannelsEnabled = ConfigOceanChannelsEnabled.Value;
                RiversEnabled = ConfigRiversEnabled.Value;

                ForestScaleFactor = ConfigForestScale.Value;
                ForestAmount = ConfigForestAmount.Value;
                ForestFactorOverrideAllTrees = ConfigForestFactorOverrideAllTrees.Value;

                OverrideStartPosition = ConfigOverrideStartPosition.Value;
                StartPositionX = ConfigStartPositionX.Value;
                StartPositionY = ConfigStartPositionY.Value;

                Locationmap = ImageMapLocation.Create(LocationConfigPath);

                Roughmap = ImageMapFloat.Create(RoughConfigPath);
                RoughmapBlend = ConfigRoughmapBlend.Value;

                Forestmap = ImageMapFloat.Create(ForestConfigPath);
                ForestmapAdd = ConfigForestmapAdd.Value;
                ForestmapMultiply = ConfigForestmapMultiply.Value;

                Paintmap = ImageMapColor.Create(PaintConfigPath);

                MapEdgeDropoff = ConfigMapEdgeDropoff.Value;
                MountainsAllowedAtCenter = ConfigMountainsAllowedAtCenter.Value;
                BiomePrecision = ConfigBiomePrecision.Value;

                Paintmap = ImageMapColor.Create(PaintConfigPath);

                Heatmap = ImageMapFloat.Create(HeatConfigPath);
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

        public void SetHeightPath(string path) => Heightmap = ImageMapFloat.Create(path);
        public string GetHeightPath() => Heightmap?.FilePath ?? string.Empty;

        public string ResolveHeightPath(string path) => ResolvePath(path, HeightFile);

        public void SetBiomePath(string path) => Biomemap = ImageMapBiome.Create(path);
        public string GetBiomePath() => Biomemap?.FilePath ?? string.Empty;
        public string ResolveBiomePath(string path) => ResolvePath(path, BiomeFile);

        public void SetLocationPath(string path) => Locationmap = ImageMapLocation.Create(path);
        public string GetLocationPath() => Locationmap?.FilePath ?? string.Empty;
        public string ResolveLocationPath(string path) => ResolvePath(path, LocationFile);

        public void SetRoughPath(string path) => Roughmap = ImageMapFloat.Create(path);
        public string GetRoughPath() => Roughmap?.FilePath ?? string.Empty;
        public string ResolveRoughPath(string path) => ResolvePath(path, RoughFile);

        public void SetForestPath(string path) => Forestmap = ImageMapFloat.Create(path);
        public string GetForestPath() => Forestmap?.FilePath ?? string.Empty;
        public string ResolveForestPath(string path) => ResolvePath(path, ForestFile);

        public void SetHeatPath(string path) => Heatmap = ImageMapFloat.Create(path);
        public string GetHeatPath() => Heatmap?.FilePath ?? string.Empty;
        public string ResolveHeatPath(string path) => ResolvePath(path, HeatFile);

        public void SetPaintPath(string path) => Paintmap = ImageMapColor.Create(path);
        public string GetPaintPath() => Paintmap?.FilePath ?? string.Empty;

        public string ResolvePaintPath(string path) => ResolvePath(path, PaintFile);
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
                output($"Rivers enabled {RiversEnabled}");

                output($"Map edge dropoff {MapEdgeDropoff}");
                output($"Mountains allowed at center {MountainsAllowedAtCenter}");

                if (Heightmap != null)
                {
                    output($"Heightmap file {Heightmap.FilePath}");
                    output($"Heightmap size {Heightmap.Size}x{Heightmap.Size}, amount {HeightmapAmount}, blend {HeightmapBlend}, add {HeightmapAdd}, mask {HeightmapMask}");
                    if (HeightmapOverrideAll)
                    {
                        output($"Heightmap overrides ALL");
                    }
                }
                else
                {
                    output($"Heightmap disabled");
                }

                if (Version < 7)
                {
                    if (UseRoughInvertedAsFlat)
                    {
                        output($"Using inverted Roughmap as Flatmap");
                    }
                    else
                    {
                        if (Flatmap != null)
                        {
                            output($"Flatmap file {Flatmap.FilePath}");
                            output($"Flatmap size {Flatmap.Size}x{Flatmap.Size}, blend {FlatmapBlend}");
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

                if (Roughmap != null)
                {
                    output($"Roughmap file {Roughmap.FilePath}");
                    output($"Roughmap size {Roughmap.Size}x{Roughmap.Size}, blend {RoughmapBlend}");
                }
                else
                {
                    output($"Roughmap disabled");
                }

                output($"Biome precision {BiomePrecision}");
                if (Biomemap != null)
                {
                    output($"Biomemap file {Biomemap.FilePath}");
                    output($"Biomemap size {Biomemap.Size}x{Biomemap.Size}");
                }
                else
                {
                    output($"Biomemap disabled");
                }
                output($"Forest scale {ForestScaleFactor}");
                output($"Forest amount {ForestAmount}");
                if (Forestmap != null)
                {
                    output($"Forestmap file {Forestmap.FilePath}");
                    output($"Forestmap size {Forestmap.Size}x{Forestmap.Size}, multiply {ForestmapMultiply}, add {ForestmapAdd}");
                    if (ForestFactorOverrideAllTrees)
                    {
                        output($"Forest Factor overrides all trees");
                    }
                    else
                    {
                        output($"Forest Factor applies only to the same trees as vanilla");
                    }
                }
                else
                {
                    output($"Forestmap disabled");
                }

                if (Locationmap != null)
                {
                    output($"Location file {Locationmap.FilePath}");
                    output($"Locationmap includes spawns for {Locationmap.RemainingAreas.Count} types");
                }
                else
                {
                    output($"Locationmap disabled");
                }

                if (OverrideStartPosition)
                {
                    output($"StartPosition {StartPositionX}, {StartPositionY}");
                }

                if (Paintmap != null)
                {
                    output($"Paintmap file {Paintmap.FilePath}");
                    output($"Paintmap size {Paintmap.Size}x{Paintmap.Size}");
                }
                else
                {
                    output($"Paintmap disabled");
                }

                if (Heatmap != null)
                {
                    output($"Heatmap file {Heatmap.FilePath}");
                    output($"Heatmap size {Heatmap.Size}x{Heatmap.Size}");
                    output($"Heatmap scale {HeatMapScale}");
                }
                else
                {
                    output($"Heatmap disabled");
                }
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
            var fileReader = new FileReader(path, fileSource);
            try
            {
                var binaryReader = (BinaryReader)fileReader;
                int count = binaryReader.ReadInt32();
                return Load(new ZPackage(binaryReader.ReadBytes(count)));
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

        public Color ApplyPaintMap(float x, float y)
        {
            if (Paintmap == null) return Color.white;
            var normalized = WorldToNormalized(x, y);
            return Paintmap.GetValue(normalized.x, normalized.y);
        }
        public float ApplyHeightmap(float x, float y, float height)
        {
            if (Heightmap == null || (HeightmapBlend == 0 && HeightmapAdd == 0 && HeightmapMask == 0))
            {
                return height;
            }

            float h = Heightmap.GetValue(x, y);
            float blendedHeight = Mathf.Lerp(height, h * HeightmapAmount, HeightmapBlend);
            return Mathf.Lerp(blendedHeight, blendedHeight * h, HeightmapMask) + h * HeightmapAdd;
        }

        public float ApplyRoughmap(float x, float y, float smoothHeight, float roughHeight)
        {
            if (Roughmap == null)
            {
                return roughHeight;
            }

            float r = Roughmap.GetValue(x, y);
            return Mathf.Lerp(smoothHeight, roughHeight, r * RoughmapBlend);
        }

        public float ApplyFlatmap(float x, float y, float flatHeight, float height)
        {
            if (Settings.ShouldHeightmapOverrideAll)
            {
                return flatHeight;
            }
            var image = UseRoughInvertedAsFlat ? Roughmap : Flatmap;
            if (image == null || RoughmapBlend == 0)
            {
                return height;
            }

            float f = UseRoughInvertedAsFlat ? 1 - image.GetValue(x, y) : image.GetValue(x, y);
            return Mathf.Lerp(height, flatHeight, f * FlatmapBlend);
        }
        public float ApplyHeatmap(float x, float y) => HeatMapScale * (Heatmap?.GetValue(x, y) ?? 0);

        public float ApplyForest(float x, float y, float forest)
        {
            float finalValue = forest;
            if (Forestmap != null)
            {
                // Map forest from weird vanilla range to 0 - 1
                float normalizedForestValue = Mathf.InverseLerp(1.850145f, 0.145071f, forest);
                float fmap = Forestmap.GetValue(x, y);
                float calculatedValue = Mathf.Lerp(normalizedForestValue, normalizedForestValue * fmap, ForestmapMultiply) + fmap * ForestmapAdd;
                // Map back to weird values
                finalValue = Mathf.Lerp(1.850145f, 0.145071f, calculatedValue);
            }

            // Clamp between the known good values (that vanilla generates)
            finalValue = Mathf.Clamp(finalValue + ForestAmountOffset, 0.145071f, 1.850145f);
            return finalValue;
        }

        public Heightmap.Biome GetBiomeOverride(float mapX, float mapY) => Biomemap?.GetValue(mapX, mapY) ?? 0;

        public Vector2? FindSpawn(string spawn) => Locationmap?.FindSpawn(spawn);
        public IEnumerable<Vector2> GetAllSpawns(string spawn) => Locationmap?.GetAllSpawns(spawn) ?? [];

        public void ReloadHeightmap()
        {
            if (Heightmap == null) return;
            if (!Heightmap.LoadSourceImage())
            {
                if (!File.Exists(HeightConfigPath) || File.Exists(Heightmap.FilePath)) return;
                LogWarning($"Cannot find image {Heightmap.FilePath}: Using default path from config.");
                Heightmap.FilePath = HeightConfigPath;
                if (!Heightmap.LoadSourceImage()) return;
            }
            Heightmap.CreateMap();
        }

        public void ReloadBiomemap()
        {
            if (Biomemap == null) return;
            if (!Biomemap.LoadSourceImage())
            {
                if (!File.Exists(BiomeConfigPath) || File.Exists(Biomemap.FilePath)) return;
                LogWarning($"Cannot find image {Biomemap.FilePath}: Using default path from config.");
                Biomemap.FilePath = BiomeConfigPath;
                if (!Biomemap.LoadSourceImage()) return;
            }
            Biomemap.CreateMap();
        }

        public void ReloadLocationmap()
        {
            if (Locationmap == null) return;
            if (!Locationmap.LoadSourceImage())
            {
                if (!File.Exists(LocationConfigPath) || File.Exists(Locationmap.FilePath)) return;
                LogWarning($"Cannot find image {Locationmap.FilePath}: Using default path from config.");
                Locationmap.FilePath = LocationConfigPath;
                if (!Locationmap.LoadSourceImage()) return;
            }
            Locationmap.CreateMap();
        }

        public void ReloadRoughmap()
        {
            if (Roughmap == null) return;
            if (!Roughmap.LoadSourceImage())
            {
                if (!File.Exists(RoughConfigPath) || File.Exists(Roughmap.FilePath)) return;
                LogWarning($"Cannot find image {Roughmap.FilePath}: Using default path from config.");
                Roughmap.FilePath = RoughConfigPath;
                if (!Roughmap.LoadSourceImage()) return;
            }
            Roughmap.CreateMap();
        }

        public void ReloadFlatmap()
        {
            if (UseRoughInvertedAsFlat)
            {
                ReloadRoughmap();
                return;
            }
            if (Flatmap == null) return;
            if (!Flatmap.LoadSourceImage())
            {
                if (!File.Exists(RoughConfigPath) || File.Exists(Flatmap.FilePath)) return;
                LogWarning($"Cannot find image {Flatmap.FilePath}: Using default path from config.");
                Flatmap.FilePath = RoughConfigPath;
                if (!Flatmap.LoadSourceImage()) return;
            }
            Flatmap.CreateMap();
        }

        public void ReloadForestmap()
        {
            if (Forestmap == null) return;
            if (!Forestmap.LoadSourceImage())
            {
                if (!File.Exists(ForestConfigPath) || File.Exists(Forestmap.FilePath)) return;
                LogWarning($"Cannot find image {Forestmap.FilePath}: Using default path from config.");
                Forestmap.FilePath = ForestConfigPath;
                if (!Forestmap.LoadSourceImage()) return;
            }
            Forestmap.CreateMap();
        }
        public void ReloadHeatmap()
        {
            if (Heatmap == null) return;
            if (!Heatmap.LoadSourceImage())
            {
                if (!File.Exists(HeatConfigPath) || File.Exists(Heatmap.FilePath)) return;
                LogWarning($"Cannot find image {Heatmap.FilePath}: Using default path from config.");
                Heatmap.FilePath = ForestConfigPath;
                if (!Heatmap.LoadSourceImage()) return;
            }
            Heatmap.CreateMap();
        }

        public void ReloadPaintmap()
        {
            if (Paintmap == null) return;
            if (!Paintmap.LoadSourceImage())
            {
                if (!File.Exists(PaintConfigPath) || File.Exists(Paintmap.FilePath)) return;
                LogWarning($"Cannot find image {Paintmap.FilePath}: Using default path from config.");
                Paintmap.FilePath = PaintConfigPath;
                if (!Paintmap.LoadSourceImage()) return;
            }
            Paintmap.CreateMap();
        }
    }
}
