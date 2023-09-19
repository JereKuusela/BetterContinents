﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UnityEngine;
using Color = UnityEngine.Color;

namespace BetterContinents;

internal class ImageMapSpawn : ImageMapBase
{
    public Dictionary<string, List<Vector2>> RemainingSpawnAreas = new();

    public ImageMapSpawn(string filePath) : base(filePath) { }

    public ImageMapSpawn(string filePath, ZPackage from) : base(filePath)
    {
        Deserialize(@from);
    }

    public bool CreateMap() => CreateMap<Rgba32>();
    public void Serialize(ZPackage to)
    {
        to.Write(RemainingSpawnAreas.Count);
        foreach (var kv in RemainingSpawnAreas)
        {
            to.Write(kv.Key);
            to.Write(kv.Value.Count);
            foreach (var v in kv.Value)
            {
                to.Write(v.x);
                to.Write(v.y);
            }
        }
    }

    private void Deserialize(ZPackage from)
    {
        int count = @from.ReadInt();
        RemainingSpawnAreas = new Dictionary<string, List<Vector2>>();
        for (int i = 0; i < count; i++)
        {
            var spawn = @from.ReadString();
            var positionsCount = @from.ReadInt();
            var positions = new List<Vector2>();
            for (int k = 0; k < positionsCount; k++)
            {
                float x = @from.ReadSingle();
                float y = @from.ReadSingle();
                positions.Add(new Vector2(x, y));
            }
            RemainingSpawnAreas.Add(spawn, positions);
        }
    }

    private struct ColorSpawn(Color32 color, string spawn)
    {
        public Color32 color = color;
        public string spawn = spawn;
    }

