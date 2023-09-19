﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UnityEngine;

namespace BetterContinents;

internal class ImageMapBiome : ImageMapBase
{
    private Heightmap.Biome[] Map = new Heightmap.Biome[0];

    public ImageMapBiome(string filePath) : base(filePath) { }

    public ImageMapBiome(string filePath, byte[] sourceData) : base(filePath, sourceData) { }

    private struct ColorBiome
    {
        public Color32 color;
        public Heightmap.Biome biome;


        public ColorBiome(Color32 color, Heightmap.Biome biome)
        {
            this.color = color;
            this.biome = biome;
        }
    }

    private static readonly ColorBiome[] BiomeColorMapping = new ColorBiome[]
    {
            /*
                    None #000000
                    Ocean #0000FF 
                    Meadows #00FF00
                    Black Forest #007F00
                    Swamp #7F7F00
                    Mountains #FFFFFF
                    Plains #FFFF00
                    Mistlands #7F7F7F
                    Deep North #00FFFF
                    Ash Lands #FF0000
                */
            new(new Color32(0, 0, 0, 255), Heightmap.Biome.None),
            new(new Color32(0, 0, 255, 255), Heightmap.Biome.Ocean),
            new(new Color32(0, 255, 0, 255), Heightmap.Biome.Meadows),
            new(new Color32(0, 127, 0, 255), Heightmap.Biome.BlackForest),
            new(new Color32(127, 127, 0, 255), Heightmap.Biome.Swamp),
            new(new Color32(255, 255, 255, 255), Heightmap.Biome.Mountain),
            new(new Color32(255, 255, 0, 255), Heightmap.Biome.Plains),
            new(new Color32(127, 127, 127, 255), Heightmap.Biome.Mistlands),
            new(new Color32(0, 255, 255, 255), Heightmap.Biome.DeepNorth),
            new(new Color32(255, 0, 0, 255), Heightmap.Biome.AshLands),
    };
    public bool CreateMap() => CreateMap<Rgba32>();

    public bool CreateMap() => CreateMap<Rgba32>();
    protected override bool LoadTextureToMap<T>(Image<T> image)
    {
        int ColorDistance(Color32 a, Color32 b) =>
            (a.r - b.r) * (a.r - b.r) + (a.g - b.g) * (a.g - b.g) + (a.b - b.b) * (a.b - b.b);

        var st = new Stopwatch();
        st.Start();

        var colorMapping = new Dictionary<Color32, Heightmap.Biome>(new Color32Comparer());
        var img = (Image<Rgba32>)(Image)image;
        Map = LoadPixels(img, pixel =>
        {
            var color = Convert(pixel);
            if (!colorMapping.TryGetValue(color, out var biome))
            {
                biome = BiomeColorMapping.OrderBy(d => ColorDistance(color, d.color)).First().biome;
                colorMapping.Add(color, biome);
            }
            return biome;
        });

        BetterContinents.Log($"Time to calculate biomes from {FilePath}: {st.ElapsedMilliseconds} ms");
        return true;
    }

    public Heightmap.Biome GetValue(float x, float y)
    {
        float xa = x * (Size - 1);
        float ya = y * (Size - 1);

        int xi = Mathf.FloorToInt(xa);
        int yi = Mathf.FloorToInt(ya);

        float xd = xa - xi;
        float yd = ya - yi;

        // "Interpolate" the 4 corners (sum the weights of the biomes at the four corners)
        Heightmap.Biome GetBiome(int _x, int _y) => Map[Mathf.Clamp(_y, 0, Size - 1) * Size + Mathf.Clamp(_x, 0, Size - 1)];

        var biomes = new Heightmap.Biome[4];
        var biomeWeights = new float[4];
        int numBiomes = 0;
        int topBiomeIdx = 0;
        void SampleBiomeWeighted(int xs, int ys, float weight)
        {
            var biome = GetBiome(xs, ys);
            int i = 0;
            for (; i < numBiomes; ++i)
            {
                if (biomes[i] == biome)
                {
                    if (biomeWeights[i] + weight > biomeWeights[topBiomeIdx])
                        topBiomeIdx = i;
                    biomeWeights[i] += weight;
                    return;
                }
            }

            if (i == numBiomes)
            {
                if (biomeWeights[numBiomes] + weight > biomeWeights[topBiomeIdx])
                    topBiomeIdx = numBiomes;
                biomes[numBiomes] = biome;
                biomeWeights[numBiomes++] = weight;
            }
        }
        SampleBiomeWeighted(xi + 0, yi + 0, (1 - xd) * (1 - yd));
        SampleBiomeWeighted(xi + 1, yi + 0, xd * (1 - yd));
        SampleBiomeWeighted(xi + 0, yi + 1, (1 - xd) * yd);
        SampleBiomeWeighted(xi + 1, yi + 1, xd * yd);

        return biomes[topBiomeIdx];
    }
}
