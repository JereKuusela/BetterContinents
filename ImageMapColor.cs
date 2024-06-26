﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UnityEngine;

namespace BetterContinents;

abstract class ImageMapColor() : ImageMapBase()
{

    private UnityEngine.Color?[] Map = [];
    public string SourceColors = "";
    protected Dictionary<Rgba32, UnityEngine.Color?> Colors = [];

    public bool CreateMap() => CreateMap<Rgba32>();

    protected bool LoadSourceImageAndColors(string defaultColors)
    {
        SourceColors = "";
        if (!base.LoadSourceImage()) return false;
        var path = Path.Combine(Path.GetDirectoryName(FilePath), Path.GetFileNameWithoutExtension(FilePath) + ".txt");
        if (!File.Exists(path))
        {
            File.WriteAllLines(path, defaultColors.Split('|'));
            ParseColors();
            return true;
        }
        try
        {
            SourceColors = string.Join("|", File.ReadAllLines(path));
            ParseColors();
        }
        catch (Exception ex)
        {
            BetterContinents.LogError($"Cannot load file {path}: {ex.Message}.");
        }
        return true;
    }

    protected override bool LoadTextureToMap<T>(Image<T> image)
    {
        var st = new Stopwatch();
        st.Start();

        var img = (Image<Rgba32>)(Image)image;
        Map = LoadPixels(img, pixel =>
        {
            if (Colors.TryGetValue(pixel, out var color))
                return color;
            return (UnityEngine.Color?)new UnityEngine.Color(pixel.R / 255f, pixel.G / 255f, pixel.B / 255f, pixel.A / 255f);
        });

        BetterContinents.Log($"Time to calculate colors from {FilePath}: {st.ElapsedMilliseconds} ms");
        return true;
    }

    protected virtual void ParseColors()
    {
        Colors = [];
    }

    public bool TryGetValue(float x, float y, out UnityEngine.Color color)
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
        if (p00 == null || p10 == null || p01 == null || p11 == null)
        {
            color = UnityEngine.Color.black;
            return false;
        }

        var a = UnityEngine.Color.Lerp((UnityEngine.Color)p00, (UnityEngine.Color)p10, xd);
        var b = UnityEngine.Color.Lerp((UnityEngine.Color)p01, (UnityEngine.Color)p11, xd);
        color = UnityEngine.Color.Lerp(a, b, yd);
        return true;
    }
}