    private static readonly ColorSpawn[] SpawnColorMapping =
    [
        new ColorSpawn(new Color32(0xFF, 0x00, 0x00, 0xFF), "StartTemple"),
        new ColorSpawn(new Color32(0xFF, 0x99, 0x00, 0xFF), "Eikthyrnir"),
        new ColorSpawn(new Color32(0x00, 0xFF, 0x00, 0xFF), "GDKing"),
        new ColorSpawn(new Color32(0xFF, 0xFF, 0x00, 0xFF), "GoblinKing"),
        new ColorSpawn(new Color32(0x00, 0xFF, 0xFF, 0xFF), "Bonemass"),
        new ColorSpawn(new Color32(0x4A, 0x86, 0xE8, 0xFF), "Dragonqueen"),
        new ColorSpawn(new Color32(0x00, 0x00, 0xFF, 0xFF), "Vendor_BlackForest"),
        new ColorSpawn(new Color32(0xE6, 0xB8, 0xAF, 0xFF), "AbandonedLogCabin02"),
        new ColorSpawn(new Color32(0xE6, 0xB8, 0xAF, 0xFF), "AbandonedLogCabin03"),
        new ColorSpawn(new Color32(0xE6, 0xB8, 0xAF, 0xFF), "AbandonedLogCabin04"),
        new ColorSpawn(new Color32(0xC9, 0xDA, 0xF8, 0xFF), "TrollCave02"),
        new ColorSpawn(new Color32(0xFF, 0xF2, 0xCC, 0xFF), "Crypt2"),
        new ColorSpawn(new Color32(0xFF, 0xF2, 0xCC, 0xFF), "Crypt3"),
        new ColorSpawn(new Color32(0xFF, 0xF2, 0xCC, 0xFF), "Crypt4"),
        new ColorSpawn(new Color32(0x45, 0x81, 0x8E, 0xFF), "SunkenCrypt4"),
        new ColorSpawn(new Color32(0xFF, 0xE5, 0x99, 0xFF), "Dolmen03"),
        new ColorSpawn(new Color32(0xFF, 0xE5, 0x99, 0xFF), "Dolmen01"),
        new ColorSpawn(new Color32(0xFF, 0xE5, 0x99, 0xFF), "Dolmen02"),
        new ColorSpawn(new Color32(0xDD, 0x7E, 0x6B, 0xFF), "Ruin3"),
        new ColorSpawn(new Color32(0xCC, 0x41, 0x25, 0xFF), "StoneTower1"),
        new ColorSpawn(new Color32(0xCC, 0x41, 0x25, 0xFF), "StoneTower3"),
        new ColorSpawn(new Color32(0x6A, 0xA8, 0x4F, 0xFF), "MountainGrave01"),
        new ColorSpawn(new Color32(0x7F, 0x60, 0x60, 0xFF), "Grave1"),
        new ColorSpawn(new Color32(0xB6, 0xD7, 0xA8, 0xFF), "InfestedTree01"),
        new ColorSpawn(new Color32(0x6D, 0x9E, 0xEB, 0xFF), "WoodHouse1"),
        new ColorSpawn(new Color32(0x6D, 0x9E, 0xEB, 0xFF), "WoodHouse10"),
        new ColorSpawn(new Color32(0x6D, 0x9E, 0xEB, 0xFF), "WoodHouse11"),
        new ColorSpawn(new Color32(0x6D, 0x9E, 0xEB, 0xFF), "WoodHouse12"),
        new ColorSpawn(new Color32(0x6D, 0x9E, 0xEB, 0xFF), "WoodHouse13"),
        new ColorSpawn(new Color32(0x6D, 0x9E, 0xEB, 0xFF), "WoodHouse2"),
        new ColorSpawn(new Color32(0x6D, 0x9E, 0xEB, 0xFF), "WoodHouse3"),
        new ColorSpawn(new Color32(0x6D, 0x9E, 0xEB, 0xFF), "WoodHouse4"),
        new ColorSpawn(new Color32(0x6D, 0x9E, 0xEB, 0xFF), "WoodHouse5"),
        new ColorSpawn(new Color32(0x6D, 0x9E, 0xEB, 0xFF), "WoodHouse6"),
        new ColorSpawn(new Color32(0x6D, 0x9E, 0xEB, 0xFF), "WoodHouse7"),
        new ColorSpawn(new Color32(0x6D, 0x9E, 0xEB, 0xFF), "WoodHouse8"),
        new ColorSpawn(new Color32(0x6D, 0x9E, 0xEB, 0xFF), "WoodHouse9"),
        new ColorSpawn(new Color32(0x76, 0xA5, 0xAF, 0xFF), "StoneHouse3"),
        new ColorSpawn(new Color32(0x76, 0xA5, 0xAF, 0xFF), "StoneHouse4"),
        new ColorSpawn(new Color32(0x93, 0xC4, 0x7D, 0xFF), "Meteorite"),
        new ColorSpawn(new Color32(0xA6, 0x1C, 0x00, 0xFF), "StoneTowerRuins04"),
        new ColorSpawn(new Color32(0xA6, 0x1C, 0x00, 0xFF), "StoneTowerRuins05"),
        new ColorSpawn(new Color32(0x13, 0x4F, 0x5C, 0xFF), "SwampRuin1"),
        new ColorSpawn(new Color32(0x13, 0x4F, 0x5C, 0xFF), "SwampRuin2"),
        new ColorSpawn(new Color32(0x27, 0x4E, 0x13, 0xFF), "Ruin1"),
        new ColorSpawn(new Color32(0x27, 0x4E, 0x13, 0xFF), "Ruin2"),
        new ColorSpawn(new Color32(0x85, 0x20, 0x0C, 0xFF), "DrakeLorestone"),
        new ColorSpawn(new Color32(0x5B, 0x0F, 0x00, 0xFF), "Runestone_Boars"),
        new ColorSpawn(new Color32(0x4F, 0xCC, 0xCC, 0xFF), "Runestone_Draugr"),
        new ColorSpawn(new Color32(0xEA, 0x99, 0x99, 0xFF), "Runestone_Greydwarfs"),
        new ColorSpawn(new Color32(0xE0, 0x66, 0x66, 0xFF), "Runestone_Meadows"),
        new ColorSpawn(new Color32(0xCC, 0x00, 0x00, 0xFF), "Runestone_Mountains"),
        new ColorSpawn(new Color32(0x99, 0x00, 0x00, 0xFF), "Runestone_Plains"),
        new ColorSpawn(new Color32(0x66, 0x00, 0x00, 0xFF), "Runestone_Swamps"),
        new ColorSpawn(new Color32(0xD0, 0xE0, 0xE3, 0xFF), "ShipSetting01"),
        new ColorSpawn(new Color32(0xFC, 0xE5, 0xCD, 0xFF), "ShipWreck01"),
        new ColorSpawn(new Color32(0xFC, 0xE5, 0xCD, 0xFF), "ShipWreck02"),
        new ColorSpawn(new Color32(0xFC, 0xE5, 0xCD, 0xFF), "ShipWreck03"),
        new ColorSpawn(new Color32(0xFC, 0xE5, 0xCD, 0xFF), "ShipWreck04"),
        new ColorSpawn(new Color32(0xBF, 0x90, 0x00, 0xFF), "GoblinCamp2"),
        new ColorSpawn(new Color32(0xFF, 0xD9, 0x66, 0xFF), "DrakeNest01"),
        new ColorSpawn(new Color32(0xF1, 0xC2, 0x32, 0xFF), "FireHole"),
        new ColorSpawn(new Color32(0xD9, 0xEA, 0xD3, 0xFF), "Greydwarf_camp1"),
        new ColorSpawn(new Color32(0xA2, 0xC4, 0xC9, 0xFF), "StoneCircle"),
        new ColorSpawn(new Color32(0xF9, 0xCB, 0x9C, 0xFF), "StoneHenge1"),
        new ColorSpawn(new Color32(0xF9, 0xCB, 0x9C, 0xFF), "StoneHenge2"),
        new ColorSpawn(new Color32(0xF9, 0xCB, 0x9C, 0xFF), "StoneHenge3"),
        new ColorSpawn(new Color32(0xF9, 0xCB, 0x9C, 0xFF), "StoneHenge4"),
        new ColorSpawn(new Color32(0xF9, 0xCB, 0x9C, 0xFF), "StoneHenge5"),
        new ColorSpawn(new Color32(0xF9, 0xCB, 0x9C, 0xFF), "StoneHenge6"),
        new ColorSpawn(new Color32(0xF6, 0xB2, 0x6B, 0xFF), "StoneTowerRuins03"),
        new ColorSpawn(new Color32(0xF6, 0xB2, 0x6B, 0xFF), "StoneTowerRuins07"),
        new ColorSpawn(new Color32(0xF6, 0xB2, 0x6B, 0xFF), "StoneTowerRuins08"),
        new ColorSpawn(new Color32(0xF6, 0xB2, 0x6B, 0xFF), "StoneTowerRuins09"),
        new ColorSpawn(new Color32(0xF6, 0xB2, 0x6B, 0xFF), "StoneTowerRuins10"),
        new ColorSpawn(new Color32(0xE6, 0x91, 0x38, 0xFF), "SwampHut5"),
        new ColorSpawn(new Color32(0xE6, 0x91, 0x38, 0xFF), "SwampHut1"),
        new ColorSpawn(new Color32(0xE6, 0x91, 0x38, 0xFF), "SwampHut2"),
        new ColorSpawn(new Color32(0xE6, 0x91, 0x38, 0xFF), "SwampHut3"),
        new ColorSpawn(new Color32(0xE6, 0x91, 0x38, 0xFF), "SwampHut4"),
        new ColorSpawn(new Color32(0xA4, 0xC2, 0xF4, 0xFF), "Waymarker01"),
        new ColorSpawn(new Color32(0xA4, 0xC2, 0xF4, 0xFF), "Waymarker02"),
        new ColorSpawn(new Color32(0x38, 0x76, 0x1D, 0xFF), "MountainWell1"),
        new ColorSpawn(new Color32(0x0C, 0x34, 0x3D, 0xFF), "SwampWell1"),
        new ColorSpawn(new Color32(0xB4, 0x5F, 0x06, 0xFF), "WoodFarm1"),
        new ColorSpawn(new Color32(0x78, 0x3F, 0x04, 0xFF), "WoodVillage1"),
        new ColorSpawn(new Color32(0x99, 0x00, 0xFF, 0xFF), "Mistlands_DvergrBossEntrance1"),
        new ColorSpawn(new Color32(0xFF, 0x69, 0xB4, 0xFF), "Hildir_camp"),
    ];
    protected override bool LoadTextureToMap<T>(Image<T> image)
    {
        var sw = new Stopwatch();
        sw.Start();
        var img = (Image<Rgba32>)(Image)image;
        var pixels = LoadPixels(img, Convert);
        int Index(int x, int y) => y * Size + x;

        bool Compare(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b;

        void FloodFill(int x, int y, Action<int, int> fillfn)
        {
            var sourceColor = pixels[Index(x, y)];
            bool CheckValidity(int xc, int yc) => xc >= 0 && xc < Size && yc >= 0 && yc < Size && Compare(pixels[Index(xc, yc)], sourceColor);

            var q = new Queue<Vector2i>(Size * Size);

            void Enqueue(int xa, int ya)
            {
                pixels[Index(xa, ya)] = Color.black;
                q.Enqueue(new Vector2i(xa, ya));
            }

            void EnqueueIfValid(int xa, int ya)
            {
                if (CheckValidity(xa, ya))
                    Enqueue(xa, ya);
            }

            Enqueue(x, y);

            while (q.Count > 0)
            {
                var point = q.Dequeue();
                var x1 = point.x;
                var y1 = point.y;
                if (q.Count > Size * Size)
                {
                    throw new Exception($"Flood fill on spawn location failed: started at pixel {x}, {Size - y}, color #{ColorUtility.ToHtmlStringRGB(sourceColor)}");
                }

                fillfn(x1, y1);

                EnqueueIfValid(x1 + 1, y1 + 0);
                EnqueueIfValid(x1 - 1, y1 + 0);
                EnqueueIfValid(x1 + 0, y1 + 1);
                EnqueueIfValid(x1 + 0, y1 - 1);
            }
        }

        // Determine the spawn positions by color first
        var colorSpawns = new Dictionary<Color32, List<Vector2>>(new Color32Comparer());
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; ++x)
            {
                int i = Index(x, y);
                var color = pixels[i];
                if (color != Color.black)
                {
                    var area = new List<Vector2>();

                    // Do this AFTER determining the SpawnColorMapping, as it changes the color in pixels to black
                    FloodFill(x, y, (fx, fy) => area.Add(new Vector2(fx / (float)Size, fy / (float)Size)));

                    if (!colorSpawns.TryGetValue(color, out var areas))
                    {
                        areas = new List<Vector2>();
                        colorSpawns.Add(color, areas);
                    }

                    // Just select the actual position from the area now, there is no point delaying this until later
                    var position = area[UnityEngine.Random.Range(0, area.Count)];
                    areas.Add(position);
                    BetterContinents.Log($"Found #{ColorUtility.ToHtmlStringRGB(color)} area of {area.Count} size at {x}, {Size - y}, selected position {position.x}, {position.y}");
                }
            }
        }

