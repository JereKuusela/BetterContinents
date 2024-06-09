using System;

namespace BetterContinents;

public partial class BetterContinents
{
  // Don't change order or remove any of these values, as they are used in the serialization logic.
  public enum DataKey
  {
    GlobalScale,
    MountainsAmount,
    SeaLevelAdjustment,
    MaxRidgeHeight,
    RidgeScale,
    RidgeBlendSigmoidB,
    RidgeBlendSigmoidXOffset,
    HeightMap,
    HeightMapPath,
    HeightMapAmount,
    HeightMapBlend,
    HeightMapAdd,
    OceanChannelsEnabled,
    RiversEnabled,
    BiomeMap,
    BiomeMapPath,
    ForestScale,
    ForestAmountOffset,
    OverrideStartPosition,
    LocationMap,
    LocationMapPath,
    RoughMap,
    RoughMapPath,
    RoughMapBlend,
    UseRoughInvertedAsFlat,
    FlatMapBlend,
    FlatMap,
    FlatMapPath,
    ForestMap,
    ForestMapPath,
    ForestMapMultiply,
    ForestMapAdd,
    DisableMapEdgeDropoff,
    MountainsAllowedAtCenter,
    ForestFactorOverrideAllTrees,
    HeightMapOverrideAll,
    HeightMapMask,
    BaseHeightNoise,
    BiomePrecision,
    PaintMap,
    PaintMapPath,
    HeatMap,
    HeatMapPath,
    HeatMapScale,
    LavaMap,
    LavaMapPath,
    MossMap,
    MossMapPath,
    AshlandGapEnabled,
    DeepNorthGapEnabled,
  }
  public partial class BetterContinentsSettings
  {
    public const int MaxVersion = 11;

