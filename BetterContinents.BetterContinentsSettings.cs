using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace BetterContinents;

public partial class BetterContinents
{
    // These are what are baked into the world when it is created
    public class BetterContinentsSettings
    {
        public const int LatestVersion = 8;

        public int Version;

        public long WorldUId;
        public bool EnabledForThisWorld;
        public float GlobalScale;
        public float MountainsAmount;
        public float SeaLevelAdjustment;
        public float MaxRidgeHeight;
        public float RidgeScale;
        public float RidgeBlendSigmoidB;
        public float RidgeBlendSigmoidXOffset;
        public float HeightmapAmount;
        public float HeightmapBlend;
        public float HeightmapAdd;
        public bool OceanChannelsEnabled;
        public bool RiversEnabled;
        public float ForestScale;
        public float ForestAmountOffset;
        public bool OverrideStartPosition;
        public float StartPositionX;
        public float StartPositionY;
        public float RoughmapBlend;
        public bool UseRoughInvertedAsFlat;
        public float FlatmapBlend;
        public float ForestmapMultiply;
        public float ForestmapAdd;
        public bool DisableMapEdgeDropoff;
        public bool MountainsAllowedAtCenter;
        public bool ForestFactorOverrideAllTrees;
        public bool HeightmapOverrideAll;
        public float HeightmapMask;
        public NoiseStackSettings? BaseHeightNoise;

        // Non-serialized
        private ImageMapFloat? Heightmap;
        private ImageMapBiome? Biomemap;
        private ImageMapLocation? Locationmap;
        private ImageMapFloat? Roughmap;
        private ImageMapFloat? Flatmap;
        private ImageMapFloat? Forestmap;

        public bool HasHeightmap => Heightmap != null;
        public bool HasBiomemap => Biomemap != null;
        public bool HasLocationmap => Locationmap != null;
        public bool HasRoughmap => Roughmap != null;
        public bool HasFlatmap => Flatmap != null;
        public bool HasForestmap => Forestmap != null;

        public bool AnyImageMap => HasHeightmap
                                   || HasRoughmap
                                   || HasFlatmap
                                   || HasBiomemap
                                   || HasLocationmap
                                   || HasForestmap;
        public bool ShouldHeightmapOverrideAll => HasHeightmap && HeightmapOverrideAll;

        public static BetterContinentsSettings Create(long worldUId)
        {
            var settings = new BetterContinentsSettings();
            settings.InitSettings(worldUId, ConfigEnabled.Value);
            return settings;
        }