        // Now we need to divvy up the color spawn areas between the associated spawn types 
        RemainingSpawnAreas = new Dictionary<string, List<Vector2>>();
        foreach (var colorPositions in colorSpawns)
        {
            var spawns = SpawnColorMapping.Where(d => Compare(d.color, colorPositions.Key)).ToList();
            if (spawns.Count > 0)
            {
                foreach (var position in colorPositions.Value)
                {
                    var spawn = spawns[UnityEngine.Random.Range(0, spawns.Count)].spawn;
                    if (!RemainingSpawnAreas.TryGetValue(spawn, out var positions))
                    {
                        positions = new List<Vector2>();
                        RemainingSpawnAreas.Add(spawn, positions);
                    }

                    positions.Add(position);
                    BetterContinents.Log($"Selected {spawn} for spawn position {position.x}, {position.y}");
                }
            }
            else
            {
                BetterContinents.Log($"No spawns are mapped to color #{ColorUtility.ToHtmlStringRGB(colorPositions.Key)} (which has {colorPositions.Value.Count} spawn positions defined)");
            }
        }

        BetterContinents.Log($"Time to calculate spawns from {FilePath}: {sw.ElapsedMilliseconds} ms");

        return true;
    }

    public Vector2? FindSpawn(string spawn)
    {
        if (RemainingSpawnAreas.TryGetValue(spawn, out var positions))
        {
            int idx = UnityEngine.Random.Range(0, positions.Count);
            var position = positions[idx];
            positions.RemoveAt(idx);
            if (positions.Count == 0)
            {
                RemainingSpawnAreas.Remove(spawn);
            }
            return position;
        }
        return null;
    }

    public IEnumerable<Vector2> GetAllSpawns(string spawn) => RemainingSpawnAreas.TryGetValue(spawn, out var positions) ? positions : Enumerable.Empty<Vector2>();
}