    public void Serialize(ZPackage pkg, bool network)
    {
      if (!EnabledForThisWorld)
      {
        pkg.Write(-1);
        return;
      }
      var version = int.TryParse(ConfigOverrideVersion.Value, out var v) ? v : MaxVersion;
      pkg.Write(version);
      if (version < 11)
      {
        SerializeLegacy(pkg, version, network);
        return;
      }

      if (GlobalScale != 0f)
      {
        pkg.Write((int)DataKey.GlobalScale);
        pkg.Write(GlobalScale);
      }
      if (MountainsAmount != 0f)
      {
        pkg.Write((int)DataKey.MountainsAmount);
        pkg.Write(MountainsAmount);
      }
      if (SeaLevelAdjustment != 0f)
      {
        pkg.Write((int)DataKey.SeaLevelAdjustment);
        pkg.Write(SeaLevelAdjustment);
      }
      if (MaxRidgeHeight != 0f)
      {
        pkg.Write((int)DataKey.MaxRidgeHeight);
        pkg.Write(MaxRidgeHeight);
      }
      if (RidgeScale != 0f)
      {
        pkg.Write((int)DataKey.RidgeScale);
        pkg.Write(RidgeScale);
      }
      if (RidgeBlendSigmoidB != 0f)
      {
        pkg.Write((int)DataKey.RidgeBlendSigmoidB);
        pkg.Write(RidgeBlendSigmoidB);
      }
      if (RidgeBlendSigmoidXOffset != 0f)
      {
        pkg.Write((int)DataKey.RidgeBlendSigmoidXOffset);
        pkg.Write(RidgeBlendSigmoidXOffset);
      }
      if (!OceanChannelsEnabled)
        pkg.Write((int)DataKey.OceanChannelsEnabled);

      if (HeightMap != null)
      {
        pkg.Write((int)DataKey.HeightMap);
        pkg.Write(HeightMap.SourceData);
        if (!network)
        {
          pkg.Write((int)DataKey.HeightMapPath);
          pkg.Write(HeightMap.FilePath);
        }
        if (HeightmapAmount != 1f)
        {
          pkg.Write((int)DataKey.HeightMapAmount);
          pkg.Write(HeightmapAmount);
        }
        if (HeightmapBlend != 1f)
        {
          pkg.Write((int)DataKey.HeightMapBlend);
          pkg.Write(HeightmapBlend);
        }
        if (HeightmapAdd != 0f)
        {
          pkg.Write((int)DataKey.HeightMapAdd);
          pkg.Write(HeightmapAdd);
        }
      }

      if (!RiversEnabled)
        pkg.Write((int)DataKey.RiversEnabled);

      if (BiomeMap != null)
      {
        pkg.Write((int)DataKey.BiomeMap);
        pkg.Write(BiomeMap.Serialize());

        if (!network)
        {
          pkg.Write((int)DataKey.BiomeMapPath);
          pkg.Write(BiomeMap.FilePath);
        }
      }
      if (ForestScale != 1f)
      {
        pkg.Write((int)DataKey.ForestScale);
        pkg.Write(ForestScale);
      }
      if (ForestAmountOffset != 0f)
      {
        pkg.Write((int)DataKey.ForestAmountOffset);
        pkg.Write(ForestAmountOffset);
      }

      if (OverrideStartPosition)
      {
        pkg.Write((int)DataKey.OverrideStartPosition);
        pkg.Write(StartPositionX);
        pkg.Write(StartPositionY);
      }

      if (LocationMap != null)
      {
        pkg.Write((int)DataKey.LocationMap);
        LocationMap.Serialize(pkg);
        if (!network)
        {
          pkg.Write((int)DataKey.LocationMapPath);
          pkg.Write(LocationMap.FilePath);
        }
      }

      if (RoughMap != null)
      {
        pkg.Write((int)DataKey.RoughMap);
        pkg.Write(RoughMap.SourceData);
        if (!network)
        {
          pkg.Write((int)DataKey.RoughMapPath);
          pkg.Write(RoughMap.FilePath);
        }
        if (RoughmapBlend != 1f)
        {
          pkg.Write((int)DataKey.RoughMapBlend);
          pkg.Write(RoughmapBlend);
        }
      }

      if (UseRoughInvertedAsFlat)
        pkg.Write((int)DataKey.UseRoughInvertedAsFlat);
      if (FlatmapBlend != 0f)
      {
        pkg.Write((int)DataKey.FlatMapBlend);
        pkg.Write(FlatmapBlend);
      }
      if (!UseRoughInvertedAsFlat && FlatMap != null)
      {
        pkg.Write((int)DataKey.FlatMap);
        pkg.Write(FlatMap.SourceData);
        if (!network)
        {
          pkg.Write((int)DataKey.FlatMapPath);
          pkg.Write(FlatMap.FilePath);
        }
      }

      if (ForestMap != null)
      {
        pkg.Write((int)DataKey.ForestMap);
        pkg.Write(ForestMap.SourceData);
        if (!network)
        {
          pkg.Write((int)DataKey.ForestMapPath);
          pkg.Write(ForestMap.FilePath);
        }

        if (ForestmapMultiply != 1f)
        {
          pkg.Write((int)DataKey.ForestMapMultiply);
          pkg.Write(ForestmapMultiply);
        }
        if (ForestmapAdd != 1f)
        {
          pkg.Write((int)DataKey.ForestMapAdd);
          pkg.Write(ForestmapAdd);
        }
      }

      if (DisableMapEdgeDropoff)
        pkg.Write((int)DataKey.DisableMapEdgeDropoff);
      if (MountainsAllowedAtCenter)
        pkg.Write((int)DataKey.MountainsAllowedAtCenter);
      if (ForestFactorOverrideAllTrees)
        pkg.Write((int)DataKey.ForestFactorOverrideAllTrees);

      if (!HeightmapOverrideAll)
        pkg.Write((int)DataKey.HeightMapOverrideAll);
      if (HeightmapMask > 0f)
      {
        pkg.Write((int)DataKey.HeightMapMask);
        pkg.Write(HeightmapMask);
      }

      if (BaseHeightNoise.NoiseLayers.Count > 0)
      {
        pkg.Write((int)DataKey.BaseHeightNoise);
        BaseHeightNoise.Serialize(pkg);
      }

      if (BiomePrecision > 0)
      {
        pkg.Write((int)DataKey.BiomePrecision);
        pkg.Write(BiomePrecision);
      }

      if (PaintMap != null)
      {
        pkg.Write((int)DataKey.PaintMap);
        pkg.Write(PaintMap.SourceData);
        if (!network)
        {
          pkg.Write((int)DataKey.PaintMapPath);
          pkg.Write(PaintMap.FilePath);
        }
      }
      if (LavaMap != null)
      {
        pkg.Write((int)DataKey.LavaMap);
        pkg.Write(LavaMap.SourceData);
        if (!network)
        {
          pkg.Write((int)DataKey.LavaMapPath);
          pkg.Write(LavaMap.FilePath);
        }
      }

      if (MossMap != null)
      {
        pkg.Write((int)DataKey.MossMap);
        pkg.Write(MossMap.SourceData);
        if (!network)
        {
          pkg.Write((int)DataKey.MossMapPath);
          pkg.Write(MossMap.FilePath);
        }
      }

      if (HeatMap != null)
      {
        pkg.Write((int)DataKey.HeatMap);
        pkg.Write(HeatMap.SourceData);
        if (!network)
        {
          pkg.Write((int)DataKey.HeatMapPath);
          pkg.Write(HeatMap.FilePath);
        }

        if (HeatMapScale != 10f)
        {
          pkg.Write((int)DataKey.HeatMapScale);
          pkg.Write(HeatMapScale);
        }
      }

      if (AshlandsGapEnabled)
        pkg.Write((int)DataKey.AshlandGapEnabled);
      if (DeepNorthGapEnabled)
        pkg.Write((int)DataKey.DeepNorthGapEnabled);
    }

