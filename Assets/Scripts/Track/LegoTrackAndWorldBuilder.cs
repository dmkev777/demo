using System.Collections.Generic;
using UnityEngine;

namespace Lego2KDrive
{
    /// <summary>
    /// Gently animates floating LEGO clouds in the sky and water foam ripples on the river.
    /// </summary>
    public class LegoWorldSceneryAnimator : MonoBehaviour
    {
        public Transform cloudRing;
        public Transform lighthouseBeacon;

        private void Update()
        {
            if (cloudRing != null)
            {
                cloudRing.Rotate(Vector3.up, 1.4f * Time.deltaTime, Space.World);
            }
            if (lighthouseBeacon != null)
            {
                lighthouseBeacon.Rotate(Vector3.up, 65f * Time.deltaTime, Space.Self);
            }
        }
    }

    /// <summary>
    /// Procedurally constructs the upgraded Bricklandia 3-Biome Circuit Diorama:
    /// - Sector 1 (Street): Brickopolis Asphalt Speedway with dashed lane lines, studded LEGO curbs,
    ///   Grandstands, Streetlights, and LEGO Skyscrapers.
    /// - Sector 2 (Off-Road): Red-Rock Canyon Dirt Track with layered LEGO Mesas, Saguaro Cacti, and Wooden Barriers.
    /// - Sector 3 (Water): Crystal River Channel with glossy water, foam strips, Tropical Palm Islands, and a LEGO Lighthouse!
    /// </summary>
    public static class LegoTrackAndWorldBuilder
    {
        public class BuiltTrackData
        {
            public GameObject trackRoot;
            public List<Vector3> waypoints = new List<Vector3>();
            public List<TerrainType> segmentTerrains = new List<TerrainType>();
            public Vector3[] startGridPositions = new Vector3[4];
            public Quaternion startGridRotation;
            public Vector3 characterSpawnPos;
            public Quaternion characterSpawnRot;
            public Vector3 raceEntryBeaconPos;
        }

        public static BuiltTrackData BuildBricklandiaCircuit()
        {
            BuiltTrackData data = new BuiltTrackData();
            GameObject root = new GameObject("Bricklandia_LegoTrackWorld");
            data.trackRoot = root;
            LegoWorldSceneryAnimator anim = root.AddComponent<LegoWorldSceneryAnimator>();

            // 1. Multi-Biome Base Terrain Plate (Top surface at y = -0.10f: close to y=0 so cars never get stuck, with 4cm separation to prevent Z-fighting!)
            GameObject grassPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grassPlate.name = "LegoWorldBaseplate";
            grassPlate.transform.SetParent(root.transform, false);
            grassPlate.transform.position = new Vector3(0f, -1.10f, 0f);
            grassPlate.transform.localScale = new Vector3(1400f, 2f, 1400f);
            grassPlate.GetComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetPlasticMaterial(new Color(0.25f, 0.37f, 0.21f), false, 0.06f);
            TerrainSurfaceTag baseTag = grassPlate.AddComponent<TerrainSurfaceTag>();
            baseTag.terrainType = TerrainType.OffRoad;

            // 2. Generate 36 Track Spine Waypoints (All at y = 0f so road colliders are 100% flush with zero slope seams or buried water!)
            int numSegments = 36;
            float trackWidth = 24f;

            for (int i = 0; i < numSegments; i++)
            {
                float t = (i / (float)numSegments) * Mathf.PI * 2f;

                float radiusX = 165f + Mathf.Sin(t * 2f) * 38f;
                float radiusZ = 135f + Mathf.Cos(t * 3f) * 28f;

                float x = Mathf.Sin(t) * radiusX;
                float z = Mathf.Cos(t) * radiusZ;

                TerrainType biome;
                if (i < 13)
                {
                    biome = TerrainType.Street;
                }
                else if (i < 25)
                {
                    biome = TerrainType.OffRoad;
                }
                else
                {
                    biome = TerrainType.Water;
                }

                data.waypoints.Add(new Vector3(x, 0f, z));
                data.segmentTerrains.Add(biome);
            }

            // 3. Build Track Segments, Detailed Markings, Studded Curbs, Landmarks & Props
            for (int i = 0; i < numSegments; i++)
            {
                int next = (i + 1) % numSegments;
                Vector3 p0 = data.waypoints[i];
                Vector3 p1 = data.waypoints[next];
                TerrainType biome = data.segmentTerrains[i];

                BuildTrackSegment(root.transform, i, p0, p1, trackWidth, biome);

                if (i % 6 == 3)
                {
                    Vector3 mid = (p0 + p1) * 0.5f;
                    Vector3 forward = (p1 - p0).normalized;
                    Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

                    for (int lane = -1; lane <= 1; lane++)
                    {
                        GameObject boxObj = new GameObject($"LegoPowerUpBox_{i}_{lane}");
                        boxObj.transform.SetParent(root.transform, false);
                        LegoPowerUpBox box = boxObj.AddComponent<LegoPowerUpBox>();
                        box.Initialize(mid + right * (lane * 5.5f) + Vector3.up * 1.5f);
                    }
                }

                if (i == 5 || i == 17 || i == 29)
                {
                    BuildBoostPad(root.transform, (p0 + p1) * 0.5f, (p1 - p0).normalized);
                }

                if (i != 0)
                {
                    BuildBiomeSceneryAndProps(root.transform, p0, p1, trackWidth, biome, i, anim, data.waypoints);
                }
            }

            // 4. Build Starting / Finish Line LEGO Arch & Grandstand at Waypoint 0
            Vector3 startCenter = data.waypoints[0];
            Vector3 startDir = (data.waypoints[1] - data.waypoints[0]).normalized;
            data.startGridRotation = Quaternion.LookRotation(startDir, Vector3.up);
            BuildStartFinishArch(root.transform, startCenter, data.startGridRotation, trackWidth);
            // Place Grandstand on the inner side (+1) so the outer side (-1) connects cleanly to the Open World City!
            BuildGrandstand(root.transform, startCenter, startDir, trackWidth, 1);

            Vector3 startRight = Vector3.Cross(Vector3.up, startDir).normalized;
            for (int r = 0; r < 4; r++)
            {
                float lateral = (r % 2 == 0 ? -3.8f : 3.8f);
                float backOffset = -4.5f - (r / 2) * 5.2f;
                data.startGridPositions[r] = startCenter
                                             + startDir * backOffset
                                             + startRight * lateral
                                             + Vector3.up * 0.8f;
                BuildRaceSlotMarker(root.transform, data.startGridPositions[r], data.startGridRotation, r);
            }

            // 5. Build Biome Entry Gate Banners
            BuildBiomeGateBanner(root.transform, data.waypoints[13], (data.waypoints[14] - data.waypoints[13]).normalized, trackWidth, LegoPalette.BrightOrange);
            BuildBiomeGateBanner(root.transform, data.waypoints[25], (data.waypoints[26] - data.waypoints[25]).normalized, trackWidth, LegoPalette.CyanGlow);

            // 6. Build Drifting Low-Poly LEGO Brick Sky Clouds + Blocky Minecraft Sun
            anim.cloudRing = BuildLegoSkyClouds(root.transform);
            BuildBlockyMinecraftSun(root.transform);

            // 7. Build the Minecraft-Style Voxel Open World Hub next to the Race Track!
            BuildMinecraftOpenWorldZone(root.transform, startCenter, startDir, startRight, trackWidth, data);

            return data;
        }

