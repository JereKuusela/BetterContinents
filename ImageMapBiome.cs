using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UnityEngine;

namespace BetterContinents;

internal class ImageMapBiome() : ImageMapBase
{
    public static ImageMapBiome? Create(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        ImageMapBiome map = new()
        {
            FilePath = path
        };
        if (!map.LoadSourceImage())
            return null;
        if (!map.CreateMap())
            return null;
        return map;
    }
    public static ImageMapBiome? Create(byte[] data)
    {
        ImageMapBiome map = new()
        {
            Map = data.Select(b => ByteToBiome[b]).ToArray(),
            Size = (int)Math.Sqrt(data.Length)
        };
        return map;
    }
    public static ImageMapBiome? Create(byte[] data, string colors, string path)
    {
        ImageMapBiome map = new()
        {
            SourceData = data,
            FilePath = path,
            Colors = ParseColors(colors)
        };
        if (!map.CreateMap())
            return null;
        return map;
    }
    // Valheim 1.0 converts biomes to indices with a switch that throws for anything that is not
    // one of the ten known values, so combined or unused values must never reach the game.
    public static bool IsValidBiome(Heightmap.Biome biome) => biome switch
    {
        Heightmap.Biome.None or Heightmap.Biome.Meadows or Heightmap.Biome.Swamp or Heightmap.Biome.Mountain
            or Heightmap.Biome.BlackForest or Heightmap.Biome.Plains or Heightmap.Biome.AshLands
            or Heightmap.Biome.DeepNorth or Heightmap.Biome.Ocean or Heightmap.Biome.Mistlands => true,
        _ => false
    };
    // Same as the game's ToIndex extension, but without throwing on unknown values.
    public static int ToSafeIndex(Heightmap.Biome biome) => IsValidBiome(biome) ? biome.ToIndex() : 0;

    // Biomes are stored as the bit index of the flag (0 = None, 1 = first bit, and so on).
    // Bits without a biome (and any byte that is not a bit index at all) map to None.
    private static Heightmap.Biome ByteToBiomeValue(int index)
    {
        if (index <= 0 || index > 32) return Heightmap.Biome.None;
        var biome = (Heightmap.Biome)(1 << (index - 1));
        return IsValidBiome(biome) ? biome : Heightmap.Biome.None;
    }
    private static readonly Heightmap.Biome[] ByteToBiome = [.. Enumerable.Range(0, 256).Select(ByteToBiomeValue)];
    public byte[] Serialize() =>
        [.. Map.Select(b =>
        {
            var idx = Array.IndexOf(ByteToBiome, b);
            return idx < 0 ? (byte)0 : (byte)idx;
        })];

    private Heightmap.Biome[] Map = [];
    private Dictionary<Heightmap.Biome, Color32> Colors = [];
    public override bool LoadSourceImage()
    {
        if (!base.LoadSourceImage()) return false;
        var path = Path.Combine(Path.GetDirectoryName(FilePath), Path.GetFileNameWithoutExtension(FilePath) + ".txt");
        if (!File.Exists(path))
        {
            File.WriteAllLines(path, DefaultColors.Split('|'));
            Colors = ParseColors(DefaultColors);
            return true;
        }
        try
        {
            var colors = string.Join("|", File.ReadAllLines(path));
            Colors = ParseColors(colors);
        }
        catch (Exception ex)
        {
            BetterContinents.LogError($"Cannot load file {path}: {ex.Message}.");
            Colors = ParseColors(DefaultColors);
        }
        return true;
    }

    private static Dictionary<Heightmap.Biome, Color32> ParseColors(string colors) => colors.Split('|')
        .Select(s => s.Trim().Split(':')).Where(s => s.Length == 2)
        .ToDictionary(
            // TryParse also accepts numbers and combined values like "All", which the game can't handle.
            s => Enum.TryParse<Heightmap.Biome>(s[0].Trim(), true, out var biome) && IsValidBiome(biome) ? biome : throw new Exception($"Invalid biome name {s[0]}"),
            s => ParseColor32(s[1])
        );

