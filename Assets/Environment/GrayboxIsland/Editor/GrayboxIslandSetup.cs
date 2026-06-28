using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Quarterdeck.Editor
{
    public static class GrayboxIslandSetup
    {
        const string IslandRootName = "Graybox_Island";
        const string RootFolder = "Assets/Environment/GrayboxIsland";
        const string TerrainDataPath = RootFolder + "/GrayboxIslandTerrainData.asset";
        const string UrpLitShader = "Universal Render Pipeline/Lit";

        const float TerrainSizeMeters = 512f;
        const float MaxTerrainHeight = 55f;
        const float IslandDistanceFromShip = 750f;

        static readonly Color GrassColor = new(0.34f, 0.52f, 0.24f);
        static readonly Color DirtColor = new(0.44f, 0.34f, 0.20f);
        static readonly Color RockColor = new(0.48f, 0.46f, 0.42f);
        static readonly Color SandColor = new(0.84f, 0.77f, 0.54f);
        static readonly Color WoodColor = new(0.42f, 0.28f, 0.16f);
        static readonly Color LighthouseColor = new(0.88f, 0.88f, 0.85f);
        static readonly Color PalmTrunkColor = new(0.45f, 0.32f, 0.18f);
        static readonly Color PalmLeafColor = new(0.22f, 0.55f, 0.20f);
        static readonly Color BushColor = new(0.28f, 0.48f, 0.18f);
        static readonly Color TreeTrunkColor = new(0.38f, 0.26f, 0.14f);
        static readonly Color TreeCanopyColor = new(0.20f, 0.42f, 0.16f);
        static readonly Color HarborWaterColor = new(0.10f, 0.38f, 0.42f, 0.75f);

        [MenuItem("Quarterdeck/Create Graybox Island")]
        public static void CreateGrayboxIsland()
        {
            EnsureFolder(RootFolder);
            EnsureFolder(RootFolder + "/TerrainLayers");
            EnsureFolder(RootFolder + "/Materials");
            EnsureFolder(RootFolder + "/Textures");

            var ship = GameObject.Find("Quarterdeck_Graybox");
            Vector3 shipPos = ship != null ? ship.transform.position : new Vector3(24.6f, 1f, 0f);
            Vector3 islandCenter = shipPos + Vector3.forward * IslandDistanceFromShip;
            islandCenter.y = 0f;

            var existing = GameObject.Find(IslandRootName);
            if (existing != null)
                Undo.DestroyObjectImmediate(existing);

            var root = new GameObject(IslandRootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Graybox Island");
            root.transform.position = Vector3.zero;

            var terrainLayers = CreateTerrainLayers();
            var terrain = CreateTerrain(islandCenter, terrainLayers, root.transform);
            var terrainData = terrain.terrainData;

            Vector3 dockWorld = TerrainPointToWorld(terrain, 0.50f, 0.14f);
            Vector3 beachWorld = TerrainPointToWorld(terrain, 0.78f, 0.48f);
            Vector3 lighthouseWorld = TerrainPointToWorld(terrain, 0.48f, 0.64f);
            lighthouseWorld.y = terrain.SampleHeight(lighthouseWorld) + 0.5f;
            Vector3 settlementWorld = TerrainPointToWorld(terrain, 0.50f, 0.24f);

            dockWorld.y = terrain.SampleHeight(dockWorld);

            var structures = new GameObject("Structures");
            Undo.RegisterCreatedObjectUndo(structures, "Create Graybox Island");
            structures.transform.SetParent(root.transform, false);

            CreateHarbor(structures.transform, terrain, dockWorld, settlementWorld);
            CreateLighthouse(structures.transform, lighthouseWorld);
            CreateRockyShoreline(structures.transform, terrain, islandCenter);

            var vegetation = new GameObject("Vegetation");
            Undo.RegisterCreatedObjectUndo(vegetation, "Create Graybox Island");
            vegetation.transform.SetParent(root.transform, false);
            ScatterVegetation(vegetation.transform, terrain, islandCenter);

            var paths = new GameObject("Paths");
            Undo.RegisterCreatedObjectUndo(paths, "Create Graybox Island");
            paths.transform.SetParent(root.transform, false);
            PaintPaths(terrainData, terrain, dockWorld, lighthouseWorld, beachWorld, TerrainPointToWorld(terrain, 0.52f, 0.58f));
            CreatePathMarkers(paths.transform, terrain, dockWorld, lighthouseWorld, beachWorld, TerrainPointToWorld(terrain, 0.52f, 0.58f));

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            float distance = Vector3.Distance(new Vector3(shipPos.x, 0f, shipPos.z), islandCenter);
            Debug.Log($"Graybox island created at {islandCenter} ({distance:F0}m from ship). Harbor faces the ship; lighthouse overlooks the bay.");
        }

        static Terrain CreateTerrain(Vector3 islandCenter, TerrainLayer[] layers, Transform parent)
        {
            if (File.Exists(TerrainDataPath))
                AssetDatabase.DeleteAsset(TerrainDataPath);

            var terrainData = new TerrainData
            {
                heightmapResolution = 513,
                alphamapResolution = 512,
                baseMapResolution = 1024,
                size = new Vector3(TerrainSizeMeters, MaxTerrainHeight, TerrainSizeMeters)
            };

            AssetDatabase.CreateAsset(terrainData, TerrainDataPath);

            var heights = GenerateHeightmap(terrainData.heightmapResolution);
            terrainData.SetHeights(0, 0, heights);

            terrainData.terrainLayers = layers;
            var alphamaps = GenerateAlphamaps(terrainData, heights);
            terrainData.SetAlphamaps(0, 0, alphamaps);

            var terrainPos = new Vector3(
                islandCenter.x - TerrainSizeMeters * 0.5f,
                0f,
                islandCenter.z - TerrainSizeMeters * 0.5f);

            var terrainGo = Terrain.CreateTerrainGameObject(terrainData);
            Undo.RegisterCreatedObjectUndo(terrainGo, "Create Graybox Island");
            terrainGo.name = "Island_Terrain";
            terrainGo.transform.SetParent(parent, false);
            terrainGo.transform.position = terrainPos;

            var terrain = terrainGo.GetComponent<Terrain>();
            terrain.drawInstanced = true;
            terrain.heightmapPixelError = 8f;
            terrain.basemapDistance = 600f;

            return terrain;
        }

        static float[,] GenerateHeightmap(int resolution)
        {
            var heights = new float[resolution, resolution];
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float nx = x / (float)(resolution - 1);
                    float nz = z / (float)(resolution - 1);

                    float dx = (nx - 0.5f) * 2f;
                    float dz = (nz - 0.5f) * 2f;
                    float dist = Mathf.Sqrt(dx * dx * 0.88f + dz * dz);
                    if (dist > 0.95f)
                    {
                        heights[z, x] = 0f;
                        continue;
                    }

                    float landMask = 1f - Smooth(Mathf.InverseLerp(0.62f, 0.94f, dist));

                    float hillX = nx - 0.50f;
                    float hillZ = nz - 0.60f;
                    float centralHill = Mathf.Exp(-(hillX * hillX * 10f + hillZ * hillZ * 8f)) * 0.52f;

                    float noise = Mathf.PerlinNoise(nx * 5.2f + 12f, nz * 5.2f + 4f) * 0.07f;
                    noise += Mathf.PerlinNoise(nx * 11f, nz * 11f) * 0.03f;

                    float harborX = (nx - 0.50f) * 2.4f;
                    float harborZ = (nz - 0.10f) * 3.2f;
                    float harborCarve = Mathf.Exp(-(harborX * harborX + harborZ * harborZ)) * 0.34f;

                    float beachX = nx - 0.80f;
                    float beachZ = nz - 0.48f;
                    float beachFlatten = Mathf.Exp(-(beachX * beachX * 18f + beachZ * beachZ * 10f)) * 0.12f;

                    float cliffBoost = 0f;
                    if (nx < 0.34f && nz > 0.22f && nz < 0.72f)
                    {
                        float cliffMask = (0.34f - nx) * 1.8f + Mathf.PerlinNoise(nx * 8f, nz * 8f) * 0.25f;
                        cliffBoost = cliffMask * 0.22f;
                    }

                    float h = landMask * (0.06f + centralHill + noise + cliffBoost + beachFlatten) - harborCarve;
                    heights[z, x] = Mathf.Clamp01(h);
                }
            }

            return heights;
        }

        static float[,,] GenerateAlphamaps(TerrainData terrainData, float[,] heights)
        {
            int alphaRes = terrainData.alphamapResolution;
            int heightRes = terrainData.heightmapResolution;
            var maps = new float[alphaRes, alphaRes, 4];

            for (int z = 0; z < alphaRes; z++)
            {
                for (int x = 0; x < alphaRes; x++)
                {
                    float nx = x / (float)(alphaRes - 1);
                    float nz = z / (float)(alphaRes - 1);

                    int hx = Mathf.RoundToInt(nx * (heightRes - 1));
                    int hz = Mathf.RoundToInt(nz * (heightRes - 1));
                    float height = heights[hz, hx];
                    float slope = SampleSlope(heights, hx, hz, heightRes);

                    float dx = (nx - 0.5f) * 2f;
                    float dz = (nz - 0.5f) * 2f;
                    float dist = Mathf.Sqrt(dx * dx * 0.88f + dz * dz);
                    if (height < 0.005f || dist > 0.95f)
                    {
                        maps[z, x, 0] = maps[z, x, 1] = maps[z, x, 2] = maps[z, x, 3] = 0f;
                        continue;
                    }

                    float sand = 0f;
                    float rock = 0f;
                    float dirt = 0f;

                    float harborSand = Mathf.Exp(-((nx - 0.50f) * (nx - 0.50f) * 8f + (nz - 0.12f) * (nz - 0.12f) * 14f)) * 0.95f;
                    float beachSand = Mathf.Exp(-((nx - 0.80f) * (nx - 0.80f) * 20f + (nz - 0.48f) * (nz - 0.48f) * 12f)) * 0.9f;
                    sand = Mathf.Max(harborSand, beachSand);

                    if (slope > 0.28f || (dist > 0.72f && height < 0.18f))
                        rock = Smooth(Mathf.InverseLerp(0.22f, 0.42f, slope)) * 0.85f;
                    if (nx < 0.36f && nz > 0.25f && nz < 0.70f)
                        rock = Mathf.Max(rock, 0.65f);

                    float settlementDirt = Mathf.Exp(-((nx - 0.50f) * (nx - 0.50f) * 16f + (nz - 0.24f) * (nz - 0.24f) * 16f)) * 0.75f;
                    dirt = settlementDirt;

                    float grass = Mathf.Clamp01(1f - sand - rock - dirt);
                    NormalizeWeights(ref grass, ref dirt, ref rock, ref sand);

                    maps[z, x, 0] = grass;
                    maps[z, x, 1] = dirt;
                    maps[z, x, 2] = rock;
                    maps[z, x, 3] = sand;
                }
            }

            return maps;
        }

        static void PaintPaths(TerrainData terrainData, Terrain terrain, Vector3 dock, Vector3 lighthouse, Vector3 beach, Vector3 hill)
        {
            var maps = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);
            PaintPathSegment(maps, terrain, dock, lighthouse, 5f);
            PaintPathSegment(maps, terrain, dock, beach, 4f);
            PaintPathSegment(maps, terrain, beach, hill, 4f);
            terrainData.SetAlphamaps(0, 0, maps);
        }

        static void PaintPathSegment(float[,,] maps, Terrain terrain, Vector3 from, Vector3 to, float widthMeters)
        {
            int steps = Mathf.CeilToInt(Vector3.Distance(from, to) / 4f);
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                var point = Vector3.Lerp(from, to, t);
                PaintDirtCircle(maps, terrain, point, widthMeters * 0.5f);
            }
        }

        static void PaintDirtCircle(float[,,] maps, Terrain terrain, Vector3 worldPoint, float radiusMeters)
        {
            var td = terrain.terrainData;
            int alphaRes = td.alphamapResolution;
            var terrainPos = terrain.transform.position;

            float localX = worldPoint.x - terrainPos.x;
            float localZ = worldPoint.z - terrainPos.z;
            float nx = localX / td.size.x;
            float nz = localZ / td.size.z;

            int cx = Mathf.RoundToInt(nx * (alphaRes - 1));
            int cz = Mathf.RoundToInt(nz * (alphaRes - 1));
            int radiusPx = Mathf.Max(1, Mathf.RoundToInt(radiusMeters / td.size.x * alphaRes));

            for (int z = cz - radiusPx; z <= cz + radiusPx; z++)
            {
                for (int x = cx - radiusPx; x <= cx + radiusPx; x++)
                {
                    if (x < 0 || x >= alphaRes || z < 0 || z >= alphaRes)
                        continue;

                    float dx = (x - cx) / (float)radiusPx;
                    float dz = (z - cz) / (float)radiusPx;
                    if (dx * dx + dz * dz > 1f)
                        continue;

                    float falloff = 1f - (dx * dx + dz * dz);
                    float grass = maps[z, x, 0];
                    float dirt = maps[z, x, 1];
                    float rock = maps[z, x, 2];
                    float sand = maps[z, x, 3];

                    dirt = Mathf.Clamp01(dirt + falloff * 0.85f);
                    grass = Mathf.Clamp01(grass * (1f - falloff * 0.9f));
                    rock *= (1f - falloff * 0.5f);
                    sand *= (1f - falloff * 0.7f);
                    NormalizeWeights(ref grass, ref dirt, ref rock, ref sand);

                    maps[z, x, 0] = grass;
                    maps[z, x, 1] = dirt;
                    maps[z, x, 2] = rock;
                    maps[z, x, 3] = sand;
                }
            }
        }

        static void CreateHarbor(Transform parent, Terrain terrain, Vector3 dockWorld, Vector3 settlementWorld)
        {
            var harbor = new GameObject("Harbor");
            Undo.RegisterCreatedObjectUndo(harbor, "Create Graybox Island");
            harbor.transform.SetParent(parent, false);

            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            Undo.RegisterCreatedObjectUndo(water, "Create Graybox Island");
            water.name = "Harbor_Water";
            water.transform.SetParent(harbor.transform, false);
            var waterCenter = TerrainPointToWorld(terrain, 0.50f, 0.11f);
            waterCenter.y = 0.12f;
            water.transform.position = waterCenter;
            water.transform.localScale = new Vector3(5.5f, 1f, 3.5f);
            Object.DestroyImmediate(water.GetComponent<Collider>());
            ApplyMaterial(water, HarborWaterColor, true);

            var pierRoot = new GameObject("Pier");
            Undo.RegisterCreatedObjectUndo(pierRoot, "Create Graybox Island");
            pierRoot.transform.SetParent(harbor.transform, false);

            for (int i = 0; i < 7; i++)
            {
                var pile = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Undo.RegisterCreatedObjectUndo(pile, "Create Graybox Island");
                pile.name = $"Pile_{i + 1}";
                pile.transform.SetParent(pierRoot.transform, false);
                float offset = -9f + i * 3f;
                var pos = dockWorld + new Vector3(offset, 0f, -6f);
                pos.y = terrain.SampleHeight(pos) + 1.2f;
                pile.transform.position = pos;
                pile.transform.localScale = new Vector3(0.35f, 2.4f, 0.35f);
                ApplyMaterial(pile, WoodColor);
            }

            var pierDeck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(pierDeck, "Create Graybox Island");
            pierDeck.name = "Pier_Deck";
            pierDeck.transform.SetParent(pierRoot.transform, false);
            var deckPos = dockWorld + new Vector3(0f, 0f, -6f);
            deckPos.y = terrain.SampleHeight(deckPos) + 2.35f;
            pierDeck.transform.position = deckPos;
            pierDeck.transform.localScale = new Vector3(20f, 0.25f, 2.5f);
            ApplyMaterial(pierDeck, WoodColor);

            var dock = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(dock, "Create Graybox Island");
            dock.name = "Dock_Platform";
            dock.transform.SetParent(harbor.transform, false);
            dock.transform.position = dockWorld + new Vector3(0f, terrain.SampleHeight(dockWorld) + 0.15f, 2f);
            dock.transform.localScale = new Vector3(14f, 0.3f, 10f);
            ApplyMaterial(dock, WoodColor);

            var settlement = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(settlement, "Create Graybox Island");
            settlement.name = "Settlement_Pad";
            settlement.transform.SetParent(harbor.transform, false);
            var padPos = settlementWorld;
            padPos.y = terrain.SampleHeight(padPos) + 0.05f;
            settlement.transform.position = padPos;
            settlement.transform.localScale = new Vector3(36f, 0.1f, 28f);
            ApplyMaterial(settlement, DirtColor);
            Object.DestroyImmediate(settlement.GetComponent<Collider>());
        }

        static void CreateLighthouse(Transform parent, Vector3 position)
        {
            var lighthouse = new GameObject("Lighthouse");
            Undo.RegisterCreatedObjectUndo(lighthouse, "Create Graybox Island");
            lighthouse.transform.SetParent(parent, false);
            lighthouse.transform.position = position;

            var tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(tower, "Create Graybox Island");
            tower.name = "Tower";
            tower.transform.SetParent(lighthouse.transform, false);
            tower.transform.localPosition = new Vector3(0f, 6f, 0f);
            tower.transform.localScale = new Vector3(2.4f, 6f, 2.4f);
            ApplyMaterial(tower, LighthouseColor);

            var lantern = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(lantern, "Create Graybox Island");
            lantern.name = "Lantern_Room";
            lantern.transform.SetParent(lighthouse.transform, false);
            lantern.transform.localPosition = new Vector3(0f, 12.8f, 0f);
            lantern.transform.localScale = new Vector3(2.8f, 1.2f, 2.8f);
            ApplyMaterial(lantern, new Color(0.75f, 0.75f, 0.72f));

            var roof = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(roof, "Create Graybox Island");
            roof.name = "Roof";
            roof.transform.SetParent(lighthouse.transform, false);
            roof.transform.localPosition = new Vector3(0f, 14.5f, 0f);
            roof.transform.localScale = new Vector3(0.01f, 2.2f, 2.2f);
            roof.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            ApplyMaterial(roof, new Color(0.75f, 0.22f, 0.18f));
        }

        static void CreateRockyShoreline(Transform parent, Terrain terrain, Vector3 islandCenter)
        {
            var shoreline = new GameObject("Rocky_Shoreline");
            Undo.RegisterCreatedObjectUndo(shoreline, "Create Graybox Island");
            shoreline.transform.SetParent(parent, false);

            var rng = new System.Random(4217);
            for (int i = 0; i < 18; i++)
            {
                float angle = (float)(rng.NextDouble() * Mathf.PI * 2f);
                float radius = 220f + (float)rng.NextDouble() * 30f;
                var pos = islandCenter + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                pos.y = terrain.SampleHeight(pos);

                var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(rock, "Create Graybox Island");
                rock.name = $"Shore_Rock_{i + 1}";
                rock.transform.SetParent(shoreline.transform, false);
                rock.transform.position = pos + Vector3.up * 0.6f;
                float s = 1.2f + (float)rng.NextDouble() * 2.5f;
                rock.transform.localScale = new Vector3(s * 1.4f, s * 0.8f, s);
                rock.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                ApplyMaterial(rock, RockColor);
            }
        }

        static void ScatterVegetation(Transform parent, Terrain terrain, Vector3 islandCenter)
        {
            var rng = new System.Random(90210);

            var palms = new GameObject("Palms");
            Undo.RegisterCreatedObjectUndo(palms, "Create Graybox Island");
            palms.transform.SetParent(parent, false);
            for (int i = 0; i < 9; i++)
                TryPlacePalm(palms.transform, terrain, islandCenter, rng);

            var bushes = new GameObject("Bushes");
            Undo.RegisterCreatedObjectUndo(bushes, "Create Graybox Island");
            bushes.transform.SetParent(parent, false);
            for (int i = 0; i < 14; i++)
                TryPlaceBush(bushes.transform, terrain, islandCenter, rng);

            var trees = new GameObject("Trees");
            Undo.RegisterCreatedObjectUndo(trees, "Create Graybox Island");
            trees.transform.SetParent(parent, false);
            for (int i = 0; i < 6; i++)
                TryPlaceTree(trees.transform, terrain, islandCenter, rng);
        }

        static void TryPlacePalm(Transform parent, Terrain terrain, Vector3 islandCenter, System.Random rng)
        {
            if (!TryRandomLandPoint(terrain, islandCenter, 40f, 200f, rng, out var pos))
                return;

            var palm = new GameObject("Palm");
            Undo.RegisterCreatedObjectUndo(palm, "Create Graybox Island");
            palm.transform.SetParent(parent, false);
            pos.y = terrain.SampleHeight(pos);
            palm.transform.position = pos;

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(trunk, "Create Graybox Island");
            trunk.name = "Trunk";
            trunk.transform.SetParent(palm.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 3f, 0f);
            trunk.transform.localScale = new Vector3(0.25f, 3f, 0.25f);
            ApplyMaterial(trunk, PalmTrunkColor);

            for (int i = 0; i < 4; i++)
            {
                var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(leaf, "Create Graybox Island");
                leaf.name = $"Leaf_{i + 1}";
                leaf.transform.SetParent(palm.transform, false);
                leaf.transform.localPosition = new Vector3(0f, 6.2f, 0f);
                leaf.transform.localRotation = Quaternion.Euler(25f, i * 72f, 0f);
                leaf.transform.localScale = new Vector3(0.15f, 0.15f, 2.8f);
                ApplyMaterial(leaf, PalmLeafColor);
            }
        }

        static void TryPlaceBush(Transform parent, Terrain terrain, Vector3 islandCenter, System.Random rng)
        {
            if (!TryRandomLandPoint(terrain, islandCenter, 35f, 210f, rng, out var pos))
                return;

            var bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Undo.RegisterCreatedObjectUndo(bush, "Create Graybox Island");
            bush.name = "Bush";
            bush.transform.SetParent(parent, false);
            pos.y = terrain.SampleHeight(pos) + 0.5f;
            bush.transform.position = pos;
            float s = 0.8f + (float)rng.NextDouble() * 0.8f;
            bush.transform.localScale = new Vector3(s * 1.3f, s * 0.7f, s * 1.3f);
            ApplyMaterial(bush, BushColor);
        }

        static void TryPlaceTree(Transform parent, Terrain terrain, Vector3 islandCenter, System.Random rng)
        {
            if (!TryRandomLandPoint(terrain, islandCenter, 50f, 190f, rng, out var pos))
                return;

            var tree = new GameObject("Tree");
            Undo.RegisterCreatedObjectUndo(tree, "Create Graybox Island");
            tree.transform.SetParent(parent, false);
            pos.y = terrain.SampleHeight(pos);
            tree.transform.position = pos;

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Undo.RegisterCreatedObjectUndo(trunk, "Create Graybox Island");
            trunk.name = "Trunk";
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            trunk.transform.localScale = new Vector3(0.35f, 2.5f, 0.35f);
            ApplyMaterial(trunk, TreeTrunkColor);

            var canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Undo.RegisterCreatedObjectUndo(canopy, "Create Graybox Island");
            canopy.name = "Canopy";
            canopy.transform.SetParent(tree.transform, false);
            canopy.transform.localPosition = new Vector3(0f, 5.5f, 0f);
            canopy.transform.localScale = Vector3.one * (2.2f + (float)rng.NextDouble());
            ApplyMaterial(canopy, TreeCanopyColor);
        }

        static void CreatePathMarkers(Transform parent, Terrain terrain, Vector3 dock, Vector3 lighthouse, Vector3 beach, Vector3 hill)
        {
            CreatePathMarker(parent, terrain, "Path_Dock_Lighthouse", dock, lighthouse);
            CreatePathMarker(parent, terrain, "Path_Dock_Beach", dock, beach);
            CreatePathMarker(parent, terrain, "Path_Beach_Hill", beach, hill);
        }

        static void CreatePathMarker(Transform parent, Terrain terrain, string name, Vector3 from, Vector3 to)
        {
            var path = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(path, "Create Graybox Island");
            path.transform.SetParent(parent, false);

            int segments = 8;
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                var pos = Vector3.Lerp(from, to, t);
                pos.y = terrain.SampleHeight(pos) + 0.08f;

                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Undo.RegisterCreatedObjectUndo(marker, "Create Graybox Island");
                marker.transform.SetParent(path.transform, false);
                marker.transform.position = pos;
                marker.transform.localScale = new Vector3(1.6f, 0.06f, 1.6f);
                ApplyMaterial(marker, DirtColor);
                Object.DestroyImmediate(marker.GetComponent<Collider>());
            }
        }

        static bool TryRandomLandPoint(Terrain terrain, Vector3 islandCenter, float minRadius, float maxRadius, System.Random rng, out Vector3 worldPoint)
        {
            for (int attempt = 0; attempt < 24; attempt++)
            {
                float angle = (float)(rng.NextDouble() * Mathf.PI * 2f);
                float radius = minRadius + (float)rng.NextDouble() * (maxRadius - minRadius);
                worldPoint = islandCenter + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                if (terrain.SampleHeight(worldPoint) > 1.5f)
                    return true;
            }

            worldPoint = islandCenter;
            return false;
        }

        static Vector3 TerrainPointToWorld(Terrain terrain, float normalizedX, float normalizedZ)
        {
            var td = terrain.terrainData;
            var pos = terrain.transform.position;
            return new Vector3(
                pos.x + normalizedX * td.size.x,
                0f,
                pos.z + normalizedZ * td.size.z);
        }

        static TerrainLayer[] CreateTerrainLayers()
        {
            return new[]
            {
                CreateTerrainLayer("Grass", GrassColor, RootFolder + "/TerrainLayers/Grass.terrainlayer"),
                CreateTerrainLayer("Dirt", DirtColor, RootFolder + "/TerrainLayers/Dirt.terrainlayer"),
                CreateTerrainLayer("Rock", RockColor, RootFolder + "/TerrainLayers/Rock.terrainlayer"),
                CreateTerrainLayer("Sand", SandColor, RootFolder + "/TerrainLayers/Sand.terrainlayer")
            };
        }

        static TerrainLayer CreateTerrainLayer(string name, Color color, string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TerrainLayer>(assetPath);
            if (existing != null)
                AssetDatabase.DeleteAsset(assetPath);

            var texturePath = RootFolder + $"/Textures/{name}.png";
            CreateSolidTexture(color, texturePath);

            var layer = new TerrainLayer
            {
                diffuseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath),
                tileSize = new Vector2(16f, 16f),
                tileOffset = Vector2.zero
            };

            AssetDatabase.CreateAsset(layer, assetPath);
            return layer;
        }

        static void CreateSolidTexture(Color color, string assetPath)
        {
            if (File.Exists(assetPath))
                AssetDatabase.DeleteAsset(assetPath);

            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
            {
                float n = Mathf.PerlinNoise((i % 64) * 0.11f, (i / 64) * 0.11f) * 0.06f;
                pixels[i] = new Color(
                    Mathf.Clamp01(color.r + n),
                    Mathf.Clamp01(color.g + n * 0.8f),
                    Mathf.Clamp01(color.b + n * 0.5f),
                    1f);
            }

            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(assetPath);
        }

        static void ApplyMaterial(GameObject go, Color color, bool transparent = false)
        {
            var shader = Shader.Find(UrpLitShader);
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", color);
            if (transparent)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.renderQueue = 3000;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        static float SampleSlope(float[,] heights, int x, int z, int resolution)
        {
            int x0 = Mathf.Clamp(x - 1, 0, resolution - 1);
            int x1 = Mathf.Clamp(x + 1, 0, resolution - 1);
            int z0 = Mathf.Clamp(z - 1, 0, resolution - 1);
            int z1 = Mathf.Clamp(z + 1, 0, resolution - 1);
            float dx = heights[z, x1] - heights[z, x0];
            float dz = heights[z1, x] - heights[z0, x];
            return Mathf.Sqrt(dx * dx + dz * dz) * resolution * 0.5f;
        }

        static void NormalizeWeights(ref float grass, ref float dirt, ref float rock, ref float sand)
        {
            float sum = grass + dirt + rock + sand;
            if (sum <= 0.0001f)
            {
                grass = 1f;
                dirt = rock = sand = 0f;
                return;
            }

            grass /= sum;
            dirt /= sum;
            rock /= sum;
            sand /= sum;
        }

        static float Smooth(float t) => t * t * (3f - 2f * t);

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parts = path.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
