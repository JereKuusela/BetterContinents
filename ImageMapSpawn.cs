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
    // No need to create any texture.
    map.Size = (int)Math.Sqrt(map.Map.Length);
    return map;
  }

  private byte[] Map = [];
  private readonly List<Color32> Colors = [];
  private readonly List<SpawnEntry> Entries = [];


  public override bool LoadSourceImage()
  {
    Colors.Clear();
    Entries.Clear();

    // White is hardcoded to disable everything.
    Colors.Add(new Color32(255, 255, 255, 255));
    Entries.Add(new SpawnEntry("none"));

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
        if (line == "") continue;
        var trimmed = line.Trim();
        if (trimmed.StartsWith("#")) continue;
        var parts = trimmed.Split(':');
        if (parts.Length != 2) continue;
        var color = ParseColor32(parts[0]);
        var spawn = parts[1].Trim();
        Colors.Add(color);
        Entries.Add(new SpawnEntry(spawn));
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
    Colors.Clear();
    Entries.Clear();

    int count = pkg.ReadInt();
    for (int i = 0; i < count; i++)
    {
      var r = pkg.ReadByte();
      var g = pkg.ReadByte();
      var b = pkg.ReadByte();
      var a = pkg.ReadByte();
      var color = new Color32(r, g, b, a);
      var spawn = pkg.ReadString();

      BetterContinents.Log($"Loaded spawn color {color} => {spawn}");
      Colors.Add(color);
      Entries.Add(new SpawnEntry(spawn));
    }

    SourceData = pkg.ReadByteArray();
    Map = SourceData;
  }


  public bool CreateMap() => CreateMap<Rgba32>();

  public void Serialize(ZPackage pkg)
  {
    pkg.Write(Colors.Count);
    for (int i = 0; i < Colors.Count; i++)
    {
      var color = Colors[i];
      pkg.Write(color.r);
      pkg.Write(color.g);
      pkg.Write(color.b);
      pkg.Write(color.a);
      var entry = Entries[i];
      pkg.Write(entry.Data);
    }
    pkg.Write(Map);
  }

  public SpawnEntry? GetEntry(float x, float y)
  {
    if (Map == null || Map.Length == 0) return null;
    float xa = x * (Size - 1);
    float ya = y * (Size - 1);

    int xi = Mathf.RoundToInt(xa);
    int yi = Mathf.RoundToInt(ya);
    var index = Map[yi * Size + xi];

    if (index >= Entries.Count) return null;
    return Entries[index];
  }

  public void SanityCheck(ZNetScene scene)
  {
    foreach (var entry in Entries)
      entry.SanityCheck(scene);
  }

  protected override bool LoadTextureToMap<T>(Image<T> image)
  {
    var st = new Stopwatch();
    st.Start();

    var img = (Image<Rgba32>)(Image)image;
    var colorToIndex = new Dictionary<Rgba32, int>();

    // Build color to index mapping
    for (int i = 0; i < Colors.Count; i++)
    {
      var c = Colors[i];
      colorToIndex[new(c.r, c.g, c.b, c.a)] = i;
    }

    BetterContinents.Log($"Colors to index: {string.Join(", ", colorToIndex.Select(kvp => $"{kvp.Key} => {kvp.Value}"))}");
    Map = LoadPixels(img, pixel =>
    {
      // Black color always means nothing is done.
      if (pixel.R == 0 && pixel.G == 0 && pixel.B == 0 && pixel.A == 255)
        return (byte)255;

      if (colorToIndex.TryGetValue(pixel, out var index))
        return (byte)index;
      else
        return (byte)0;
    });

    BetterContinents.Log($"Time to calculate colors from {FilePath}: {st.ElapsedMilliseconds} ms");
    return true;
  }
}

internal class SpawnEntry
{
  private readonly HashSet<string> Enabled = [];
  private readonly HashSet<string> Disabled = [];
  private readonly bool? All;
  public readonly string Data;

  public SpawnEntry(string data)
  {
    Data = data;
    var parts = data.Split(',').Select(s => s.Trim());
    foreach (var part in parts)
    {
      if (part == "all")
        All = true;
      else if (part == "none")
        All = false;
      else if (part.StartsWith("-"))
        Disabled.Add(part.Substring(1));
      else
        Enabled.Add(part);
    }
  }

  public void SanityCheck(ZNetScene scene)
  {
    foreach (var name in Enabled.ToArray())
    {
      Enabled.Remove(name);
      Enabled.Add(SanityCheck(scene, name));
    }
    foreach (var name in Disabled.ToArray())
    {
      Disabled.Remove(name);
      Disabled.Add(SanityCheck(scene, name));
    }
  }
  private string SanityCheck(ZNetScene scene, string name)
  {
    if (scene.GetPrefab(name)) return name;
    foreach (var item in scene.m_namedPrefabs.Values)
    {
      if (item.name.Equals(name, StringComparison.OrdinalIgnoreCase))
        return item.name;
    }
    return name;
  }

  public bool HasEnabled(string name) => Enabled.Contains(name) || (All == true && !Disabled.Contains(name));
  public bool HasDisabled(string name) => Disabled.Contains(name) || (All == false && !Enabled.Contains(name));
}