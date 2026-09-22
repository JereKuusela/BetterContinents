using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BetterContinents;

public partial class BetterContinents
{
  public static bool GetBiomeColorPatch(Heightmap __instance, float ix, float iy, ref Color __result)
  {
    var x = __instance.transform.position.x + ix * 64f - 32f;
    var y = __instance.transform.position.z + iy * 64f - 32f;
    return Settings.ApplyTerrainMap(x, y, ref __result) == false;
  }

  private static bool GetBiomePatch(Heightmap __instance, Vector3 point, ref Heightmap.Biome __result)
  {
    if (__instance.m_isDistantLod) return true;
    __result = GetBiome(__instance, point);
    return false;
  }
  private static Heightmap.Biome GetBiome(Heightmap obj, Vector3 point)
  {
    if (obj.m_cornerBiomes[0].Biome == obj.m_cornerBiomes[1].Biome && obj.m_cornerBiomes[0].Biome == obj.m_cornerBiomes[2].Biome && obj.m_cornerBiomes[0].Biome == obj.m_cornerBiomes[3].Biome)
      return obj.m_cornerBiomes[0].Biome;
    obj.WorldToNormalizedHM(point, out var ix, out var iy);
    for (int i = 1; i < Heightmap.s_tempBiomeWeights.Length; i++)
    {
      Heightmap.s_tempBiomeWeights[i] = 0f;
    }
    var size = Settings.BiomePrecision + 1;
    var x = ix * size;
    var y = iy * size;
    var sx = (int)x;
    var sy = (int)y;
    var ex = sx + 1;
    var ey = sy + 1;
    if (sx == size)
    {
      sx -= 1;
      ex -= 1;
    }
    if (sy == size)
    {
      sy -= 1;
      ey -= 1;
    }
    size += 1;
    var corner1 = 4 + sx * size + sy;
    var corner2 = 4 + ex * size + sy;
    var corner3 = 4 + sx * size + ey;
    var corner4 = 4 + ex * size + ey;
    if (obj.m_cornerBiomes.Length <= corner4)
      return obj.m_cornerBiomes[0].Biome;

    Heightmap.s_tempBiomeWeights[obj.m_cornerBiomes[corner1].Biome.ToIndex()] += Heightmap.Distance(x, y, sx, sy);
    Heightmap.s_tempBiomeWeights[obj.m_cornerBiomes[corner2].Biome.ToIndex()] += Heightmap.Distance(x, y, ex, sy);
    Heightmap.s_tempBiomeWeights[obj.m_cornerBiomes[corner3].Biome.ToIndex()] += Heightmap.Distance(x, y, sx, ey);
    Heightmap.s_tempBiomeWeights[obj.m_cornerBiomes[corner4].Biome.ToIndex()] += Heightmap.Distance(x, y, ex, ey);
    int num = Heightmap.Biome.None.ToIndex();
    float num2 = -99999f;
    for (int j = 1; j < Heightmap.s_tempBiomeWeights.Length; j++)
    {
      if (Heightmap.s_tempBiomeWeights[j] > num2)
      {
        num = j;
        num2 = Heightmap.s_tempBiomeWeights[j];
      }
    }
    return ((Heightmap.BiomeIndex)num).ToBiome();
  }

  // Valheim 1.0 stores biome sectors (biome plus alt biome data) in the corner array instead of
  // plain biomes. Sectors for points where the world data doesn't match the Better Continents biome.
  private static readonly BiomeSector[] PlainSectors =
    [.. Enumerable.Range(0, (int)Heightmap.BiomeIndex.Count).Select(i => new BiomeSector(null, ((Heightmap.BiomeIndex)i).ToBiome()))];

  // The world sector data has a resolution of 12 meters and is built from the patched GetBiome,
  // so it's used when it agrees with the biome here, to keep any alt biomes of that sector.
  private static BiomeSector GetSector(WorldGenerator worldGen, Heightmap.Biome biome, float wx, float wy)
  {
    var sector = worldGen.GetBiomeSector(wx, wy);
    if (sector != null && sector.Biome == biome)
      return sector;
    return PlainSectors[ImageMapBiome.ToSafeIndex(biome)];
  }