    private static readonly string DefaultColors = "None: 000000|Meadows: 00FF00|BlackForest: 007F00|Swamp: 7F7F00|Mountain: FFFFFF|Plains: FFFF00|Mistlands: 7F7F7F|AshLands: FF0000|DeepNorth: 00FFFF|Ocean: 0000FF";
    public bool CreateMap() => CreateMap<Rgba32>();
    protected override bool LoadTextureToMap<T>(Image<T> image)
    {
        if (Colors.Count == 0)
        {
            BetterContinents.LogError($"No biome colors defined for image {FilePath}.");
            Colors = ParseColors(DefaultColors);
        }
        static int ColorDistance(Color32 a, Color32 b) =>
            (a.r - b.r) * (a.r - b.r) + (a.g - b.g) * (a.g - b.g) + (a.b - b.b) * (a.b - b.b);

        var st = new Stopwatch();
        st.Start();

        var colorMapping = new Dictionary<Color32, Heightmap.Biome>(new Color32Comparer());
        var img = (Image<Rgba32>)(Image)image;
        Map = LoadPixels(img, pixel =>
        {
            var color = Convert(pixel);
            if (!colorMapping.TryGetValue(color, out var biome))
            {
                biome = Colors.OrderBy(d => ColorDistance(color, d.Value)).First().Key;
                colorMapping.Add(color, biome);
            }
            return biome;
        });

        BetterContinents.Log($"Time to calculate biomes from {FilePath}: {st.ElapsedMilliseconds} ms");
        return true;
    }

    public Heightmap.Biome GetValue(float x, float y)
    {
        int topBiomeIdx = 0;
        int numBiomes = 0;
        float xa = x * (Size - 1);
        float ya = y * (Size - 1);

        int xi = Mathf.FloorToInt(xa);
        int yi = Mathf.FloorToInt(ya);

        float xd = xa - xi;
        float yd = ya - yi;

        var biomes = new Heightmap.Biome[4];
        var biomeWeights = new float[4];
        SampleBiomeWeighted(xi + 0, yi + 0, (1 - xd) * (1 - yd), biomeWeights, biomes, ref numBiomes, ref topBiomeIdx);
        SampleBiomeWeighted(xi + 1, yi + 0, xd * (1 - yd), biomeWeights, biomes, ref numBiomes, ref topBiomeIdx);
        SampleBiomeWeighted(xi + 0, yi + 1, (1 - xd) * yd, biomeWeights, biomes, ref numBiomes, ref topBiomeIdx);
        SampleBiomeWeighted(xi + 1, yi + 1, xd * yd, biomeWeights, biomes, ref numBiomes, ref topBiomeIdx);

        return biomes[topBiomeIdx];
    }

    private void SampleBiomeWeighted(int xs, int ys, float weight, float[] biomeWeights, Heightmap.Biome[] biomes, ref int numBiomes, ref int topBiomeIdx)
    {
        var biome = Map[Mathf.Clamp(ys, 0, Size - 1) * Size + Mathf.Clamp(xs, 0, Size - 1)];
        int i = 0;
        for (; i < numBiomes; ++i)
        {
            if (biomes[i] == biome)
            {
                if (biomeWeights[i] + weight > biomeWeights[topBiomeIdx])
                    topBiomeIdx = i;
                biomeWeights[i] += weight;
                return;
            }
        }

        if (i == numBiomes)
        {
            if (biomeWeights[numBiomes] + weight > biomeWeights[topBiomeIdx])
                topBiomeIdx = numBiomes;
            biomes[numBiomes] = biome;
            biomeWeights[numBiomes++] = weight;
        }
    }

    public override void SerializeLegacy(ZPackage pkg, int version, bool network)
    {
        base.SerializeLegacy(pkg, version, network);
        if (version >= 8)
        {
            var colors = Colors.Select(d => $"{d.Key}:{d.Value.r},{d.Value.g},{d.Value.b},{d.Value.a}");
            pkg.Write(string.Join("|", colors));
        }
    }
    public static ImageMapBiome? LoadLegacy(ZPackage pkg, int version)
    {
        var path = pkg.ReadString();
        if (string.IsNullOrEmpty(path))
            return null;
        var data = pkg.ReadByteArray();
        var colors = version >= 8 ? pkg.ReadString() : DefaultColors;
        return Create(data, colors, path);
    }
}
