// 0.0.2c
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UnityEngine;

namespace BetterContinents;

/**
 * Sampled export of the live height function. Lives separately from the
 * heightmap IMPORT pipeline (ImageMapFloat) because round-tripping isn't the
 * goal — these are diagnostic dumps for users authoring biome maps from a
 * procedurally generated world. The range is sample-driven (min/max over the
 * grid) so we preserve full dynamic range in 16 bits without pre-committing
 * to a fixed scale; the chosen range is logged so the user can interpret the
 * gray values. PNG row 0 corresponds to world north so re-import via BC's
 * existing heightmap loader (which flips on load) lines back up.
 */
internal static class ExportCommands
{
    private const int DefaultSize = 4096;
    private const int MaxSize = 16384;

    public static void ExportHeightmap(string arg)
    {
        if (WorldGenerator.instance == null || WorldGenerator.instance.m_world == null)
        {
            Console.instance.Print("[BC] export_heightmap: no world is loaded");
            return;
        }

        int size = ParseSize(arg);
        if (size <= 0) return;

        string outDir = Path.Combine(
            Utils.GetSaveDataPath(FileHelpers.FileSource.Local),
            "BetterContinents",
            WorldGenerator.instance.m_world.m_name);
        Directory.CreateDirectory(outDir);

        string stamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
        string filename = $"heightmap_{stamp}_{size}.png";
        string path = Path.Combine(outDir, filename);

        int threads = Math.Max(1, Environment.ProcessorCount - 2);
        Console.instance.Print($"[BC] export_heightmap: sampling {size}x{size} on {threads} threads, this may take a while...");

        Stopwatch sw = new Stopwatch();
        sw.Start();

        float radius = BetterContinents.TotalRadius;
        float[] heights = new float[size * size];

        /**
         * Two-pass design: first parallel-fill the height array (per-row work
         * is independent and GetBaseHeight is a pure compute path — Mathf
         * noise, BC settings reads, no Unity scene access), then a serial
         * sweep does min/max and encode. The min/max sweep is linear and
         * very fast compared to sampling, so it's not worth the lock-free
         * gymnastics that would let us fold it into the parallel pass.
         *
         * Capping at ProcessorCount - 2 leaves headroom for the OS and the
         * Unity render/main thread so the game stays responsive during the
         * sample run.
         */
        ParallelOptions opts = new ParallelOptions { MaxDegreeOfParallelism = threads };
        Parallel.For(0, size, opts, y =>
        {
            float wy = (0.5f - (y + 0.5f) / size) * 2f * radius;
            int rowOffset = y * size;
            for (int x = 0; x < size; x++)
            {
                float wx = ((x + 0.5f) / size - 0.5f) * 2f * radius;
                heights[rowOffset + x] = WorldGenerator.instance.GetBaseHeight(wx, wy, false);
            }
        });

        long sampleMs = sw.ElapsedMilliseconds;
        sw.Restart();

        float minH = float.PositiveInfinity;
        float maxH = float.NegativeInfinity;
        for (int i = 0; i < heights.Length; i++)
        {
            float h = heights[i];
            if (h < minH) minH = h;
            if (h > maxH) maxH = h;
        }

        float range = maxH - minH;
        if (range < 1e-6f) range = 1f;

        using Image<L16> image = new(size, size);
        image.ProcessPixelRows(acc =>
        {
            for (int y = 0; y < acc.Height; y++)
            {
                // var here is a deliberate concession: Span<L16> is defined in
                // both mscorlib and System.Memory.dll on net4.8, so the
                // explicit form is ambiguous to the compiler. var resolves
                // unambiguously through the method return type.
                var row = acc.GetRowSpan(y);
                int srcRow = y * size;
                for (int x = 0; x < row.Length; x++)
                {
                    float t = (heights[srcRow + x] - minH) / range;
                    ushort v = (ushort)Mathf.Clamp(t * 65535f, 0f, 65535f);
                    row[x] = new L16(v);
                }
            }
        });

        image.SaveAsPng(path);

        Console.instance.Print($"[BC] export_heightmap done: {path}");
        Console.instance.Print($"[BC]   sampled {size * size} points in {sampleMs} ms, wrote in {sw.ElapsedMilliseconds} ms");
        Console.instance.Print($"[BC]   normalized height range: min={minH:F4} ({minH * 200f:F1} m), max={maxH:F4} ({maxH * 200f:F1} m)");
    }

    private static int ParseSize(string arg)
    {
        if (string.IsNullOrEmpty(arg)) return DefaultSize;
        if (!int.TryParse(arg, out int size))
        {
            Console.instance.Print($"[BC] export_heightmap: could not parse size '{arg}'");
            return -1;
        }
        if (size <= 0)
        {
            Console.instance.Print($"[BC] export_heightmap: size must be positive");
            return -1;
        }
        if (size > MaxSize)
        {
            Console.instance.Print($"[BC] export_heightmap: capping size at {MaxSize}");
            size = MaxSize;
        }
        return size;
    }
}