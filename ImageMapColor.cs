using System.Collections.Generic;
using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UnityEngine;

namespace BetterContinents;

internal class ImageMapColor() : ImageMapBase()
{
    public static ImageMapColor? Create(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        ImageMapColor map = new()
        {
            FilePath = path
        };
        if (!map.LoadSourceImage())
            return null;
        if (!map.CreateMap())
            return null;
        return map;
    }
    public static ImageMapColor? Create(byte[] data, string path)
    {
        ImageMapColor map = new()
        {
            FilePath = path,
            SourceData = data
        };
        if (!map.CreateMap())
            return null;
        return map;
    }
    public static ImageMapColor? Create(byte[] data) => Create(data, "");
    private UnityEngine.Color[] Map = [];

    public bool CreateMap() => CreateMap<Rgba32>();
    protected override bool LoadTextureToMap<T>(Image<T> image)
    {
        var st = new Stopwatch();
        st.Start();

        var colorMapping = new Dictionary<Color32, Heightmap.Biome>(new Color32Comparer());
        var img = (Image<Rgba32>)(Image)image;
        Map = LoadPixels(img, pixel => new UnityEngine.Color(pixel.R / 255f, pixel.G / 255f, pixel.B / 255f, pixel.A / 255f));

        BetterContinents.Log($"Time to calculate paints from {FilePath}: {st.ElapsedMilliseconds} ms");
        return true;
    }


    public static ImageMapColor? LoadLegacy(ZPackage pkg)
    {
        var path = pkg.ReadString();
        if (string.IsNullOrEmpty(path))
            return null;
        return Create(pkg.ReadByteArray(), path);
    }


    public UnityEngine.Color GetValue(float x, float y)
    {
        float xa = x * (Size - 1);
        float ya = y * (Size - 1);

        int xi = Mathf.FloorToInt(xa);
        int yi = Mathf.FloorToInt(ya);

        float xd = xa - xi;
        float yd = ya - yi;

        int x0 = Mathf.Clamp(xi, 0, Size - 1);
        int x1 = Mathf.Clamp(xi + 1, 0, Size - 1);
        int y0 = Mathf.Clamp(yi, 0, Size - 1);
        int y1 = Mathf.Clamp(yi + 1, 0, Size - 1);

        var p00 = Map[y0 * Size + x0];
        var p10 = Map[y0 * Size + x1];
        var p01 = Map[y1 * Size + x0];
        var p11 = Map[y1 * Size + x1];

        var a = UnityEngine.Color.Lerp(p00, p10, xd);
        var b = UnityEngine.Color.Lerp(p01, p11, xd);
        return UnityEngine.Color.Lerp(a, b, yd);
    }
}
