using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UnityEngine;

namespace BetterContinents;

internal class ImageMapBiome(string filePath) : ImageMapBase(filePath)
{
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
            s => Enum.TryParse<Heightmap.Biome>(s[0].Trim(), true, out var biome) ? biome : throw new Exception($"Invalid biome name {s[0]}"),
            s =>
            {
                var split = s[1].Trim().Split(',').Select(s => s.Trim()).ToArray();
                if (split.Length < 3)
                    throw new Exception($"Invalid biome color {s[1]}");
                var a = split.Length == 3 ? "255" : split[3];
                return new Color32(byte.Parse(split[0]), byte.Parse(split[1]), byte.Parse(split[2]), byte.Parse(a));
            }
        );

    private static readonly string DefaultColors = "None: 0,0,0|Ocean: 0,0,255|Plains: 255,255,0|BlackForest: 0,127,0|Swamp: 127,127,0|Mountains: 255,255,255|Mistlands: 127,127,127|DeepNorth: 0,255,255|AshLands: 255,0,0";
    public bool CreateMap() => CreateMap<Rgba32>();
    protected override bool LoadTextureToMap<T>(Image<T> image)
    {
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
        float xa = x * (Size - 1);
        float ya = y * (Size - 1);

        int xi = Mathf.FloorToInt(xa);
        int yi = Mathf.FloorToInt(ya);

        float xd = xa - xi;
        float yd = ya - yi;

        // "Interpolate" the 4 corners (sum the weights of the biomes at the four corners)
        Heightmap.Biome GetBiome(int _x, int _y) => Map[Mathf.Clamp(_y, 0, Size - 1) * Size + Mathf.Clamp(_x, 0, Size - 1)];

        var biomes = new Heightmap.Biome[4];
        var biomeWeights = new float[4];
        int numBiomes = 0;
        int topBiomeIdx = 0;
        void SampleBiomeWeighted(int xs, int ys, float weight)
        {
            var biome = GetBiome(xs, ys);
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
        SampleBiomeWeighted(xi + 0, yi + 0, (1 - xd) * (1 - yd));
        SampleBiomeWeighted(xi + 1, yi + 0, xd * (1 - yd));
        SampleBiomeWeighted(xi + 0, yi + 1, (1 - xd) * yd);
        SampleBiomeWeighted(xi + 1, yi + 1, xd * yd);

        return biomes[topBiomeIdx];
    }

    public override void Serialize(ZPackage pkg, int version, bool network)
    {
        base.Serialize(pkg, version, network);
        if (version >= 8)
        {
            var colors = Colors.Select(d => $"{d.Key}:{d.Value.r},{d.Value.g},{d.Value.b},{d.Value.a}");
            pkg.Write(string.Join("|", colors));
        }
    }
    public static ImageMapBiome? Load(ZPackage pkg, int version)
    {
        var path = pkg.ReadString();
        if (string.IsNullOrEmpty(path))
            return null;
        var map = new ImageMapBiome(path);
        map.Deserialize(pkg, version);
        return map.CreateMap() ? map : null;
    }
    public void Deserialize(ZPackage pkg, int version)
    {
        SourceData = pkg.ReadByteArray();
        if (version >= 8)
            Colors = ParseColors(pkg.ReadString());
    }
}
