using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UnityEngine;

namespace BetterContinents;

internal class ImageMapColor() : ImageMapBase()
{
    public static ImageMapColor? Create(string path, Rgba32? defaultColor)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        ImageMapColor map = new()
        {
            FilePath = path,
            DefaultColor = defaultColor
        };
        if (!map.LoadSourceImage())
            return null;
        if (!map.CreateMap())
            return null;
        return map;
    }
    public static ImageMapColor? Create(byte[] data, string path, Rgba32? defaultColor)
    {
        ImageMapColor map = new()
        {
            FilePath = path,
            SourceData = data,
            DefaultColor = defaultColor
        };
        if (!map.CreateMap())
            return null;
        return map;
    }
    public static ImageMapColor? Create(byte[] data, Rgba32? defaultColor) => Create(data, "", defaultColor);
    public Rgba32? DefaultColor;
    private UnityEngine.Color[] Map = [];
    private UnityEngine.Color?[] NullableMap = [];

    public bool CreateMap() => CreateMap<Rgba32>();
    protected override bool LoadTextureToMap<T>(Image<T> image)
    {
        var st = new Stopwatch();
        st.Start();

        var img = (Image<Rgba32>)(Image)image;
        if (DefaultColor == null)
        {
            Map = LoadPixels(img, pixel => new UnityEngine.Color(pixel.R / 255f, pixel.G / 255f, pixel.B / 255f, pixel.A / 255f));
        }
        else
        {
            NullableMap = LoadPixels(img, pixel => (UnityEngine.Color?)new UnityEngine.Color(pixel.R / 255f, pixel.G / 255f, pixel.B / 255f, pixel.A / 255f));
            var defaultColor = new UnityEngine.Color(DefaultColor.Value.R / 255f, DefaultColor.Value.G / 255f, DefaultColor.Value.B / 255f, DefaultColor.Value.A / 255f);
            for (int i = 0; i < NullableMap.Length; i++)
            {
                if (NullableMap[i] == defaultColor)
                    NullableMap[i] = null;
            }
        }


        BetterContinents.Log($"Time to calculate colors from {FilePath}: {st.ElapsedMilliseconds} ms");
        return true;
    }


    public static ImageMapColor? LoadLegacy(ZPackage pkg)
    {
        var path = pkg.ReadString();
        if (string.IsNullOrEmpty(path))
            return null;
        return Create(pkg.ReadByteArray(), path, null);
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
    public bool TryGetValue(float x, float y, ref UnityEngine.Color color)
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

        var p00 = NullableMap[y0 * Size + x0];
        var p10 = NullableMap[y0 * Size + x1];
        var p01 = NullableMap[y1 * Size + x0];
        var p11 = NullableMap[y1 * Size + x1];
        if (p00 == null || p10 == null || p01 == null || p11 == null)
            return false;

        var a = UnityEngine.Color.Lerp((UnityEngine.Color)p00, (UnityEngine.Color)p10, xd);
        var b = UnityEngine.Color.Lerp((UnityEngine.Color)p01, (UnityEngine.Color)p11, xd);
        color = UnityEngine.Color.Lerp(a, b, yd);
        return true;
    }
}