    private void Deserialize(ZPackage pkg)
    {
      Version = pkg.ReadInt();
      if (Version == -1)
      {
        EnabledForThisWorld = false;
        return;
      }
      if (Version < 11)
      {
        DeserializeLegacy(pkg, Version);
        return;
      }
      EnabledForThisWorld = true;

      while (pkg.m_stream.Position < pkg.m_stream.Length)
      {
        var key = (DataKey)pkg.ReadInt();
        string path;
        switch (key)
        {
          case DataKey.GlobalScale:
            GlobalScale = pkg.ReadSingle();
            break;
          case DataKey.MountainsAmount:
            MountainsAmount = pkg.ReadSingle();
            break;
          case DataKey.SeaLevelAdjustment:
            SeaLevelAdjustment = pkg.ReadSingle();
            break;
          case DataKey.MaxRidgeHeight:
            MaxRidgeHeight = pkg.ReadSingle();
            break;
          case DataKey.RidgeScale:
            RidgeScale = pkg.ReadSingle();
            break;
          case DataKey.RidgeBlendSigmoidB:
            RidgeBlendSigmoidB = pkg.ReadSingle();
            break;
          case DataKey.RidgeBlendSigmoidXOffset:
            RidgeBlendSigmoidXOffset = pkg.ReadSingle();
            break;
          case DataKey.HeightMap:
            HeightMap = ImageMapFloat.Create(pkg.ReadByteArray());
            break;
          case DataKey.HeightMapPath:
            path = pkg.ReadString();
            if (HeightMap != null)
              HeightMap.FilePath = path;
            break;
          case DataKey.HeightMapAmount:
            HeightmapAmount = pkg.ReadSingle();
            break;
          case DataKey.HeightMapBlend:
            HeightmapBlend = pkg.ReadSingle();
            break;
          case DataKey.HeightMapAdd:
            HeightmapAdd = pkg.ReadSingle();
            break;
          case DataKey.OceanChannelsEnabled:
            OceanChannelsEnabled = false;
            break;
          case DataKey.RiversEnabled:
            RiversEnabled = false;
            break;
          case DataKey.BiomeMap:
            BiomeMap = ImageMapBiome.Create(pkg.ReadByteArray());
            break;
          case DataKey.BiomeMapPath:
            path = pkg.ReadString();
            if (BiomeMap != null)
              BiomeMap.FilePath = path;
            break;
          case DataKey.ForestScale:
            ForestScale = pkg.ReadSingle();
            break;
          case DataKey.ForestAmountOffset:
            ForestAmountOffset = pkg.ReadSingle();
            break;
          case DataKey.OverrideStartPosition:
            OverrideStartPosition = true;
            StartPositionX = pkg.ReadSingle();
            StartPositionY = pkg.ReadSingle();
            break;
          case DataKey.LocationMap:
            LocationMap = ImageMapLocation.Create(pkg);
            break;
          case DataKey.LocationMapPath:
            path = pkg.ReadString();
            if (LocationMap != null)
              LocationMap.FilePath = path;
            break;
          case DataKey.RoughMap:
            RoughMap = ImageMapFloat.Create(pkg.ReadByteArray());
            break;
          case DataKey.RoughMapPath:
            path = pkg.ReadString();
            if (RoughMap != null)
              RoughMap.FilePath = path;
            break;
          case DataKey.RoughMapBlend:
            RoughmapBlend = pkg.ReadSingle();
            break;
          case DataKey.UseRoughInvertedAsFlat:
            UseRoughInvertedAsFlat = true;
            break;
          case DataKey.FlatMapBlend:
            FlatmapBlend = pkg.ReadSingle();
            break;
          case DataKey.FlatMap:
            FlatMap = ImageMapFloat.Create(pkg.ReadByteArray());
            break;
          case DataKey.FlatMapPath:
            path = pkg.ReadString();
            if (FlatMap != null)
              FlatMap.FilePath = path;
            break;
          case DataKey.ForestMap:
            ForestMap = ImageMapFloat.Create(pkg.ReadByteArray());
            break;
          case DataKey.ForestMapPath:
            path = pkg.ReadString();
            if (ForestMap != null)
              ForestMap.FilePath = path;
            break;
          case DataKey.ForestMapMultiply:
            ForestmapMultiply = pkg.ReadSingle();
            break;
          case DataKey.ForestMapAdd:
            ForestmapAdd = pkg.ReadSingle();
            break;
          case DataKey.DisableMapEdgeDropoff:
            DisableMapEdgeDropoff = true;
            break;
          case DataKey.MountainsAllowedAtCenter:
            MountainsAllowedAtCenter = true;
            break;
          case DataKey.ForestFactorOverrideAllTrees:
            ForestFactorOverrideAllTrees = true;
            break;
          case DataKey.HeightMapOverrideAll:
            HeightmapOverrideAll = false;
            break;
          case DataKey.HeightMapMask:
            HeightmapMask = pkg.ReadSingle();
            break;
          case DataKey.BaseHeightNoise:
            BaseHeightNoise = NoiseStackSettings.Deserialize(pkg);
            break;
          case DataKey.BiomePrecision:
            BiomePrecision = pkg.ReadInt();
            break;
          case DataKey.PaintMap:
            PaintMap = ImageMapColor.Create(pkg.ReadByteArray());
            break;
          case DataKey.PaintMapPath:
            path = pkg.ReadString();
            if (PaintMap != null)
              PaintMap.FilePath = path;
            break;
          case DataKey.LavaMap:
            LavaMap = ImageMapFloat.Create(pkg.ReadByteArray());
            break;
          case DataKey.LavaMapPath:
            path = pkg.ReadString();
            if (LavaMap != null)
              LavaMap.FilePath = path;
            break;
          case DataKey.MossMap:
            MossMap = ImageMapFloat.Create(pkg.ReadByteArray());
            break;
          case DataKey.MossMapPath:
            path = pkg.ReadString();
            if (MossMap != null)
              MossMap.FilePath = path;
            break;
          case DataKey.HeatMap:
            HeatMap = ImageMapFloat.Create(pkg.ReadByteArray());
            break;
          case DataKey.HeatMapPath:
            path = pkg.ReadString();
            if (HeatMap != null)
              HeatMap.FilePath = path;
            break;
          case DataKey.HeatMapScale:
            HeatMapScale = pkg.ReadSingle();
            break;
          case DataKey.AshlandGapEnabled:
            AshlandsGapEnabled = true;
            break;
          case DataKey.DeepNorthGapEnabled:
            DeepNorthGapEnabled = true;
            break;
          default:
            LogError("Failed to load the save file. Unknown feature: " + key);
            EnabledForThisWorld = false;
            return;
        }

      }
    }
    public void SerializeLegacy(ZPackage pkg, int version, bool network)
    {
      pkg.Write(0L);

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

        if (HeightMap == null)
          pkg.Write(string.Empty);
        else
        {
          HeightMap.SerializeLegacy(pkg, version, network);
          pkg.Write(HeightmapAmount);
          pkg.Write(HeightmapBlend);
          pkg.Write(HeightmapAdd);
        }
        pkg.Write(OceanChannelsEnabled);

        if (version >= 2)
        {
          pkg.Write(RiversEnabled);

          if (BiomeMap == null)
            pkg.Write("");
          else
            BiomeMap.SerializeLegacy(pkg, version, network);
          pkg.Write(ForestScale);
          pkg.Write(ForestAmountOffset);

          pkg.Write(OverrideStartPosition);
          pkg.Write(StartPositionX);
          pkg.Write(StartPositionY);
        }

        if (version >= 3)
        {
          if (LocationMap == null)
            pkg.Write("");
          else
            LocationMap.SerializeLegacy(pkg, version, network);
        }

        if (version >= 5)
        {
          if (RoughMap == null)
            pkg.Write("");
          else
          {
            RoughMap.SerializeLegacy(pkg, version, network);
            pkg.Write(RoughmapBlend);
          }

          pkg.Write(UseRoughInvertedAsFlat);
          pkg.Write(FlatmapBlend);
          if (!UseRoughInvertedAsFlat)
          {
            if (FlatMap == null)
              pkg.Write("");
            else
              FlatMap.SerializeLegacy(pkg, version, network);
          }

          if (ForestMap == null)
            pkg.Write("");
          else
          {
            ForestMap.SerializeLegacy(pkg, version, network);
            pkg.Write(ForestmapMultiply);
            pkg.Write(ForestmapAdd);
          }

          pkg.Write(DisableMapEdgeDropoff);
          pkg.Write(MountainsAllowedAtCenter);
          pkg.Write(ForestFactorOverrideAllTrees);
        }

        if (version >= 6)
        {
          pkg.Write(HeightmapOverrideAll);
          pkg.Write(HeightmapMask);
        }

        if (version >= 7)
        {
          BaseHeightNoise.Serialize(pkg);
        }
        if (version >= 9)
        {
          pkg.Write(BiomePrecision);
        }
        if (version >= 10)
        {
          if (PaintMap == null)
            pkg.Write("");
          else
            PaintMap.SerializeLegacy(pkg, version, network);
        }
      }
    }

