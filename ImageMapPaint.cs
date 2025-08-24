using System;
using System.Collections.Generic;
using System.Linq;
using SixLabors.ImageSharp.PixelFormats;
using UnityEngine;

namespace BetterContinents;

internal class ImageMapPaint() : ImageMapColor()
{
    public static ImageMapPaint? Create(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        ImageMapPaint map = new()
        {
            FilePath = path,
        };
        if (!map.LoadSourceImage())
            return null;
        if (!map.CreateMap())
            return null;
        return map;
    }
    public static ImageMapPaint? Create(byte[] data, string path, string colors)
    {
        ImageMapPaint map = new()
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
    public static ImageMapPaint? LoadLegacy(ZPackage pkg)
    {
        var path = pkg.ReadString();
        if (string.IsNullOrEmpty(path))
            return null;
        return Create(pkg.ReadByteArray(), path);
    }

    public static ImageMapPaint? Create(byte[] data, string colors) => Create(data, "", colors);
    private static readonly string DefaultColors = "";

    public override bool LoadSourceImage() => LoadSourceImageAndColors(DefaultColors);
    protected override void ParseColors()
    {
        Colors = ParseColors(SourceColors);
    }
    private static Dictionary<Rgba32, Color32?> ParseColors(string colors) =>
        colors.Split('|')
        .Select(s => s.Trim().Split(':')).Where(s => s.Length == 2)
        .Select(s => Tuple.Create(ParseRGBA(s[1]), (Color32?)ParseColor32(s[0])))
        .Distinct(new Comparer())
        .ToDictionary(s => s.Item1, s => s.Item2);



    class Comparer : IEqualityComparer<Tuple<Rgba32, Color32?>>
    {
        public bool Equals(Tuple<Rgba32, Color32?> x, Tuple<Rgba32, Color32?> y) => x.Item1.Equals(y.Item1);
        public int GetHashCode(Tuple<Rgba32, Color32?> obj) => obj.Item1.GetHashCode();
    }
}
