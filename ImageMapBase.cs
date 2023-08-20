using System;
using System.Diagnostics;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using UnityEngine;

namespace BetterContinents;

// Loads and stores source image file in original format.
// Derived types will define the final type of the image pixels (the "map"), and
// how to access them
internal abstract class ImageMapBase
{
    public string FilePath;

    public byte[] SourceData;

    public int Size;

    public ImageMapBase(string filePath)
    {
        FilePath = filePath;
        SourceData = new byte[0];
    }

    public ImageMapBase(string filePath, byte[] sourceData) : this(filePath)
    {
        SourceData = sourceData;
    }

    public bool LoadSourceImage()
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
    public bool CreateMap() => CreateMap<Rgba32>();
    public bool CreateMap<T>() where T : unmanaged, IPixel<T>
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

        static bool IsPowerOfTwo(int x) => (x & (x - 1)) == 0;
        if (!IsPowerOfTwo(width))
        {
            BetterContinents.LogError(
                $"Cannot use texture {FilePath}: it is not a power of two size (e.g. 256, 512, 1024, 2048)");
            return false;
        }

        if (width > 4096)
        {
            BetterContinents.LogError(
                $"Cannot use texture {FilePath}: it is too big ({width}x{height}), keep the size to less or equal to 4096x4096");
            return false;
        }

        return true;
    }
}
