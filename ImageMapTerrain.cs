using System;
using System.Collections.Generic;
using System.Linq;
using SixLabors.ImageSharp.PixelFormats;
using UnityEngine;

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

    private static readonly Dictionary<string, Color32?> TerrainGrounds = new() {
        {"default", null},
        {"meadows", new Color32(0, 0, 0, 0)},
        {"blackforest", new Color32(0, 0, 255, 0)},
        {"swamp", new Color32(255, 0, 0, 0)},
        {"mountain", new Color32(0, 255, 0, 0)},
        {"plains", new Color32(0, 0, 0, 255)},
        {"mistlands", new Color32(0, 0, 255, 255)},
        {"ashlands", new Color32(255, 0, 0, 255)},
        {"deepnorth", new Color32(0, 255, 0, 0)},
        {"ocean", new Color32(0, 0, 0, 0)}
    };
    public override bool LoadSourceImage() => LoadSourceImageAndColors(DefaultColors);
    protected override void ParseColors()
    {
        Colors = ParseColors(SourceColors == "" ? DefaultColors : SourceColors);
    }
    private static Dictionary<Rgba32, Color32?> ParseColors(string colors) =>
        colors.Split('|')
        .Select(s => s.Trim().Split(':')).Where(s => s.Length == 2)
        .Select(s => Tuple.Create(ParseRGBA(s[1]), TerrainGrounds.TryGetValue(s[0].Trim().ToLower(), out var color) ? color : ParseColor32(s[0])))
        .Distinct(new Comparer())
        .ToDictionary(s => s.Item1, s => s.Item2);

    class Comparer : IEqualityComparer<Tuple<Rgba32, Color32?>>
    {
        public bool Equals(Tuple<Rgba32, Color32?> x, Tuple<Rgba32, Color32?> y) => x.Item1.Equals(y.Item1);
        public int GetHashCode(Tuple<Rgba32, Color32?> obj) => obj.Item1.GetHashCode();
    }
}