  private static Color GetBiomeColor(Heightmap obj, float ix, float iy)
  {
    if (obj.m_cornerBiomes[0] == obj.m_cornerBiomes[1] && obj.m_cornerBiomes[0] == obj.m_cornerBiomes[2] && obj.m_cornerBiomes[0] == obj.m_cornerBiomes[3])
    {
      return Heightmap.GetBiomeColor(obj.m_cornerBiomes[0]);
    }
    var size = Settings.BiomePrecision + 1;
    var x = ix * size;
    var y = iy * size;
    var sx = (int)x;
    var sy = (int)y;
    var ex = sx + 1;
    var ey = sy + 1;
    if (sx == size)
    {
      sx -= 1;
      ex -= 1;
    }
    if (sy == size)
    {
      sy -= 1;
      ey -= 1;
    }
    size += 1;
    var corner1 = 4 + sx * size + sy;
    var corner2 = 4 + ex * size + sy;
    var corner3 = 4 + sx * size + ey;
    var corner4 = 4 + ex * size + ey;
    if (obj.m_cornerBiomes.Length <= corner4)
    {
      return Color.black;
    }
    var biomeColor = Heightmap.GetBiomeColor(obj.m_cornerBiomes[corner1]);
    var biomeColor2 = Heightmap.GetBiomeColor(obj.m_cornerBiomes[corner2]);
    var biomeColor3 = Heightmap.GetBiomeColor(obj.m_cornerBiomes[corner3]);
    var biomeColor4 = Heightmap.GetBiomeColor(obj.m_cornerBiomes[corner4]);
    Color32 a = Color32.Lerp(biomeColor, biomeColor2, x - sx);
    Color32 b = Color32.Lerp(biomeColor3, biomeColor4, x - sx);
    return Color32.Lerp(a, b, y - sy);
  }
  private static bool BuildPatch(HeightmapBuilder.HMBuildData data)
  {
    Build(data);
    return false;
  }

