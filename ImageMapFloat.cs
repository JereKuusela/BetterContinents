using System;
using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UnityEngine;

namespace BetterContinents;

internal class ImageMapFloat : ImageMapBase
{
    public static ImageMapFloat? Create(string path, bool alpha)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        ImageMapFloat map = new()
        {
            FilePath = path
        };
        if (!map.LoadSourceImage())
            return null;
        if (!map.CreateMap(alpha))
            return null;
        return map;
    }
    public static ImageMapFloat? Create(byte[] data, string path, bool legacy = false)
    {
        ImageMapFloat map = new()
        {
            FilePath = path,
            SourceData = data
        };
        if (legacy)
        {
            if (!map.CreateMapLegacy())
                return null;
        }
        else
        {
            if (!map.CreateMap(false))
                return null;
        }
        return map;
    }
    public static ImageMapFloat? Create(byte[] data, bool alpha)
    {
        ImageMapFloat map = new()
        {
            SourceData = data
        };
        if (!map.CreateMap(alpha))
            return null;
        return map;
    }
    private float[] Map = [];
    private float[] AlphaMap = [];

    public bool CreateMap(bool alpha) => alpha ? CreateMap<La16>() : CreateMap<L16>();
    public bool CreateMapLegacy() => CreateMap<Rgba32>();
    protected override bool LoadTextureToMap<T>(Image<T> image)
    {
        var sw = new Stopwatch();
        sw.Start();
        Map = LoadPixels(image, pixel => pixel.ToVector4().X);
        if (image is Image<La16> img)
            AlphaMap = LoadPixels(img, pixel => pixel.A / 65535f);
        else AlphaMap = [];

        BetterContinents.Log($"Time to process {FilePath}: {sw.ElapsedMilliseconds} ms");

        return true;
    }

    public float GetValue(float x, float y)
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

        float p00 = Map[y0 * Size + x0];
        float p10 = Map[y0 * Size + x1];
        float p01 = Map[y1 * Size + x0];
        float p11 = Map[y1 * Size + x1];

        return Mathf.Lerp(
            Mathf.Lerp(p00, p10, xd),
            Mathf.Lerp(p01, p11, xd),
            yd
        );
    }

}