        private static void BuildTrackSegment(Transform parent, int index, Vector3 p0, Vector3 p1, float width, TerrainType biome)
        {
            Vector3 mid = (p0 + p1) * 0.5f;
            Vector3 delta = p1 - p0;
            float length = delta.magnitude + 3.4f;
            Quaternion rot = Quaternion.LookRotation(delta.normalized, Vector3.up);

            // Prevent Z-fighting where adjacent segments overlap
            float antiZFightOffset = (index % 2) * 0.018f;

            GameObject segObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segObj.name = $"TrackSegment_{index}_{biome}";
            segObj.transform.SetParent(parent, false);
            segObj.transform.position = mid - Vector3.up * (0.5f - antiZFightOffset);
            segObj.transform.rotation = rot;
            segObj.transform.localScale = new Vector3(width, 1.0f, length);

            BoxCollider segCol = segObj.GetComponent<BoxCollider>();
            if (segCol != null)
            {
                // Offset collider center opposite to antiZFightOffset so the physical road surface is 100% smooth
                segCol.center = new Vector3(0f, -antiZFightOffset, 0f);
            }

            Color roadColor;
            Color curbColorA;
            Color curbColorB;
            float smoothness = 0.45f;

            switch (biome)
            {
                case TerrainType.OffRoad:
                    roadColor = LegoPalette.TanSand;
                    curbColorA = LegoPalette.CanyonTerracotta;
                    curbColorB = LegoPalette.BrightOrange;
                    smoothness = 0.25f;
                    break;
                case TerrainType.Water:
                    roadColor = new Color(0.06f, 0.52f, 0.92f);
                    curbColorA = LegoPalette.CyanGlow;
                    curbColorB = LegoPalette.PureWhite;
                    smoothness = 0.95f; // Mirror-glossy water surface
                    break;
                default:
                    roadColor = new Color(0.19f, 0.21f, 0.24f);
                    curbColorA = LegoPalette.BrightRed;
                    curbColorB = LegoPalette.PureWhite;
                    smoothness = 0.40f;
                    break;
            }

            segObj.GetComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(roadColor, false, smoothness);
            TerrainSurfaceTag surfaceTag = segObj.AddComponent<TerrainSurfaceTag>();
            surfaceTag.terrainType = biome;

            // Surface Lane Details (Raised 3cm above road surface so they never Z-fight)
            GameObject centerStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(centerStripe.GetComponent<Collider>());
            centerStripe.transform.SetParent(segObj.transform, false);
            centerStripe.transform.localPosition = new Vector3(0f, 0.53f, 0f);
            centerStripe.transform.localScale = new Vector3(0.025f, 0.04f, 0.48f);
            Color stripeCol = biome == TerrainType.Street ? LegoPalette.PureWhite :
                              (biome == TerrainType.Water ? LegoPalette.CyanGlow : LegoPalette.CanyonTerracotta);
            centerStripe.GetComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetPlasticMaterial(stripeCol, biome == TerrainType.Water);

            // Side Shoulder Stripes / Water Foam Edges
            Color curbCol = (index % 2 == 0) ? curbColorA : curbColorB;
            for (int side = -1; side <= 1; side += 2)
            {
                // Shoulder Line
                GameObject shoulder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(shoulder.GetComponent<Collider>());
                shoulder.transform.SetParent(segObj.transform, false);
                shoulder.transform.localPosition = new Vector3(side * 0.44f, 0.53f, 0f);
                shoulder.transform.localScale = new Vector3(0.015f, 0.04f, 0.90f);
                Color shCol = biome == TerrainType.Water ? LegoPalette.PureWhite : LegoPalette.BrightYellow;
                shoulder.GetComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(shCol);

                // 3D LEGO Curb Border
                GameObject curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                curb.name = $"Curb_{side}";
                Object.Destroy(curb.GetComponent<Collider>());
                curb.transform.SetParent(segObj.transform, false);
                curb.transform.localPosition = new Vector3(side * 0.515f, 0.56f, 0f);
                curb.transform.localScale = new Vector3(0.06f, 0.38f, 1.0f);
                curb.GetComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(curbCol);

                // Studded LEGO Guardrail Post at each segment joint (Visual only, no invisible walls so cars can freely enter/exit Open World!)
                GameObject post = new GameObject($"LegoGuardPost_{side}");
                post.transform.SetParent(parent, false);
                Vector3 right = Vector3.Cross(Vector3.up, delta.normalized).normalized;
                post.transform.position = p0 + right * (side * (width * 0.52f)) + Vector3.up * 0.2f;
                post.transform.rotation = rot;
                MeshFilter pmf = post.AddComponent<MeshFilter>();
                pmf.sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(2, 4, 2, BrickShape.StandardBrick);
                MeshRenderer pmr = post.AddComponent<MeshRenderer>();
                pmr.sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(curbCol, biome == TerrainType.Water);
            }
        }

        private static void BuildBoostPad(Transform parent, Vector3 pos, Vector3 forward)
        {
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "LegoBoostPad";
            pad.transform.SetParent(parent, false);
            pad.transform.position = pos + Vector3.up * 0.08f;
            pad.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            pad.transform.localScale = new Vector3(7.5f, 0.25f, 5.5f);

            BoxCollider bc = pad.GetComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(1f, 4f, 1f);

            pad.GetComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.BrightYellow, true);
            pad.AddComponent<LegoBoostPad>();
        }

        /// <summary>
        /// Checks whether a candidate 3D position is within minClearance meters of ANY segment of the race track.
        /// Prevents trees, bushes, rocks, mesas, or hills from ever overlapping or blocking the race track!
        /// </summary>
        private static bool IsTooCloseToTrack(List<Vector3> waypoints, Vector3 worldPos, float minClearance)
        {
            if (waypoints == null || waypoints.Count == 0) return false;

            int count = waypoints.Count;
            Vector2 pt = new Vector2(worldPos.x, worldPos.z);
            float minSq = minClearance * minClearance;

            for (int i = 0; i < count; i++)
            {
                Vector3 a3 = waypoints[i];
                Vector3 b3 = waypoints[(i + 1) % count];
                Vector2 a = new Vector2(a3.x, a3.z);
                Vector2 b = new Vector2(b3.x, b3.z);

                Vector2 ab = b - a;
                float lenSq = ab.sqrMagnitude;
                float t = lenSq > 0.001f ? Mathf.Clamp01(Vector2.Dot(pt - a, ab) / lenSq) : 0f;
                Vector2 closest = a + ab * t;
                if ((pt - closest).sqrMagnitude < minSq)
                    return true;
            }
            return false;
        }

        private static void BuildBiomeSceneryAndProps(
            Transform parent,
            Vector3 p0,
            Vector3 p1,
            float width,
            TerrainType biome,
            int index,
            LegoWorldSceneryAnimator anim,
            List<Vector3> allWaypoints)
        {
            Vector3 mid = (p0 + p1) * 0.5f;
            Vector3 forward = (p1 - p0).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);

            // 1. Small Destructible LEGO Props placed neatly near the side shoulder (leaving the center road wide open!)
            if (index % 3 == 1)
            {
                float shoulderSide = (index % 2 == 0) ? -1f : 1f;
                Vector3 propPos = mid + right * (shoulderSide * (width * 0.36f)) + Vector3.up * 0.1f;
                CreateDestructibleBrickStack(parent, propPos, rot, biome);
            }