  private static void Build(HeightmapBuilder.HMBuildData data)
  {
    int num = data.m_width + 1;
    int num2 = num * num;
    Vector3 vector = data.m_center + new Vector3((float)data.m_width * data.m_scale * -0.5f, 0f, (float)data.m_width * data.m_scale * -0.5f);
    WorldGenerator worldGen = data.m_worldGen;
    var biome = worldGen.GetBiome(vector.x, vector.z);
    var biome2 = worldGen.GetBiome(vector.x + data.m_width * data.m_scale, vector.z);
    var biome3 = worldGen.GetBiome(vector.x, vector.z + data.m_width * data.m_scale);
    var biome4 = worldGen.GetBiome(vector.x + data.m_width * data.m_scale, vector.z + data.m_width * data.m_scale);
    if (biome == biome2 && biome == biome3 && biome == biome4)
    {
      var sector = GetSector(worldGen, biome, vector.x, vector.z);
      data.m_cornerBiomes = [sector, sector, sector, sector];
    }
    else
    {
      var size = Settings.BiomePrecision + 2;
      // Duplicate corners to simplify logic.
      data.m_cornerBiomes = new BiomeSector[4 + size * size];
      data.m_cornerBiomes[0] = GetSector(worldGen, biome, vector.x, vector.z);
      data.m_cornerBiomes[1] = GetSector(worldGen, biome2, vector.x + data.m_width * data.m_scale, vector.z);
      data.m_cornerBiomes[2] = GetSector(worldGen, biome3, vector.x, vector.z + data.m_width * data.m_scale);
      data.m_cornerBiomes[3] = GetSector(worldGen, biome4, vector.x + data.m_width * data.m_scale, vector.z + data.m_width * data.m_scale);
      // Precision 1 = 3x3, 2 = 5x5, 3 = 7x7, etc.
      var last = Settings.BiomePrecision + 1;
      var multiplier = 1f / last;
      var index = 3;
      for (int x = 0; x <= last; x++)
      {
        for (int y = 0; y <= last; y++)
        {
          index += 1;
          var wx = vector.x + data.m_width * data.m_scale * x * multiplier;
          var wy = vector.z + data.m_width * data.m_scale * y * multiplier;
          data.m_cornerBiomes[index] = GetSector(worldGen, worldGen.GetBiome(wx, wy), wx, wy);
        }
      }
    }
    data.m_baseHeights = new List<float>(num * num);
    for (int i = 0; i < num2; i++)
    {
      data.m_baseHeights.Add(0f);
    }
    // Valheim 1.0 paints the whole width + 1 grid, matching the paint mask texture size.
    int num3 = num * num;
    data.m_baseMask = new Color[num3];
    for (int j = 0; j < num3; j++)
    {
      data.m_baseMask[j] = new Color(0f, 0f, 0f, 0f);
    }
    Color[] array = new Color[4];
    for (int k = 0; k < num; k++)
    {
      float wy = (float)((double)vector.z + (double)k * (double)data.m_scale);
      float t = DUtils.SmoothStep(0f, 1f, (float)((double)k / (double)data.m_width));
      for (int l = 0; l < num; l++)
      {
        float wx = (float)((double)vector.x + (double)l * (double)data.m_scale);
        float t2 = DUtils.SmoothStep(0f, 1f, (float)((double)l / (double)data.m_width));
        Color color = Color.black;
        float value;
        if (data.m_distantLod)
        {
          Heightmap.Biome biome5 = worldGen.GetBiome(wx, wy);
          value = worldGen.GetBiomeHeight(biome5, wx, wy, out color, false);
        }
        else if (biome3 == biome && biome2 == biome && biome4 == biome)
        {
          value = worldGen.GetBiomeHeight(biome, wx, wy, out color, false);
        }
        else
        {
          float biomeHeight = worldGen.GetBiomeHeight(biome, wx, wy, out array[0], false);
          float biomeHeight2 = worldGen.GetBiomeHeight(biome2, wx, wy, out array[1], false);
          float biomeHeight3 = worldGen.GetBiomeHeight(biome3, wx, wy, out array[2], false);
          float biomeHeight4 = worldGen.GetBiomeHeight(biome4, wx, wy, out array[3], false);
          float a = DUtils.Lerp(biomeHeight, biomeHeight2, t2);
          float b = DUtils.Lerp(biomeHeight3, biomeHeight4, t2);
          value = DUtils.Lerp(a, b, t);
          Color a2 = Color.Lerp(array[0], array[1], t2);
          Color b2 = Color.Lerp(array[2], array[3], t2);
          color = Color.Lerp(a2, b2, t);
        }
        data.m_baseHeights[k * num + l] = value;
        data.m_baseMask[k * num + l] = color;
      }
    }
    if (data.m_distantLod)
    {
      for (int m = 0; m < 4; m++)
      {
        List<float> list = new List<float>(data.m_baseHeights);
        for (int n = 1; n < num - 1; n++)
        {
          for (int num4 = 1; num4 < num - 1; num4++)
          {
            float num5 = list[n * num + num4];
            float num6 = list[(n - 1) * num + num4];
            float num7 = list[(n + 1) * num + num4];
            float num8 = list[n * num + num4 - 1];
            float num9 = list[n * num + num4 + 1];
            if (Mathf.Abs(num5 - num6) > 10f)
            {
              num5 = (num5 + num6) * 0.5f;
            }
            if (Mathf.Abs(num5 - num7) > 10f)
            {
              num5 = (num5 + num7) * 0.5f;
            }
            if (Mathf.Abs(num5 - num8) > 10f)
            {
              num5 = (num5 + num8) * 0.5f;
            }
            if (Mathf.Abs(num5 - num9) > 10f)
            {
              num5 = (num5 + num9) * 0.5f;
            }
            data.m_baseHeights[n * num + num4] = num5;
          }
        }
      }
    }
  }
}