    private void DeserializeLegacy(ZPackage pkg, int version)
    {
      Version = version;
      pkg.ReadLong();

      EnabledForThisWorld = pkg.ReadBool();
      if (!EnabledForThisWorld)
        return;
      Log("Loading legacy settings " + Version);

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
        HeightMap = ImageMapFloat.Create(pkg.ReadByteArray(), heightmapFilePath, Version <= 4);
        HeightmapAmount = pkg.ReadSingle();
        HeightmapBlend = pkg.ReadSingle();
        HeightmapAdd = pkg.ReadSingle();
      }
      OceanChannelsEnabled = pkg.ReadBool();

      RiversEnabled = true;
      ForestScale = 1f;
      ForestAmountOffset = 0;
      OverrideStartPosition = false;
      StartPositionX = 0;
      StartPositionY = 0;
      BaseHeightNoise = new();
      HeightmapOverrideAll = false;
      HeightmapMask = 0;
      BiomePrecision = 0;

      if (Version >= 2)
      {
        RiversEnabled = pkg.ReadBool();
        BiomeMap = ImageMapBiome.LoadLegacy(pkg, Version);

        ForestScale = pkg.ReadSingle();
        ForestAmountOffset = pkg.ReadSingle();

        OverrideStartPosition = pkg.ReadBool();
        StartPositionX = pkg.ReadSingle();
        StartPositionY = pkg.ReadSingle();
      }
      if (Version >= 3)
        LocationMap = ImageMapLocation.LoadLegacy(pkg, Version);
      if (Version >= 5)
      {
        string roughmapFilePath = pkg.ReadString();
        if (!string.IsNullOrEmpty(roughmapFilePath))
        {
          RoughMap = ImageMapFloat.Create(pkg.ReadByteArray(), roughmapFilePath);
          RoughmapBlend = pkg.ReadSingle();
        }

        UseRoughInvertedAsFlat = pkg.ReadBool();
        FlatmapBlend = pkg.ReadSingle();
        if (!UseRoughInvertedAsFlat)
        {
          string flatmapFilePath = pkg.ReadString();
          if (!string.IsNullOrEmpty(flatmapFilePath))
          {
            FlatMap = ImageMapFloat.Create(pkg.ReadByteArray(), flatmapFilePath);
          }
        }
        string forestmapFilePath = pkg.ReadString();
        if (!string.IsNullOrEmpty(forestmapFilePath))
        {
          ForestMap = ImageMapFloat.Create(pkg.ReadByteArray(), forestmapFilePath);
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
      if (Version >= 9)
        BiomePrecision = pkg.ReadInt();
      if (Version >= 10)
        PaintMap = ImageMapColor.LoadLegacy(pkg);
    }
  }
}