        public static BetterContinentsSettings Disabled(long worldUId = -1)
        {
            var settings = new BetterContinentsSettings();
            settings.InitSettings(worldUId, false);
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

        private static string HeightPath(string defaultFilename, string projectDir) => GetPath(projectDir, HeightFile, defaultFilename);
        private static string BiomePath(string defaultFilename, string projectDir) => GetPath(projectDir, BiomeFile, defaultFilename);
        private static string LocationPath(string defaultFilename, string projectDir) => GetPath(projectDir, LocationFile, defaultFilename);
        private static string RoughPath(string defaultFilename, string projectDir) => GetPath(projectDir, RoughFile, defaultFilename);
        private static string ForestPath(string defaultFilename, string projectDir) => GetPath(projectDir, ForestFile, defaultFilename);

        private static string HeightConfigPath => HeightPath(ConfigHeightFile.Value, ConfigMapSourceDir.Value);
        private static string BiomeConfigPath => BiomePath(ConfigBiomeFile.Value, ConfigMapSourceDir.Value);
        private static string LocationConfigPath => LocationPath(ConfigLocationFile.Value, ConfigMapSourceDir.Value);
        private static string RoughConfigPath => RoughPath(ConfigRoughFile.Value, ConfigMapSourceDir.Value);
        private static string ForestConfigPath => ForestPath(ConfigForestFile.Value, ConfigMapSourceDir.Value);


        private void InitSettings(long worldUId, bool enabled)
        {
            Log($"Init settings for new world");

            Version = LatestVersion;

            WorldUId = worldUId;

            EnabledForThisWorld = enabled;

            if (EnabledForThisWorld)
            {
                ContinentSize = ConfigContinentSize.Value;
                SeaLevel = ConfigSeaLevelAdjustment.Value;

                string heightmapPath = HeightConfigPath;
                if (!string.IsNullOrEmpty(heightmapPath))
                {
                    HeightmapAmount = ConfigHeightmapAmount.Value;
                    HeightmapBlend = ConfigHeightmapBlend.Value;
                    HeightmapAdd = ConfigHeightmapAdd.Value;
                    HeightmapMask = ConfigHeightmapMask.Value;
                    HeightmapOverrideAll = ConfigHeightmapOverrideAll.Value;

                    Heightmap = new ImageMapFloat(heightmapPath);
                    if (!Heightmap.LoadSourceImage() || !Heightmap.CreateMap())
                        Heightmap = null;
                }

                BaseHeightNoise = NoiseStackSettings.Default();

                string biomemapPath = BiomeConfigPath;
                if (!string.IsNullOrEmpty(biomemapPath))
                {

                    Biomemap = new(biomemapPath);
                    if (!Biomemap.LoadSourceImage() || !Biomemap.CreateMap())
                        Biomemap = null;
                }

                OceanChannelsEnabled = ConfigOceanChannelsEnabled.Value;
                RiversEnabled = ConfigRiversEnabled.Value;

                ForestScaleFactor = ConfigForestScale.Value;
                ForestAmount = ConfigForestAmount.Value;
                ForestFactorOverrideAllTrees = ConfigForestFactorOverrideAllTrees.Value;

                OverrideStartPosition = ConfigOverrideStartPosition.Value;
                StartPositionX = ConfigStartPositionX.Value;
                StartPositionY = ConfigStartPositionY.Value;

                string locationPath = LocationConfigPath;
                if (!string.IsNullOrEmpty(locationPath))
                {
                    Locationmap = new(locationPath);
                    if (!Locationmap.LoadSourceImage() || !Locationmap.CreateMap())
                        Locationmap = null;
                }

                string roughmapPath = RoughConfigPath;
                if (!string.IsNullOrEmpty(roughmapPath))
                {
                    RoughmapBlend = ConfigRoughmapBlend.Value;

                    Roughmap = new(roughmapPath);
                    if (!Roughmap.LoadSourceImage() || !Roughmap.CreateMap())
                        Roughmap = null;
                }

                string forestmapPath = ForestConfigPath;
                if (!string.IsNullOrEmpty(forestmapPath))
                {
                    ForestmapAdd = ConfigForestmapAdd.Value;
                    ForestmapMultiply = ConfigForestmapMultiply.Value;

                    Forestmap = new(forestmapPath);
                    if (!Forestmap.LoadSourceImage() || !Forestmap.CreateMap())
                        Forestmap = null;
                }

                MapEdgeDropoff = ConfigMapEdgeDropoff.Value;
                MountainsAllowedAtCenter = ConfigMountainsAllowedAtCenter.Value;
            }
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

        public void SetHeightPath(string path, string projectDir = "")
        {
            string finalPath = HeightPath(path, projectDir);
            if (!string.IsNullOrEmpty(finalPath))
            {
                Heightmap = new(finalPath);
                if (!Heightmap.LoadSourceImage() || !Heightmap.CreateMap())
                    Heightmap = null;
            }
            else
            {
                Heightmap = null;
            }
        }

        public string GetHeightPath() => Heightmap?.FilePath ?? string.Empty;

        public void DisableHeightmap() => Heightmap = null;

        public void SetBiomePath(string path, string projectDir = "")
        {
            string finalPath = BiomePath(path, projectDir);
            if (!string.IsNullOrEmpty(finalPath))
            {
                Biomemap = new(finalPath);
                if (!Biomemap.LoadSourceImage() || !Biomemap.CreateMap())
                    Biomemap = null;
            }
            else
            {
                Biomemap = null;
            }
        }
        public string GetBiomePath() => Biomemap?.FilePath ?? string.Empty;
        public void DisableBiomemap() => Biomemap = null;

        public void SetLocationPath(string path, string projectDir = "")
        {
            var finalPath = LocationPath(path, projectDir);
            if (!string.IsNullOrEmpty(finalPath))
            {
                Locationmap = new(finalPath);
                if (!Locationmap.LoadSourceImage() || !Locationmap.CreateMap())
                    Locationmap = null;
            }
            else
            {
                Locationmap = null;
            }
        }
        public string GetLocationPath() => Locationmap?.FilePath ?? string.Empty;
        public void DisableLocationMap() => Locationmap = null;

        public void SetRoughPath(string path, string projectDir = "")
        {
            string finalPath = RoughPath(path, projectDir);
            if (!string.IsNullOrEmpty(finalPath))
            {
                Roughmap = new(finalPath);
                if (!Roughmap.LoadSourceImage() || !Roughmap.CreateMap())
                    Roughmap = null;
            }
            else
            {
                Roughmap = null;
            }
        }
        public string GetRoughPath() => Roughmap?.FilePath ?? string.Empty;
        public void DisableRoughmap() => Roughmap = null;

        public void SetForestPath(string path, string projectDir = "")
        {
            string finalPath = ForestPath(path, projectDir);
            if (!string.IsNullOrEmpty(finalPath))
            {
                Forestmap = new(finalPath);
                if (!Forestmap.LoadSourceImage() || !Forestmap.CreateMap())
                    Forestmap = null;
            }
            else
            {
                Forestmap = null;
            }
        }
        public string GetForestPath() => Forestmap?.FilePath ?? string.Empty;
        public void DisableForestmap() => Forestmap = null;
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
            output($"Version {Version}");
            output($"WorldUId {WorldUId}");

            if (EnabledForThisWorld)
            {
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
                    BaseHeightNoise?.Dump(str => output($"    {str}"));
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
            }
            else
            {
                output($"DISABLED");
            }
        }

        public ZPackage Serialize()
        {
            var pkg = new ZPackage();
            Serialize(pkg, false);
            return pkg;
        }

        public void Serialize(ZPackage pkg, bool network)
        {
            pkg.Write(Version);

            pkg.Write(WorldUId);

            pkg.Write(EnabledForThisWorld);

            if (EnabledForThisWorld)
            {
                pkg.Write(GlobalScale);
                pkg.Write(MountainsAmount);
                pkg.Write(SeaLevelAdjustment);

                pkg.Write(MaxRidgeHeight);
                pkg.Write(RidgeScale);
                pkg.Write(RidgeBlendSigmoidB);
                pkg.Write(RidgeBlendSigmoidXOffset);

                if (Heightmap == null)
                    pkg.Write(string.Empty);
                else
                {
                    Heightmap.Serialize(pkg, Version, network);
                    pkg.Write(HeightmapAmount);
                    pkg.Write(HeightmapBlend);
                    pkg.Write(HeightmapAdd);
                }
                pkg.Write(OceanChannelsEnabled);

                if (Version >= 2)
                {
                    pkg.Write(RiversEnabled);

                    if (Biomemap == null)
                        pkg.Write("");
                    else
                        Biomemap.Serialize(pkg, Version, network);
                    pkg.Write(ForestScale);
                    pkg.Write(ForestAmountOffset);

                    pkg.Write(OverrideStartPosition);
                    pkg.Write(StartPositionX);
                    pkg.Write(StartPositionY);
                }

                if (Version >= 3)
                {
                    if (Locationmap == null)
                        pkg.Write("");
                    else
                        Locationmap.Serialize(pkg, Version, network);
                }

                if (Version >= 5)
                {
                    if (Roughmap == null)
                        pkg.Write("");
                    else
                    {
                        Roughmap.Serialize(pkg, Version, network);
                        pkg.Write(RoughmapBlend);
                    }

                    pkg.Write(UseRoughInvertedAsFlat);
                    pkg.Write(FlatmapBlend);
                    if (!UseRoughInvertedAsFlat)
                    {
                        if (Flatmap == null)
                            pkg.Write("");
                        else
                            Flatmap.Serialize(pkg, Version, network);
                    }

                    if (Forestmap == null)
                        pkg.Write("");
                    else
                    {
                        Forestmap.Serialize(pkg, Version, network);
                        pkg.Write(ForestmapMultiply);
                        pkg.Write(ForestmapAdd);
                    }

                    pkg.Write(DisableMapEdgeDropoff);
                    pkg.Write(MountainsAllowedAtCenter);
                    pkg.Write(ForestFactorOverrideAllTrees);
                }

                if (Version >= 6)
                {
                    pkg.Write(HeightmapOverrideAll);
                    pkg.Write(HeightmapMask);
                }

                if (Version >= 7)
                {
                    BaseHeightNoise?.Serialize(pkg);
                }
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

        private void Deserialize(ZPackage pkg)
        {
            Version = pkg.ReadInt();
            if (Version > LatestVersion)
            {
                LogError($"BetterContinents mod is out of date: world expects config version {Version}, mod config version is {LatestVersion}");
                throw new Exception($"BetterContinents mod is out of date: world expects config version {Version}, mod config version is {LatestVersion}");
            }

            WorldUId = pkg.ReadLong();

            EnabledForThisWorld = pkg.ReadBool();

            if (EnabledForThisWorld)
            {
                GlobalScale = pkg.ReadSingle();
                MountainsAmount = pkg.ReadSingle();
                SeaLevelAdjustment = pkg.ReadSingle();

                MaxRidgeHeight = pkg.ReadSingle();
                RidgeScale = pkg.ReadSingle();
                RidgeBlendSigmoidB = pkg.ReadSingle();
                RidgeBlendSigmoidXOffset = pkg.ReadSingle();

                string heightmapFilePath = pkg.ReadString();
                if (!string.IsNullOrEmpty(heightmapFilePath))
                {
                    Heightmap = new ImageMapFloat(heightmapFilePath, pkg.ReadByteArray());
                    var result = Version > 4 ? Heightmap.CreateMap() : Heightmap.CreateMapLegacy();
                    if (!result)
                        Heightmap = null;
                    HeightmapAmount = pkg.ReadSingle();
                    HeightmapBlend = pkg.ReadSingle();
                    HeightmapAdd = pkg.ReadSingle();
                }

                OceanChannelsEnabled = pkg.ReadBool();

                RiversEnabled = true;
                ForestScale = 1;
                ForestAmountOffset = 0;
                OverrideStartPosition = false;
                StartPositionX = 0;
                StartPositionY = 0;
                BaseHeightNoise = null;
                HeightmapOverrideAll = false;
                HeightmapMask = 0;

                if (Version >= 2)
                {
                    RiversEnabled = pkg.ReadBool();
                    Biomemap = ImageMapBiome.Load(pkg, Version);

                    ForestScale = pkg.ReadSingle();
                    ForestAmountOffset = pkg.ReadSingle();

                    OverrideStartPosition = pkg.ReadBool();
                    StartPositionX = pkg.ReadSingle();
                    StartPositionY = pkg.ReadSingle();
                }
                if (Version >= 3)
                    Locationmap = ImageMapLocation.Load(pkg, Version);
                if (Version >= 5)
                {
                    string roughmapFilePath = pkg.ReadString();
                    if (!string.IsNullOrEmpty(roughmapFilePath))
                    {
                        Roughmap = new ImageMapFloat(roughmapFilePath, pkg.ReadByteArray());
                        if (!Roughmap.CreateMap())
                            Roughmap = null;
                        RoughmapBlend = pkg.ReadSingle();
                    }

                    UseRoughInvertedAsFlat = pkg.ReadBool();
                    FlatmapBlend = pkg.ReadSingle();
                    if (!UseRoughInvertedAsFlat)
                    {
                        string flatmapFilePath = pkg.ReadString();
                        if (!string.IsNullOrEmpty(flatmapFilePath))
                        {
                            Flatmap = new ImageMapFloat(flatmapFilePath, pkg.ReadByteArray());
                            if (!Flatmap.CreateMap())
                                Flatmap = null;
                        }
                    }

                    string forestmapFilePath = pkg.ReadString();
                    if (!string.IsNullOrEmpty(forestmapFilePath))
                    {
                        Forestmap = new ImageMapFloat(forestmapFilePath, pkg.ReadByteArray());
                        if (!Forestmap.CreateMap())
                            Forestmap = null;
                        ForestmapMultiply = pkg.ReadSingle();
                        ForestmapAdd = pkg.ReadSingle();
                    }

                    DisableMapEdgeDropoff = pkg.ReadBool();
                    MountainsAllowedAtCenter = pkg.ReadBool();
                    ForestFactorOverrideAllTrees = pkg.ReadBool();
                }
                if (Version >= 6)
                {
                    HeightmapOverrideAll = pkg.ReadBool();
                    HeightmapMask = pkg.ReadSingle();
                }
                if (Version >= 7)
                    BaseHeightNoise = NoiseStackSettings.Deserialize(pkg);
            }
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
        public IEnumerable<Vector2> GetAllSpawns(string spawn) => Locationmap?.GetAllSpawns(spawn) ?? new List<Vector2>();

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
    }
}
