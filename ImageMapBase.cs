// 0.0.3c
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using UnityEngine;

namespace BetterContinents;

// Loads and stores source image file in original format.
// Derived types will define the final type of the image pixels (the "map"), and
// how to access them
internal abstract class ImageMapBase()
{
    public string FilePath = "";

    public byte[] SourceData = [];

    public int Size;

    /**
     * Sidecar cache, content-addressed by MD5 of SourceData.
     *
     * Earlier draft (0.0.2c) keyed on FilePath + mtime + size. That worked only
     * for the path-based Create flow in BetterContinentsSettings (first-time
     * world creation reading from disk). The common path — loading a previously
     * saved world — comes through Serialize.cs and uses the byte[]-based
     * Create overloads, where FilePath is never set; the cache never fired
     * and we kept eating the full 30+s decode every load.
     *
     * Hashing SourceData unifies both paths: wherever the bytes come from,
     * the hash identifies them. mtime/size matching becomes irrelevant
     * because the content speaks for itself. MD5 is fine here — we want a
     * stable content fingerprint, not crypto, and the ~200ms it costs on a
     * 50 MB buffer is rounding error against a 12s decode.
     *
     * CacheTypeTag still gates participation: subclasses without an override
     * return 0 and the cache is a no-op for them, so partially-ported types
     * (Biome, Color, Location, Spawn currently) compile and run as before.
     * The tag is also written into the header so a Float cache file can't be
     * read into a Biome instance even if their hashes somehow collided.
     *
     * Hash is memoised in _sourceHash so we don't recompute when TryLoad
     * miss leads to SaveToCache.
     */
    private const int CacheMagic = 0x4243_5343; // "BCSC"
    private const int CacheVersion = 2;
    public static string CurrentWorldName = "";

    protected virtual uint CacheTypeTag => 0u;

    private string? _sourceHash;

    private string GetSourceHash()
    {
        if (_sourceHash != null) return _sourceHash;
        if (SourceData.Length == 0)
        {
            _sourceHash = "";
            return _sourceHash;
        }
        using MD5 md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(SourceData);
        _sourceHash = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        return _sourceHash;
    }

    private string GetCachePath()
    {
        string worldFolder = string.IsNullOrEmpty(CurrentWorldName) ? "_unscoped" : CurrentWorldName;
        string cacheDir = Path.Combine(Utils.GetSaveDataPath(FileHelpers.FileSource.Local),
            "BetterContinents", worldFolder, "cache");
        return Path.Combine(cacheDir, GetSourceHash() + ".bcbin");
    }

