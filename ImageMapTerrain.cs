using System;
using System.Collections.Generic;
using System.Linq;
using SixLabors.ImageSharp.PixelFormats;

namespace BetterContinents;

internal class ImageMapTerrain() : ImageMapColor()
{
    public static ImageMapTerrain? Create(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        ImageMapTerrain map = new()
        {
            FilePath = path,
        };
        if (!map.LoadSourceImage())
            return null;
        if (!map.CreateMap())
            return null;
        return map;
    }
    public static ImageMapTerrain? Create(byte[] data, string path, string colors)
    {
        ImageMapTerrain map = new()
        {
            FilePath = path,
            SourceData = data,
            SourceColors = colors
        };
        map.ParseColors();
        if (!map.CreateMap())
            return null;
        return map;
    }
    public static ImageMapTerrain? Create(byte[] data, string colors) => Create(data, "", colors);
    private static readonly string DefaultColors = "Default: 000000|Meadows: 00FF00|BlackForest: 007F00|Swamp: 7F7F00|Mountain: FFFFFF|Plains: FFFF00|Mistlands: 7F7F7F|AshLands: FF0000|DeepNorth: 00FFFF|Ocean: 0000FF";

    private static readonly Dictionary<string, UnityEngine.Color?> TerrainGrounds = new() {
        {"default", null},
        {"meadows", new UnityEngine.Color(0, 0, 0, 0)},
        {"blackforest", new UnityEngine.Color(0, 0, 1, 0)},
        {"swamp", new UnityEngine.Color(1, 0, 0, 0)},
        {"mountain", new UnityEngine.Color(0, 1, 0, 0)},
        {"plains", new UnityEngine.Color(0, 0, 0, 1)},
        {"mistlands", new UnityEngine.Color(0, 0, 1, 1)},
        {"ashlands", new UnityEngine.Color(1, 0, 0, 1)},
        {"deepnorth", new UnityEngine.Color(0, 1, 0, 0)},
        {"ocean", new UnityEngine.Color(0, 0, 0, 0)}
    };
    public override bool LoadSourceImage() => LoadSourceImageAndColors(DefaultColors);
    protected override void ParseColors()
    {
        Colors = ParseColors(SourceColors == "" ? DefaultColors : SourceColors);
    }
    private static Dictionary<Rgba32, UnityEngine.Color?> ParseColors(string colors) =>
        colors.Split('|')
        .Select(s => s.Trim().Split(':')).Where(s => s.Length == 2)
        .Select(s => Tuple.Create(ParseRGBA(s[1]), TerrainGrounds.TryGetValue(s[0].Trim().ToLower(), out var color) ? color : ParseColor(s[0])))
        .Distinct(new Comparer())
        .ToDictionary(s => s.Item1, s => s.Item2);

    class Comparer : IEqualityComparer<Tuple<Rgba32, UnityEngine.Color?>>
    {
        public bool Equals(Tuple<Rgba32, UnityEngine.Color?> x, Tuple<Rgba32, UnityEngine.Color?> y) => x.Item1.Equals(y.Item1);
        public int GetHashCode(Tuple<Rgba32, UnityEngine.Color?> obj) => obj.Item1.GetHashCode();
    }
}
