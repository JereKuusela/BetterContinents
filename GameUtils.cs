using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BetterContinents
{
    public static class GameUtils
    {
        private static void RegenerateHeightmaps()
        {
            var sw = new Stopwatch();
            sw.Start();
            
            // Regenerate all heightmaps            
            foreach (var heightmap in Resources.FindObjectsOfTypeAll<Heightmap>())
            {
                heightmap.CancelInvoke();
                heightmap.Clear();
                heightmap.Regenerate();
            }
            BetterContinents.Log($"Regenerating heightmaps took {sw.ElapsedMilliseconds} ms");
        }

        private static void RegenerateDistantLod()
        {
            foreach (var lod in Object.FindObjectsOfType<TerrainLod>())
            {
                lod.m_heightmapState = TerrainLod.HeightmapState.NeedsRebuild;
            }
        }

        private static Dictionary<ZDOID, ZDO> GetObjectsByID() => ZDOMan.instance.m_objectsByID;
        
        public static void BeginTerrainChanges()
        {
            // Stop and reset the heightmap generator first
            HeightmapBuilder.instance.Dispose();
            new HeightmapBuilder();
        }

        public static void EndTerrainChanges()
        { 
            Refresh();
        }

        public static void Refresh()
        {
            DespawnAll();
            
            ClutterSystem.instance.ClearAll();
            ResetLocationInstances();
            RegenerateDistantLod();
            
            FastMinimapRegen();
        }

        public static void RegenerateLocations()
        {
            DespawnAll();
            
            ClutterSystem.instance.ClearAll();
            ResetLocationInstances();

            ZoneSystem.instance.GenerateLocations();
            ResetLocPins();
        }

        private static int MinimapOrigTextureSize = 0;
        private static float MinimapOrigPixelSize = 0;

        public static int MinimapDownscalingPower = 2;

        public static void FastMinimapRegen()
        {
            int MinimapDownscaling = (int) Mathf.Pow(2, Mathf.Clamp(MinimapDownscalingPower, 0, 3));
            if (MinimapOrigTextureSize == 0 
                || Minimap.instance.m_textureSize != MinimapOrigTextureSize / MinimapDownscaling)
            {
                if(MinimapOrigTextureSize == 0)
                {
                    MinimapOrigTextureSize = Minimap.instance.m_textureSize;
                    MinimapOrigPixelSize = Minimap.instance.m_pixelSize;
                }
                Minimap.instance.m_textureSize = MinimapOrigTextureSize / MinimapDownscaling;
                Minimap.instance.m_pixelSize = MinimapOrigPixelSize * MinimapDownscaling;
                Minimap.instance.m_mapTexture = new Texture2D(Minimap.instance.m_textureSize, Minimap.instance.m_textureSize, TextureFormat.RGBA32, false);
                Minimap.instance.m_mapTexture.wrapMode = TextureWrapMode.Clamp;
                Minimap.instance.m_forestMaskTexture = new Texture2D(Minimap.instance.m_textureSize, Minimap.instance.m_textureSize, TextureFormat.RGBA32, false);
                Minimap.instance.m_forestMaskTexture.wrapMode = TextureWrapMode.Clamp;
                Minimap.instance.m_heightTexture = new Texture2D(Minimap.instance.m_textureSize, Minimap.instance.m_textureSize, TextureFormat.RFloat, false);
                Minimap.instance.m_heightTexture.wrapMode = TextureWrapMode.Clamp;
                Minimap.instance.m_fogTexture = new Texture2D(Minimap.instance.m_textureSize, Minimap.instance.m_textureSize, TextureFormat.RGBA32, false);
                Minimap.instance.m_fogTexture.wrapMode = TextureWrapMode.Clamp;
                Minimap.instance.m_explored = new bool[Minimap.instance.m_textureSize * Minimap.instance.m_textureSize];
                Minimap.instance.m_mapImageLarge.material = Object.Instantiate<Material>(Minimap.instance.m_mapImageLarge.material);
                Minimap.instance.m_mapImageSmall.material = Object.Instantiate<Material>(Minimap.instance.m_mapImageSmall.material);
                Minimap.instance.m_mapImageLarge.material.SetTexture("_MainTex", Minimap.instance.m_mapTexture);
                Minimap.instance.m_mapImageLarge.material.SetTexture("_MaskTex", Minimap.instance.m_forestMaskTexture);
                Minimap.instance.m_mapImageLarge.material.SetTexture("_HeightTex", Minimap.instance.m_heightTexture);
                Minimap.instance.m_mapImageLarge.material.SetTexture("_FogTex", Minimap.instance.m_fogTexture);
                Minimap.instance.m_mapImageSmall.material.SetTexture("_MainTex", Minimap.instance.m_mapTexture);
                Minimap.instance.m_mapImageSmall.material.SetTexture("_MaskTex", Minimap.instance.m_forestMaskTexture);
                Minimap.instance.m_mapImageSmall.material.SetTexture("_HeightTex", Minimap.instance.m_heightTexture);
                Minimap.instance.m_mapImageSmall.material.SetTexture("_FogTex", Minimap.instance.m_fogTexture);
            }
            Minimap.instance.ForceRegen();
            Minimap.instance.ExploreAll();
        }
        
        public static void SaveMinimap(string path, int size)
        {
            BetterContinents.instance.StartCoroutine(SaveMinimapImpl(path, size));
        }

        private static GameObject CreateQuad(float width, float height, float z, Material material)
        {
            var gameObject = new GameObject();
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;//new Material(Shader.Find("Standard"));

            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();

            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(-width / 2, -height / 2, z),
                new Vector3(width / 2, -height / 2, z),
                new Vector3(-width / 2, height / 2, z),
                new Vector3(width / 2, height / 2, z)
            };
            mesh.vertices = vertices;

            int[] tris = new int[6]
            {
                // lower left triangle
                0, 2, 1,
                // upper right triangle
                2, 3, 1
            };
            mesh.triangles = tris;

            Vector3[] normals = new Vector3[4]
            {
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward
            };
            mesh.normals = normals;

            Vector2[] uv = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };
            mesh.uv = uv;

            meshFilter.mesh = mesh;

            return gameObject;
        }

        private static Texture? CloudTexture;
        private static Texture? TransparentTexture;

        private static bool MinimapCloudsEnabled =>
            Minimap.instance.m_mapImageLarge.material.GetTexture("_CloudTex") != TransparentTexture;

        private static void EnableMinimapClouds()
        {
            if (!MinimapCloudsEnabled)
            {
                Minimap.instance.m_mapImageLarge.material.SetTexture("_CloudTex", CloudTexture);
            }
        }
        
        private static void DisableMinimapClouds()
        {
            if(MinimapCloudsEnabled)
            {
                var mat = Minimap.instance.m_mapImageLarge.material;

                CloudTexture = mat.GetTexture("_CloudTex");
                if (TransparentTexture == null)
                {
                    TransparentTexture = UI.CreateFillTexture(new Color32(0, 0, 0, 0));
                }

                mat.SetTexture("_CloudTex", TransparentTexture);
            }
        }
        
        private static IEnumerator SaveMinimapImpl(string path, int size)
        {
            bool wasLarge = Minimap.instance.m_largeRoot.activeSelf;
            if (!wasLarge)
            {
                Minimap.instance.SetMapMode(Minimap.MapMode.Large);
                Minimap.instance.CenterMap(Vector3.zero);
            }

            bool wasClouds = MinimapCloudsEnabled;
            DisableMinimapClouds();
            
            var mapPanelObject = CreateQuad(100, 100, 10, Minimap.instance.m_mapImageLarge.material);

            mapPanelObject.layer = 19;

            var renderTexture = new RenderTexture(size, size, 24);
            var cameraObject = new GameObject();
            cameraObject.layer = 19;
            var camera = cameraObject.AddComponent<Camera>();
            camera.targetTexture = renderTexture;
            camera.orthographic = true;
            camera.rect = new Rect(0, 0, renderTexture.width, renderTexture.height); 
            camera.nearClipPlane = 0;
            camera.farClipPlane = 100;
            camera.orthographicSize = 50;
            camera.cullingMask = 1 << 19;
            camera.Render();
            
            yield return new WaitForEndOfFrame();

            RenderTexture.active = renderTexture;
            var tex = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            Console.instance.Print($"Screenshot of minimap saved to {path}");
            
            File.WriteAllBytes(path, ImageConversion.EncodeToPNG(tex));

            Object.Destroy(mapPanelObject);
            Object.Destroy(cameraObject);
            Object.Destroy(renderTexture);
            Object.Destroy(tex);
            
            if (!wasLarge)
            {
                Minimap.instance.SetMapMode(Minimap.MapMode.Small);
            }

            if (wasClouds)
            {
                EnableMinimapClouds();
            }
        }

        private static void ResetLocPins()
        {
            Minimap.instance.UpdateLocationPins(1000);
        }

        private static void ResetLocationInstances()
        {
            // For each location recreate its instance with the correct height, marking it as unplaced (as we deleted it above we hope!)
            ZoneSystem.instance.m_locationInstances =
                ZoneSystem.instance.m_locationInstances.ToDictionary(kv => kv.Key,
                    kv => new ZoneSystem.LocationInstance
                    {
                        m_location = kv.Value.m_location,
                        m_placed = false,
                        m_position = new Vector3(
                            kv.Value.m_position.x,
                            WorldGenerator.instance.GetHeight(kv.Value.m_position.x, kv.Value.m_position.z),
                            kv.Value.m_position.z
                        )
                    });
            ResetLocPins();
        }

        // Does NOT support sub directories in the resources...
        public static void UnpackDirectoryFromResources(string resourceDirectory, string targetDirectory)
        {
            var execAssembly = Assembly.GetExecutingAssembly();

            try
            {
                Directory.CreateDirectory(targetDirectory);
                
                BetterContinents.Log($"Extracting all files from {resourceDirectory} to {targetDirectory}");

                if (!resourceDirectory.EndsWith("."))
                    resourceDirectory += ".";

                foreach (string fullResourceName in execAssembly.GetManifestResourceNames()
                             .Where(str => str.StartsWith(resourceDirectory)))
                {
                    string targetFileName =
                        Path.Combine(targetDirectory, fullResourceName.Replace(resourceDirectory, ""));
                    if (!File.Exists(targetFileName))
                    {
                        BetterContinents.Log($"Extracting {fullResourceName} to {targetFileName} ...");
                        using var stream = execAssembly.GetManifestResourceStream(fullResourceName);
                        using var targetStream = File.OpenWrite(targetFileName);
                        stream?.CopyTo(targetStream);
                    }
                    else
                    {
                        BetterContinents.Log($"{targetFileName} already exists, skipping extraction");
                    }
                }
            }
            catch (Exception ex)
            {
                BetterContinents.LogError($"Failed to unpack resource directory {resourceDirectory} to {targetDirectory}: {ex.Message}");
            }
        }

        public static AssetBundle GetAssetBundleFromResources(string partialResourceName)
        {
            var execAssembly = Assembly.GetExecutingAssembly();

            try
            {
                string fullResourceName = execAssembly.GetManifestResourceNames()
                    .Single(str => str.EndsWith(partialResourceName));
                BetterContinents.Log($"Loading asset bundle {fullResourceName}");
                using var stream = execAssembly.GetManifestResourceStream(fullResourceName);
                return AssetBundle.LoadFromStream(stream);
            }
            catch(Exception ex)
            {
                BetterContinents.LogError($"Failed to get asset bundle {partialResourceName}: {ex.Message}");
                return null!;
            }
        }

        public static void DespawnAll()
        {
            ZNetScene.instance.RemoveObjects(new List<ZDO>
            {
                Player.m_localPlayer.m_nview.m_zdo
            }, new List<ZDO>());

            foreach (var kv in ZoneSystem.instance.m_zones)
            {
                Object.Destroy(kv.Value.m_root);
            }

            ZoneSystem.instance.m_zones.Clear();
            ZoneSystem.instance.m_generatedZones.Clear();
        }

        public static void ResetAll()
        {
            DespawnAll();

            var playerZDO = Player.m_localPlayer.m_nview.m_zdo;

            // Clear all the ZDOs except the player
            var zdoToDestroy = GetObjectsByID().Values
                .Where(z => z != playerZDO)
                .ToList();

            foreach (var zdo in zdoToDestroy)
            {
                ZDOMan.instance.HandleDestroyedZDO(zdo.m_uid);
            }
            
            ZDOMan.instance.ResetSectorArray();
            
            ZDOMan.instance.AddToSector(playerZDO, playerZDO.GetSector());
            
            ResetLocationInstances();
        }
        
        public static Dictionary<Vector2i, ZoneSystem.LocationInstance> GetLocationInstances() =>
            (Dictionary<Vector2i, ZoneSystem.LocationInstance>) AccessTools.Field(typeof(ZoneSystem), "m_locationInstances").GetValue(ZoneSystem.instance);

        public static void ShowOnMap(params string[] list)
        {
            var locationInstances = GetLocationInstances();
            foreach (var lg in locationInstances.Values.GroupBy(l => l.m_location.m_prefabName))
            {
                if (list == null || list.Length == 0 || list.Any(f => lg.Key.ToLower().StartsWith(f.ToLower())))
                {
                    BetterContinents.Log($"Marking {lg.Count()} {lg.Key} locations on map");
                    int idx = 0;
                    foreach (var li in lg)
                    {
                        Minimap.instance.AddPin(li.m_position, Minimap.PinType.Icon3,
                            $"{li.m_location.m_prefabName} {idx++}", false, false);
                    }
                }
            }
        }

        public static void HideOnMap(params string[] list)
        {
            var pins = Minimap.instance.m_pins;
            if (list == null || list.Length == 0)
            {
                foreach (var pin in pins.ToList())
                {
                    Minimap.instance.RemovePin(pin);
                }
            }
            else
            {
                var locationInstances = GetLocationInstances();
                foreach (var lg in locationInstances.Values.GroupBy(l => l.m_location.m_prefabName))
                {
                    if (list.Any(f => lg.Key.ToLower().StartsWith(f.ToLower())))
                    {
                        BetterContinents.Log($"Hiding {lg.Count()} {lg.Key} locations from the map");
                        int idx = 0;
                        foreach (var li in lg)
                        {
                            var name = $"{li.m_location.m_prefabName} {idx++}";
                            var pin = pins.FirstOrDefault(p => p.m_name == name && p.m_pos == li.m_position);
                            if (pin != null)
                            {
                                Minimap.instance.RemovePin(pins.FirstOrDefault());
                            }
                        }
                    }
                }
            }
        }

        public static void SimpleParallelFor(int taskCount, int from, int to, Action<int> action)
        {
            var tasks = new Task[taskCount];
            int perTaskCount = (to - from) / taskCount;
            for (int i = 0, f = from; i < taskCount - 1; i++, f += perTaskCount)
            {
                int taskFrom = f;
                int taskTo = f + perTaskCount;
                tasks[i] = Task.Run(() =>
                {
                    for (int j = taskFrom; j < taskTo; j++)
                    {
                        action(j);
                    }
                });
            }
            // Make sure last task definitely captures all the values
            tasks[taskCount - 1] = Task.Run(() =>
            {
                for (int j = from + (taskCount - 1) * perTaskCount; j < to; j++)
                {
                    action(j);
                }
            });
            Task.WaitAll(tasks);
        }
    }
}