            // 2. Rich Biome Scenery set safely back from both sides of the track (checked against all track segments!)
            for (int side = -1; side <= 1; side += 2)
            {
                // Leave the Open World Boulevard entrance (near segments 35, 0, 1 on side -1) completely clear
                if ((index == 35 || index == 1) && side == -1)
                    continue;

                Vector3 nearEdge = mid + right * (side * (width * 0.5f + 8.5f));
                Vector3 farEdge = mid + right * (side * (width * 0.5f + 26f));

                if (biome == TerrainType.Street)
                {
                    Vector3 lampPos = mid + right * (side * (width * 0.5f + 3.2f));
                    if (!IsTooCloseToTrack(allWaypoints, lampPos, width * 0.5f + 1.8f))
                        BuildLegoStreetlight(parent, lampPos, Quaternion.LookRotation(-right * side, Vector3.up));

                    if (index % 2 == 0 && !IsTooCloseToTrack(allWaypoints, nearEdge, width * 0.5f + 6.0f))
                        BuildDecorativeLegoTree(parent, nearEdge, biome);

                    if (index % 3 == 0 && !IsTooCloseToTrack(allWaypoints, farEdge, width * 0.5f + 16.0f))
                        BuildLegoCityBuilding(parent, farEdge, rot, index + side);
                }
                else if (biome == TerrainType.OffRoad)
                {
                    if (!IsTooCloseToTrack(allWaypoints, nearEdge, width * 0.5f + 6.0f))
                        BuildLegoSaguaroCactus(parent, nearEdge, rot);

                    if (index % 2 == 0 && !IsTooCloseToTrack(allWaypoints, farEdge, width * 0.5f + 18.0f))
                        BuildLegoCanyonMesa(parent, farEdge, rot, index);
                }
                else if (biome == TerrainType.Water)
                {
                    if (index % 2 == 0 && !IsTooCloseToTrack(allWaypoints, nearEdge, width * 0.5f + 6.5f))
                        BuildTropicalPalmIsland(parent, nearEdge);

                    if (index == 30 && side == 1 && !IsTooCloseToTrack(allWaypoints, farEdge, width * 0.5f + 16.0f))
                        anim.lighthouseBeacon = BuildLegoLighthouse(parent, farEdge);
                }
            }
        }

        private static void BuildLegoStreetlight(Transform parent, Vector3 pos, Quaternion rot)
        {
            GameObject lamp = new GameObject("LegoStreetlight");
            lamp.transform.SetParent(parent, false);
            lamp.transform.position = pos;
            lamp.transform.rotation = rot;

            GameObject pole = new GameObject("Pole");
            pole.transform.SetParent(lamp.transform, false);
            pole.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(1, 16, 1, BrickShape.StandardBrick);
            pole.AddComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.DarkGrey);

            GameObject arm = new GameObject("Arm");
            arm.transform.SetParent(lamp.transform, false);
            arm.transform.localPosition = new Vector3(0f, 3.5f, 0.5f);
            arm.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(1, 1, 4, BrickShape.FlatTile);
            arm.AddComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.DarkGrey);

            GameObject bulb = new GameObject("GlowBulb");
            bulb.transform.SetParent(lamp.transform, false);
            bulb.transform.localPosition = new Vector3(0f, 3.3f, 0.9f);
            bulb.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(2, 1, 2, BrickShape.FlatTile);
            bulb.AddComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.BrightYellow, true);
        }

        private static void BuildLegoCityBuilding(Transform parent, Vector3 pos, Quaternion rot, int seed)
        {
            GameObject bldg = new GameObject("LegoCitySkyscraper");
            bldg.transform.SetParent(parent, false);
            bldg.transform.position = new Vector3(pos.x, -3.5f, pos.z);
            bldg.transform.rotation = rot;

            Color[] wallColors = { LegoPalette.BrightBlue, LegoPalette.BrightRed, LegoPalette.PureWhite, LegoPalette.BrightOrange };
            Color mainCol = wallColors[Mathf.Abs(seed) % wallColors.Length];
            int floors = 4 + (Mathf.Abs(seed) % 4);

            for (int f = 0; f < floors; f++)
            {
                GameObject tier = new GameObject($"Floor_{f}");
                tier.transform.SetParent(bldg.transform, false);
                tier.transform.localPosition = new Vector3(0f, f * 3.6f, 0f);
                tier.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(8, 12, 8, BrickShape.StandardBrick);
                tier.AddComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(mainCol);

                // Glowing LEGO Window Strip
                GameObject win = new GameObject($"WindowBand_{f}");
                win.transform.SetParent(bldg.transform, false);
                win.transform.localPosition = new Vector3(0f, f * 3.6f + 2.6f, 0f);
                win.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(9, 4, 9, BrickShape.FlatTile);
                win.AddComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.CyanGlow, true);
            }
        }

        private static void BuildRaceSlotMarker(Transform parent, Vector3 position, Quaternion rotation, int index)
        {
            GameObject marker = new GameObject($"RaceSlot_{index + 1}");
            marker.transform.SetParent(parent, false);
            marker.transform.SetPositionAndRotation(new Vector3(position.x, 0.06f, position.z), rotation);
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(stripe.GetComponent<Collider>());
                stripe.transform.SetParent(marker.transform, false);
                stripe.transform.localPosition = new Vector3(side * 1.5f, 0f, 0f);
                stripe.transform.localScale = new Vector3(0.12f, 0.025f, 4f);
                stripe.GetComponent<Renderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.CyanGlow, true);
            }
            GameObject text = new GameObject("SlotLabel");
            text.transform.SetParent(marker.transform, false);
            text.transform.localPosition = Vector3.up * 2.8f;
            TextMesh label = text.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 48;
            label.characterSize = 0.10f;
            LegoRaceSlotMarker state = marker.AddComponent<LegoRaceSlotMarker>();
            state.SlotIndex = index;
            state.Label = label;
        }

        private static void BuildGrandstand(Transform parent, Vector3 center, Vector3 forward, float trackWidth, int side)
        {
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            GameObject stand = new GameObject($"LegoGrandstand_{side}");
            stand.transform.SetParent(parent, false);
            stand.transform.position = center + right * (side * (trackWidth * 0.5f + 11f)) - forward * 6f;
            stand.transform.rotation = Quaternion.LookRotation(-right * side, Vector3.up);

            Color[] tierCols = { LegoPalette.BrightRed, LegoPalette.BrightYellow, LegoPalette.BrightBlue };
            for (int step = 0; step < 3; step++)
            {
                GameObject bench = new GameObject($"Tier_{step}");
                bench.transform.SetParent(stand.transform, false);
                bench.transform.localPosition = new Vector3(0f, step * 1.1f, -step * 1.4f);
                bench.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(12, 5, 4, BrickShape.StandardBrick);
                bench.AddComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(tierCols[step]);
            }
        }

        private static void BuildLegoSaguaroCactus(Transform parent, Vector3 pos, Quaternion rot)
        {
            GameObject cactus = new GameObject("LegoSaguaroCactus");
            cactus.transform.SetParent(parent, false);
            cactus.transform.position = pos;
            cactus.transform.rotation = rot;

            Material greenMat = LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.BrightGreen);

            GameObject mainStem = new GameObject("MainStem");
            mainStem.transform.SetParent(cactus.transform, false);
            mainStem.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(2, 12, 2, BrickShape.StandardBrick);
            mainStem.AddComponent<MeshRenderer>().sharedMaterial = greenMat;

            for (int side = -1; side <= 1; side += 2)
            {
                GameObject arm = new GameObject($"CactusArm_{side}");
                arm.transform.SetParent(cactus.transform, false);
                arm.transform.localPosition = new Vector3(side * 0.9f, 1.1f + (side + 1) * 0.25f, 0f);
                arm.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(2, 6, 2, BrickShape.StandardBrick);
                arm.AddComponent<MeshRenderer>().sharedMaterial = greenMat;
            }
        }

        private static void BuildLegoCanyonMesa(Transform parent, Vector3 pos, Quaternion rot, int seed)
        {
            GameObject mesa = new GameObject("LegoCanyonMesa");
            mesa.transform.SetParent(parent, false);
            mesa.transform.position = new Vector3(pos.x, -3.5f, pos.z);
            mesa.transform.rotation = rot;

            Color[] mesaColors = { LegoPalette.CanyonTerracotta, LegoPalette.TanSand, LegoPalette.BrightOrange };
            for (int layer = 0; layer < 4; layer++)
            {
                int w = 12 - layer * 2;
                GameObject step = new GameObject($"MesaLayer_{layer}");
                step.transform.SetParent(mesa.transform, false);
                step.transform.localPosition = new Vector3(0f, layer * 2.6f, 0f);
                step.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(w, 12, w, BrickShape.StandardBrick);
                step.AddComponent<MeshRenderer>().sharedMaterial =
                    LegoMeshAndMaterialFactory.GetPlasticMaterial(mesaColors[layer % mesaColors.Length], false, 0.35f);
            }
        }

        private static void BuildTropicalPalmIsland(Transform parent, Vector3 pos)
        {
            GameObject island = new GameObject("LegoPalmIsland");
            island.transform.SetParent(parent, false);
            island.transform.position = new Vector3(pos.x, -1.5f, pos.z);

            GameObject sand = new GameObject("SandBase");
            sand.transform.SetParent(island.transform, false);
            sand.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(8, 3, 8, BrickShape.StandardBrick);
            sand.AddComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.TanSand);

            BuildDecorativeLegoTree(island.transform, new Vector3(0f, 0.6f, 0f), TerrainType.Water);
        }

        private static Transform BuildLegoLighthouse(Transform parent, Vector3 pos)
        {
            GameObject lh = new GameObject("LegoCoastalLighthouse");
            lh.transform.SetParent(parent, false);
            lh.transform.position = new Vector3(pos.x, -2.5f, pos.z);

            for (int ring = 0; ring < 6; ring++)
            {
                GameObject seg = new GameObject($"TowerRing_{ring}");
                seg.transform.SetParent(lh.transform, false);
                seg.transform.localPosition = new Vector3(0f, ring * 2.2f, 0f);
                int sz = ring < 3 ? 6 : 5;
                seg.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(sz, 10, sz, BrickShape.StandardBrick);
                Color c = (ring % 2 == 0) ? LegoPalette.BrightRed : LegoPalette.PureWhite;
                seg.AddComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(c);
            }

            GameObject beacon = new GameObject("RotatingBeaconLight");
            beacon.transform.SetParent(lh.transform, false);
            beacon.transform.localPosition = new Vector3(0f, 13.5f, 0f);
            beacon.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(4, 4, 8, BrickShape.StandardBrick);
            beacon.AddComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.BrightYellow, true);
            return beacon.transform;
        }

        private static Transform BuildLegoSkyClouds(Transform parent)
        {
            GameObject cloudRoot = new GameObject("LegoSkyCloudRing");
            cloudRoot.transform.SetParent(parent, false);
            cloudRoot.transform.position = new Vector3(0f, 52f, 0f);

            int cloudCount = 16;
            Material cloudMat = LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.PureWhite, false, 0.3f);

            for (int i = 0; i < cloudCount; i++)
            {
                float angle = (i / (float)cloudCount) * Mathf.PI * 2f;
                float dist = 175f + (i % 3) * 45f;
                float heightOffset = (i % 4) * 6f;

                GameObject cloud = new GameObject($"LegoCloud_{i}");
                cloud.transform.SetParent(cloudRoot.transform, false);
                cloud.transform.localPosition = new Vector3(Mathf.Cos(angle) * dist, heightOffset, Mathf.Sin(angle) * dist);
                cloud.transform.localRotation = Quaternion.Euler(0f, i * 28f, 0f);
                cloud.transform.localScale = new Vector3(3.2f, 1.8f, 3.2f);

                GameObject cBase = new GameObject("CloudBase");
                cBase.transform.SetParent(cloud.transform, false);
                cBase.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(8, 3, 5, BrickShape.StandardBrick);
                cBase.AddComponent<MeshRenderer>().sharedMaterial = cloudMat;

                GameObject cTop = new GameObject("CloudPuff");
                cTop.transform.SetParent(cloud.transform, false);
                cTop.transform.localPosition = new Vector3(0f, 0.65f, 0f);
                cTop.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(5, 3, 4, BrickShape.StandardBrick);
                cTop.AddComponent<MeshRenderer>().sharedMaterial = cloudMat;
            }

            return cloudRoot.transform;
        }

        private static void CreateDestructibleBrickStack(Transform parent, Vector3 pos, Quaternion rot, TerrainType biome)
        {
            GameObject propRoot = new GameObject($"DestructibleLegoProp_{biome}");
            propRoot.transform.SetParent(parent, false);
            propRoot.transform.position = pos;
            propRoot.transform.rotation = rot;

            BoxCollider trigger = propRoot.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2.4f, 2.4f, 2.0f);
            trigger.center = new Vector3(0f, 1.0f, 0f);

            GameObject vis = new GameObject("VisualBricks");
            vis.transform.SetParent(propRoot.transform, false);

            Color c1 = biome == TerrainType.Water ? LegoPalette.BrightRed :
                       (biome == TerrainType.OffRoad ? LegoPalette.BrightOrange : LegoPalette.BrightYellow);
            Color c2 = LegoPalette.PureWhite;

            for (int layer = 0; layer < 3; layer++)
            {
                GameObject b = new GameObject($"Layer_{layer}");
                b.transform.SetParent(vis.transform, false);
                b.transform.localPosition = new Vector3(0f, layer * 0.45f, 0f);
                MeshFilter mf = b.AddComponent<MeshFilter>();
                mf.sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(4, 2, 2, BrickShape.StandardBrick);
                MeshRenderer mr = b.AddComponent<MeshRenderer>();
                mr.sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(layer % 2 == 0 ? c1 : c2);
            }

            DestructibleLegoProp destructible = propRoot.AddComponent<DestructibleLegoProp>();
            destructible.SetupVisuals(vis, c1, c2);
        }

        private static void BuildDecorativeLegoTree(Transform parent, Vector3 pos, TerrainType biome)
        {
            GameObject tree = new GameObject("LegoSceneryTree");
            tree.transform.SetParent(parent, false);
            tree.transform.position = pos;

            GameObject trunk = new GameObject("Trunk");
            trunk.transform.SetParent(tree.transform, false);
            trunk.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(2, 7, 2, BrickShape.StandardBrick);
            trunk.AddComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(new Color(0.46f, 0.28f, 0.12f));

            GameObject canopy1 = new GameObject("CanopyLower");
            canopy1.transform.SetParent(tree.transform, false);
            canopy1.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            canopy1.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(6, 4, 6, BrickShape.StandardBrick);
            Color leafCol = biome == TerrainType.Water ? LegoPalette.LimeRival : LegoPalette.BrightGreen;
            canopy1.AddComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(leafCol);

            GameObject canopy2 = new GameObject("CanopyTop");
            canopy2.transform.SetParent(tree.transform, false);
            canopy2.transform.localPosition = new Vector3(0f, 2.35f, 0f);
            canopy2.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(4, 3, 4, BrickShape.StandardBrick);
            canopy2.AddComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(leafCol);
        }

        private static void BuildStartFinishArch(Transform parent, Vector3 center, Quaternion rot, float width)
        {
            GameObject arch = new GameObject("StartFinishLegoArch");
            arch.transform.SetParent(parent, false);
            arch.transform.position = center;
            arch.transform.rotation = rot;

            float halfW = width * 0.48f;
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(pillar.GetComponent<Collider>());
                pillar.transform.SetParent(arch.transform, false);
                pillar.transform.localPosition = new Vector3(side * halfW, 4.5f, 0f);
                pillar.transform.localScale = new Vector3(1.6f, 9f, 1.6f);
                pillar.GetComponent<MeshRenderer>().sharedMaterial =
                    LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.BrightRed);
            }

            int numCheckers = 12;
            float blockWidth = (width + 2f) / numCheckers;
            for (int row = 0; row < 2; row++)
            {
                for (int col = 0; col < numCheckers; col++)
                {
                    GameObject chk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.Destroy(chk.GetComponent<Collider>());
                    chk.transform.SetParent(arch.transform, false);
                    float x = -halfW + (col + 0.5f) * blockWidth;
                    float y = 8.2f + row * 1.1f;
                    chk.transform.localPosition = new Vector3(x, y, 0f);
                    chk.transform.localScale = new Vector3(blockWidth * 0.98f, 1.05f, 1.4f);
                    Color c = ((row + col) % 2 == 0) ? LegoPalette.PureWhite : LegoPalette.TireBlack;
                    chk.GetComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(c);
                }
            }
        }

        private static void BuildBiomeGateBanner(Transform parent, Vector3 center, Vector3 forward, float width, Color gateColor)
        {
            GameObject gate = new GameObject("BiomeTransformGate");
            gate.transform.SetParent(parent, false);
            gate.transform.position = center;
            gate.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

            float halfW = width * 0.48f;
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(post.GetComponent<Collider>());
                post.transform.SetParent(gate.transform, false);
                post.transform.localPosition = new Vector3(side * halfW, 3.5f, 0f);
                post.transform.localScale = new Vector3(1.2f, 7f, 1.2f);
                post.GetComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(gateColor, true);
            }

            GameObject topBar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(topBar.GetComponent<Collider>());
            topBar.transform.SetParent(gate.transform, false);
            topBar.transform.localPosition = new Vector3(0f, 6.8f, 0f);
            topBar.transform.localScale = new Vector3(width + 1f, 0.9f, 1.0f);
            topBar.GetComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(gateColor, true);
        }

        private static void BuildBlockyMinecraftSun(Transform parent)
        {
            GameObject sun = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(sun.GetComponent<Collider>());
            sun.name = "BlockyMinecraftSun";
            sun.transform.SetParent(parent, false);
            sun.transform.position = new Vector3(220f, 240f, -260f);
            sun.transform.localScale = new Vector3(42f, 42f, 42f);
            sun.GetComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.BrightYellow, true);
        }

        /// <summary>
        /// Builds a Grand Minecraft & Voxel City ("Bricklandia Block City") connected to the Race Track:
        /// - Paved Stone City Plazas, Asphalt City Boulevards, Brick Sidewalks & Crosswalks (no glaring green ground!)
        /// - Central City Fountain Square & Streetlights
        /// - 7 Enterable Hollow Houses, Shops & Town Hall (with open doorways, windows, beds, tables, and ceiling lamps!)
        /// - Crystal Harbor Lake with Wooden Boardwalk Pier
        /// - Stepped Voxel Hills & Forest Park surrounding the City
        /// </summary>
        private static void BuildMinecraftOpenWorldZone(
            Transform parent,
            Vector3 startCenter,
            Vector3 startDir,
            Vector3 startRight,
            float trackWidth,
            BuiltTrackData data)
        {
            GameObject owRoot = new GameObject("Minecraft_OpenWorld_City");
            owRoot.transform.SetParent(parent, false);

            // IMPORTANT: startRight points INWARD into the race track oval; -startRight (outDir) points OUTWARD away from the entire track!
            Vector3 outDir = -startRight;

            Vector3 cityCenter = startCenter - startDir * 6f + outDir * (trackWidth * 0.5f + 64f);
            // City stone surface is at y = -0.06f (4cm ABOVE grassPlate top at -0.10f so ZERO Z-fighting and ZERO tall step!)
            cityCenter.y = -0.06f;

            // Character spawns in the Grand City Plaza looking down the Main Boulevard toward the Houses & Race Track!
            data.characterSpawnPos = cityCenter + outDir * 8f + new Vector3(0f, 1.3f, 0f);
            data.characterSpawnRot = Quaternion.LookRotation(-outDir, Vector3.up);

            // Race Entry Beacon sits right next to the Player's parked car on the outer edge of the Starting Grid
            data.raceEntryBeaconPos = startCenter - startDir * 5.5f + outDir * (trackWidth * 0.5f + 2.0f);

            // 1. Solid Stone City Foundation (top surface at y = -0.06f, bottom at y = -0.86f)
            GameObject cityPavement = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cityPavement.name = "CityStonePavement";
            cityPavement.transform.SetParent(owRoot.transform, false);
            cityPavement.transform.position = new Vector3(cityCenter.x, -0.46f, cityCenter.z);
            cityPavement.transform.rotation = Quaternion.LookRotation(-outDir, Vector3.up);
            cityPavement.transform.localScale = new Vector3(96f, 0.80f, 96f);
            cityPavement.GetComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetPlasticMaterial(new Color(0.54f, 0.53f, 0.50f), false, 0.12f);
            cityPavement.AddComponent<TerrainSurfaceTag>().terrainType = TerrainType.Street;

            // Main Boulevard connecting City Center directly to the Race Track Entrance (Top surface at y = -0.02f)
            Material roadMat = LegoMeshAndMaterialFactory.GetPlasticMaterial(new Color(0.22f, 0.24f, 0.27f), false, 0.16f);
            GameObject mainAve = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mainAve.name = "CityMainBoulevard";
            mainAve.transform.SetParent(owRoot.transform, false);
            Vector3 aveStart = cityCenter + outDir * 44f;
            Vector3 aveEnd = data.raceEntryBeaconPos;
            Vector3 aveMid = (aveStart + aveEnd) * 0.5f;
            mainAve.transform.position = new Vector3(aveMid.x, -0.07f, aveMid.z);
            mainAve.transform.rotation = Quaternion.LookRotation(-outDir, Vector3.up);
            mainAve.transform.localScale = new Vector3(10.5f, 0.10f, Vector3.Distance(aveStart, aveEnd));
            mainAve.GetComponent<MeshRenderer>().sharedMaterial = roadMat;
            mainAve.AddComponent<TerrainSurfaceTag>().terrainType = TerrainType.Street;

            // Cross City Street (Split into two arms so it NEVER overlaps mainAve at the center!)
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject crossArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crossArm.name = $"CityCrossStreet_{side}";
                crossArm.transform.SetParent(owRoot.transform, false);
                Vector3 armPos = cityCenter + startDir * (side * 25.5f);
                crossArm.transform.position = new Vector3(armPos.x, -0.07f, armPos.z);
                crossArm.transform.rotation = Quaternion.LookRotation(startDir, Vector3.up);
                crossArm.transform.localScale = new Vector3(10.5f, 0.10f, 39f);
                crossArm.GetComponent<MeshRenderer>().sharedMaterial = roadMat;
                crossArm.AddComponent<TerrainSurfaceTag>().terrainType = TerrainType.Street;
            }

            // 2. Central City Fountain & Plaza Monument
            BuildCityFountain(owRoot.transform, cityCenter);

            // 3. Glowing Race Entry Beacon Ring & Signpost at the Track Entrance
            GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(beacon.GetComponent<Collider>());
            beacon.name = "RaceStartZoneBeacon";
            beacon.transform.SetParent(owRoot.transform, false);
            beacon.transform.position = data.raceEntryBeaconPos + Vector3.up * 0.15f;
            beacon.transform.localScale = new Vector3(LegoRaceLobby.EntryRadius * 2f, 0.15f, LegoRaceLobby.EntryRadius * 2f);
            beacon.GetComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.CyanGlow, true);

            GameObject beaconPillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(beaconPillar.GetComponent<Collider>());
            beaconPillar.transform.SetParent(owRoot.transform, false);
            beaconPillar.transform.position = data.raceEntryBeaconPos + outDir * 2.2f + Vector3.up * 6f;
            beaconPillar.transform.localScale = new Vector3(0.6f, 12f, 0.6f);
            beaconPillar.GetComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.BrightYellow, true);

            // 4. Build 7 Enterable Hollow Houses, Shops & Town Hall (Players can walk inside every one!)
            Quaternion faceSouth = Quaternion.LookRotation(-startDir, Vector3.up);
            Quaternion faceNorth = Quaternion.LookRotation(startDir, Vector3.up);
            Quaternion faceTowardTrack = Quaternion.LookRotation(-outDir, Vector3.up);
            Quaternion faceAwayFromTrack = Quaternion.LookRotation(outDir, Vector3.up);

            // House 1: Red Brick Family Villa
            BuildEnterableVoxelHouse(owRoot.transform, cityCenter + startDir * 15f - outDir * 18f, faceSouth,
                10, 8, 5, new Color(0.72f, 0.24f, 0.20f), new Color(0.38f, 0.22f, 0.14f), true);

            // House 2: Modern White Quartz Villa
            BuildEnterableVoxelHouse(owRoot.transform, cityCenter + startDir * 15f + outDir * 18f, faceSouth,
                10, 8, 5, new Color(0.90f, 0.90f, 0.88f), new Color(0.20f, 0.24f, 0.30f), true);

            // House 3: Cozy Oak Timber Cabin
            BuildEnterableVoxelHouse(owRoot.transform, cityCenter - startDir * 15f - outDir * 18f, faceNorth,
                9, 8, 5, new Color(0.66f, 0.46f, 0.24f), new Color(0.55f, 0.22f, 0.14f), true);

            // House 4: Golden Sandstone Mansion
            BuildEnterableVoxelHouse(owRoot.transform, cityCenter - startDir * 15f + outDir * 18f, faceNorth,
                11, 9, 5, new Color(0.84f, 0.74f, 0.50f), new Color(0.78f, 0.52f, 0.12f), true);

            // House 5: Grand City Town Hall & Tower (Far end of Main Boulevard, facing the Race Track!)
            BuildEnterableVoxelHouse(owRoot.transform, cityCenter + outDir * 34f, faceTowardTrack,
                13, 10, 6, new Color(0.76f, 0.32f, 0.24f), new Color(0.24f, 0.28f, 0.34f), true, true);

            // House 6: Blue Harbor Cafe
            BuildEnterableVoxelHouse(owRoot.transform, cityCenter + startDir * 32f - outDir * 14f, faceAwayFromTrack,
                9, 8, 5, new Color(0.22f, 0.48f, 0.78f), new Color(0.88f, 0.88f, 0.90f), true);

            // House 7: Race Paddock Garage Lounge (Near the Race Track Entrance, safely outside the track!)
            BuildEnterableVoxelHouse(owRoot.transform, cityCenter - startDir * 14f - outDir * 34f, faceNorth,
                9, 8, 5, new Color(0.32f, 0.35f, 0.40f), LegoPalette.BrightOrange, true);

            // 5. Open DIY Building Plot ("Khu Đất Trống Để Bạn Tự Xây Nhà") next to House 6
            BuildDIYConstructionPlot(owRoot.transform, cityCenter + startDir * 32f + outDir * 16f, faceTowardTrack);

            // 6. Streetlights along the City Boulevards
            for (int i = -3; i <= 3; i++)
            {
                if (i == 0) continue;
                BuildLegoStreetlight(owRoot.transform, cityCenter + outDir * (i * 11f) + startDir * 6.2f, faceSouth);
                BuildLegoStreetlight(owRoot.transform, cityCenter + outDir * (i * 11f) - startDir * 6.2f, faceNorth);
            }

            // 7. Crystal Harbor Lake & Wooden Pier outside the City Gate (far outside the race track!)
            Vector3 lakeCenter = cityCenter + startDir * 64f + outDir * 12f;
            if (!IsTooCloseToTrack(data.waypoints, lakeCenter, trackWidth * 0.5f + 22f))
            {
                BuildVoxelLake(owRoot.transform, lakeCenter);
            }

            // 8. Stepped Voxel Hills & Forest around the outer perimeter of the City (verified clear of the Race Track!)
            Vector3[] hillCenters = new Vector3[]
            {
                cityCenter - startDir * 68f + outDir * 15f,
                cityCenter - startDir * 56f + outDir * 58f,
                cityCenter + outDir * 74f
            };
            float[] hillSizes = { 28f, 34f, 30f };
            int[] hillLayers = { 5, 6, 5 };

            for (int h = 0; h < hillCenters.Length; h++)
            {
                if (!IsTooCloseToTrack(data.waypoints, hillCenters[h], trackWidth * 0.5f + hillSizes[h] * 0.65f + 8f))
                {
                    BuildSteppedVoxelHill(owRoot.transform, hillCenters[h], hillSizes[h], hillLayers[h]);
                }
            }

            Vector2[] treeOffsets = new Vector2[]
            {
                new Vector2(-28f, -32f), new Vector2(-28f, 32f), new Vector2(28f, 34f), new Vector2(44f, -24f),
                new Vector2(44f, 24f), new Vector2(-44f, -18f), new Vector2(-44f, 18f), new Vector2(-52f, 10f),
                new Vector2(52f, -20f), new Vector2(52f, 35f), new Vector2(8f, 52f), new Vector2(-8f, 52f)
            };

            for (int i = 0; i < treeOffsets.Length; i++)
            {
                Vector3 tPos = cityCenter + startDir * treeOffsets[i].x + outDir * treeOffsets[i].y;
                if (!IsTooCloseToTrack(data.waypoints, tPos, trackWidth * 0.5f + 8f))
                {
                    BuildMinecraftTree(owRoot.transform, tPos, i % 2 == 0);
                }
            }
        }

        private static void BuildCityFountain(Transform parent, Vector3 center)
        {
            GameObject ftn = new GameObject("CityCentralFountain");
            ftn.transform.SetParent(parent, false);
            ftn.transform.position = new Vector3(center.x, -0.02f, center.z); // Sits cleanly on top of mainAve (-0.02f)

            GameObject basin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            basin.transform.SetParent(ftn.transform, false);
            basin.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            basin.transform.localScale = new Vector3(7.5f, 0.8f, 7.5f);
            basin.GetComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.LightGrey, false, 0.3f);

            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cube);
            water.transform.SetParent(ftn.transform, false);
            water.transform.localPosition = new Vector3(0f, 0.86f, 0f);
            water.transform.localScale = new Vector3(6.2f, 0.12f, 6.2f);
            water.GetComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.CyanGlow, true, 0.9f);

            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pillar.transform.SetParent(ftn.transform, false);
            pillar.transform.localPosition = new Vector3(0f, 2.5f, 0f);
            pillar.transform.localScale = new Vector3(1.4f, 3.0f, 1.4f);
            pillar.GetComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.PureWhite, false, 0.4f);

            GameObject topGlow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topGlow.transform.SetParent(ftn.transform, false);
            topGlow.transform.localPosition = new Vector3(0f, 4.4f, 0f);
            topGlow.transform.localScale = new Vector3(2.2f, 0.8f, 2.2f);
            topGlow.GetComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.CyanGlow, true);
        }

        private static void BuildDIYConstructionPlot(Transform parent, Vector3 center, Quaternion rot)
        {
            GameObject plot = new GameObject("DIY_PlayerBuildingPlot");
            plot.transform.SetParent(parent, false);
            plot.transform.position = new Vector3(center.x, -0.06f, center.z);
            plot.transform.rotation = rot;

            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.transform.SetParent(plot.transform, false);
            pad.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            pad.transform.localScale = new Vector3(12f, 0.16f, 12f);
            pad.GetComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetPlasticMaterial(new Color(0.76f, 0.68f, 0.52f), false, 0.2f);
            pad.AddComponent<TerrainSurfaceTag>().terrainType = TerrainType.Street;
        }

        /// <summary>
        /// Builds a hollow, enterable Minecraft/Voxel House with:
        /// - Walk-in Front Doorway
        /// - Framed Glass Windows on Left, Right, and Back walls
        /// - Wooden Interior Floor, Bed, Table, Chair, and Glowing Ceiling Lantern
        /// - Stepped Pyramid Roof (plus optional Clock Tower for Town Hall)
        /// - Every wall block has WorldVoxelBlockTag so the player can also break/modify or expand the house!
        /// </summary>
        private static void BuildEnterableVoxelHouse(
            Transform parent,
            Vector3 center,
            Quaternion rot,
            int width,
            int depth,
            int wallHeight,
            Color wallColor,
            Color roofColor,
            bool furnishInterior,
            bool addClockTower = false)
        {
            GameObject house = new GameObject("EnterableVoxelHouse");
            house.transform.SetParent(parent, false);
            house.transform.position = center;
            house.transform.rotation = rot;

            Material wallMat = LegoMeshAndMaterialFactory.GetPlasticMaterial(wallColor, false, 0.20f);
            Material trimMat = LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.PureWhite, false, 0.25f);
            Material floorMat = LegoMeshAndMaterialFactory.GetPlasticMaterial(new Color(0.64f, 0.45f, 0.25f), false, 0.22f);
            Material roofMat = LegoMeshAndMaterialFactory.GetPlasticMaterial(roofColor, false, 0.24f);
            Material glassMat = LegoMeshAndMaterialFactory.GetPlasticMaterial(new Color(0.55f, 0.85f, 0.96f), true, 0.85f);

            float halfW = width * 0.5f;
            float halfD = depth * 0.5f;
            float h = wallHeight;

            // 1. Interior Wooden Plank Floor (Raised 15cm above cityPavement so zero Z-fighting!)
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.SetParent(house.transform, false);
            floor.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            floor.transform.localScale = new Vector3(width - 0.6f, 0.30f, depth - 0.6f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

            // Front Porch Step (Placed outside the front wall at halfD + 1.15f)
            GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
            step.transform.SetParent(house.transform, false);
            step.transform.localPosition = new Vector3(0f, 0.12f, halfD + 1.15f);
            step.transform.localScale = new Vector3(3.4f, 0.24f, 1.5f);
            step.GetComponent<MeshRenderer>().sharedMaterial = trimMat;

            // 2. Front Wall (+Z) with Open Walk-in Doorway (2.4m wide x 3.2m tall)
            float doorWidth = 2.4f;
            float doorHeight = 3.2f;
            float sideWallW = (width - doorWidth) * 0.5f;

            CreateHouseWallPanel(house.transform, new Vector3(-(doorWidth * 0.5f + sideWallW * 0.5f), h * 0.5f, halfD),
                new Vector3(sideWallW, h, 0.6f), wallMat, wallColor);
            CreateHouseWallPanel(house.transform, new Vector3((doorWidth * 0.5f + sideWallW * 0.5f), h * 0.5f, halfD),
                new Vector3(sideWallW, h, 0.6f), wallMat, wallColor);
            CreateHouseWallPanel(house.transform, new Vector3(0f, doorHeight + (h - doorHeight) * 0.5f, halfD),
                new Vector3(doorWidth, h - doorHeight, 0.6f), wallMat, wallColor);

            // White Door Frame Pillars (Placed cleanly in front of wall at halfD + 0.4f so no overlap!)
            CreateHouseWallPanel(house.transform, new Vector3(-doorWidth * 0.5f - 0.15f, doorHeight * 0.5f, halfD + 0.42f),
                new Vector3(0.3f, doorHeight, 0.24f), trimMat, LegoPalette.PureWhite);
            CreateHouseWallPanel(house.transform, new Vector3(doorWidth * 0.5f + 0.15f, doorHeight * 0.5f, halfD + 0.42f),
                new Vector3(0.3f, doorHeight, 0.24f), trimMat, LegoPalette.PureWhite);

            // 3. Back Wall (-Z) with Window
            BuildWallWithWindow(house.transform, new Vector3(0f, 0f, -halfD), Quaternion.identity, width, h, wallMat, glassMat, wallColor);

            // 4. Left Wall (-X) & Right Wall (+X) with Windows (Trimmed by 1.2m so corners never overlap Front/Back walls!)
            BuildWallWithWindow(house.transform, new Vector3(-halfW + 0.3f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), depth - 0.6f, h, wallMat, glassMat, wallColor);
            BuildWallWithWindow(house.transform, new Vector3(halfW - 0.3f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), depth - 0.6f, h, wallMat, glassMat, wallColor);

            // 5. Stepped Voxel Roof
            for (int r = 0; r < 3; r++)
            {
                GameObject roofStep = GameObject.CreatePrimitive(PrimitiveType.Cube);
                roofStep.transform.SetParent(house.transform, false);
                roofStep.transform.localPosition = new Vector3(0f, h + 0.4f + r * 0.75f, 0f);
                roofStep.transform.localScale = new Vector3(width + 1.2f - r * 2.2f, 0.75f, depth + 1.2f - r * 2.2f);
                roofStep.GetComponent<MeshRenderer>().sharedMaterial = roofMat;
            }

            // Optional Clock Tower for City Hall
            if (addClockTower)
            {
                GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tower.transform.SetParent(house.transform, false);
                tower.transform.localPosition = new Vector3(0f, h + 5.5f, 0f);
                tower.transform.localScale = new Vector3(3.8f, 6.5f, 3.8f);
                tower.GetComponent<MeshRenderer>().sharedMaterial = wallMat;

                GameObject clockFace = GameObject.CreatePrimitive(PrimitiveType.Cube);
                clockFace.transform.SetParent(house.transform, false);
                clockFace.transform.localPosition = new Vector3(0f, h + 6.8f, 1.95f);
                clockFace.transform.localScale = new Vector3(2.2f, 2.2f, 0.2f);
                clockFace.GetComponent<MeshRenderer>().sharedMaterial =
                    LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.BrightYellow, true);
            }

            // 6. Interior Furniture (Bed, Table, Chair, Glowing Ceiling Lamp)
            if (furnishInterior)
            {
                // Ceiling Glowstone Lantern
                GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lamp.transform.SetParent(house.transform, false);
                lamp.transform.localPosition = new Vector3(0f, h - 0.3f, 0f);
                lamp.transform.localScale = new Vector3(1.2f, 0.4f, 1.2f);
                lamp.GetComponent<MeshRenderer>().sharedMaterial =
                    LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.BrightYellow, true);

                // Minecraft Bed in Back-Left Corner
                GameObject bedBlanket = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bedBlanket.transform.SetParent(house.transform, false);
                bedBlanket.transform.localPosition = new Vector3(-halfW + 1.8f, 0.5f, -halfD + 2.2f);
                bedBlanket.transform.localScale = new Vector3(1.6f, 0.6f, 2.4f);
                bedBlanket.GetComponent<MeshRenderer>().sharedMaterial =
                    LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.BrightRed, false, 0.2f);

                GameObject pillow = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillow.transform.SetParent(house.transform, false);
                pillow.transform.localPosition = new Vector3(-halfW + 1.8f, 0.85f, -halfD + 1.4f);
                pillow.transform.localScale = new Vector3(1.4f, 0.25f, 0.8f);
                pillow.GetComponent<MeshRenderer>().sharedMaterial = trimMat;

                // Wooden Table in Back-Right Corner
                GameObject tableTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tableTop.transform.SetParent(house.transform, false);
                tableTop.transform.localPosition = new Vector3(halfW - 2.0f, 1.0f, -halfD + 2.0f);
                tableTop.transform.localScale = new Vector3(1.8f, 0.2f, 1.8f);
                tableTop.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

                GameObject tableLeg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tableLeg.transform.SetParent(house.transform, false);
                tableLeg.transform.localPosition = new Vector3(halfW - 2.0f, 0.5f, -halfD + 2.0f);
                tableLeg.transform.localScale = new Vector3(0.4f, 0.8f, 0.4f);
                tableLeg.GetComponent<MeshRenderer>().sharedMaterial = floorMat;
            }
        }

        private static void BuildWallWithWindow(
            Transform parent,
            Vector3 centerOffset,
            Quaternion localRot,
            float length,
            float height,
            Material wallMat,
            Material glassMat,
            Color wallCol)
        {
            GameObject wallRoot = new GameObject("WallWithWindow");
            wallRoot.transform.SetParent(parent, false);
            wallRoot.transform.localPosition = centerOffset;
            wallRoot.transform.localRotation = localRot;

            float winW = 2.4f;
            float winBottom = 1.4f;
            float winTop = 3.2f;
            float sideW = (length - winW) * 0.5f;

            // Left & Right solid wall pillars
            CreateHouseWallPanel(wallRoot.transform, new Vector3(-(winW * 0.5f + sideW * 0.5f), height * 0.5f, 0f),
                new Vector3(sideW, height, 0.6f), wallMat, wallCol);
            CreateHouseWallPanel(wallRoot.transform, new Vector3((winW * 0.5f + sideW * 0.5f), height * 0.5f, 0f),
                new Vector3(sideW, height, 0.6f), wallMat, wallCol);

            // Below & Above window sill
            CreateHouseWallPanel(wallRoot.transform, new Vector3(0f, winBottom * 0.5f, 0f),
                new Vector3(winW, winBottom, 0.6f), wallMat, wallCol);
            CreateHouseWallPanel(wallRoot.transform, new Vector3(0f, winTop + (height - winTop) * 0.5f, 0f),
                new Vector3(winW, height - winTop, 0.6f), wallMat, wallCol);

            // Glass Pane
            GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Cube);
            glass.transform.SetParent(wallRoot.transform, false);
            glass.transform.localPosition = new Vector3(0f, (winBottom + winTop) * 0.5f, 0f);
            glass.transform.localScale = new Vector3(winW, winTop - winBottom, 0.15f);
            glass.GetComponent<MeshRenderer>().sharedMaterial = glassMat;
            WorldVoxelBlockTag tag = glass.AddComponent<WorldVoxelBlockTag>();
            tag.blockColor = new Color(0.55f, 0.85f, 0.96f);
        }

        private static void CreateHouseWallPanel(Transform parent, Vector3 localPos, Vector3 scale, Material mat, Color col)
        {
            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.transform.SetParent(parent, false);
            panel.transform.localPosition = localPos;
            panel.transform.localScale = scale;
            panel.GetComponent<MeshRenderer>().sharedMaterial = mat;
            WorldVoxelBlockTag tag = panel.AddComponent<WorldVoxelBlockTag>();
            tag.blockColor = col;
        }

        private static void BuildSteppedVoxelHill(Transform parent, Vector3 center, float baseSize, int layers)
        {
            GameObject hill = new GameObject("MinecraftVoxelHill");
            hill.transform.SetParent(parent, false);
            hill.transform.position = new Vector3(center.x, -0.10f, center.z);

            Material dirtMat = LegoMeshAndMaterialFactory.GetPlasticMaterial(new Color(0.48f, 0.32f, 0.16f), false, 0.25f);
            Material grassMat = LegoMeshAndMaterialFactory.GetPlasticMaterial(new Color(0.24f, 0.68f, 0.22f), false, 0.30f);

            for (int l = 0; l < layers; l++)
            {
                float sz = baseSize - l * (baseSize / (layers + 1f));
                float y = l * 1.1f;

                GameObject dirtLayer = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dirtLayer.transform.SetParent(hill.transform, false);
                dirtLayer.transform.localPosition = new Vector3(0f, y + 0.35f, 0f);
                dirtLayer.transform.localScale = new Vector3(sz, 0.7f, sz);
                dirtLayer.GetComponent<MeshRenderer>().sharedMaterial = dirtMat;
                dirtLayer.AddComponent<TerrainSurfaceTag>().terrainType = TerrainType.OffRoad;

                GameObject grassCap = GameObject.CreatePrimitive(PrimitiveType.Cube);
                grassCap.transform.SetParent(hill.transform, false);
                grassCap.transform.localPosition = new Vector3(0f, y + 0.9f, 0f);
                grassCap.transform.localScale = new Vector3(sz + 0.15f, 0.4f, sz + 0.15f);
                grassCap.GetComponent<MeshRenderer>().sharedMaterial = grassMat;
                grassCap.AddComponent<TerrainSurfaceTag>().terrainType = TerrainType.OffRoad;
            }

            // Put a tree on top of the voxel hill!
            BuildMinecraftTree(hill.transform, new Vector3(0f, layers * 1.1f, 0f), false);
        }

        private static void BuildVoxelLake(Transform parent, Vector3 center)
        {
            GameObject lake = new GameObject("MinecraftVoxelLake");
            lake.transform.SetParent(parent, false);
            lake.transform.position = new Vector3(center.x, -0.10f, center.z); // Sits on grassPlate top (-0.10f)

            // Sandy beach border (top at +0.06f)
            GameObject sand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sand.transform.SetParent(lake.transform, false);
            sand.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            sand.transform.localScale = new Vector3(28f, 0.06f, 24f);
            sand.GetComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetPlasticMaterial(LegoPalette.TanSand, false, 0.15f);
            sand.AddComponent<TerrainSurfaceTag>().terrainType = TerrainType.OffRoad;

            // Glossy Blue Water Basin (top at +0.10f, 4cm above sand!)
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Cube);
            water.transform.SetParent(lake.transform, false);
            water.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            water.transform.localScale = new Vector3(22f, 0.04f, 18f);
            water.GetComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetPlasticMaterial(new Color(0.08f, 0.56f, 0.95f), false, 0.92f);
            TerrainSurfaceTag wTag = water.AddComponent<TerrainSurfaceTag>();
            wTag.terrainType = TerrainType.Water;
        }

        private static void BuildMinecraftTree(Transform parent, Vector3 pos, bool isBirch)
        {
            GameObject tree = new GameObject(isBirch ? "MinecraftBirchTree" : "MinecraftOakTree");
            tree.transform.SetParent(parent, false);
            tree.transform.position = pos;

            Color trunkCol = isBirch ? new Color(0.90f, 0.88f, 0.82f) : new Color(0.44f, 0.28f, 0.12f);
            Color leafCol = isBirch ? new Color(0.36f, 0.76f, 0.24f) : new Color(0.16f, 0.62f, 0.18f);

            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trunk.transform.SetParent(tree.transform, false);
            trunk.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            trunk.transform.localScale = new Vector3(1.1f, 4.4f, 1.1f);
            trunk.GetComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(trunkCol, false, 0.25f);

            GameObject leavesLower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leavesLower.transform.SetParent(tree.transform, false);
            leavesLower.transform.localPosition = new Vector3(0f, 4.2f, 0f);
            leavesLower.transform.localScale = new Vector3(4.2f, 2.0f, 4.2f);
            leavesLower.GetComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(leafCol, false, 0.30f);

            GameObject leavesUpper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leavesUpper.transform.SetParent(tree.transform, false);
            leavesUpper.transform.localPosition = new Vector3(0f, 5.7f, 0f);
            leavesUpper.transform.localScale = new Vector3(2.6f, 1.4f, 2.6f);
            leavesUpper.GetComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetPlasticMaterial(leafCol, false, 0.30f);
        }

        private static void BuildVoxelCabin(Transform parent, Vector3 pos, Quaternion rot)
        {
            GameObject cabin = new GameObject("MinecraftVoxelCabin");
            cabin.transform.SetParent(parent, false);
            cabin.transform.position = pos;
            cabin.transform.rotation = rot;

            Material woodMat = LegoMeshAndMaterialFactory.GetPlasticMaterial(new Color(0.62f, 0.42f, 0.22f), false, 0.3f);
            Material roofMat = LegoMeshAndMaterialFactory.GetPlasticMaterial(new Color(0.42f, 0.22f, 0.12f), false, 0.3f);

            GameObject walls = GameObject.CreatePrimitive(PrimitiveType.Cube);
            walls.transform.SetParent(cabin.transform, false);
            walls.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            walls.transform.localScale = new Vector3(7.5f, 4.4f, 6.5f);
            walls.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            for (int r = 0; r < 3; r++)
            {
                GameObject roofStep = GameObject.CreatePrimitive(PrimitiveType.Cube);
                roofStep.transform.SetParent(cabin.transform, false);
                roofStep.transform.localPosition = new Vector3(0f, 4.7f + r * 0.8f, 0f);
                roofStep.transform.localScale = new Vector3(8.5f - r * 2.2f, 0.8f, 7.5f - r * 1.5f);
                roofStep.GetComponent<MeshRenderer>().sharedMaterial = roofMat;
            }
        }
    }
}
