// 0.0.2c
using System;
using System.Diagnostics;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UnityEngine;

namespace BetterContinents;

internal class ImageMapFloat : ImageMapBase
{
    /**
     * All three Create overloads now go through the sidecar. The hash is
     * computed from SourceData, so wherever the bytes came from — disk read
     * via LoadSourceImage, or ZPackage deserialization via Serialize.cs —
     * the same image produces the same cache key.
     *
     * In the byte[]-based paths the caller has already done the I/O and put
     * the bytes in our hands, so the only cost on a cache hit is one MD5
     * pass over the buffer (~100-200 ms for an 8k image) plus the binary
     * read (~50-100 ms). On a miss we still do the full decode and write
     * the cache for next time.
     */
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
        if (map.TryLoadFloatCache(alpha))
            return map;
        if (!map.CreateMap(alpha))
            return null;
        map.SaveFloatCache();
        return map;
    }
    public static ImageMapFloat? Create(byte[] data, string path, bool legacy = false)
    {
        ImageMapFloat map = new()
        {
            FilePath = path,
            SourceData = data
        };
        if (map.TryLoadFloatCache(false))
            return map;
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
        map.SaveFloatCache();
        return map;
    }
    public static ImageMapFloat? Create(byte[] data, bool alpha)
    {
        ImageMapFloat map = new()
        {
            SourceData = data
        };
        if (map.TryLoadFloatCache(alpha))
            return map;
        if (!map.CreateMap(alpha))
            return null;
        map.SaveFloatCache();
        return map;
    }
    private float[] Map = [];
    private float[] AlphaMap = [];

    protected override uint CacheTypeTag => 0x464C5430u; // "FLT0"

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

    /**
     * Float arrays serialize as raw little-endian bytes via Buffer.BlockCopy.
     * Two arrays in this type (Map, optional AlphaMap), each prefixed by its
     * element count so a missing AlphaMap is a zero-length entry instead of
     * absent data. The alpha argument is intentionally ignored on read — the
     * cache stores whatever was processed at write time, and Map/AlphaMap
     * reflect that.
     */
    private bool TryLoadFloatCache(bool alpha)
    {
        return TryLoadFromCache(br =>
        {
            int mapCount = br.ReadInt32();
            Map = new float[mapCount];
            if (mapCount > 0)
            {
                byte[] buf = br.ReadBytes(mapCount * sizeof(float));
                Buffer.BlockCopy(buf, 0, Map, 0, buf.Length);
            }

            int alphaCount = br.ReadInt32();
            if (alphaCount > 0)
            {
                AlphaMap = new float[alphaCount];
                byte[] buf = br.ReadBytes(alphaCount * sizeof(float));
                Buffer.BlockCopy(buf, 0, AlphaMap, 0, buf.Length);
            }
            else
            {
                AlphaMap = [];
            }
        });
    }

    private void SaveFloatCache()
    {
        SaveToCache(bw =>
        {
            bw.Write(Map.Length);
            if (Map.Length > 0)
            {
                byte[] buf = new byte[Map.Length * sizeof(float)];
                Buffer.BlockCopy(Map, 0, buf, 0, buf.Length);
                bw.Write(buf);
            }

            bw.Write(AlphaMap.Length);
            if (AlphaMap.Length > 0)
            {
                byte[] buf = new byte[AlphaMap.Length * sizeof(float)];
                Buffer.BlockCopy(AlphaMap, 0, buf, 0, buf.Length);
                bw.Write(buf);
            }
        });
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