    protected bool TryLoadFromCache(Action<BinaryReader> readPayload)
    {
        if (CacheTypeTag == 0u) return false;
        if (SourceData.Length == 0) return false;

        string hash = GetSourceHash();
        if (string.IsNullOrEmpty(hash)) return false;

        string cachePath = GetCachePath();
        if (!File.Exists(cachePath)) return false;

        try
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            using FileStream fs = File.OpenRead(cachePath);
            using BinaryReader br = new BinaryReader(fs);

            if (br.ReadInt32() != CacheMagic) return false;
            if (br.ReadInt32() != CacheVersion) return false;
            if (br.ReadUInt32() != CacheTypeTag) return false;
            Size = br.ReadInt32();

            readPayload(br);

            string nameForLog = string.IsNullOrEmpty(FilePath) ? hash.Substring(0, 12) : Path.GetFileName(FilePath);
            BetterContinents.Log($"Sidecar cache hit {nameForLog}: {sw.ElapsedMilliseconds} ms");
            return true;
        }
        catch (Exception ex)
        {
            BetterContinents.LogWarning($"Sidecar cache read failed for {FilePath} ({hash}): {ex.Message}");
            return false;
        }
    }

    protected void SaveToCache(Action<BinaryWriter> writePayload)
    {
        if (CacheTypeTag == 0u) return;
        if (SourceData.Length == 0) return;

        string hash = GetSourceHash();
        if (string.IsNullOrEmpty(hash)) return;

        string cachePath = GetCachePath();
        try
        {
            string? dir = Path.GetDirectoryName(cachePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            using FileStream fs = File.Create(cachePath);
            using BinaryWriter bw = new BinaryWriter(fs);

            bw.Write(CacheMagic);
            bw.Write(CacheVersion);
            bw.Write(CacheTypeTag);
            bw.Write(Size);

            writePayload(bw);
        }
        catch (Exception ex)
        {
            BetterContinents.LogWarning($"Sidecar cache write failed for {FilePath} ({hash}): {ex.Message}");
        }
    }

    public virtual bool LoadSourceImage()
    {
        if (!File.Exists(FilePath))
        {
            BetterContinents.LogWarning($"Cannot find image {FilePath}: Image was not reloaded.");
            return false;
        }
        try
        {
            SourceData = File.ReadAllBytes(FilePath);
            return true;
        }
        catch (Exception ex)
        {
            BetterContinents.LogError($"Cannot load image {FilePath}: {ex.Message}");
            return false;
        }
    }

    protected static Color32 Convert(Rgba32 pixel) => new(pixel.R, pixel.G, pixel.B, pixel.A);

    protected Image<T> LoadImage<T>() where T : unmanaged, IPixel<T> => Image.Load<T>(Configuration.Default, SourceData);

    protected abstract bool LoadTextureToMap<T>(Image<T> image) where T : unmanaged, IPixel<T>;

    public R[] LoadPixels<T, R>(Image<T> image, Func<T, R> converter) where T : unmanaged, IPixel<T>
    {
        var pixels = new R[image.Width * image.Height];
        image.ProcessPixelRows(acc =>
        {
            for (int y = 0; y < acc.Height; y++)
            {
                var row = acc.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    pixels[y * row.Length + x] = converter(row[x]);
                }
            }
        });
        return pixels;
    }
    protected bool CreateMap<T>() where T : unmanaged, IPixel<T>
    {
        try
        {
            var sw = new Stopwatch();
            sw.Start();

            // Cast disambiguates to the correct return type for some reason
            using var image = LoadImage<T>();
            if (!ValidateDimensions(image.Width, image.Height))
            {
                return false;
            }
            Size = image.Width;

            image.Mutate(x => x.Flip(FlipMode.Vertical));

            BetterContinents.Log($"Time to load {FilePath}: {sw.ElapsedMilliseconds} ms");

            return LoadTextureToMap(image);
        }
        catch (Exception ex)
        {
            BetterContinents.LogError($"Cannot load texture {FilePath}: {ex.Message}");
            return false;
        }
    }

    protected bool ValidateDimensions(int width, int height)
    {
        if (width != height)
        {
            BetterContinents.LogError(
                $"Cannot use texture {FilePath}: its width ({width}) does not match its height ({height})");
            return false;
        }
        return true;
    }

    public virtual void SerializeLegacy(ZPackage pkg, int version, bool network)
    {
        // File path may contain sensitive imformation so its removed from network serialization.
        pkg.Write(network ? "?" : FilePath);
        pkg.Write(SourceData);
    }

    protected static Color32 ParseColor32(string color)
    {
        var rgba = ParseRGBA(color);
        return new Color32(rgba.R, rgba.G, rgba.B, rgba.A);
    }
    protected static Rgba32 ParseRGBA(string color)
    {
        color = color.Trim();
        var split = color.Split(',').ToArray();
        if (split.Length == 1)
        {
            if (SixLabors.ImageSharp.Color.TryParseHex(color, out var c))
                return c;
            else
            {
                BetterContinents.LogWarning($"Cannot parse color {color}");
                return new Rgba32(0, 0, 0, 0);
            }
        }
        if (split.Length < 3)
        {
            BetterContinents.LogWarning($"Cannot parse color {color}");
            return new Rgba32(0, 0, 0, 0);
        }
        var a = split.Length == 3 ? "255" : split[3];
        return new Rgba32(byte.Parse(split[0]), byte.Parse(split[1]), byte.Parse(split[2]), byte.Parse(a));
    }
}