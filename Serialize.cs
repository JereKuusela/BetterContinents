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
    Heightmap,
    HeightmapPath,
    HeightmapAmount,
    HeightmapBlend,
    HeightmapAdd,
    OceanChannelsEnabled,
    RiversEnabled,
    Biomemap,
    BiomemapPath,
    ForestScale,
    ForestAmountOffset,
    OverrideStartPosition,
    Locationmap,
    LocationmapPath,
    Roughmap,
    RoughmapPath,
    RoughmapBlend,
    UseRoughInvertedAsFlat,
    FlatmapBlend,
    Flatmap,
    FlatmapPath,
    Forestmap,
    ForestmapPath,
    ForestmapMultiply,
    ForestmapAdd,
    DisableMapEdgeDropoff,
    MountainsAllowedAtCenter,
    ForestFactorOverrideAllTrees,
    HeightmapOverrideAll,
    HeightmapMask,
    BaseHeightNoise,
    BiomePrecision,
    Paintmap,
    PaintmapPath,
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

      if (Heightmap != null)
      {
        pkg.Write((int)DataKey.Heightmap);
        pkg.Write(Heightmap.SourceData);
        if (!network)
        {
          pkg.Write((int)DataKey.HeightmapPath);
          pkg.Write(Heightmap.FilePath);
        }
        if (HeightmapAmount != 1f)
        {
          pkg.Write((int)DataKey.HeightmapAmount);
          pkg.Write(HeightmapAmount);
        }
        if (HeightmapBlend != 1f)
        {
          pkg.Write((int)DataKey.HeightmapBlend);
          pkg.Write(HeightmapBlend);
        }
        if (HeightmapAdd != 0f)
        {
          pkg.Write((int)DataKey.HeightmapAdd);
          pkg.Write(HeightmapAdd);
        }
      }

      if (!RiversEnabled)
        pkg.Write((int)DataKey.RiversEnabled);

      if (Biomemap != null)
      {
        pkg.Write((int)DataKey.Biomemap);
        pkg.Write(Biomemap.Serialize());

        if (!network)
        {
          pkg.Write((int)DataKey.BiomemapPath);
          pkg.Write(Biomemap.FilePath);
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

      if (Locationmap != null)
      {
        pkg.Write((int)DataKey.Locationmap);
        Locationmap.Serialize(pkg);
        if (!network)
        {
          pkg.Write((int)DataKey.LocationmapPath);
          pkg.Write(Locationmap.FilePath);
        }
      }

      if (Roughmap != null)
      {
        pkg.Write((int)DataKey.Roughmap);
        pkg.Write(Roughmap.SourceData);
        if (!network)
        {
          pkg.Write((int)DataKey.RoughmapPath);
          pkg.Write(Roughmap.FilePath);
        }
        if (RoughmapBlend != 1f)
        {
          pkg.Write((int)DataKey.RoughmapBlend);
          pkg.Write(RoughmapBlend);
        }
      }

      if (UseRoughInvertedAsFlat)
        pkg.Write((int)DataKey.UseRoughInvertedAsFlat);
      if (FlatmapBlend != 0f)
      {
        pkg.Write((int)DataKey.FlatmapBlend);
        pkg.Write(FlatmapBlend);
      }
      if (!UseRoughInvertedAsFlat && Flatmap != null)
      {
        pkg.Write((int)DataKey.Flatmap);
        pkg.Write(Flatmap.SourceData);
        if (!network)
        {
          pkg.Write((int)DataKey.FlatmapPath);
          pkg.Write(Flatmap.FilePath);
        }
      }

      if (Forestmap != null)
      {
        pkg.Write((int)DataKey.Forestmap);
        pkg.Write(Forestmap.SourceData);
        if (!network)
        {
          pkg.Write((int)DataKey.ForestmapPath);
          pkg.Write(Forestmap.FilePath);
        }

        if (ForestmapMultiply != 1f)
        {
          pkg.Write((int)DataKey.ForestmapMultiply);
          pkg.Write(ForestmapMultiply);
        }
        if (ForestmapAdd != 1f)
        {
          pkg.Write((int)DataKey.ForestmapAdd);
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
        pkg.Write((int)DataKey.HeightmapOverrideAll);
      if (HeightmapMask > 0f)
      {
        pkg.Write((int)DataKey.HeightmapMask);
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

      if (Paintmap != null)
      {
        pkg.Write((int)DataKey.Paintmap);
        pkg.Write(Paintmap.SourceData);
        if (!network)
        {
          pkg.Write((int)DataKey.PaintmapPath);
          pkg.Write(Paintmap.FilePath);
        }
      }

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
          case DataKey.Heightmap:
            Heightmap = ImageMapFloat.Create(pkg.ReadByteArray());
            break;
          case DataKey.HeightmapPath:
            path = pkg.ReadString();
            if (Heightmap != null)
              Heightmap.FilePath = path;
            break;
          case DataKey.HeightmapAmount:
            HeightmapAmount = pkg.ReadSingle();
            break;
          case DataKey.HeightmapBlend:
            HeightmapBlend = pkg.ReadSingle();
            break;
          case DataKey.HeightmapAdd:
            HeightmapAdd = pkg.ReadSingle();
            break;
          case DataKey.OceanChannelsEnabled:
            OceanChannelsEnabled = false;
            break;
          case DataKey.RiversEnabled:
            RiversEnabled = false;
            break;
          case DataKey.Biomemap:
            Biomemap = ImageMapBiome.Create(pkg.ReadByteArray());
            break;
          case DataKey.BiomemapPath:
            path = pkg.ReadString();
            if (Biomemap != null)
              Biomemap.FilePath = path;
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
          case DataKey.Locationmap:
            Locationmap = ImageMapLocation.Create(pkg);
            break;
          case DataKey.LocationmapPath:
            path = pkg.ReadString();
            if (Locationmap != null)
              Locationmap.FilePath = path;
            break;
          case DataKey.Roughmap:
            Roughmap = ImageMapFloat.Create(pkg.ReadByteArray());
            break;
          case DataKey.RoughmapPath:
            path = pkg.ReadString();
            if (Roughmap != null)
              Roughmap.FilePath = path;
            break;
          case DataKey.RoughmapBlend:
            RoughmapBlend = pkg.ReadSingle();
            break;
          case DataKey.UseRoughInvertedAsFlat:
            UseRoughInvertedAsFlat = true;
            break;
          case DataKey.FlatmapBlend:
            FlatmapBlend = pkg.ReadSingle();
            break;
          case DataKey.Flatmap:
            Flatmap = ImageMapFloat.Create(pkg.ReadByteArray());
            break;
          case DataKey.FlatmapPath:
            path = pkg.ReadString();
            if (Flatmap != null)
              Flatmap.FilePath = path;
            break;
          case DataKey.Forestmap:
            Forestmap = ImageMapFloat.Create(pkg.ReadByteArray());
            break;
          case DataKey.ForestmapPath:
            path = pkg.ReadString();
            if (Forestmap != null)
              Forestmap.FilePath = path;
            break;
          case DataKey.ForestmapMultiply:
            ForestmapMultiply = pkg.ReadSingle();
            break;
          case DataKey.ForestmapAdd:
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
          case DataKey.HeightmapOverrideAll:
            HeightmapOverrideAll = false;
            break;
          case DataKey.HeightmapMask:
            HeightmapMask = pkg.ReadSingle();
            break;
          case DataKey.BaseHeightNoise:
            BaseHeightNoise = NoiseStackSettings.Deserialize(pkg);
            break;
          case DataKey.BiomePrecision:
            BiomePrecision = pkg.ReadInt();
            break;
          case DataKey.Paintmap:
            Paintmap = ImageMapColor.Create(pkg.ReadByteArray());
            break;
          case DataKey.PaintmapPath:
            path = pkg.ReadString();
            if (Paintmap != null)
              Paintmap.FilePath = path;
            break;
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

        if (Heightmap == null)
          pkg.Write(string.Empty);
        else
        {
          Heightmap.SerializeLegacy(pkg, version, network);
          pkg.Write(HeightmapAmount);
          pkg.Write(HeightmapBlend);
          pkg.Write(HeightmapAdd);
        }
        pkg.Write(OceanChannelsEnabled);

        if (version >= 2)
        {
          pkg.Write(RiversEnabled);

          if (Biomemap == null)
            pkg.Write("");
          else
            Biomemap.SerializeLegacy(pkg, version, network);
          pkg.Write(ForestScale);
          pkg.Write(ForestAmountOffset);

          pkg.Write(OverrideStartPosition);
          pkg.Write(StartPositionX);
          pkg.Write(StartPositionY);
        }

        if (version >= 3)
        {
          if (Locationmap == null)
            pkg.Write("");
          else
            Locationmap.SerializeLegacy(pkg, version, network);
        }

        if (version >= 5)
        {
          if (Roughmap == null)
            pkg.Write("");
          else
          {
            Roughmap.SerializeLegacy(pkg, version, network);
            pkg.Write(RoughmapBlend);
          }

          pkg.Write(UseRoughInvertedAsFlat);
          pkg.Write(FlatmapBlend);
          if (!UseRoughInvertedAsFlat)
          {
            if (Flatmap == null)
              pkg.Write("");
            else
              Flatmap.SerializeLegacy(pkg, version, network);
          }

          if (Forestmap == null)
            pkg.Write("");
          else
          {
            Forestmap.SerializeLegacy(pkg, version, network);
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
          if (Paintmap == null)
            pkg.Write("");
          else
            Paintmap.SerializeLegacy(pkg, version, network);
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
      Log("Loading heightmap");
      if (!string.IsNullOrEmpty(heightmapFilePath))
      {
        Heightmap = ImageMapFloat.Create(pkg.ReadByteArray(), heightmapFilePath, Version <= 4);
        Log("Loaded heightmap");
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
        Log("Loading biomemap");
        Biomemap = ImageMapBiome.LoadLegacy(pkg, Version);
        Log("Loaded biomemap");

        ForestScale = pkg.ReadSingle();
        ForestAmountOffset = pkg.ReadSingle();

        OverrideStartPosition = pkg.ReadBool();
        StartPositionX = pkg.ReadSingle();
        StartPositionY = pkg.ReadSingle();
      }
      Log("Loading locationmap");
      if (Version >= 3)
        Locationmap = ImageMapLocation.LoadLegacy(pkg, Version);
      if (Version >= 5)
      {
        string roughmapFilePath = pkg.ReadString();
        if (!string.IsNullOrEmpty(roughmapFilePath))
        {
          Roughmap = ImageMapFloat.Create(pkg.ReadByteArray(), roughmapFilePath);
          RoughmapBlend = pkg.ReadSingle();
        }

        UseRoughInvertedAsFlat = pkg.ReadBool();
        FlatmapBlend = pkg.ReadSingle();
        if (!UseRoughInvertedAsFlat)
        {
          string flatmapFilePath = pkg.ReadString();
          if (!string.IsNullOrEmpty(flatmapFilePath))
          {
            Flatmap = ImageMapFloat.Create(pkg.ReadByteArray(), flatmapFilePath);
          }
        }
        string forestmapFilePath = pkg.ReadString();
        if (!string.IsNullOrEmpty(forestmapFilePath))
        {
          Forestmap = ImageMapFloat.Create(pkg.ReadByteArray(), forestmapFilePath);
          ForestmapMultiply = pkg.ReadSingle();
          ForestmapAdd = pkg.ReadSingle();
        }

        DisableMapEdgeDropoff = pkg.ReadBool();
        MountainsAllowedAtCenter = pkg.ReadBool();
        ForestFactorOverrideAllTrees = pkg.ReadBool();
      }
      Log("Loading locationma2p");
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
        Paintmap = ImageMapColor.LoadLegacy(pkg);
    }
  }
}