using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UnityEngine;

namespace BetterContinents;

internal class ImageMapSpawn() : ImageMapBase
{
    public static ImageMapSpawn? Create(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;
        ImageMapSpawn map = new()
        {
            FilePath = path
        };
        if (!map.LoadSourceImage())
            return null;

        if (!map.CreateMap())
            return null;
        return map;
    }
    public static ImageMapSpawn? Create(ZPackage pkg, string path)
    {
        ImageMapSpawn map = new()
        {
            FilePath = path
        };
        map.Deserialize(pkg);
        map.CreateMap();
        return map;
    }
    private UnityEngine.Color[] Map = [];
    private readonly Dictionary<UnityEngine.Color, SpawnEntry> Colors = [];


    public override bool LoadSourceImage()
    {
        Colors.Clear();
        if (!base.LoadSourceImage()) return false;
        var path = Path.Combine(Path.GetDirectoryName(FilePath), Path.GetFileNameWithoutExtension(FilePath) + ".txt");
        if (!File.Exists(path))
        {
            File.WriteAllText(path, "");
            return true;
        }
        try
        {
            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                var parts = line.Split(':');
                if (parts.Length != 2) continue;
                var color = ParseColor(parts[0]);
                if (color == null) continue;
                var spawn = parts[1];
                Colors[color.Value] = new SpawnEntry(spawn);
            }
        }
        catch (Exception ex)
        {
            BetterContinents.LogError($"Cannot load file {path}: {ex.Message}.");
        }
        return true;
    }
    public void Deserialize(ZPackage pkg)
    {
        int count = pkg.ReadInt();
        Colors.Clear();
        for (int i = 0; i < count; i++)
        {
            var r = pkg.ReadShort();
            var g = pkg.ReadShort();
            var b = pkg.ReadShort();
            var a = pkg.ReadShort();
            var color = new UnityEngine.Color(r / 255f, g / 255f, b / 255f, a / 255f);
            var spawn = pkg.ReadString();
            Colors[color] = new SpawnEntry(spawn);
        }
        SourceData = pkg.ReadByteArray();
    }


    public bool CreateMap() => CreateMap<Rgba32>();

    public bool IsEnabled(Vector2 zone, SpawnSystem.SpawnData spawnData)
    {
        if (Map == null || Map.Length == 0) return spawnData.m_enabled;
        var zonePos = ZoneSystem.instance.GetZone(zone);
        float xa = zonePos.x * (Size - 1);
        float ya = zonePos.y * (Size - 1);

        int xi = Mathf.RoundToInt(xa);
        int yi = Mathf.RoundToInt(ya);
        var color = Map[yi * Size + xi];
        if (!Colors.TryGetValue(color, out var entry)) return spawnData.m_enabled;
        return entry.IsEnabled(spawnData);
    }

    protected override bool LoadTextureToMap<T>(Image<T> image)
    {
        var st = new Stopwatch();
        st.Start();

        var img = (Image<Rgba32>)(Image)image;
        Map = LoadPixels(img, pixel => new UnityEngine.Color(pixel.R / 255f, pixel.G / 255f, pixel.B / 255f, pixel.A / 255f));

        BetterContinents.Log($"Time to calculate colors from {FilePath}: {st.ElapsedMilliseconds} ms");
        return true;
    }
}

internal class SpawnEntry
{
    private readonly HashSet<string> Enabled = [];
    private readonly HashSet<string> Disabled = [];

    public SpawnEntry(string data)
    {
        var parts = data.Split('|');
        foreach (var part in parts)
        {
            if (part.StartsWith("-"))
                Disabled.Add(part.Substring(1));
            else
                Enabled.Add(part);
        }
    }

    public bool IsEnabled(SpawnSystem.SpawnData spawnData)
    {
        if (Enabled.Contains(spawnData.m_prefab.name)) return true;
        if (Disabled.Contains(spawnData.m_prefab.name)) return false;
        return spawnData.m_enabled;
    }
}