using System.Collections.Generic;
using UnityEngine;

namespace Lego2KDrive
{
    /// <summary>
    /// Gently animates floating LEGO clouds in the sky and water foam ripples on the river.
    /// </summary>
    public class LegoFoliageSwayAnimator : MonoBehaviour
    {
        public Vector3 axis = new Vector3(0.4f, 0.25f, 1.0f);
        public float speed = 1.85f;
        public float amplitudeDeg = 2.6f;
        public float phase = 0f;

        private Quaternion _baseLocalRotation;
        private Vector3 _normAxis;

        private void Awake()
        {
            _baseLocalRotation = transform.localRotation;
            _normAxis = axis.sqrMagnitude > 0.001f ? axis.normalized : Vector3.forward;
        }

        private void Update()
        {
            float s = Mathf.Sin(Time.time * speed + phase);
            transform.localRotation = _baseLocalRotation * Quaternion.AngleAxis(s * amplitudeDeg, _normAxis);
        }
    }

    public class LegoWorldSceneryAnimator : MonoBehaviour
    {
        public Transform cloudRing;
        public Transform lighthouseBeacon;
        public List<Transform> windmillRotors = new List<Transform>();

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
            for (int i = 0; i < windmillRotors.Count; i++)
            {
                if (windmillRotors[i] != null)
                {
                    windmillRotors[i].Rotate(Vector3.forward, 36f * Time.deltaTime, Space.Self);
                }
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
            public Vector3 garageEntryPos;
            public Vector3 garageInteriorCarPos;
            public Quaternion garageInteriorCarRot;
            public Vector3 testArenaSpawnPos;
            public Quaternion testArenaSpawnRot;
            public Bounds testArenaBounds;
            public Vector3 hubCityCenter;
            public Vector3 hubLakePos;
            public Vector3 hubMinePos;
            public List<Vector2> extraBuildingClearCenters = new List<Vector2>();
        }

        public static BuiltTrackData BuildBricklandiaCircuit()
        {
            BuiltTrackData data = new BuiltTrackData();
            GameObject root = new GameObject("Bricklandia_LegoTrackWorld");
            data.trackRoot = root;
            LegoWorldSceneryAnimator anim = root.AddComponent<LegoWorldSceneryAnimator>();

            // 1. Lush Multi-Tone Low-Poly Valley Meadow Base Terrain (zero black outlines, crisp textured grass/clover/loam)
            GameObject grassPlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            grassPlate.name = "LegoWorldBaseplate";
            grassPlate.transform.SetParent(root.transform, false);
            grassPlate.transform.position = new Vector3(0f, -1.10f, 0f);
            grassPlate.transform.localScale = new Vector3(1600f, 2f, 1600f);
            grassPlate.GetComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetTexturedLowPolyMaterial(
                    new Color(0.32f, 0.56f, 0.24f), SurfacePattern.MeadowTerrain, 0.10f, 0.0f, 0.28f, 0.85f);
            TerrainSurfaceTag baseTag = grassPlate.AddComponent<TerrainSurfaceTag>();
            baseTag.terrainType = TerrainType.OffRoad;

            // 2. Generate 36 Track Spine Waypoints (waypoints[0..1] preserved for MultiplayerServer race-lobby.js)
            int numSegments = 36;
            float trackWidth = 24f;

            for (int i = 0; i < numSegments; i++)
            {
                float t = (i / (float)numSegments) * Mathf.PI * 2f;

                float radiusX = 165f + Mathf.Sin(t * 2f) * 38f;
                float radiusZ = 135f + Mathf.Cos(t * 3f) * 28f;

                float x = Mathf.Sin(t) * radiusX;
                float z = Mathf.Cos(t) * radiusZ;

                // Continuous Asphalt Street (0..23) + Scenic Mountain Dirt Road (24..35) — no boat track
                TerrainType biome = (i < 24) ? TerrainType.Street : TerrainType.OffRoad;

                data.waypoints.Add(new Vector3(x, 0f, z));
                data.segmentTerrains.Add(biome);
            }

            // 3. Build Track Segments, Curbs, Boost Pads & Low-Poly Roadside Props
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

            // 4. Starting / Finish Arch & 4-Slot Grid at Waypoint 0
            Vector3 startCenter = data.waypoints[0];
            Vector3 startDir = (data.waypoints[1] - data.waypoints[0]).normalized;
            data.startGridRotation = Quaternion.LookRotation(startDir, Vector3.up);
            BuildStartFinishArch(root.transform, startCenter, data.startGridRotation, trackWidth);
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

            // 5. Transition Gate into Off-Road Mountain Dirt Road
            BuildBiomeGateBanner(root.transform, data.waypoints[24], (data.waypoints[25] - data.waypoints[24]).normalized, trackWidth, LegoPalette.BrightOrange);

            // 6. Build Motorverse Low-Poly Village, Red-Brick Garage, Test Drive Arena, Alpine Lake & Mine Tunnel FIRST
            BuildMinecraftOpenWorldZone(root.transform, startCenter, startDir, startRight, trackWidth, data, anim);
            BuildInfieldAndOutskirtsArchitecture(root.transform, data, trackWidth, startCenter, startDir, startRight, anim);
            BuildSculptedMeadowTerrain(root.transform, data, trackWidth, startCenter, startDir, startRight);

            // 7. Low-Poly Sunrise Sky, Glowing Sun Disc, Puffy Clouds & Encircling Alpine Mountains (kept clear of Hub & Arena)
            anim.cloudRing = BuildLegoSkyClouds(root.transform);
            BuildBlockyMinecraftSun(root.transform);
            BuildAlpineMountainRange(root.transform, data, data.waypoints, trackWidth);
            BuildLowPolyRollingHills(root.transform, data.waypoints, trackWidth, startCenter, data);

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
            float smoothness;

            if (biome == TerrainType.OffRoad)
            {
                roadColor = LegoPalette.LowPolyDirtTrack;
                curbColorA = LegoPalette.LowPolyDirtShoulder;
                curbColorB = LegoPalette.WorkshopWood;
                smoothness = 0.14f;
            }
            else
            {
                roadColor = LegoPalette.LowPolyAsphalt;
                curbColorA = LegoPalette.BrightRed;
                curbColorB = LegoPalette.PureWhite;
                smoothness = 0.22f;
            }

            SurfacePattern roadPat = biome == TerrainType.OffRoad ? SurfacePattern.StoneAshlar : SurfacePattern.AsphaltGrain;
            float patScale = biome == TerrainType.OffRoad ? 0.34f : 0.56f;
            segObj.GetComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetTexturedLowPolyMaterial(
                roadColor, roadPat, smoothness, 0.0f, patScale, 0.28f);
            TerrainSurfaceTag surfaceTag = segObj.AddComponent<TerrainSurfaceTag>();
            surfaceTag.terrainType = biome;

            // Surface Lane Details
            GameObject centerStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(centerStripe.GetComponent<Collider>());
            centerStripe.transform.SetParent(segObj.transform, false);
            centerStripe.transform.localPosition = new Vector3(0f, 0.53f, 0f);
            centerStripe.transform.localScale = new Vector3(0.024f, 0.04f, 0.48f);
            Color stripeCol = biome == TerrainType.Street ? LegoPalette.PureWhite : LegoPalette.LowPolyDirtShoulder;
            centerStripe.GetComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetLowPolyMaterial(stripeCol, false, 0.16f);

            Color curbCol = (index % 2 == 0) ? curbColorA : curbColorB;
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject shoulder = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(shoulder.GetComponent<Collider>());
                shoulder.transform.SetParent(segObj.transform, false);
                shoulder.transform.localPosition = new Vector3(side * 0.44f, 0.53f, 0f);
                shoulder.transform.localScale = new Vector3(0.015f, 0.04f, 0.90f);
                Color shCol = biome == TerrainType.Street ? LegoPalette.BrightYellow : LegoPalette.WorkshopWood;
                shoulder.GetComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetLowPolyMaterial(shCol, false, 0.16f);

                GameObject curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                curb.name = $"Curb_{side}";
                Object.Destroy(curb.GetComponent<Collider>());
                curb.transform.SetParent(segObj.transform, false);
                curb.transform.localPosition = new Vector3(side * 0.515f, 0.55f, 0f);
                curb.transform.localScale = new Vector3(0.055f, 0.24f, 1.0f);
                curb.GetComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetLowPolyMaterial(curbCol, false, 0.18f);

                if (index % 2 == 0)
                {
                    GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    post.name = $"RoadsidePost_{side}";
                    Object.Destroy(post.GetComponent<Collider>());
                    post.transform.SetParent(parent, false);
                    Vector3 right = Vector3.Cross(Vector3.up, delta.normalized).normalized;
                    post.transform.position = p0 + right * (side * (width * 0.52f)) + Vector3.up * 0.42f;
                    post.transform.rotation = rot;
                    post.transform.localScale = new Vector3(0.28f, 0.85f, 0.28f);
                    post.GetComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetLowPolyMaterial(curbCol, false, 0.18f);
                }
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

        private static bool IsTooCloseToOpenWorldHub(Vector3 worldPos, BuiltTrackData data, float extraMargin = 0f)
        {
            if (data == null) return false;
            Vector2 p = new Vector2(worldPos.x, worldPos.z);

            bool Near(Vector3 target, float baseRadius)
            {
                if (target == Vector3.zero) return false;
                float r = baseRadius + extraMargin;
                Vector2 t = new Vector2(target.x, target.z);
                return (p - t).sqrMagnitude < r * r;
            }

            if (data.testArenaBounds.size.sqrMagnitude > 0.1f && Near(data.testArenaBounds.center, 95f)) return true;
            if (Near(data.garageInteriorCarPos, 95f)) return true;
            if (Near(data.garageEntryPos, 95f)) return true;
            if (Near(data.characterSpawnPos, 75f)) return true;
            if (Near(data.raceEntryBeaconPos, 75f)) return true;
            if (data.hubCityCenter != Vector3.zero && Near(data.hubCityCenter, 90f)) return true;
            if (data.hubLakePos != Vector3.zero && Near(data.hubLakePos, 60f)) return true;
            if (data.hubMinePos != Vector3.zero && Near(data.hubMinePos, 55f)) return true;
            if (data.extraBuildingClearCenters != null)
            {
                for (int i = 0; i < data.extraBuildingClearCenters.Count; i++)
                {
                    Vector2 c = data.extraBuildingClearCenters[i];
                    float r = 24f + extraMargin;
                    if ((p - c).sqrMagnitude < r * r) return true;
                }
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

            // 2. Low-Poly Valley Scenery safely clear of all track segments and hub entrances
            for (int side = -1; side <= 1; side += 2)
            {
                // Keep Open World / Garage Boulevard / Test Arena entrance completely clear on outer side near segments 34..3
                if ((index == 34 || index == 35 || index == 1 || index == 2 || index == 3) && side == -1)
                    continue;

                Vector3 nearEdge = mid + right * (side * (width * 0.5f + 7.5f));
                Vector3 farEdge = mid + right * (side * (width * 0.5f + 22f));

                float[] vergeOffsets = { 4.2f, 8.8f, 14.5f };
                for (int gv = 0; gv < vergeOffsets.Length; gv++)
                {
                    Vector3 gPos = mid + right * (side * (width * 0.5f + vergeOffsets[gv])) + forward * ((gv - 1) * 2.2f);
                    if (!IsTooCloseToTrack(allWaypoints, gPos, width * 0.5f + 2.8f))
                    {
                        BuildGrassTuft(parent, gPos, index * 31 + side * 7 + gv);
                    }
                }

                if (biome == TerrainType.Street)
                {
                    Vector3 lampPos = mid + right * (side * (width * 0.5f + 3.2f));
                    if (!IsTooCloseToTrack(allWaypoints, lampPos, width * 0.5f + 1.8f))
                        BuildLegoStreetlight(parent, lampPos, Quaternion.LookRotation(-right * side, Vector3.up));

                    if (index % 2 == 0 && !IsTooCloseToTrack(allWaypoints, mid + right * (side * (width * 0.5f + 1.9f)), width * 0.5f + 1.1f))
                    {
                        BuildRoadsideWoodGuardrail(parent, mid + right * (side * (width * 0.5f + 2.1f)), rot);
                    }

                    if (!IsTooCloseToTrack(allWaypoints, nearEdge, width * 0.5f + 5.5f))
                    {
                        if (index % 2 == 0)
                            BuildDecorativeLegoTree(parent, nearEdge, biome);
                        else
                            BuildWildflowerCluster(parent, nearEdge, index + side);
                    }

                    if (index % 4 == 0 && side == 1 && !IsTooCloseToTrack(allWaypoints, nearEdge + right * 2.5f, width * 0.5f + 3.0f))
                    {
                        BuildChevronCurveSign(parent, nearEdge + right * 1.4f, Quaternion.LookRotation(-forward, Vector3.up));
                    }

                    if (index % 3 == 0 && !IsTooCloseToTrack(allWaypoints, farEdge, width * 0.5f + 14.0f))
                        BuildLowPolyRock(parent, farEdge, 2.2f + (index % 3) * 0.8f, index * 37);
                }
                else
                {
                    if (!IsTooCloseToTrack(allWaypoints, nearEdge, width * 0.5f + 5.5f))
                    {
                        BuildDecorativeLegoTree(parent, nearEdge, biome);
                        BuildLowPolyRock(parent, nearEdge + forward * 3.5f, 1.4f, index * 19 + side);
                    }

                    if (index % 3 == 0 && side == -1 && !IsTooCloseToTrack(allWaypoints, nearEdge - right * 1.2f, width * 0.5f + 2.8f))
                    {
                        BuildChevronCurveSign(parent, nearEdge, Quaternion.LookRotation(-forward, Vector3.up));
                    }

                    if (index % 2 == 0 && !IsTooCloseToTrack(allWaypoints, farEdge, width * 0.5f + 16.0f))
                        BuildDecorativeLegoTree(parent, farEdge, biome);

                    if (index == 29 && side == 1 && !IsTooCloseToTrack(allWaypoints, farEdge + right * 20f, width * 0.5f + 20f))
                    {
                        BuildCoastalSeaBay(parent, farEdge + right * 36f);
                        anim.lighthouseBeacon = BuildLegoLighthouse(parent, farEdge + right * 22f);
                    }
                }
            }
        }

        private static void BuildLegoStreetlight(Transform parent, Vector3 pos, Quaternion rot)
        {
            GameObject lamp = new GameObject("LowPolyStreetlight");
            lamp.transform.SetParent(parent, false);
            lamp.transform.position = pos;
            lamp.transform.rotation = rot;

            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(pole.GetComponent<Collider>());
            pole.transform.SetParent(lamp.transform, false);
            pole.transform.localPosition = new Vector3(0f, 2.1f, 0f);
            pole.transform.localScale = new Vector3(0.18f, 2.1f, 0.18f);
            pole.GetComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetLowPolyMaterial(LegoPalette.DarkGrey, false, 0.18f);

            CreateBoxPart(lamp.transform, "Arm", new Vector3(0f, 4.15f, 0.45f), new Vector3(0.16f, 0.14f, 1.05f), LegoPalette.DarkGrey, false, false, 0.18f);
            CreateBoxPart(lamp.transform, "GlowBulb", new Vector3(0f, 3.98f, 0.88f), new Vector3(0.32f, 0.18f, 0.36f), LegoPalette.SunWarmGold, true, false, 0.25f);
        }

        private static void BuildRoadsideWoodGuardrail(Transform parent, Vector3 pos, Quaternion rot)
        {
            GameObject rail = new GameObject("LowPolyWoodGuardrail");
            rail.transform.SetParent(parent, false);
            rail.transform.position = pos;
            rail.transform.rotation = rot;
            CreateBoxPart(rail.transform, "PostF", new Vector3(0f, 0.45f,  2.2f), new Vector3(0.24f, 0.90f, 0.24f), LegoPalette.WorkshopWood, false, false, 0.12f);
            CreateBoxPart(rail.transform, "PostB", new Vector3(0f, 0.45f, -2.2f), new Vector3(0.24f, 0.90f, 0.24f), LegoPalette.WorkshopWood, false, false, 0.12f);
            CreateBoxPart(rail.transform, "RailTop", new Vector3(0f, 0.72f, 0f), new Vector3(0.14f, 0.18f, 5.4f), LegoPalette.WorkshopWood, false, false, 0.12f);
        }

        private static void BuildChevronCurveSign(Transform parent, Vector3 pos, Quaternion rot)
        {
            GameObject sign = new GameObject("LowPolyChevronSign");
            sign.transform.SetParent(parent, false);
            sign.transform.position = pos;
            sign.transform.rotation = rot;
            CreateBoxPart(sign.transform, "Post", new Vector3(0f, 1.0f, 0f), new Vector3(0.16f, 2.0f, 0.16f), LegoPalette.WorkshopWood, false, false, 0.12f);
            CreateBoxPart(sign.transform, "Board", new Vector3(0f, 2.15f, 0f), new Vector3(1.15f, 0.85f, 0.12f), LegoPalette.CarbonBlack, false, false, 0.16f);
            GameObject chev1 = CreateBoxPart(sign.transform, "ChevTop", new Vector3(0f, 2.26f, 0.08f), new Vector3(0.55f, 0.16f, 0.06f), LegoPalette.BrightYellow, true, false, 0.16f);
            chev1.transform.localRotation = Quaternion.Euler(0f, 0f, -24f);
            GameObject chev2 = CreateBoxPart(sign.transform, "ChevBot", new Vector3(0f, 2.04f, 0.08f), new Vector3(0.55f, 0.16f, 0.06f), LegoPalette.BrightYellow, true, false, 0.16f);
            chev2.transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
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
                stripe.GetComponent<Renderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetLowPolyMaterial(LegoPalette.CyanGlow, true, 0.25f);
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
            GameObject stand = new GameObject($"LowPolyGrandstand_{side}");
            stand.transform.SetParent(parent, false);
            stand.transform.position = center + right * (side * (trackWidth * 0.5f + 12.2f)) - forward * 6f;
            stand.transform.rotation = Quaternion.LookRotation(-right * side, Vector3.up);

            // Stone ashlar podium & concrete apron
            CreateTexturedBoxPart(stand.transform, "StonePlinth", new Vector3(0f, 0.30f, -2.0f),
                new Vector3(14.8f, 0.70f, 7.6f), LegoPalette.LowPolyRockGrey, SurfacePattern.StoneAshlar, false, true, 0.14f, 0f, 0.42f, 0.36f);
            CreateTexturedBoxPart(stand.transform, "BackWall", new Vector3(0f, 2.85f, -5.2f),
                new Vector3(14.2f, 4.9f, 0.55f), LegoPalette.WorkshopBrickRed, SurfacePattern.BrickMasonry, false, true, 0.14f, 0f, 0.48f, 0.34f);

            // 4 stepped spectator seating tiers (stone steps + timber bench planking)
            Color[] tierCols = { LegoPalette.WorkshopBrickRed, LegoPalette.WorkshopTeal, LegoPalette.WorkshopWood, LegoPalette.WorkshopBrickRed };
            for (int step = 0; step < 4; step++)
            {
                float yBase = 0.85f + step * 0.88f;
                float zOff = 0.65f - step * 1.35f;
                CreateTexturedBoxPart(stand.transform, $"TierRiser_{step}", new Vector3(0f, yBase, zOff),
                    new Vector3(13.6f, 0.86f, 1.40f), tierCols[step], SurfacePattern.BrickMasonry, false, true, 0.14f, 0f, 0.52f, 0.28f);
                CreateTexturedBoxPart(stand.transform, $"BenchDeck_{step}", new Vector3(0f, yBase + 0.44f, zOff + 0.12f),
                    new Vector3(13.6f, 0.14f, 1.15f), LegoPalette.WorkshopWood, SurfacePattern.WoodPlanks, false, false, 0.16f, 0f, 0.72f, 0.32f);
            }

            // Safety front steel guardrail along front walkway
            CreateTexturedBoxPart(stand.transform, "FrontRailBar", new Vector3(0f, 1.25f, 1.65f),
                new Vector3(13.8f, 0.10f, 0.10f), LegoPalette.SilverMetallic, SurfacePattern.None, false, false, 0.55f, 0.35f);
            for (int rp = -3; rp <= 3; rp++)
            {
                CreateTexturedBoxPart(stand.transform, $"FrontRailPost_{rp}", new Vector3(rp * 2.15f, 0.90f, 1.65f),
                    new Vector3(0.11f, 0.75f, 0.11f), LegoPalette.CarbonBlack, SurfacePattern.None, false, false, 0.24f, 0.2f);
            }

            // Structural steel columns supporting pitched cantilevered canopy
            for (int col = -1; col <= 1; col++)
            {
                float cx = col * 5.8f;
                CreateTexturedBoxPart(stand.transform, $"RearColumn_{col}", new Vector3(cx, 3.45f, -5.0f),
                    new Vector3(0.32f, 6.2f, 0.32f), LegoPalette.CarbonBlack, SurfacePattern.None, false, false, 0.28f, 0.35f);
                GameObject strut = CreateTexturedBoxPart(stand.transform, $"CanopyStrut_{col}", new Vector3(cx, 5.75f, -2.4f),
                    new Vector3(0.24f, 0.26f, 6.2f), LegoPalette.CarbonBlack, SurfacePattern.None, false, false, 0.28f, 0.35f);
                strut.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
            }

            // VIP Glass Press Box at top rear center
            CreateTexturedBoxPart(stand.transform, "PressBoxFrame", new Vector3(0f, 4.9f, -4.2f),
                new Vector3(4.6f, 1.8f, 1.85f), LegoPalette.CloudWarmWhite, SurfacePattern.StoneAshlar, false, false, 0.20f, 0f, 0.55f, 0.22f);
            CreateTexturedBoxPart(stand.transform, "PressBoxGlass", new Vector3(0f, 4.92f, -3.26f),
                new Vector3(4.0f, 1.15f, 0.10f), new Color(0.56f, 0.78f, 0.92f), SurfacePattern.None, false, false, 0.86f, 0.28f);
            CreateTexturedBoxPart(stand.transform, "PressBoxMullion", new Vector3(0f, 4.92f, -3.22f),
                new Vector3(0.12f, 1.20f, 0.14f), LegoPalette.CarbonBlack, SurfacePattern.None, false, false, 0.25f, 0.3f);

            // Sloped tile/architectural canopy with fascia trim & under-canopy warm lamps
            GameObject canopy = CreateTexturedBoxPart(stand.transform, "Canopy", new Vector3(0f, 6.25f, -2.1f),
                new Vector3(15.2f, 0.32f, 6.8f), LegoPalette.CanyonTerracotta, SurfacePattern.RoofTiles, false, false, 0.18f, 0f, 0.45f, 0.32f);
            canopy.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
            GameObject fascia = CreateTexturedBoxPart(stand.transform, "CanopyFascia", new Vector3(0f, 5.78f, 1.25f),
                new Vector3(15.3f, 0.38f, 0.24f), LegoPalette.CloudWarmWhite, SurfacePattern.None, false, false, 0.22f);
            fascia.transform.localRotation = Quaternion.Euler(-8f, 0f, 0f);

            for (int l = -1; l <= 1; l += 2)
            {
                CreateBoxPart(stand.transform, $"CanopyLamp_{l}", new Vector3(l * 3.6f, 5.88f, -0.4f),
                    new Vector3(1.1f, 0.12f, 0.25f), LegoPalette.SunWarmGold, true, false, 0.25f);
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
            GameObject lh = new GameObject("LowPolyCoastalLighthouse");
            lh.transform.SetParent(parent, false);
            lh.transform.position = new Vector3(pos.x, -1.2f, pos.z);

            for (int ring = 0; ring < 5; ring++)
            {
                GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.Destroy(seg.GetComponent<Collider>());
                seg.name = $"TowerRing_{ring}";
                seg.transform.SetParent(lh.transform, false);
                seg.transform.localPosition = new Vector3(0f, ring * 2.5f + 1.25f, 0f);
                float diam = 3.2f - ring * 0.30f;
                seg.transform.localScale = new Vector3(diam, 1.3f, diam);
                Color c = (ring % 2 == 0) ? LegoPalette.WorkshopBrickRed : LegoPalette.CloudWarmWhite;
                seg.GetComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetLowPolyMaterial(c, false, 0.14f);
            }

            CreateBoxPart(lh.transform, "GalleryDeck", new Vector3(0f, 12.7f, 0f), new Vector3(2.8f, 0.35f, 2.8f), LegoPalette.CarbonBlack, false, false, 0.18f);
            GameObject beacon = CreateBoxPart(lh.transform, "RotatingBeaconLight", new Vector3(0f, 13.7f, 0f), new Vector3(1.4f, 1.2f, 2.2f), LegoPalette.SunWarmGold, true, false, 0.25f);
            GameObject roof = CreateBoxPart(lh.transform, "RoofCap", new Vector3(0f, 14.7f, 0f), new Vector3(2.2f, 0.65f, 2.2f), LegoPalette.WorkshopBrickRed, false, false, 0.14f);
            roof.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            return beacon.transform;
        }

        private static Transform BuildLegoSkyClouds(Transform parent)
        {
            GameObject cloudRoot = new GameObject("LowPolySkyCloudRing");
            cloudRoot.transform.SetParent(parent, false);
            cloudRoot.transform.position = new Vector3(0f, 56f, 0f);

            int cloudCount = 18;
            Material cloudMat = LegoMeshAndMaterialFactory.GetLowPolyMaterial(LegoPalette.CloudWarmWhite, false, 0.08f);
            Mesh cloudMesh = LegoMeshAndMaterialFactory.GetLowPolyCloudMesh();

            for (int i = 0; i < cloudCount; i++)
            {
                float angle = (i / (float)cloudCount) * Mathf.PI * 2f;
                float dist = 175f + (i % 3) * 52f;
                float heightOffset = (i % 4) * 7f;

                GameObject cloud = new GameObject($"LowPolyCloud_{i}");
                cloud.transform.SetParent(cloudRoot.transform, false);
                cloud.transform.localPosition = new Vector3(Mathf.Cos(angle) * dist, heightOffset, Mathf.Sin(angle) * dist);
                cloud.transform.localRotation = Quaternion.Euler(0f, i * 31f, 0f);
                float sc = 8.5f + (i % 3) * 2.8f;
                cloud.transform.localScale = new Vector3(sc, sc * 0.55f, sc * 0.78f);
                cloud.AddComponent<MeshFilter>().sharedMesh = cloudMesh;
                cloud.AddComponent<MeshRenderer>().sharedMaterial = cloudMat;
            }

            return cloudRoot.transform;
        }

        private static void BuildLowPolyRock(Transform parent, Vector3 pos, float scale, float rotY)
        {
            GameObject rock = new GameObject("LowPolyBoulder");
            rock.transform.SetParent(parent, false);
            rock.transform.position = new Vector3(pos.x, -0.15f, pos.z);
            rock.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
            rock.transform.localScale = new Vector3(scale * 1.15f, scale * 0.85f, scale);
            rock.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetLowPolyRockMesh();
            Color col = Color.Lerp(LegoPalette.LowPolyRockGrey, LegoPalette.LowPolyMountainSlate, (Mathf.Abs(rotY) % 3f) * 0.15f);
            rock.AddComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetLowPolyMaterial(col, false, 0.12f);
        }

        private static void BuildWildflowerCluster(Transform parent, Vector3 pos, int seed)
        {
            Color[] petals = {
                LegoPalette.BrightYellow,
                LegoPalette.PurpleRival,
                LegoPalette.PureWhite,
                LegoPalette.BrightOrange,
                new Color(0.95f, 0.42f, 0.62f),
                new Color(0.45f, 0.72f, 0.98f)
            };
            Color chosenPetal = petals[Mathf.Abs(seed) % petals.Length];

            GameObject cluster = new GameObject("LowPolyWildflowers");
            cluster.transform.SetParent(parent, false);
            float baseY = Mathf.Abs(pos.y) > 0.05f ? pos.y : -0.10f;
            cluster.transform.position = new Vector3(pos.x, baseY, pos.z);
            cluster.transform.rotation = Quaternion.Euler(0f, seed * 47f, 0f);
            float sc = 1.85f + (Mathf.Abs(seed) % 3) * 0.32f;
            cluster.transform.localScale = Vector3.one * sc;
            cluster.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetLowPolyFlowerPatchMesh(chosenPetal);
            // Vertex colors embed stems, petals, and centers cleanly; keep _Color pure white.
            cluster.AddComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetSwayingFoliageMaterial(Color.white, 0.85f);
        }

        private static void BuildGrassTuft(Transform parent, Vector3 pos, int seed)
        {
            GameObject tuft = new GameObject("LowPolyGrassTuft");
            tuft.transform.SetParent(parent, false);
            float baseY = Mathf.Abs(pos.y) > 0.05f ? pos.y : -0.10f;
            tuft.transform.position = new Vector3(pos.x, baseY, pos.z);
            tuft.transform.rotation = Quaternion.Euler(0f, seed * 73f, 0f);
            float sc = 1.45f + (Mathf.Abs(seed) % 5) * 0.32f;
            tuft.transform.localScale = new Vector3(sc, sc * 0.92f, sc);
            tuft.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetSwayingGrassTuftMesh();

            Color grassCol = Color.Lerp(LegoPalette.LowPolyGrassBright, LegoPalette.LowPolyGrassWarm, (Mathf.Abs(seed) % 5) * 0.18f);
            tuft.AddComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetSwayingFoliageMaterial(grassCol, 0.90f);
        }

        private static void BuildCoastalSeaBay(Transform parent, Vector3 center)
        {
            GameObject sea = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(sea.GetComponent<Collider>());
            sea.name = "CoastalSeaBay";
            sea.transform.SetParent(parent, false);
            sea.transform.position = new Vector3(center.x, -0.32f, center.z);
            sea.transform.localScale = new Vector3(95f, 0.25f, 95f);
            sea.GetComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetLowPolyMaterial(LegoPalette.LowPolySeaBlue, false, 0.65f);
        }

        private static void CreateDestructibleBrickStack(Transform parent, Vector3 pos, Quaternion rot, TerrainType biome)
        {
            GameObject propRoot = new GameObject($"LowPolyHazardCrate_{biome}");
            propRoot.transform.SetParent(parent, false);
            propRoot.transform.position = pos;
            propRoot.transform.rotation = rot;

            BoxCollider trigger = propRoot.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2.4f, 2.4f, 2.0f);
            trigger.center = new Vector3(0f, 1.0f, 0f);

            GameObject vis = new GameObject("VisualBricks");
            vis.transform.SetParent(propRoot.transform, false);

            Color c1 = biome == TerrainType.OffRoad ? LegoPalette.BrightOrange : LegoPalette.WorkshopBrickRed;
            Color c2 = LegoPalette.CloudWarmWhite;

            for (int layer = 0; layer < 2; layer++)
            {
                GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Object.Destroy(barrel.GetComponent<Collider>());
                barrel.transform.SetParent(vis.transform, false);
                barrel.transform.localPosition = new Vector3((layer - 0.5f) * 0.72f, 0.45f, 0f);
                barrel.transform.localScale = new Vector3(0.62f, 0.45f, 0.62f);
                barrel.GetComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetLowPolyMaterial(layer % 2 == 0 ? c1 : c2, false, 0.16f);
            }

            DestructibleLegoProp destructible = propRoot.AddComponent<DestructibleLegoProp>();
            destructible.SetupVisuals(vis, c1, c2);
        }

        private static void BuildDecorativeLegoTree(Transform parent, Vector3 pos, TerrainType biome)
        {
            GameObject tree = new GameObject("LowPolyPineTree");
            tree.transform.SetParent(parent, false);
            float scale = 2.15f + Mathf.Abs(Mathf.Sin(pos.x * 0.13f + pos.z * 0.07f)) * 1.35f;
            float baseY = Mathf.Abs(pos.y) > 0.05f ? pos.y : -0.10f;
            tree.transform.position = new Vector3(pos.x, baseY, pos.z);
            tree.transform.rotation = Quaternion.Euler(0f, (pos.x + pos.z) * 17f, 0f);
            tree.transform.localScale = Vector3.one * scale;

            Material barkMat = LegoMeshAndMaterialFactory.GetTexturedLowPolyMaterial(
                LegoPalette.LowPolyTrunkBrown, SurfacePattern.TreeBark, 0.14f, 0.0f, 1.65f, 0.56f);

            GameObject trunkLower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(trunkLower.GetComponent<Collider>());
            trunkLower.name = "Trunk";
            trunkLower.transform.SetParent(tree.transform, false);
            trunkLower.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            trunkLower.transform.localScale = new Vector3(0.46f, 0.78f, 0.46f);
            trunkLower.GetComponent<MeshRenderer>().sharedMaterial = barkMat;

            GameObject trunkUpper = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(trunkUpper.GetComponent<Collider>());
            trunkUpper.name = "TrunkUpper";
            trunkUpper.transform.SetParent(tree.transform, false);
            trunkUpper.transform.localPosition = new Vector3(0.03f, 1.65f, -0.02f);
            trunkUpper.transform.localRotation = Quaternion.Euler(2f, 0f, -2f);
            trunkUpper.transform.localScale = new Vector3(0.32f, 0.65f, 0.32f);
            trunkUpper.GetComponent<MeshRenderer>().sharedMaterial = barkMat;

            // 4 Root Flares anchored into turf + 4 Angled Structural Branches & Fork Twigs
            for (int r = 0; r < 4; r++)
            {
                float rAng = r * 90f + 18f;
                GameObject root = new GameObject($"RootFlare_{r}");
                root.transform.SetParent(tree.transform, false);
                float rad = rAng * Mathf.Deg2Rad;
                root.transform.localPosition = new Vector3(Mathf.Cos(rad) * 0.25f, 0.14f, Mathf.Sin(rad) * 0.25f);
                root.transform.localRotation = Quaternion.Euler(0f, -rAng, 24f);
                root.transform.localScale = new Vector3(0.38f, 0.28f, 0.22f);
                GameObject rVis = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(rVis.GetComponent<Collider>());
                rVis.transform.SetParent(root.transform, false);
                rVis.GetComponent<MeshRenderer>().sharedMaterial = barkMat;

                GameObject branch = new GameObject($"Branch_{r}");
                branch.transform.SetParent(tree.transform, false);
                float bDeg = r * 90f + 46f;
                float bAng = bDeg * Mathf.Deg2Rad;
                float bHeight = 1.18f + r * 0.24f;
                branch.transform.localPosition = new Vector3(Mathf.Cos(bAng) * 0.36f, bHeight, Mathf.Sin(bAng) * 0.36f);
                branch.transform.localRotation = Quaternion.Euler(0f, -bDeg, -42f);
                GameObject bVis = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(bVis.GetComponent<Collider>());
                bVis.transform.SetParent(branch.transform, false);
                bVis.transform.localScale = new Vector3(0.84f, 0.14f, 0.14f);
                bVis.GetComponent<MeshRenderer>().sharedMaterial = barkMat;

                GameObject forkTwig = new GameObject($"BranchFork_{r}");
                forkTwig.transform.SetParent(branch.transform, false);
                forkTwig.transform.localPosition = new Vector3(0.28f, 0.06f, 0f);
                forkTwig.transform.localRotation = Quaternion.Euler(0f, (r % 2 == 0 ? 28f : -28f), 26f);
                GameObject fVis = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(fVis.GetComponent<Collider>());
                fVis.transform.SetParent(forkTwig.transform, false);
                fVis.transform.localScale = new Vector3(0.46f, 0.09f, 0.09f);
                fVis.GetComponent<MeshRenderer>().sharedMaterial = barkMat;
            }

            Color leafCol = (biome == TerrainType.Street) ? LegoPalette.LowPolyPineMid : LegoPalette.LowPolyPineDark;
            GameObject canopy = new GameObject("PineCanopy");
            canopy.transform.SetParent(tree.transform, false);
            canopy.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            canopy.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetLowPolyPineTreeMesh();
            canopy.AddComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetSwayingFoliageMaterial(leafCol, 0.42f);

            // 16 small animated swaying leaf clusters across 4 tiers & apex for rich silhouette & gentle wind motion
            Mesh leafClusterMesh = LegoMeshAndMaterialFactory.GetDetailedLeafClusterMesh();
            float[] tierHeights = { 1.25f, 1.95f, 2.65f, 3.30f };
            float[] tierRadii = { 1.18f, 0.94f, 0.68f, 0.42f };
            float seedBase = (pos.x * 0.17f + pos.z * 0.31f);

            for (int t = 0; t < 4; t++)
            {
                for (int c = 0; c < 4; c++)
                {
                    int idx = t * 4 + c;
                    float deg = c * 90f + t * 34f;
                    float rad = deg * Mathf.Deg2Rad;
                    GameObject lc = new GameObject($"LeafCluster_{idx}");
                    lc.transform.SetParent(tree.transform, false);
                    lc.transform.localPosition = new Vector3(
                        Mathf.Cos(rad) * tierRadii[t],
                        tierHeights[t],
                        Mathf.Sin(rad) * tierRadii[t]);
                    lc.transform.localRotation = Quaternion.Euler(26f, -deg + 90f, (c % 2 == 0 ? 8f : -8f));
                    float cs = (0.98f + (idx % 3) * 0.18f) * (1.0f - t * 0.12f);
                    lc.transform.localScale = Vector3.one * cs;
                    lc.AddComponent<MeshFilter>().sharedMesh = leafClusterMesh;
                    Color sprigCol = Color.Lerp(leafCol, LegoPalette.LowPolyGrassWarm, (idx % 4) * 0.16f);
                    lc.AddComponent<MeshRenderer>().sharedMaterial =
                        LegoMeshAndMaterialFactory.GetSwayingFoliageMaterial(sprigCol, 0.92f);

                    LegoFoliageSwayAnimator sway = lc.AddComponent<LegoFoliageSwayAnimator>();
                    sway.axis = new Vector3(Mathf.Sin(rad), 0.35f, Mathf.Cos(rad));
                    sway.speed = 1.65f + (idx % 4) * 0.28f;
                    sway.amplitudeDeg = 3.4f;
                    sway.phase = seedBase + idx * 0.65f;
                }
            }

            GameObject apex = new GameObject("LeafCluster_Apex");
            apex.transform.SetParent(tree.transform, false);
            apex.transform.localPosition = new Vector3(0f, 3.85f, 0f);
            apex.transform.localScale = Vector3.one * 0.85f;
            apex.AddComponent<MeshFilter>().sharedMesh = leafClusterMesh;
            apex.AddComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetSwayingFoliageMaterial(
                    Color.Lerp(leafCol, LegoPalette.LowPolyGrassBright, 0.22f), 0.95f);
            LegoFoliageSwayAnimator apexSway = apex.AddComponent<LegoFoliageSwayAnimator>();
            apexSway.axis = new Vector3(0.7f, 0.2f, 0.7f);
            apexSway.speed = 2.0f;
            apexSway.amplitudeDeg = 3.8f;
            apexSway.phase = seedBase + 1.2f;

            BuildGrassTuft(parent, new Vector3(pos.x + 0.55f, baseY, pos.z + 0.45f), Mathf.RoundToInt(Mathf.Abs(pos.x * 7f + pos.z * 11f)));
        }

        private static void BuildStartFinishArch(Transform parent, Vector3 center, Quaternion rot, float width)
        {
            GameObject arch = new GameObject("LowPolyStartFinishGantry");
            arch.transform.SetParent(parent, false);
            arch.transform.position = center;
            arch.transform.rotation = rot;

            float halfW = width * 0.49f;
            for (int side = -1; side <= 1; side += 2)
            {
                float px = side * halfW;
                CreateTexturedBoxPart(arch.transform, $"Pedestal_{side}", new Vector3(px, 0.75f, 0f),
                    new Vector3(1.85f, 1.5f, 1.85f), LegoPalette.LowPolyRockGrey, SurfacePattern.StoneAshlar, false, false, 0.16f, 0f, 0.45f, 0.35f);
                CreateTexturedBoxPart(arch.transform, $"BrickPier_{side}", new Vector3(px, 2.7f, 0f),
                    new Vector3(1.42f, 2.5f, 1.42f), LegoPalette.WorkshopBrickRed, SurfacePattern.BrickMasonry, false, false, 0.16f, 0f, 0.55f, 0.32f);

                // Steel truss upper tower columns
                for (int cz = -1; cz <= 1; cz += 2)
                {
                    CreateTexturedBoxPart(arch.transform, $"TowerChord_{side}_{cz}", new Vector3(px, 6.2f, cz * 0.45f),
                        new Vector3(0.32f, 4.8f, 0.32f), LegoPalette.CarbonBlack, SurfacePattern.None, false, false, 0.30f, 0.35f);
                }
                for (int b = 0; b < 3; b++)
                {
                    float by = 4.6f + b * 1.45f;
                    GameObject brace = CreateTexturedBoxPart(arch.transform, $"TowerBrace_{side}_{b}", new Vector3(px, by, 0f),
                        new Vector3(0.24f, 0.16f, 1.35f), LegoPalette.SilverMetallic, SurfacePattern.None, false, false, 0.48f, 0.4f);
                    brace.transform.localRotation = Quaternion.Euler((b % 2 == 0) ? 38f : -38f, 0f, 0f);
                }
            }

            // Overhead motorsport truss frame (top/bottom chords + diagonal web members)
            CreateTexturedBoxPart(arch.transform, "GantryBottomChord", new Vector3(0f, 7.65f, 0f),
                new Vector3(width + 2.2f, 0.32f, 1.35f), LegoPalette.CarbonBlack, SurfacePattern.None, false, false, 0.30f, 0.35f);
            CreateTexturedBoxPart(arch.transform, "GantryTopChord", new Vector3(0f, 9.35f, 0f),
                new Vector3(width + 2.4f, 0.34f, 1.40f), LegoPalette.CarbonBlack, SurfacePattern.None, false, false, 0.30f, 0.35f);
            CreateBoxPart(arch.transform, "GantryAccentStrip", new Vector3(0f, 7.45f, 0f),
                new Vector3(width + 1.4f, 0.18f, 1.45f), LegoPalette.CyanGlow, true, false, 0.25f);

            int trussSegs = 6;
            float spanStep = width / trussSegs;
            for (int s = 0; s < trussSegs; s++)
            {
                float sx = -halfW + (s + 0.5f) * spanStep;
                GameObject diag = CreateTexturedBoxPart(arch.transform, $"GantryDiag_{s}", new Vector3(sx, 8.5f, 0.52f),
                    new Vector3(spanStep * 1.15f, 0.16f, 0.16f), LegoPalette.SilverMetallic, SurfacePattern.None, false, false, 0.45f, 0.35f);
                diag.transform.localRotation = Quaternion.Euler(0f, 0f, (s % 2 == 0) ? 26f : -26f);
            }

            int numCheckers = 12;
            float blockWidth = width / numCheckers;
            for (int col = 0; col < numCheckers; col++)
            {
                Color c = (col % 2 == 0) ? LegoPalette.CloudWarmWhite : LegoPalette.CarbonBlack;
                CreateBoxPart(arch.transform, $"BannerCheck_{col}", new Vector3(-halfW + (col + 0.5f) * blockWidth, 8.5f, 0f),
                    new Vector3(blockWidth * 0.94f, 0.98f, 0.92f), c, false, false, 0.16f);
            }

            // Starting Lights Housing Pod centered on gantry
            CreateTexturedBoxPart(arch.transform, "StartSignalPod", new Vector3(0f, 8.55f, 0.68f),
                new Vector3(4.2f, 0.95f, 0.36f), LegoPalette.CarbonBlack, SurfacePattern.None, false, false, 0.26f, 0.3f);
            for (int lamp = 0; lamp < 4; lamp++)
            {
                float lx = -1.2f + lamp * 0.8f;
                Color lCol = (lamp < 2) ? LegoPalette.BrightRed : LegoPalette.SunWarmGold;
                CreateBoxPart(arch.transform, $"SignalLamp_{lamp}", new Vector3(lx, 8.55f, 0.86f),
                    new Vector3(0.42f, 0.42f, 0.12f), lCol, true, false, 0.25f);
            }
        }

        private static void BuildBiomeGateBanner(Transform parent, Vector3 center, Vector3 forward, float width, Color gateColor)
        {
            GameObject gate = new GameObject("OffRoadSectorPortal");
            gate.transform.SetParent(parent, false);
            gate.transform.position = center;
            gate.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

            float halfW = width * 0.48f;
            CreateBoxPart(gate.transform, "PostL", new Vector3(-halfW, 3.6f, 0f), new Vector3(1.0f, 7.2f, 1.0f), LegoPalette.WorkshopWood, false, false, 0.12f);
            CreateBoxPart(gate.transform, "PostR", new Vector3( halfW, 3.6f, 0f), new Vector3(1.0f, 7.2f, 1.0f), LegoPalette.WorkshopWood, false, false, 0.12f);
            CreateBoxPart(gate.transform, "CrossBar", new Vector3(0f, 6.8f, 0f), new Vector3(width + 1.4f, 1.0f, 1.1f), gateColor, false, false, 0.14f);
        }

        private static void BuildBlockyMinecraftSun(Transform parent)
        {
            GameObject sunRoot = new GameObject("LowPolySunriseSun");
            sunRoot.transform.SetParent(parent, false);
            sunRoot.transform.position = new Vector3(-160f, 175f, 380f);
            sunRoot.transform.LookAt(Vector3.zero);

            // Round glowing sun disc (Image 2 warm golden-hour sunrise)
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(disc.GetComponent<Collider>());
            disc.name = "SunriseSunDisc";
            disc.transform.SetParent(sunRoot.transform, false);
            disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            disc.transform.localScale = new Vector3(44f, 0.8f, 44f);
            disc.GetComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetLowPolyMaterial(LegoPalette.SunWarmGold, true, 0.1f);

            GameObject halo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(halo.GetComponent<Collider>());
            halo.name = "SunriseSunHalo";
            halo.transform.SetParent(sunRoot.transform, false);
            halo.transform.localPosition = new Vector3(0f, 0f, -2f);
            halo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            halo.transform.localScale = new Vector3(68f, 0.4f, 68f);
            halo.GetComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetLowPolyMaterial(new Color(1.0f, 0.78f, 0.36f), true, 0.05f);
        }

        private static Mesh _gablePrismMesh;

        private static Mesh GetGablePrismMesh()
        {
            if (_gablePrismMesh != null) return _gablePrismMesh;

            Mesh mesh = new Mesh { name = "ArchitecturalGablePrism" };
            Vector3 fl = new Vector3(-0.5f, 0f,  0.5f);
            Vector3 fr = new Vector3( 0.5f, 0f,  0.5f);
            Vector3 ft = new Vector3( 0.0f, 1f,  0.5f);
            Vector3 bl = new Vector3(-0.5f, 0f, -0.5f);
            Vector3 br = new Vector3( 0.5f, 0f, -0.5f);
            Vector3 bt = new Vector3( 0.0f, 1f, -0.5f);

            mesh.vertices = new Vector3[]
            {
                fl, fr, ft,             // Front (+Z)
                br, bl, bt,             // Back (-Z)
                bl, fl, ft, bt,         // Left roof slope
                fr, br, bt, ft,         // Right roof slope
                bl, br, fr, fl          // Bottom (-Y)
            };
            mesh.triangles = new int[]
            {
                0, 1, 2,
                3, 4, 5,
                6, 7, 8,   6, 8, 9,
                10, 11, 12,  10, 12, 13,
                14, 15, 16,  14, 16, 17
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            _gablePrismMesh = mesh;
            return _gablePrismMesh;
        }

        private static bool IsFootprintClear(List<Vector3> waypoints, BuiltTrackData data, Vector3 worldPos, float trackWidth, float footprintRadius)
        {
            float trackClearance = trackWidth * 0.5f + footprintRadius + 18f;
            if (IsTooCloseToTrack(waypoints, worldPos, trackClearance)) return false;
            if (IsTooCloseToOpenWorldHub(worldPos, data, footprintRadius + 14f)) return false;
            return true;
        }

        private static void BuildAlpineMountainRange(Transform parent, BuiltTrackData data, List<Vector3> waypoints, float trackWidth)
        {
            GameObject rangeRoot = new GameObject("AlpineMountainRange");
            rangeRoot.transform.SetParent(parent, false);
            Mesh mountainMesh = LegoMeshAndMaterialFactory.GetLowPolyMountainMesh();

            int outerPeaks = 28;
            for (int i = 0; i < outerPeaks; i++)
            {
                float angle = (i / (float)outerPeaks) * Mathf.PI * 2f;
                float width = 76f + (i % 4) * 16f;
                float height = 56f + (i % 5) * 15f;
                float peakFootprint = width * 1.15f;

                float dist = 335f + (i % 3) * 32f;
                Vector3 peakPos = new Vector3(Mathf.Cos(angle) * dist, -0.5f, Mathf.Sin(angle) * dist);
                int attempts = 0;
                while (!IsFootprintClear(waypoints, data, peakPos, trackWidth, peakFootprint) && attempts < 14)
                {
                    dist += 24f;
                    peakPos = new Vector3(Mathf.Cos(angle) * dist, -0.5f, Mathf.Sin(angle) * dist);
                    attempts++;
                }
                if (!IsFootprintClear(waypoints, data, peakPos, trackWidth, peakFootprint))
                    continue;

                GameObject peak = new GameObject($"AlpinePeak_{i}");
                peak.transform.SetParent(rangeRoot.transform, false);
                peak.transform.position = peakPos;
                peak.transform.rotation = Quaternion.Euler(0f, i * 41f, 0f);
                peak.transform.localScale = new Vector3(width, height, width * 0.92f);
                peak.AddComponent<MeshFilter>().sharedMesh = mountainMesh;
                Color peakCol = (i % 2 == 0) ? LegoPalette.LowPolyMountainSlate : Color.Lerp(LegoPalette.LowPolyMountainSlate, LegoPalette.LowPolyMountainGreen, 0.35f);
                peak.AddComponent<MeshRenderer>().sharedMaterial =
                    LegoMeshAndMaterialFactory.GetTexturedLowPolyMaterial(peakCol, SurfacePattern.AlpineRock, 0.14f, 0.0f, 0.16f, 0.58f);

                // Snow cap cone overlay on tallest peaks
                if (i % 2 == 0)
                {
                    GameObject cap = new GameObject("SnowCap");
                    cap.transform.SetParent(peak.transform, false);
                    cap.transform.localPosition = new Vector3(0.01f, 0.62f, -0.01f);
                    cap.transform.localScale = new Vector3(0.44f, 0.40f, 0.44f);
                    cap.AddComponent<MeshFilter>().sharedMesh = mountainMesh;
                    cap.AddComponent<MeshRenderer>().sharedMaterial =
                        LegoMeshAndMaterialFactory.GetTexturedLowPolyMaterial(LegoPalette.LowPolyMountainSnow, SurfacePattern.AlpineRock, 0.18f, 0.0f, 0.18f, 0.26f);
                }

                // Foreground lower green foothills guaranteed clear of entire track & open-world hub
                float footWidth = width * 0.52f;
                float footFootprint = footWidth * 1.15f;
                float footAngle = angle + 0.11f;
                float footDist = dist - 42f;
                Vector3 footPos = new Vector3(Mathf.Cos(footAngle) * footDist, -0.5f, Mathf.Sin(footAngle) * footDist);
                if (IsFootprintClear(waypoints, data, footPos, trackWidth, footFootprint))
                {
                    GameObject foot = new GameObject($"Foothill_{i}");
                    foot.transform.SetParent(rangeRoot.transform, false);
                    foot.transform.position = footPos;
                    foot.transform.rotation = Quaternion.Euler(0f, i * 29f, 0f);
                    foot.transform.localScale = new Vector3(footWidth, height * 0.42f, footWidth);
                    foot.AddComponent<MeshFilter>().sharedMesh = mountainMesh;
                    foot.AddComponent<MeshRenderer>().sharedMaterial =
                        LegoMeshAndMaterialFactory.GetTexturedLowPolyMaterial(LegoPalette.LowPolyMountainGreen, SurfacePattern.MeadowTerrain, 0.12f, 0.0f, 0.24f, 0.68f);
                }
            }
        }

        private static void BuildLowPolyRollingHills(Transform parent, List<Vector3> waypoints, float trackWidth, Vector3 startCenter, BuiltTrackData data)
        {
            GameObject hillRoot = new GameObject("LowPolyRollingHills");
            hillRoot.transform.SetParent(parent, false);
            Mesh moundMesh = LegoMeshAndMaterialFactory.GetLowPolyMountainMesh();

            for (int i = 0; i < 24; i++)
            {
                float sx = 24f + (i % 4) * 4f;
                float sy = 8.5f + (i % 3) * 3.2f;
                float footprintRadius = sx * 1.15f;

                float angle = (i / 24f) * Mathf.PI * 2f + 0.14f;
                float dist = 238f + (i % 3) * 24f;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * dist, -0.5f, Mathf.Sin(angle) * dist);

                int attempts = 0;
                while ((!IsFootprintClear(waypoints, data, pos, trackWidth, footprintRadius) || Vector3.Distance(pos, startCenter) < footprintRadius + 75f)
                       && attempts < 12)
                {
                    dist += 22f;
                    pos = new Vector3(Mathf.Cos(angle) * dist, -0.5f, Mathf.Sin(angle) * dist);
                    attempts++;
                }

                if (!IsFootprintClear(waypoints, data, pos, trackWidth, footprintRadius)) continue;
                if (Vector3.Distance(pos, startCenter) < footprintRadius + 75f) continue;

                GameObject hill = new GameObject($"MeadowHill_{i}");
                hill.transform.SetParent(hillRoot.transform, false);
                hill.transform.position = pos;
                hill.transform.rotation = Quaternion.Euler(0f, i * 53f, 0f);
                hill.transform.localScale = new Vector3(sx, sy, sx * 0.85f);
                hill.AddComponent<MeshFilter>().sharedMesh = moundMesh;
                Color hCol = (i % 2 == 0) ? LegoPalette.LowPolyGrassWarm : LegoPalette.LowPolyMountainGreen;
                hill.AddComponent<MeshRenderer>().sharedMaterial =
                    LegoMeshAndMaterialFactory.GetTexturedLowPolyMaterial(hCol, SurfacePattern.MeadowTerrain, 0.10f, 0.0f, 0.24f, 0.68f);
            }
        }

        private static void BuildAnimatedWindmill(
            Transform parent,
            Vector3 pos,
            Quaternion rot,
            LegoWorldSceneryAnimator anim,
            int seed)
        {
            GameObject mill = new GameObject("LowPolyWindmill");
            mill.transform.SetParent(parent, false);
            mill.transform.position = new Vector3(pos.x, 0f, pos.z);
            mill.transform.rotation = rot;

            Color stoneCol = LegoPalette.LowPolyRockGrey;
            Color warmStucco = Color.Lerp(LegoPalette.CloudWarmWhite, LegoPalette.TanSand, 0.26f);
            Color timberCol = LegoPalette.WorkshopWood;
            Color[] roofChoices = { LegoPalette.CanyonTerracotta, LegoPalette.WorkshopBrickRed, LegoPalette.WorkshopTeal };
            Color roofCol = roofChoices[Mathf.Abs(seed) % roofChoices.Length];
            Color sailCream = LegoPalette.CloudWarmWhite;
            Color glassCol = new Color(0.56f, 0.78f, 0.90f);

            // 1. Stepped Stone Ashlar Plinth + Entrance Steps
            CreateTexturedBoxPart(mill.transform, "WindmillPlinth", new Vector3(0f, 0.45f, 0f),
                new Vector3(7.4f, 1.00f, 7.4f), stoneCol, SurfacePattern.StoneAshlar, false, true, 0.14f, 0f, 0.52f, 0.36f);
            CreateTexturedBoxPart(mill.transform, "WindmillPlinthCap", new Vector3(0f, 0.96f, 0f),
                new Vector3(7.65f, 0.14f, 7.65f), sailCream, SurfacePattern.StoneAshlar, false, false, 0.16f, 0f, 0.55f, 0.24f);
            CreateTexturedBoxPart(mill.transform, "StepTop", new Vector3(0f, 0.32f, 4.05f),
                new Vector3(2.8f, 0.52f, 0.90f), stoneCol, SurfacePattern.StoneAshlar, false, true, 0.14f);
            CreateTexturedBoxPart(mill.transform, "StepLow", new Vector3(0f, 0.14f, 4.72f),
                new Vector3(3.0f, 0.26f, 0.65f), stoneCol, SurfacePattern.StoneAshlar, false, true, 0.14f);

            // 2. Lower Brick/Stone Tapered Tower + Battered Corner Buttresses + Arched Oak Portal
            CreateTexturedBoxPart(mill.transform, "LowerTowerBody", new Vector3(0f, 3.25f, 0f),
                new Vector3(5.6f, 4.65f, 5.6f), warmStucco, SurfacePattern.BrickMasonry, false, true, 0.14f, 0f, 0.52f, 0.30f);

            for (int bx = -1; bx <= 1; bx += 2)
            {
                for (int bz = -1; bz <= 1; bz += 2)
                {
                    GameObject pier = CreateTexturedBoxPart(mill.transform, $"Buttress_{bx}_{bz}",
                        new Vector3(bx * 2.75f, 3.20f, bz * 2.75f),
                        new Vector3(0.68f, 4.60f, 0.68f), stoneCol, SurfacePattern.StoneAshlar, false, false, 0.14f, 0f, 0.60f, 0.34f);
                    pier.transform.localRotation = Quaternion.Euler(-bz * 3.8f, 0f, bx * 3.8f);
                }
            }

            CreateTexturedBoxPart(mill.transform, "DoorArchFrame", new Vector3(0f, 2.15f, 2.84f),
                new Vector3(2.05f, 2.45f, 0.22f), stoneCol, SurfacePattern.StoneAshlar, false, false, 0.14f);
            CreateTexturedBoxPart(mill.transform, "DoorOakLeaf", new Vector3(0f, 2.05f, 2.88f),
                new Vector3(1.52f, 2.22f, 0.18f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.16f, 0f, 1.0f, 0.38f);
            CreateBoxPart(mill.transform, "DoorSconce", new Vector3(1.32f, 2.85f, 3.00f),
                new Vector3(0.24f, 0.32f, 0.24f), LegoPalette.SunWarmGold, true, false, 0.25f);

            // 3. Timber Observation Gallery Deck (y = 5.65m) + Guardrails
            CreateTexturedBoxPart(mill.transform, "BeltStoneCourse", new Vector3(0f, 5.56f, 0f),
                new Vector3(6.1f, 0.24f, 6.1f), stoneCol, SurfacePattern.StoneAshlar, false, false, 0.14f);
            CreateTexturedBoxPart(mill.transform, "BalconyDeck", new Vector3(0f, 5.75f, 0f),
                new Vector3(7.4f, 0.22f, 7.4f), timberCol, SurfacePattern.WoodPlanks, false, true, 0.14f, 0f, 0.72f, 0.34f);

            for (int side = -1; side <= 1; side += 2)
            {
                GameObject braceX = CreateTexturedBoxPart(mill.transform, $"BraceX_{side}",
                    new Vector3(side * 3.15f, 5.05f, 0f), new Vector3(0.22f, 1.35f, 0.24f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.14f);
                braceX.transform.localRotation = Quaternion.Euler(0f, 0f, -side * 34f);
                GameObject braceZ = CreateTexturedBoxPart(mill.transform, $"BraceZ_{side}",
                    new Vector3(0f, 5.05f, side * 3.15f), new Vector3(0.24f, 1.35f, 0.22f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.14f);
                braceZ.transform.localRotation = Quaternion.Euler(side * 34f, 0f, 0f);
            }

            float railY = 6.32f;
            float rSpan = 3.56f;
            CreateTexturedBoxPart(mill.transform, "RailTopN", new Vector3(0f, railY + 0.22f,  rSpan), new Vector3(7.3f, 0.12f, 0.14f), sailCream, SurfacePattern.WoodPlanks, false, false, 0.16f);
            CreateTexturedBoxPart(mill.transform, "RailTopS", new Vector3(0f, railY + 0.22f, -rSpan), new Vector3(7.3f, 0.12f, 0.14f), sailCream, SurfacePattern.WoodPlanks, false, false, 0.16f);
            CreateTexturedBoxPart(mill.transform, "RailTopE", new Vector3( rSpan, railY + 0.22f, 0f), new Vector3(0.14f, 0.12f, 7.3f), sailCream, SurfacePattern.WoodPlanks, false, false, 0.16f);
            CreateTexturedBoxPart(mill.transform, "RailTopW", new Vector3(-rSpan, railY + 0.22f, 0f), new Vector3(0.14f, 0.12f, 7.3f), sailCream, SurfacePattern.WoodPlanks, false, false, 0.16f);
            for (int px = -3; px <= 3; px += 2)
            {
                float offset = px * 1.15f;
                CreateBoxPart(mill.transform, $"PostN_{px}", new Vector3(offset, railY,  rSpan), new Vector3(0.12f, 0.85f, 0.12f), timberCol, false, false, 0.16f);
                CreateBoxPart(mill.transform, $"PostS_{px}", new Vector3(offset, railY, -rSpan), new Vector3(0.12f, 0.85f, 0.12f), timberCol, false, false, 0.16f);
                CreateBoxPart(mill.transform, $"PostE_{px}", new Vector3( rSpan, railY, offset), new Vector3(0.12f, 0.85f, 0.12f), timberCol, false, false, 0.16f);
                CreateBoxPart(mill.transform, $"PostW_{px}", new Vector3(-rSpan, railY, offset), new Vector3(0.12f, 0.85f, 0.12f), timberCol, false, false, 0.16f);
            }

            // 4. Upper Tapered Tower Stage + Windows + Stepped Conical Roof Cap
            CreateTexturedBoxPart(mill.transform, "UpperTowerBody", new Vector3(0f, 8.05f, 0f),
                new Vector3(4.5f, 4.40f, 4.5f), warmStucco, SurfacePattern.WoodPlanks, false, true, 0.14f, 0f, 0.65f, 0.26f);
            for (int cx = -1; cx <= 1; cx += 2)
            {
                for (int cz = -1; cz <= 1; cz += 2)
                {
                    CreateTexturedBoxPart(mill.transform, $"UpperTimber_{cx}_{cz}",
                        new Vector3(cx * 2.22f, 8.05f, cz * 2.22f), new Vector3(0.24f, 4.45f, 0.24f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.16f);
                }
            }

            // Upper tower glass windows on 3 non-rotor faces + lower side windows
            Quaternion[] winDirs = { Quaternion.Euler(0f, 90f, 0f), Quaternion.Euler(0f, -90f, 0f), Quaternion.Euler(0f, 180f, 0f) };
            for (int wi = 0; wi < winDirs.Length; wi++)
            {
                GameObject wFrame = new GameObject($"MillWin_{wi}");
                wFrame.transform.SetParent(mill.transform, false);
                Vector3 fwd = winDirs[wi] * Vector3.forward;
                wFrame.transform.localPosition = new Vector3(fwd.x * 2.27f, 8.20f, fwd.z * 2.27f);
                wFrame.transform.localRotation = winDirs[wi];
                CreateTexturedBoxPart(wFrame.transform, "Sill", new Vector3(0f, -0.62f, 0.08f), new Vector3(1.15f, 0.16f, 0.24f), stoneCol, SurfacePattern.StoneAshlar, false, false, 0.14f);
                CreateBoxPart(wFrame.transform, "Glass", new Vector3(0f, 0f, 0.02f), new Vector3(0.86f, 1.08f, 0.08f), glassCol, false, false, 0.72f);
                CreateBoxPart(wFrame.transform, "MullionV", new Vector3(0f, 0f, 0.06f), new Vector3(0.08f, 1.08f, 0.08f), sailCream, false, false, 0.18f);
                CreateBoxPart(wFrame.transform, "MullionH", new Vector3(0f, 0f, 0.06f), new Vector3(0.86f, 0.08f, 0.08f), sailCream, false, false, 0.18f);
            }

            CreateTexturedBoxPart(mill.transform, "CapEaveTrim", new Vector3(0f, 10.28f, 0f),
                new Vector3(5.0f, 0.24f, 5.0f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.16f);
            CreateTexturedBoxPart(mill.transform, "RoofTier1", new Vector3(0f, 10.70f, 0f),
                new Vector3(4.6f, 0.75f, 4.6f), roofCol, SurfacePattern.RoofTiles, false, false, 0.18f, 0f, 0.56f, 0.34f);
            CreateTexturedBoxPart(mill.transform, "RoofTier2", new Vector3(0f, 11.40f, 0f),
                new Vector3(3.3f, 0.75f, 3.3f), roofCol, SurfacePattern.RoofTiles, false, false, 0.18f, 0f, 0.56f, 0.34f);

            GameObject roofCapTip = new GameObject("RoofPeakCone");
            roofCapTip.transform.SetParent(mill.transform, false);
            roofCapTip.transform.localPosition = new Vector3(0f, 11.72f, 0f);
            roofCapTip.transform.localScale = new Vector3(1.85f, 2.10f, 1.85f);
            roofCapTip.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetLowPolyMountainMesh();
            roofCapTip.AddComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetTexturedLowPolyMaterial(roofCol, SurfacePattern.RoofTiles, 0.18f, 0f, 0.56f, 0.34f);

            // Axle housing + Animated 4-Blade Lattice Sail Rotor
            CreateTexturedBoxPart(mill.transform, "ShaftHousing", new Vector3(0f, 10.70f, 2.15f),
                new Vector3(1.10f, 0.95f, 1.55f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.14f);

            GameObject rotorHub = new GameObject("WindmillRotorHub");
            rotorHub.transform.SetParent(mill.transform, false);
            rotorHub.transform.localPosition = new Vector3(0f, 10.75f, 2.95f);
            rotorHub.transform.localRotation = Quaternion.Euler(-5f, 0f, 20f * (Mathf.Abs(seed) % 4));

            CreateBoxPart(rotorHub.transform, "IronBoss", Vector3.zero, new Vector3(0.88f, 0.88f, 0.56f), LegoPalette.DarkGrey, false, false, 0.45f);
            CreateBoxPart(rotorHub.transform, "IronPin", new Vector3(0f, 0f, 0.28f), new Vector3(0.40f, 0.40f, 0.26f), LegoPalette.SilverMetallic, false, false, 0.65f);

            for (int arm = 0; arm < 4; arm++)
            {
                GameObject blade = new GameObject($"SailArm_{arm}");
                blade.transform.SetParent(rotorHub.transform, false);
                blade.transform.localRotation = Quaternion.Euler(0f, 0f, arm * 90f);

                // Heavy oak spar
                CreateTexturedBoxPart(blade.transform, "Spar", new Vector3(0f, 3.05f, 0f),
                    new Vector3(0.24f, 6.10f, 0.22f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.15f);

                // Pitched sail lattice + canvas cloth offset slightly to one side of the spar
                GameObject sailPanel = new GameObject("SailCanvas");
                sailPanel.transform.SetParent(blade.transform, false);
                sailPanel.transform.localPosition = new Vector3(0.68f, 3.45f, 0.04f);
                sailPanel.transform.localRotation = Quaternion.Euler(0f, 11f, 0f);

                CreateBoxPart(sailPanel.transform, "Cloth", Vector3.zero, new Vector3(1.12f, 4.75f, 0.06f), sailCream, false, false, 0.12f);
                for (int r = -2; r <= 2; r++)
                {
                    CreateBoxPart(sailPanel.transform, $"Rung_{r}", new Vector3(0f, r * 0.95f, 0.04f),
                        new Vector3(1.22f, 0.09f, 0.08f), timberCol, false, false, 0.14f);
                }
                CreateBoxPart(sailPanel.transform, "OuterTrim", new Vector3(0.56f, 0f, 0.04f),
                    new Vector3(0.09f, 4.82f, 0.08f), timberCol, false, false, 0.14f);
            }

            if (anim != null)
            {
                anim.windmillRotors.Add(rotorHub.transform);
            }

            // Wooden grain barrel & burlap flour sack beside steps
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(barrel.GetComponent<Collider>());
            barrel.name = "GrainBarrel";
            barrel.transform.SetParent(mill.transform, false);
            barrel.transform.localPosition = new Vector3(-2.35f, 1.52f, 3.35f);
            barrel.transform.localScale = new Vector3(0.75f, 0.50f, 0.75f);
            barrel.GetComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetTexturedLowPolyMaterial(timberCol, SurfacePattern.WoodPlanks, 0.14f, 0f, 0.9f, 0.35f);

            GameObject sack = CreateBoxPart(mill.transform, "FlourSack", new Vector3(2.25f, 1.32f, 3.35f),
                new Vector3(0.85f, 0.62f, 0.65f), LegoPalette.TanSand, false, false, 0.10f);
            sack.transform.localRotation = Quaternion.Euler(0f, 22f, 0f);
        }

        private static void BuildArchitecturalChurch(Transform parent, Vector3 pos, Quaternion rot)
        {
            GameObject church = new GameObject("LowPolyVillageChurch");
            church.transform.SetParent(parent, false);
            church.transform.position = new Vector3(pos.x, 0f, pos.z);
            church.transform.rotation = rot;

            Color limestone = new Color(0.89f, 0.86f, 0.80f);
            Color stonePlinth = LegoPalette.LowPolyRockGrey;
            Color roofRed = LegoPalette.WorkshopBrickRed;
            Color spireTeal = LegoPalette.WorkshopTeal;
            Color woodDark = new Color(0.38f, 0.23f, 0.13f);
            Color stainBlue = new Color(0.24f, 0.56f, 0.82f);

            // 1. Stone Podium & Wide Sanctuary Staircase
            CreateTexturedBoxPart(church.transform, "ChurchPlinth", new Vector3(0f, 0.42f, 0f),
                new Vector3(13.4f, 0.92f, 19.6f), stonePlinth, SurfacePattern.StoneAshlar, false, true, 0.14f, 0f, 0.54f, 0.36f);
            CreateTexturedBoxPart(church.transform, "PlinthWaterTable", new Vector3(0f, 0.90f, 0f),
                new Vector3(13.65f, 0.14f, 19.85f), LegoPalette.CloudWarmWhite, SurfacePattern.StoneAshlar, false, false, 0.16f);

            CreateTexturedBoxPart(church.transform, "FrontStair1", new Vector3(0f, 0.38f, 10.35f),
                new Vector3(4.8f, 0.56f, 1.15f), stonePlinth, SurfacePattern.StoneAshlar, false, true, 0.14f);
            CreateTexturedBoxPart(church.transform, "FrontStair2", new Vector3(0f, 0.18f, 11.35f),
                new Vector3(5.1f, 0.28f, 0.95f), stonePlinth, SurfacePattern.StoneAshlar, false, true, 0.14f);
            for (int bs = -1; bs <= 1; bs += 2)
            {
                CreateTexturedBoxPart(church.transform, $"StairPost_{bs}", new Vector3(bs * 2.55f, 0.75f, 11.15f),
                    new Vector3(0.45f, 1.35f, 0.45f), stonePlinth, SurfacePattern.StoneAshlar, false, false, 0.14f);
                CreateBoxPart(church.transform, $"StairLantern_{bs}", new Vector3(bs * 2.55f, 1.62f, 11.15f),
                    new Vector3(0.32f, 0.36f, 0.32f), LegoPalette.SunWarmGold, true, false, 0.25f);
            }

            // 2. Main Limestone Nave Hall + Rear Chancel Apse + Side Stone Buttresses
            float naveW = 10.6f;
            float naveH = 6.8f;
            float naveD = 16.4f;
            CreateTexturedBoxPart(church.transform, "NaveBody", new Vector3(0f, 0.92f + naveH * 0.5f, -0.4f),
                new Vector3(naveW, naveH, naveD), limestone, SurfacePattern.StoneAshlar, false, true, 0.14f, 0f, 0.50f, 0.28f);

            // Rear Chancel Apse projection
            CreateTexturedBoxPart(church.transform, "ApseBody", new Vector3(0f, 3.35f, -9.65f),
                new Vector3(6.8f, 5.2f, 2.8f), limestone, SurfacePattern.StoneAshlar, false, true, 0.14f, 0f, 0.50f, 0.28f);
            GameObject apseRoof = new GameObject("ApseGable");
            apseRoof.transform.SetParent(church.transform, false);
            apseRoof.transform.localPosition = new Vector3(0f, 5.95f, -9.65f);
            apseRoof.transform.localScale = new Vector3(7.1f, 1.85f, 3.0f);
            apseRoof.AddComponent<MeshFilter>().sharedMesh = GetGablePrismMesh();
            apseRoof.AddComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetTexturedLowPolyMaterial(roofRed, SurfacePattern.RoofTiles, 0.18f, 0f, 0.58f, 0.36f);

            for (int side = -1; side <= 1; side += 2)
            {
                for (int b = 0; b < 4; b++)
                {
                    float bz = -5.8f + b * 4.0f;
                    CreateTexturedBoxPart(church.transform, $"Buttress_{side}_{b}",
                        new Vector3(side * (naveW * 0.5f + 0.26f), 3.65f, bz),
                        new Vector3(0.56f, 5.6f, 0.65f), stonePlinth, SurfacePattern.StoneAshlar, false, false, 0.14f);
                }
                for (int w = 0; w < 3; w++)
                {
                    float wz = -3.8f + w * 4.0f;
                    CreateTexturedBoxPart(church.transform, $"LancetTrim_{side}_{w}",
                        new Vector3(side * (naveW * 0.5f + 0.05f), 4.25f, wz),
                        new Vector3(0.18f, 3.05f, 1.32f), stonePlinth, SurfacePattern.StoneAshlar, false, false, 0.14f);
                    CreateBoxPart(church.transform, $"StainedGlass_{side}_{w}",
                        new Vector3(side * (naveW * 0.5f + 0.10f), 4.25f, wz),
                        new Vector3(0.10f, 2.65f, 0.92f), (w % 2 == 0) ? stainBlue : LegoPalette.SunWarmGold, true, false, 0.65f);
                }
            }

            // 3. Nave Gable Prism + Dual Terracotta Pitched Roof + Ridge Coping
            float naveTopY = 0.92f + naveH;
            float pitchDeg = 28f;
            float roofRise = Mathf.Tan(pitchDeg * Mathf.Deg2Rad) * (naveW * 0.5f);
            GameObject naveGable = new GameObject("NaveGablePrism");
            naveGable.transform.SetParent(church.transform, false);
            naveGable.transform.localPosition = new Vector3(0f, naveTopY - 0.02f, -0.4f);
            naveGable.transform.localScale = new Vector3(naveW - 0.06f, roofRise, naveD - 0.06f);
            naveGable.AddComponent<MeshFilter>().sharedMesh = GetGablePrismMesh();
            naveGable.AddComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetTexturedLowPolyMaterial(limestone, SurfacePattern.StoneAshlar, 0.14f, 0f, 0.50f, 0.28f);

            float slopeLen = (naveW * 0.5f + 0.9f) / Mathf.Cos(pitchDeg * Mathf.Deg2Rad);
            float midX = naveW * 0.25f + 0.26f;
            float midY = naveTopY + roofRise * 0.5f - 0.04f;
            GameObject nrL = CreateTexturedBoxPart(church.transform, "NaveRoofL", new Vector3(-midX, midY, -0.4f),
                new Vector3(slopeLen, 0.34f, naveD + 1.2f), roofRed, SurfacePattern.RoofTiles, false, true, 0.18f, 0f, 0.56f, 0.36f);
            nrL.transform.localRotation = Quaternion.Euler(0f, 0f, pitchDeg);
            GameObject nrR = CreateTexturedBoxPart(church.transform, "NaveRoofR", new Vector3( midX, midY, -0.4f),
                new Vector3(slopeLen, 0.34f, naveD + 1.2f), roofRed, SurfacePattern.RoofTiles, false, true, 0.18f, 0f, 0.56f, 0.36f);
            nrR.transform.localRotation = Quaternion.Euler(0f, 0f, -pitchDeg);
            CreateTexturedBoxPart(church.transform, "NaveRidgeCap", new Vector3(0f, naveTopY + roofRise + 0.08f, -0.4f),
                new Vector3(0.45f, 0.30f, naveD + 1.35f), stonePlinth, SurfacePattern.StoneAshlar, false, false, 0.14f);

            // 4. Tall Front Bell Tower (z = +6.85f) + 4-Face Clock + Belfry Arches + Spire & Golden Cross
            float towerZ = 6.85f;
            CreateTexturedBoxPart(church.transform, "BellTowerLower", new Vector3(0f, 5.9f, towerZ),
                new Vector3(5.1f, 10.1f, 5.1f), limestone, SurfacePattern.StoneAshlar, false, true, 0.14f, 0f, 0.50f, 0.30f);
            CreateTexturedBoxPart(church.transform, "TowerBeltLedge", new Vector3(0f, 10.95f, towerZ),
                new Vector3(5.55f, 0.28f, 5.55f), stonePlinth, SurfacePattern.StoneAshlar, false, false, 0.14f);

            // Belfry Corner Columns & Hanging Bronze Bell (visible from outside)
            float belfryY = 12.65f;
            for (int cx = -1; cx <= 1; cx += 2)
            {
                for (int cz = -1; cz <= 1; cz += 2)
                {
                    CreateTexturedBoxPart(church.transform, $"BelfryPier_{cx}_{cz}",
                        new Vector3(cx * 2.0f, belfryY, towerZ + cz * 2.0f),
                        new Vector3(0.95f, 3.2f, 0.95f), limestone, SurfacePattern.StoneAshlar, false, true, 0.14f);
                }
            }
            CreateBoxPart(church.transform, "BelfryCoreDark", new Vector3(0f, belfryY, towerZ),
                new Vector3(2.8f, 2.8f, 2.8f), LegoPalette.CarbonBlack, false, false, 0.10f);
            GameObject bell = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(bell.GetComponent<Collider>());
            bell.name = "SanctuaryBell";
            bell.transform.SetParent(church.transform, false);
            bell.transform.localPosition = new Vector3(0f, belfryY + 0.1f, towerZ + 0.85f);
            bell.transform.localScale = new Vector3(0.95f, 0.72f, 0.95f);
            bell.GetComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetLowPolyMaterial(LegoPalette.GoldChrome, false, 0.75f);

            // Spire Cornice + Steep Copper/Slate Pyramid + Golden Cross (reaching ~22.6m!)
            CreateTexturedBoxPart(church.transform, "BelfryCrownLedge", new Vector3(0f, 14.35f, towerZ),
                new Vector3(5.7f, 0.34f, 5.7f), stonePlinth, SurfacePattern.StoneAshlar, false, false, 0.14f);
            GameObject spire = new GameObject("ChurchSpirePyramid");
            spire.transform.SetParent(church.transform, false);
            spire.transform.localPosition = new Vector3(0f, 14.48f, towerZ);
            spire.transform.localScale = new Vector3(3.15f, 7.6f, 3.15f);
            spire.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetLowPolyMountainMesh();
            spire.AddComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetTexturedLowPolyMaterial(spireTeal, SurfacePattern.RoofTiles, 0.20f, 0.12f, 0.56f, 0.30f);

            CreateBoxPart(church.transform, "CrossVert", new Vector3(0f, 22.1f, towerZ),
                new Vector3(0.24f, 2.1f, 0.24f), LegoPalette.GoldChrome, true, false, 0.82f);
            CreateBoxPart(church.transform, "CrossBar", new Vector3(0f, 22.45f, towerZ),
                new Vector3(1.32f, 0.24f, 0.24f), LegoPalette.GoldChrome, true, false, 0.82f);

            // 5. Monumental Front Sanctuary Door + Rose Window + Tower Clock Face
            float facadeZ = towerZ + 2.58f;
            CreateTexturedBoxPart(church.transform, "SanctuaryPortalFrame", new Vector3(0f, 2.45f, facadeZ),
                new Vector3(2.95f, 3.35f, 0.36f), stonePlinth, SurfacePattern.StoneAshlar, false, false, 0.14f);
            CreateTexturedBoxPart(church.transform, "SanctuaryOakDoor", new Vector3(0f, 2.30f, facadeZ + 0.08f),
                new Vector3(2.25f, 2.95f, 0.22f), woodDark, SurfacePattern.WoodPlanks, false, false, 0.15f, 0f, 1.0f, 0.40f);
            CreateBoxPart(church.transform, "DoorCenterMullion", new Vector3(0f, 2.30f, facadeZ + 0.20f),
                new Vector3(0.14f, 2.90f, 0.10f), stonePlinth, false, false, 0.14f);

            CreateTexturedBoxPart(church.transform, "RoseWindowRing", new Vector3(0f, 6.25f, facadeZ),
                new Vector3(2.35f, 2.35f, 0.28f), stonePlinth, SurfacePattern.StoneAshlar, false, false, 0.14f);
            CreateBoxPart(church.transform, "RoseWindowCore", new Vector3(0f, 6.25f, facadeZ + 0.12f),
                new Vector3(1.75f, 1.75f, 0.14f), LegoPalette.CyanGlow, true, false, 0.65f);
            CreateBoxPart(church.transform, "RoseWindowBarV", new Vector3(0f, 6.25f, facadeZ + 0.20f),
                new Vector3(0.14f, 1.85f, 0.10f), stonePlinth, false, false, 0.14f);
            CreateBoxPart(church.transform, "RoseWindowBarH", new Vector3(0f, 6.25f, facadeZ + 0.20f),
                new Vector3(1.85f, 0.14f, 0.10f), stonePlinth, false, false, 0.14f);

            // Tower clock on front face
            CreateBoxPart(church.transform, "ClockBezel", new Vector3(0f, 9.25f, facadeZ),
                new Vector3(2.15f, 2.15f, 0.22f), LegoPalette.DarkGrey, false, false, 0.35f);
            CreateBoxPart(church.transform, "ClockDial", new Vector3(0f, 9.25f, facadeZ + 0.12f),
                new Vector3(1.75f, 1.75f, 0.10f), LegoPalette.CloudWarmWhite, false, false, 0.20f);
            CreateBoxPart(church.transform, "ClockHandHour", new Vector3(0f, 9.50f, facadeZ + 0.18f),
                new Vector3(0.12f, 0.68f, 0.08f), LegoPalette.CarbonBlack, false, false, 0.25f);
            GameObject minHand = CreateBoxPart(church.transform, "ClockHandMin", new Vector3(0.24f, 9.25f, facadeZ + 0.18f),
                new Vector3(0.56f, 0.10f, 0.08f), LegoPalette.CarbonBlack, false, false, 0.25f);
            minHand.transform.localRotation = Quaternion.Euler(0f, 0f, -15f);
        }

        private static void BuildCommercialShop(Transform parent, Vector3 pos, Quaternion rot, int variantIndex)
        {
            GameObject shop = new GameObject("LowPolyCommercialShop");
            shop.transform.SetParent(parent, false);
            shop.transform.position = new Vector3(pos.x, 0f, pos.z);
            shop.transform.rotation = rot;

            int v = Mathf.Abs(variantIndex) % 4;
            string[] shopTitles = { "BAKERY & CAFE", "MOTOR SPEED SHOP", "VALLEY MARKET", "ALPINE COFFEE" };
            Color[] wallColors =
            {
                new Color(0.93f, 0.85f, 0.70f),
                new Color(0.78f, 0.81f, 0.85f),
                new Color(0.90f, 0.84f, 0.62f),
                new Color(0.80f, 0.88f, 0.84f)
            };
            Color[] roofColors =
            {
                LegoPalette.CanyonTerracotta,
                LegoPalette.DarkGrey,
                LegoPalette.WorkshopBrickRed,
                LegoPalette.WorkshopTeal
            };
            Color[] stripeA =
            {
                LegoPalette.BrightRed,
                LegoPalette.BrightBlue,
                LegoPalette.BrightGreen,
                LegoPalette.BrightOrange
            };

            Color wallCol = wallColors[v];
            Color roofCol = roofColors[v];
            Color awningColA = stripeA[v];
            Color awningColB = LegoPalette.CloudWarmWhite;
            Color stoneCol = LegoPalette.LowPolyRockGrey;
            Color timberCol = LegoPalette.WorkshopWood;
            Color glassCol = new Color(0.56f, 0.80f, 0.94f);

            float w = 9.8f;
            float d = 7.4f;
            float h = 6.9f;
            float frontZ = d * 0.5f;

            // 1. Stone Plinth + Forecourt Step + Ground & Second Story Massing
            CreateTexturedBoxPart(shop.transform, "ShopPlinth", new Vector3(0f, 0.28f, 0f),
                new Vector3(w + 0.45f, 0.62f, d + 0.45f), stoneCol, SurfacePattern.StoneAshlar, false, true, 0.14f, 0f, 0.58f, 0.34f);
            CreateTexturedBoxPart(shop.transform, "ForecourtStep", new Vector3(0f, 0.15f, frontZ + 0.72f),
                new Vector3(w + 0.35f, 0.30f, 1.05f), stoneCol, SurfacePattern.StoneAshlar, false, true, 0.14f);

            float groundH = 3.25f;
            float upperH = h - 0.62f - groundH;
            float groundY = 0.62f + groundH * 0.5f;
            float beltY = 0.62f + groundH;
            float upperY = beltY + upperH * 0.5f;

            CreateTexturedBoxPart(shop.transform, "GroundShopFront", new Vector3(0f, groundY, 0f),
                new Vector3(w, groundH, d), Color.Lerp(wallCol, stoneCol, 0.22f), SurfacePattern.BrickMasonry, false, true, 0.14f, 0f, 0.54f, 0.30f);
            CreateTexturedBoxPart(shop.transform, "BeltCourse", new Vector3(0f, beltY, 0f),
                new Vector3(w + 0.26f, 0.22f, d + 0.26f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.16f);
            CreateTexturedBoxPart(shop.transform, "UpperStory", new Vector3(0f, upperY, 0f),
                new Vector3(w - 0.06f, upperH, d - 0.06f), wallCol, SurfacePattern.WoodPlanks, false, true, 0.14f, 0f, 0.68f, 0.22f);

            for (int cx = -1; cx <= 1; cx += 2)
            {
                for (int cz = -1; cz <= 1; cz += 2)
                {
                    CreateTexturedBoxPart(shop.transform, $"ShopPost_{cx}_{cz}",
                        new Vector3(cx * (w * 0.5f - 0.04f), 0.62f + (h - 0.62f) * 0.5f, cz * (d * 0.5f - 0.04f)),
                        new Vector3(0.30f, h - 0.60f, 0.30f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.16f);
                }
            }

            // 2. Wide Illuminated Glass Display Storefronts + Recessed Entry Door
            for (int side = -1; side <= 1; side += 2)
            {
                float wx = side * 3.05f;
                CreateTexturedBoxPart(shop.transform, $"DisplayFrame_{side}", new Vector3(wx, 1.92f, frontZ + 0.04f),
                    new Vector3(2.85f, 2.05f, 0.18f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.16f);
                CreateBoxPart(shop.transform, $"DisplayWarmBack_{side}", new Vector3(wx, 1.92f, frontZ + 0.05f),
                    new Vector3(2.55f, 1.72f, 0.06f), LegoPalette.SunWarmGold, true, false, 0.25f);
                CreateBoxPart(shop.transform, $"DisplayGlass_{side}", new Vector3(wx, 1.92f, frontZ + 0.11f),
                    new Vector3(2.60f, 1.78f, 0.06f), glassCol, false, false, 0.85f);
            }

            CreateTexturedBoxPart(shop.transform, "ShopDoorFrame", new Vector3(0f, 1.78f, frontZ + 0.05f),
                new Vector3(1.75f, 2.32f, 0.18f), LegoPalette.CloudWarmWhite, SurfacePattern.StoneAshlar, false, false, 0.18f);
            CreateTexturedBoxPart(shop.transform, "ShopDoorLeaf", new Vector3(0f, 1.74f, frontZ + 0.09f),
                new Vector3(1.38f, 2.14f, 0.14f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.18f);
            CreateBoxPart(shop.transform, "ShopDoorGlass", new Vector3(0f, 2.08f, frontZ + 0.15f),
                new Vector3(0.88f, 0.85f, 0.06f), glassCol, false, false, 0.82f);

            // 3. Striped Fabric Awning (7 pitched slats) + Crisp Framed Shop Signboard
            for (int s = -3; s <= 3; s++)
            {
                GameObject slat = CreateBoxPart(shop.transform, $"AwningSlat_{s}",
                    new Vector3(s * 1.24f, 3.30f, frontZ + 0.72f),
                    new Vector3(1.25f, 0.12f, 1.55f),
                    ((s + 3) % 2 == 0) ? awningColA : awningColB, false, false, 0.14f);
                slat.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
            }

            CreateTexturedBoxPart(shop.transform, "SignBoard", new Vector3(0f, beltY + 0.18f, frontZ + 0.14f),
                new Vector3(6.8f, 0.82f, 0.18f), LegoPalette.CarbonBlack, SurfacePattern.WoodPlanks, false, false, 0.22f);
            GameObject signObj = new GameObject("ShopSignText");
            signObj.transform.SetParent(shop.transform, false);
            signObj.transform.localPosition = new Vector3(0f, beltY + 0.18f, frontZ + 0.28f);
            signObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            TextMesh tm = signObj.AddComponent<TextMesh>();
            tm.text = shopTitles[v];
            tm.fontSize = 40;
            tm.characterSize = 0.11f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = LegoPalette.SunWarmGold;

            // 4. Second Story Shuttered Windows + Flower Boxes + Pitched Roof + Brick Chimney
            for (int side = -1; side <= 1; side += 2)
            {
                float ux = side * 2.55f;
                CreateTexturedBoxPart(shop.transform, $"UpFrame_{side}", new Vector3(ux, upperY + 0.08f, frontZ + 0.04f),
                    new Vector3(1.48f, 1.52f, 0.16f), LegoPalette.CloudWarmWhite, SurfacePattern.StoneAshlar, false, false, 0.16f);
                CreateBoxPart(shop.transform, $"UpGlass_{side}", new Vector3(ux, upperY + 0.08f, frontZ + 0.10f),
                    new Vector3(1.16f, 1.20f, 0.06f), glassCol, false, false, 0.78f);
                CreateTexturedBoxPart(shop.transform, $"Planter_{side}", new Vector3(ux, upperY - 0.78f, frontZ + 0.22f),
                    new Vector3(1.52f, 0.26f, 0.36f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.16f);
            }

            float pitchDeg = 25f;
            float roofRise = Mathf.Tan(pitchDeg * Mathf.Deg2Rad) * (w * 0.5f);
            GameObject gable = new GameObject("ShopGablePrism");
            gable.transform.SetParent(shop.transform, false);
            gable.transform.localPosition = new Vector3(0f, h - 0.02f, 0f);
            gable.transform.localScale = new Vector3(w - 0.06f, roofRise, d - 0.06f);
            gable.AddComponent<MeshFilter>().sharedMesh = GetGablePrismMesh();
            gable.AddComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetTexturedLowPolyMaterial(wallCol, SurfacePattern.WoodPlanks, 0.14f, 0f, 0.68f, 0.22f);

            float slopeLen = (w * 0.5f + 0.85f) / Mathf.Cos(pitchDeg * Mathf.Deg2Rad);
            float midX = w * 0.25f + 0.25f;
            float midY = h + roofRise * 0.5f - 0.04f;
            GameObject srL = CreateTexturedBoxPart(shop.transform, "ShopRoofL", new Vector3(-midX, midY, 0f),
                new Vector3(slopeLen, 0.30f, d + 1.3f), roofCol, SurfacePattern.RoofTiles, false, true, 0.18f, 0f, 0.58f, 0.36f);
            srL.transform.localRotation = Quaternion.Euler(0f, 0f, pitchDeg);
            GameObject srR = CreateTexturedBoxPart(shop.transform, "ShopRoofR", new Vector3( midX, midY, 0f),
                new Vector3(slopeLen, 0.30f, d + 1.3f), roofCol, SurfacePattern.RoofTiles, false, true, 0.18f, 0f, 0.58f, 0.36f);
            srR.transform.localRotation = Quaternion.Euler(0f, 0f, -pitchDeg);

            CreateTexturedBoxPart(shop.transform, "ShopChimney", new Vector3(w * 0.30f, h + roofRise * 0.65f, -d * 0.22f),
                new Vector3(1.05f, 2.3f, 1.05f), LegoPalette.WorkshopBrickRed, SurfacePattern.BrickMasonry, false, false, 0.14f);

            // 5. Outdoor Street Prop Flavor (Cafe Bistro Table or Merch Crate Stack)
            if (v % 2 == 0)
            {
                CreateTexturedBoxPart(shop.transform, "BistroTable", new Vector3(-3.1f, 0.72f, frontZ + 1.85f),
                    new Vector3(1.1f, 0.10f, 1.1f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.16f);
                CreateBoxPart(shop.transform, "BistroLeg", new Vector3(-3.1f, 0.36f, frontZ + 1.85f),
                    new Vector3(0.18f, 0.68f, 0.18f), LegoPalette.DarkGrey, false, false, 0.25f);
            }
            else
            {
                CreateTexturedBoxPart(shop.transform, "CrateA", new Vector3(3.15f, 0.45f, frontZ + 1.65f),
                    new Vector3(0.95f, 0.65f, 0.75f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.14f);
                CreateBoxPart(shop.transform, "ShopMerchAccent", new Vector3(3.15f, 0.85f, frontZ + 1.65f),
                    new Vector3(0.65f, 0.24f, 0.55f), awningColA, false, false, 0.25f);
            }
        }

        private static void BuildInfieldAndOutskirtsArchitecture(
            Transform parent,
            BuiltTrackData data,
            float trackWidth,
            Vector3 startCenter,
            Vector3 startDir,
            Vector3 startRight,
            LegoWorldSceneryAnimator anim)
        {
            GameObject archRoot = new GameObject("Motorverse_InfieldAndOutskirts");
            archRoot.transform.SetParent(parent, false);

            List<Vector2> occupied = new List<Vector2>();
            void MarkProtected(Vector3 p)
            {
                if (p != Vector3.zero)
                {
                    occupied.Add(new Vector2(p.x, p.z));
                }
            }

            Vector3 outDir = -startRight;
            Vector3 cityCenter = data.hubCityCenter;
            Vector3 garageCenter = cityCenter - startDir * 22f + outDir * 6f;
            MarkProtected(cityCenter);
            MarkProtected(garageCenter);
            MarkProtected(data.garageEntryPos);
            MarkProtected(data.garageInteriorCarPos);
            MarkProtected(data.testArenaBounds.center);
            MarkProtected(data.hubLakePos);
            MarkProtected(data.hubMinePos);
            MarkProtected(data.characterSpawnPos);
            MarkProtected(data.raceEntryBeaconPos);
            MarkProtected(cityCenter - startDir * 12f + outDir * 30f);
            MarkProtected(cityCenter + startDir * 5f + outDir * 30f);
            MarkProtected(cityCenter - startDir * 29f + outDir * 28f);

            bool CanPlaceAt(Vector3 candidate, float minTrackClear, float minOtherClear)
            {
                if (IsTooCloseToTrack(data.waypoints, candidate, minTrackClear)) return false;
                Vector2 p2 = new Vector2(candidate.x, candidate.z);
                for (int i = 0; i < occupied.Count; i++)
                {
                    if ((p2 - occupied[i]).sqrMagnitude < minOtherClear * minOtherClear)
                    {
                        return false;
                    }
                }
                return true;
            }

            void RegisterPlaced(Vector3 candidate)
            {
                Vector2 p2 = new Vector2(candidate.x, candidate.z);
                occupied.Add(p2);
                if (data.extraBuildingClearCenters != null)
                {
                    data.extraBuildingClearCenters.Add(p2);
                }
            }

            // --- ZONE A: Central Infield Town Plaza around (0, 0, 0) ---
            CreateTexturedBoxPart(archRoot.transform, "InfieldTownPlazaStone", new Vector3(0f, -0.05f, 0f),
                new Vector3(54f, 0.12f, 54f), new Color(0.68f, 0.69f, 0.70f), SurfacePattern.StoneAshlar, false, true, 0.16f, 0f, 0.68f, 0.30f);
            CreateTexturedBoxPart(archRoot.transform, "InfieldBoulevardNS", new Vector3(0f, -0.06f, 0f),
                new Vector3(9.5f, 0.10f, 82f), new Color(0.65f, 0.66f, 0.67f), SurfacePattern.StoneAshlar, false, true, 0.16f, 0f, 0.68f, 0.28f);
            CreateTexturedBoxPart(archRoot.transform, "InfieldBoulevardEW", new Vector3(0f, -0.06f, 0f),
                new Vector3(82f, 0.10f, 9.5f), new Color(0.65f, 0.66f, 0.67f), SurfacePattern.StoneAshlar, false, true, 0.16f, 0f, 0.68f, 0.28f);

            BuildCityFountain(archRoot.transform, Vector3.zero);
            MarkProtected(Vector3.zero);
            for (int c = 0; c < 4; c++)
            {
                float ang = (c * 90f + 45f) * Mathf.Deg2Rad;
                Vector3 corner = new Vector3(Mathf.Cos(ang) * 9.5f, 0f, Mathf.Sin(ang) * 9.5f);
                Quaternion toCenter = Quaternion.LookRotation((Vector3.zero - corner).normalized, Vector3.up);
                BuildParkBench(archRoot.transform, corner, toCenter);
                BuildWalkwayBollardLantern(archRoot.transform, new Vector3(Mathf.Cos(ang) * 13.5f, 0f, Mathf.Sin(ang) * 13.5f));
                BuildDecorativeLegoTree(archRoot.transform, new Vector3(Mathf.Cos(ang) * 18.5f, 0f, Mathf.Sin(ang) * 18.5f), TerrainType.Street);
            }

            // Infield Church (facing plaza south -> +Z)
            Vector3 infieldChurchPos = new Vector3(0f, 0f, -24f);
            if (CanPlaceAt(infieldChurchPos, trackWidth * 0.5f + 18f, 12f))
            {
                BuildArchitecturalChurch(archRoot.transform, infieldChurchPos, Quaternion.identity);
                RegisterPlaced(infieldChurchPos);
            }

            // 4 Infield Commercial Shops ringing the plaza
            Vector3[] infShopPos =
            {
                new Vector3(-24f, 0f,  9f),
                new Vector3( 24f, 0f,  9f),
                new Vector3(-23f, 0f, -11f),
                new Vector3( 23f, 0f, -11f)
            };
            for (int i = 0; i < infShopPos.Length; i++)
            {
                Vector3 sp = infShopPos[i];
                if (CanPlaceAt(sp, trackWidth * 0.5f + 15f, 14f))
                {
                    Vector3 lookDir = new Vector3(-sp.x, 0f, -sp.z * 0.35f).normalized;
                    BuildCommercialShop(archRoot.transform, sp, Quaternion.LookRotation(lookDir, Vector3.up), i);
                    RegisterPlaced(sp);
                }
            }

            // 3 Infield Cozy Cottages outside the plaza boulevards
            Vector3[] infVillaPos =
            {
                new Vector3(  0f, 0f,  34f),
                new Vector3(-38f, 0f,  26f),
                new Vector3( 38f, 0f, -26f)
            };
            Color[] villaWalls =
            {
                new Color(0.88f, 0.80f, 0.65f),
                new Color(0.92f, 0.90f, 0.85f),
                new Color(0.86f, 0.74f, 0.60f)
            };
            Color[] villaRoofs = { LegoPalette.CanyonTerracotta, LegoPalette.WorkshopTeal, LegoPalette.WorkshopBrickRed };
            for (int i = 0; i < infVillaPos.Length; i++)
            {
                Vector3 vp = infVillaPos[i];
                if (CanPlaceAt(vp, trackWidth * 0.5f + 14f, 15f))
                {
                    Quaternion rot = Quaternion.LookRotation((-vp).normalized, Vector3.up);
                    BuildLowPolyVillageBuilding(archRoot.transform, vp, rot, villaWalls[i], villaRoofs[i], 9.8f, 7.2f, 5.4f);
                    RegisterPlaced(vp);
                }
            }

            // 2 Infield Animated Windmills on open meadow flanks inside track loop
            Vector3[] infMillPos = { new Vector3(-56f, 0f, 8f), new Vector3(56f, 0f, -6f) };
            for (int i = 0; i < infMillPos.Length; i++)
            {
                Vector3 mp = infMillPos[i];
                if (CanPlaceAt(mp, trackWidth * 0.5f + 16f, 18f))
                {
                    Quaternion rot = Quaternion.LookRotation((-mp).normalized, Vector3.up);
                    BuildAnimatedWindmill(archRoot.transform, mp, rot, anim, 10 + i);
                    RegisterPlaced(mp);
                }
            }

            // --- ZONE B: Motorverse Open-World Hub Perimeter ---
            Vector3 hubChurchPos = cityCenter + startDir * 44f + outDir * 52f;
            if (CanPlaceAt(hubChurchPos, trackWidth * 0.5f + 18f, 22f))
            {
                BuildArchitecturalChurch(archRoot.transform, hubChurchPos, Quaternion.LookRotation((-outDir - startDir * 0.25f).normalized, Vector3.up));
                RegisterPlaced(hubChurchPos);
            }

            Vector3 hubWindmillPos = cityCenter + startDir * 68f + outDir * 6f;
            if (CanPlaceAt(hubWindmillPos, trackWidth * 0.5f + 16f, 20f))
            {
                BuildAnimatedWindmill(archRoot.transform, hubWindmillPos, Quaternion.LookRotation((-startDir).normalized, Vector3.up), anim, 21);
                RegisterPlaced(hubWindmillPos);
            }

            Vector3[] hubShopPos =
            {
                cityCenter + startDir * 64f + outDir * 30f,
                cityCenter - startDir * 46f + outDir * 48f
            };
            for (int i = 0; i < hubShopPos.Length; i++)
            {
                Vector3 sp = hubShopPos[i];
                if (CanPlaceAt(sp, trackWidth * 0.5f + 15f, 19f))
                {
                    Vector3 faceHub = (cityCenter - sp);
                    faceHub.y = 0f;
                    BuildCommercialShop(archRoot.transform, sp, Quaternion.LookRotation(faceHub.normalized, Vector3.up), i + 1);
                    RegisterPlaced(sp);
                }
            }

            // --- ZONE C: Outer Trackside Perimeter Outskirts (windmills, wayside church, shops & cottages) ---
            int wpCount = data.waypoints.Count;
            if (wpCount >= 32)
            {
                int[] windmillSegs = { 6, 14, 21, 28 };
                for (int i = 0; i < windmillSegs.Length; i++)
                {
                    int seg = windmillSegs[i];
                    Vector3 p0 = data.waypoints[seg];
                    Vector3 p1 = data.waypoints[(seg + 1) % wpCount];
                    Vector3 mid = (p0 + p1) * 0.5f;
                    Vector3 outward = new Vector3(mid.x, 0f, mid.z).normalized;
                    Vector3 pos = mid + outward * (trackWidth * 0.5f + 36f);
                    if (CanPlaceAt(pos, trackWidth * 0.5f + 18f, 22f))
                    {
                        BuildAnimatedWindmill(archRoot.transform, pos, Quaternion.LookRotation(-outward, Vector3.up), anim, 30 + i);
                        RegisterPlaced(pos);
                    }
                }

                int waysideChurchSeg = 17;
                {
                    Vector3 p0 = data.waypoints[waysideChurchSeg];
                    Vector3 p1 = data.waypoints[(waysideChurchSeg + 1) % wpCount];
                    Vector3 mid = (p0 + p1) * 0.5f;
                    Vector3 outward = new Vector3(mid.x, 0f, mid.z).normalized;
                    Vector3 pos = mid + outward * (trackWidth * 0.5f + 38f);
                    if (CanPlaceAt(pos, trackWidth * 0.5f + 18f, 22f))
                    {
                        BuildArchitecturalChurch(archRoot.transform, pos, Quaternion.LookRotation(-outward, Vector3.up));
                        RegisterPlaced(pos);
                    }
                }

                int[] outerShopSegs = { 4, 10, 19, 25, 31 };
                for (int i = 0; i < outerShopSegs.Length; i++)
                {
                    int seg = outerShopSegs[i];
                    Vector3 p0 = data.waypoints[seg];
                    Vector3 p1 = data.waypoints[(seg + 1) % wpCount];
                    Vector3 mid = (p0 + p1) * 0.5f;
                    Vector3 outward = new Vector3(mid.x, 0f, mid.z).normalized;
                    Vector3 pos = mid + outward * (trackWidth * 0.5f + 35f);
                    if (CanPlaceAt(pos, trackWidth * 0.5f + 16f, 19f))
                    {
                        Quaternion rot = Quaternion.LookRotation(-outward, Vector3.up);
                        BuildCommercialShop(archRoot.transform, pos, rot, i);
                        BuildWalkwayBollardLantern(archRoot.transform, pos - outward * 6.5f);
                        RegisterPlaced(pos);
                    }
                }

                int[] outerVillaSegs = { 8, 12, 23, 30 };
                for (int i = 0; i < outerVillaSegs.Length; i++)
                {
                    int seg = outerVillaSegs[i];
                    Vector3 p0 = data.waypoints[seg];
                    Vector3 p1 = data.waypoints[(seg + 1) % wpCount];
                    Vector3 mid = (p0 + p1) * 0.5f;
                    Vector3 outward = new Vector3(mid.x, 0f, mid.z).normalized;
                    Vector3 pos = mid + outward * (trackWidth * 0.5f + 36f);
                    if (CanPlaceAt(pos, trackWidth * 0.5f + 16f, 19f))
                    {
                        BuildLowPolyVillageBuilding(archRoot.transform, pos, Quaternion.LookRotation(-outward, Vector3.up),
                            villaWalls[i % villaWalls.Length], villaRoofs[i % villaRoofs.Length], 10.2f, 7.6f, 5.6f);
                        RegisterPlaced(pos);
                    }
                }
            }
        }

        private static void BuildSculptedMeadowTerrain(
            Transform parent,
            BuiltTrackData data,
            float trackWidth,
            Vector3 startCenter,
            Vector3 startDir,
            Vector3 startRight)
        {
            const int gridN = 56;
            const float halfSpan = 250f;
            float step = (halfSpan * 2f) / gridN;

            Vector3 outDir = -startRight;
            Vector3 cityCenter = data.hubCityCenter;
            Vector2 aveStartXZ = new Vector2(cityCenter.x + startDir.x * 2f - outDir.x * 4f, cityCenter.z + startDir.z * 2f - outDir.z * 4f);
            Vector2 aveEndXZ = new Vector2(data.raceEntryBeaconPos.x, data.raceEntryBeaconPos.z);
            Vector2 driveStartXZ = new Vector2(data.garageEntryPos.x - startDir.x * 2f, data.garageEntryPos.z - startDir.z * 2f);
            Vector2 driveEndXZ = new Vector2(aveStartXZ.x + startDir.x * 6f, aveStartXZ.y + startDir.z * 6f);

            Vector3 garageCenter = cityCenter - startDir * 22f + outDir * 6f;
            Vector2 garageXZ = new Vector2(garageCenter.x, garageCenter.z);
            Vector2 wingXZ = new Vector2(garageCenter.x + startRight.x * 12.5f, garageCenter.z + startRight.z * 12.5f);
            Vector2 arenaXZ = new Vector2(data.testArenaBounds.center.x, data.testArenaBounds.center.z);
            Vector2 lakeXZ = new Vector2(data.hubLakePos.x, data.hubLakePos.z);
            Vector2 mineXZ = new Vector2(data.hubMinePos.x, data.hubMinePos.z);
            Vector2 spawnXZ = new Vector2(data.characterSpawnPos.x, data.characterSpawnPos.z);

            Vector2 cot1XZ = new Vector2(cityCenter.x - startDir.x * 12f + outDir.x * 30f, cityCenter.z - startDir.z * 12f + outDir.z * 30f);
            Vector2 cot2XZ = new Vector2(cityCenter.x + startDir.x * 5f + outDir.x * 30f, cityCenter.z + startDir.z * 5f + outDir.z * 30f);
            Vector2 cot3XZ = new Vector2(cityCenter.x - startDir.x * 29f + outDir.x * 28f, cityCenter.z - startDir.z * 29f + outDir.z * 28f);

            Vector2 pathP0 = new Vector2(data.characterSpawnPos.x + outDir.x * 5f, data.characterSpawnPos.z + outDir.z * 5f);
            Vector2 pathP1 = new Vector2(cityCenter.x - startDir.x * 4f + outDir.x * 21f, cityCenter.z - startDir.z * 4f + outDir.z * 21f);
            Vector2 pathP2 = new Vector2(data.hubLakePos.x + outDir.x * 10f + startDir.x * 16f, data.hubLakePos.z + outDir.z * 10f + startDir.z * 16f);

            float DistToSegmentXZ(Vector2 p, Vector2 a, Vector2 b)
            {
                Vector2 ab = b - a;
                float lenSq = ab.sqrMagnitude;
                float t = lenSq > 0.001f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / lenSq) : 0f;
                return Vector2.Distance(p, a + ab * t);
            }

            float Attenuate(float dist, float innerR, float feather)
            {
                return Mathf.SmoothStep(0f, 1f, (dist - innerR) / feather);
            }

            int wpCount = data.waypoints.Count;
            Vector2[] wpXZ = new Vector2[wpCount];
            for (int w = 0; w < wpCount; w++)
                wpXZ[w] = new Vector2(data.waypoints[w].x, data.waypoints[w].z);

            float roadExclusion = trackWidth * 0.5f + 2.5f;
            Vector3[,] grid = new Vector3[gridN + 1, gridN + 1];

            for (int iz = 0; iz <= gridN; iz++)
            {
                float wz = -halfSpan + iz * step;
                for (int ix = 0; ix <= gridN; ix++)
                {
                    float wx = -halfSpan + ix * step;
                    Vector2 p = new Vector2(wx, wz);

                    float mask = 1f;
                    float minTrackDist = float.MaxValue;
                    for (int wi = 0; wi < wpCount; wi++)
                    {
                        float d = DistToSegmentXZ(p, wpXZ[wi], wpXZ[(wi + 1) % wpCount]);
                        if (d < minTrackDist) minTrackDist = d;
                    }
                    mask = Mathf.Min(mask, Attenuate(minTrackDist, roadExclusion, 7.0f));
                    mask = Mathf.Min(mask, Attenuate(DistToSegmentXZ(p, aveStartXZ, aveEndXZ), 9.6f, 5.0f));
                    mask = Mathf.Min(mask, Attenuate(DistToSegmentXZ(p, driveStartXZ, driveEndXZ), 10.8f, 5.0f));
                    mask = Mathf.Min(mask, Attenuate(Vector2.Distance(p, garageXZ), 16.5f, 5.5f));
                    mask = Mathf.Min(mask, Attenuate(Vector2.Distance(p, wingXZ), 11.2f, 4.5f));
                    mask = Mathf.Min(mask, Attenuate(Vector2.Distance(p, arenaXZ), 23.0f, 5.0f));
                    mask = Mathf.Min(mask, Attenuate(Vector2.Distance(p, lakeXZ), 18.5f, 5.0f));
                    mask = Mathf.Min(mask, Attenuate(Vector2.Distance(p, mineXZ), 12.0f, 4.5f));
                    mask = Mathf.Min(mask, Attenuate(Vector2.Distance(p, cot1XZ), 9.5f, 4.0f));
                    mask = Mathf.Min(mask, Attenuate(Vector2.Distance(p, cot2XZ), 9.5f, 4.0f));
                    mask = Mathf.Min(mask, Attenuate(Vector2.Distance(p, cot3XZ), 9.5f, 4.0f));
                    mask = Mathf.Min(mask, Attenuate(DistToSegmentXZ(p, pathP0, pathP1), 2.8f, 3.0f));
                    mask = Mathf.Min(mask, Attenuate(DistToSegmentXZ(p, pathP1, pathP2), 2.8f, 3.0f));
                    mask = Mathf.Min(mask, Attenuate(p.magnitude, 42f, 12f));
                    if (data.extraBuildingClearCenters != null)
                    {
                        for (int bi = 0; bi < data.extraBuildingClearCenters.Count; bi++)
                        {
                            mask = Mathf.Min(mask, Attenuate(Vector2.Distance(p, data.extraBuildingClearCenters[bi]), 10.5f, 5.0f));
                        }
                    }

                    // Multi-octave Perlin + sinusoidal undulations creating crisp sunlit ridges & hollows ("lồi lõm")
                    float nBroad = Mathf.PerlinNoise(wx * 0.042f + 12.7f, wz * 0.042f + 34.1f);
                    float nKnoll = Mathf.PerlinNoise(wx * 0.135f + 71.3f, wz * 0.135f + 19.8f);
                    float wave = Mathf.Sin(wx * 0.085f + wz * 0.045f) * Mathf.Cos(wz * 0.075f - wx * 0.035f);
                    float rawOpen = -0.09f + Mathf.Clamp01(nBroad * 0.62f + nKnoll * 0.30f + (wave * 0.5f + 0.5f) * 0.08f) * 0.57f;

                    // Keep immediate spawn plaza gently rippled so character feet stay clean
                    float spawnOuter = Attenuate(Vector2.Distance(p, spawnXZ), 4.5f, 4.0f);
                    float spawnGentle = Mathf.Clamp(rawOpen, -0.085f, 0.045f);
                    float openY = Mathf.Lerp(spawnGentle, rawOpen, spawnOuter);

                    float wy = Mathf.Lerp(-0.22f, openY, mask);
                    grid[ix, iz] = new Vector3(wx, wy, wz);
                }
            }

            int triCount = gridN * gridN * 2;
            int vertCount = triCount * 3;
            Vector3[] vertices = new Vector3[vertCount];
            Color[] colors = new Color[vertCount];
            int[] triangles = new int[vertCount];

            Color cDeepClover = new Color(0.19f, 0.45f, 0.21f);
            Color cFreshGrass = new Color(0.30f, 0.57f, 0.24f);
            Color cWarmLime   = new Color(0.50f, 0.66f, 0.23f);
            Color cOliveMoss  = new Color(0.38f, 0.53f, 0.26f);
            Color cEarthyLoam = new Color(0.56f, 0.47f, 0.31f);

            int vIdx = 0;
            void EmitTriangle(Vector3 a, Vector3 b, Vector3 c, int seedHash)
            {
                Vector3 center = (a + b + c) / 3f;
                float hNorm = Mathf.Clamp01((center.y - (-0.08f)) / 0.52f);
                float patchA = Mathf.PerlinNoise(center.x * 0.032f + 19.3f, center.z * 0.032f + 7.1f);
                float patchB = Mathf.PerlinNoise(center.x * 0.085f + 61.7f, center.z * 0.085f + 41.2f);
                float loamField = Mathf.PerlinNoise(center.x * 0.058f + 113.5f, center.z * 0.058f + 29.8f);

                Color col = Color.Lerp(cDeepClover, cFreshGrass, Mathf.SmoothStep(0.10f, 0.58f, hNorm * 0.65f + patchA * 0.35f));
                col = Color.Lerp(col, cOliveMoss, Mathf.SmoothStep(0.45f, 0.85f, patchB) * 0.65f);
                col = Color.Lerp(col, cWarmLime, Mathf.SmoothStep(0.42f, 0.90f, hNorm * 0.72f + patchA * 0.28f));

                float loamMask = Mathf.SmoothStep(0.59f, 0.75f, loamField);
                col = Color.Lerp(col, cEarthyLoam, loamMask * 0.88f);

                float facetJitter = 0.96f + ((seedHash & 31) / 31f) * 0.08f;
                col *= facetJitter;
                col.a = 1f;

                vertices[vIdx] = a;
                vertices[vIdx + 1] = b;
                vertices[vIdx + 2] = c;
                colors[vIdx] = col;
                colors[vIdx + 1] = col;
                colors[vIdx + 2] = col;
                triangles[vIdx] = vIdx;
                triangles[vIdx + 1] = vIdx + 1;
                triangles[vIdx + 2] = vIdx + 2;
                vIdx += 3;
            }

            for (int iz = 0; iz < gridN; iz++)
            {
                for (int ix = 0; ix < gridN; ix++)
                {
                    Vector3 v00 = grid[ix, iz];
                    Vector3 v10 = grid[ix + 1, iz];
                    Vector3 v01 = grid[ix, iz + 1];
                    Vector3 v11 = grid[ix + 1, iz + 1];
                    int h = ((ix * 73856093) ^ (iz * 19349663)) & 0xFFFF;

                    if (((ix + iz) & 1) == 0)
                    {
                        EmitTriangle(v00, v01, v11, h);
                        EmitTriangle(v00, v11, v10, h ^ 17);
                    }
                    else
                    {
                        EmitTriangle(v00, v01, v10, h);
                        EmitTriangle(v10, v01, v11, h ^ 17);
                    }
                }
            }

            Mesh mesh = new Mesh { name = "LowPolySculptedMeadowMesh" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject meadow = new GameObject("LowPolySculptedMeadow");
            meadow.transform.SetParent(parent, false);
            meadow.AddComponent<MeshFilter>().sharedMesh = mesh;
            meadow.AddComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetTexturedLowPolyMaterial(
                    Color.white, SurfacePattern.MeadowTerrain, 0.11f, 0.0f, 0.30f, 0.78f);
        }

        private static void BuildMinecraftOpenWorldZone(
            Transform parent,
            Vector3 startCenter,
            Vector3 startDir,
            Vector3 startRight,
            float trackWidth,
            BuiltTrackData data,
            LegoWorldSceneryAnimator anim)
        {
            GameObject hubRoot = new GameObject("Motorverse_LowPoly_ValleyHub");
            hubRoot.transform.SetParent(parent, false);

            Vector3 outDir = -startRight;
            Vector3 cityCenter = startCenter - startDir * 6f + outDir * (trackWidth * 0.5f + 64f);
            cityCenter.y = -0.06f;
            data.hubCityCenter = cityCenter;

            // Keep exact raceEntryBeaconPos coordinate synchronized with MultiplayerServer/race-lobby.js
            data.raceEntryBeaconPos = startCenter - startDir * 5.5f + outDir * (trackWidth * 0.5f + 2.0f);

            // 1. Clean Textured Asphalt Motorverse Driveway & Courtyard connecting Garage, Arena, and Track Gate
            Material asphaltMat = LegoMeshAndMaterialFactory.GetTexturedLowPolyMaterial(
                LegoPalette.LowPolyAsphalt, SurfacePattern.AsphaltGrain, 0.16f, 0.0f, 0.56f, 0.26f);
            GameObject mainAve = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mainAve.name = "TrackAccessRoad";
            mainAve.transform.SetParent(hubRoot.transform, false);
            Vector3 aveStart = cityCenter + startDir * 2f - outDir * 4f;
            Vector3 aveEnd = data.raceEntryBeaconPos;
            Vector3 aveMid = (aveStart + aveEnd) * 0.5f;
            mainAve.transform.position = new Vector3(aveMid.x, -0.07f, aveMid.z);
            mainAve.transform.rotation = Quaternion.LookRotation((aveEnd - aveStart).normalized, Vector3.up);
            mainAve.transform.localScale = new Vector3(10.5f, 0.10f, Vector3.Distance(aveStart, aveEnd) + 6f);
            mainAve.GetComponent<MeshRenderer>().sharedMaterial = asphaltMat;
            mainAve.AddComponent<TerrainSurfaceTag>().terrainType = TerrainType.Street;

            // 2. Glowing Race Entry Beacon Ring at the Track Entrance (Cyan)
            GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(beacon.GetComponent<Collider>());
            beacon.name = "RaceStartZoneBeacon";
            beacon.transform.SetParent(hubRoot.transform, false);
            beacon.transform.position = data.raceEntryBeaconPos + Vector3.up * 0.10f;
            beacon.transform.localScale = new Vector3(LegoRaceLobby.EntryRadius * 2f, 0.12f, LegoRaceLobby.EntryRadius * 2f);
            beacon.GetComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetLowPolyMaterial(LegoPalette.CyanGlow, true, 0.35f);

            // 3. Motorverse Red-Brick Garage Workshop & Fenced Test Drive Arena
            Vector3 garageCenter = cityCenter - startDir * 22f + outDir * 6f;
            BuildMotorverseGarage(hubRoot.transform, garageCenter, startDir, data);

            GameObject driveway = GameObject.CreatePrimitive(PrimitiveType.Cube);
            driveway.name = "GarageCourtyardRoad";
            driveway.transform.SetParent(hubRoot.transform, false);
            Vector3 driveStart = data.garageEntryPos - startDir * 2f;
            Vector3 driveEnd = aveStart + startDir * 6f;
            Vector3 driveMid = (driveStart + driveEnd) * 0.5f;
            driveway.transform.position = new Vector3(driveMid.x, -0.07f, driveMid.z);
            driveway.transform.rotation = Quaternion.LookRotation((driveEnd - driveStart).normalized, Vector3.up);
            driveway.transform.localScale = new Vector3(13f, 0.10f, Vector3.Distance(driveStart, driveEnd) + 8f);
            driveway.GetComponent<MeshRenderer>().sharedMaterial = asphaltMat;
            driveway.AddComponent<TerrainSurfaceTag>().terrainType = TerrainType.Street;

            Vector3 arenaCenter = cityCenter + startDir * 28f + outDir * 14f;
            BuildFencedTestDriveArena(hubRoot.transform, arenaCenter, startDir, data);

            // Place Player Character ~16m straight in front of the Garage gate facing directly toward the workshop & mountains
            Vector3 gFwd = (data.garageEntryPos - data.garageInteriorCarPos);
            gFwd.y = 0f;
            gFwd = gFwd.sqrMagnitude > 0.01f ? gFwd.normalized : startDir;
            data.characterSpawnPos = data.garageEntryPos + gFwd * 16.5f + new Vector3(0f, 1.3f, 0f);
            Vector3 lookToGarage = (data.garageEntryPos - data.characterSpawnPos);
            lookToGarage.y = 0f;
            data.characterSpawnRot = lookToGarage.sqrMagnitude > 0.01f
                ? Quaternion.LookRotation(lookToGarage.normalized, Vector3.up)
                : Quaternion.LookRotation(-startDir, Vector3.up);

            // 4. Multi-Story Alpine Villas & Motorverse Valley Club Pavilion
            Quaternion faceRoad = Quaternion.LookRotation(-outDir, Vector3.up);
            BuildLowPolyVillageBuilding(hubRoot.transform, cityCenter - startDir * 12f + outDir * 30f, faceRoad,
                new Color(0.86f, 0.76f, 0.60f), LegoPalette.CanyonTerracotta, 10.6f, 7.8f, 5.8f);
            BuildLowPolyVillageBuilding(hubRoot.transform, cityCenter + startDir * 5f + outDir * 30f, faceRoad,
                new Color(0.92f, 0.90f, 0.85f), LegoPalette.WorkshopTeal, 9.6f, 7.2f, 5.3f);
            BuildLowPolyVillageBuilding(hubRoot.transform, cityCenter - startDir * 29f + outDir * 28f,
                Quaternion.LookRotation((-outDir - startDir * 0.25f).normalized, Vector3.up),
                new Color(0.84f, 0.72f, 0.56f), LegoPalette.WorkshopBrickRed, 11.4f, 8.2f, 6.2f);

            // 5. Scenic Alpine Lake with Post-and-Rail Wooden Fences & Wildflowers (far clear of track)
            Vector3 lakeCenter = cityCenter - startDir * 58f + outDir * 14f;
            data.hubLakePos = lakeCenter;
            if (!IsTooCloseToTrack(data.waypoints, lakeCenter, trackWidth * 0.5f + 22f))
            {
                BuildScenicAlpineLake(hubRoot.transform, lakeCenter, startDir);
            }

            // 6. Scenic Mine Tunnel Entrance built into a rocky bluff (visual landmark only — zero mining gameplay)
            Vector3 minePos = cityCenter + outDir * 46f - startDir * 8f;
            data.hubMinePos = minePos;
            if (!IsTooCloseToTrack(data.waypoints, minePos, trackWidth * 0.5f + 16f))
            {
                BuildScenicMineTunnel(hubRoot.transform, minePos, Quaternion.LookRotation(-outDir, Vector3.up));
            }

            // 7. Streetlights & Dense Low-Poly Pine Forest + Swaying Grass + Boulders + Wildflowers across Valley Hub
            BuildLegoStreetlight(hubRoot.transform, data.garageEntryPos + gFwd * 7f - startRight * 7.2f, Quaternion.LookRotation(startRight, Vector3.up));
            BuildLegoStreetlight(hubRoot.transform, data.garageEntryPos + gFwd * 7f + startRight * 7.2f, Quaternion.LookRotation(-startRight, Vector3.up));

            for (int r = 0; r < 48; r++)
            {
                float ang = r * 2.39996f;
                float rad = 18f + Mathf.Sqrt(r) * 8.2f;
                Vector3 nPos = cityCenter + startDir * (Mathf.Cos(ang) * rad) + outDir * (Mathf.Sin(ang) * rad);
                if (IsTooCloseToTrack(data.waypoints, nPos, trackWidth * 0.5f + 8f)) continue;
                if (Vector2.Distance(new Vector2(nPos.x, nPos.z), new Vector2(garageCenter.x, garageCenter.z)) < 23f) continue;
                if (Vector3.Distance(nPos, data.characterSpawnPos) < 6.5f) continue;
                if (Vector3.Distance(nPos, lakeCenter) < 20f) continue;
                if (Vector3.Distance(nPos, minePos) < 15f) continue;
                if (Vector2.Distance(new Vector2(nPos.x, nPos.z), new Vector2(arenaCenter.x, arenaCenter.z)) < 26f) continue;
                if (data.testArenaBounds.Contains(new Vector3(nPos.x, arenaCenter.y, nPos.z))) continue;

                BuildDecorativeLegoTree(hubRoot.transform, nPos, TerrainType.Street);
                BuildGrassTuft(hubRoot.transform, nPos + startDir * 2.1f - outDir * 1.4f, r * 3 + 1);
                BuildGrassTuft(hubRoot.transform, nPos - startDir * 1.9f + outDir * 2.2f, r * 3 + 2);
                if (r % 2 == 0) BuildLowPolyRock(hubRoot.transform, nPos + outDir * 3.4f, 1.3f + (r % 3) * 0.45f, r * 33f);
                if (r % 2 == 1) BuildWildflowerCluster(hubRoot.transform, nPos - startDir * 2.6f, r);
            }

            // 8. Stone-Curbed Pedestrian Sidewalks, Garden Footpaths, Benches, Swaying Grass & Wildflowers
            BuildPedestrianWalkwaysAndLushGroundCover(
                hubRoot.transform, data, cityCenter, startDir, outDir,
                garageCenter, arenaCenter, lakeCenter, minePos,
                aveStart, aveEnd, driveStart, driveEnd);
        }

        private static void BuildPedestrianWalkwaysAndLushGroundCover(
            Transform parent,
            BuiltTrackData data,
            Vector3 cityCenter,
            Vector3 startDir,
            Vector3 outDir,
            Vector3 garageCenter,
            Vector3 arenaCenter,
            Vector3 lakeCenter,
            Vector3 minePos,
            Vector3 aveStart,
            Vector3 aveEnd,
            Vector3 driveStart,
            Vector3 driveEnd)
        {
            GameObject walkwayRoot = new GameObject("PedestrianWalkways_And_LushFoliage");
            walkwayRoot.transform.SetParent(parent, false);

            Material stoneWalkMat = LegoMeshAndMaterialFactory.GetTexturedLowPolyMaterial(
                new Color(0.84f, 0.80f, 0.72f), SurfacePattern.StoneAshlar, 0.14f, 0.0f, 0.62f, 0.32f);
            Material curbStoneMat = LegoMeshAndMaterialFactory.GetTexturedLowPolyMaterial(
                LegoPalette.LowPolyRockGrey, SurfacePattern.StoneAshlar, 0.14f, 0.0f, 0.55f, 0.34f);

            bool IsSpotClearForDecor(Vector3 p, float garageExclusion)
            {
                if (IsTooCloseToTrack(data.waypoints, p, 12.5f)) return false;
                if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(garageCenter.x, garageCenter.z)) < garageExclusion) return false;
                if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(arenaCenter.x, arenaCenter.z)) < 20.5f) return false;
                if (data.testArenaBounds.Contains(new Vector3(p.x, arenaCenter.y, p.z))) return false;
                if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(lakeCenter.x, lakeCenter.z)) < 16.5f) return false;
                if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(minePos.x, minePos.z)) < 11.5f) return false;
                return true;
            }

            void BuildFlankingSidewalks(Vector3 segStart, Vector3 segEnd, float roadWidth, float startTrim, int seedOffset)
            {
                Vector3 span = segEnd - segStart;
                span.y = 0f;
                float totalLen = span.magnitude;
                if (totalLen <= startTrim + 4f) return;
                Vector3 fwd = span / totalLen;
                Vector3 rgt = Vector3.Cross(Vector3.up, fwd).normalized;
                Vector3 trimmedStart = segStart + fwd * startTrim;
                float len = totalLen - startTrim;
                Vector3 mid = (trimmedStart + segEnd) * 0.5f;
                Quaternion rot = Quaternion.LookRotation(fwd, Vector3.up);

                for (int side = -1; side <= 1; side += 2)
                {
                    float walkOffset = side * (roadWidth * 0.5f + 1.55f);
                    float curbOffset = side * (roadWidth * 0.5f + 0.22f);

                    GameObject curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.Destroy(curb.GetComponent<Collider>());
                    curb.name = $"WalkwayCurb_{seedOffset}_{side}";
                    curb.transform.SetParent(walkwayRoot.transform, false);
                    curb.transform.SetPositionAndRotation(new Vector3(mid.x, -0.055f, mid.z) + rgt * curbOffset, rot);
                    curb.transform.localScale = new Vector3(0.32f, 0.11f, len);
                    curb.GetComponent<MeshRenderer>().sharedMaterial = curbStoneMat;

                    GameObject walk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.Destroy(walk.GetComponent<Collider>());
                    walk.name = $"PedestrianWalkway_{seedOffset}_{side}";
                    walk.transform.SetParent(walkwayRoot.transform, false);
                    walk.transform.SetPositionAndRotation(new Vector3(mid.x, -0.06f, mid.z) + rgt * walkOffset, rot);
                    walk.transform.localScale = new Vector3(2.55f, 0.08f, len);
                    walk.GetComponent<MeshRenderer>().sharedMaterial = stoneWalkMat;

                    // Dense swaying grass tufts & multi-color wildflowers lining walkway border
                    int steps = Mathf.Max(4, Mathf.FloorToInt(len / 2.8f));
                    for (int s = 0; s <= steps; s++)
                    {
                        float dAlong = (s / (float)steps) * len;
                        Vector3 basePt = trimmedStart + fwd * dAlong;
                        Vector3 vergeInner = basePt + rgt * (side * (roadWidth * 0.5f + 3.25f));
                        Vector3 vergeOuter = basePt + rgt * (side * (roadWidth * 0.5f + 4.35f));

                        if (IsSpotClearForDecor(vergeInner, 13.5f))
                        {
                            BuildGrassTuft(walkwayRoot.transform, vergeInner, seedOffset * 41 + s * 5 + side);
                            if ((s + (side > 0 ? 1 : 0)) % 2 == 0)
                            {
                                BuildWildflowerCluster(walkwayRoot.transform, vergeInner + fwd * 0.85f, seedOffset * 19 + s + side);
                            }
                        }

                        if (IsSpotClearForDecor(vergeOuter, 14.5f) && s % 2 == 1)
                        {
                            BuildGrassTuft(walkwayRoot.transform, vergeOuter, seedOffset * 53 + s * 7 - side);
                        }

                        // Walkway bollard lamps & wood-slat park benches along open stretches
                        if (s > 0 && s < steps && s % 4 == 2 && IsSpotClearForDecor(vergeInner, 15.0f))
                        {
                            BuildWalkwayBollardLantern(walkwayRoot.transform, basePt + rgt * (side * (roadWidth * 0.5f + 2.55f)));
                        }
                        if (side == 1 && s > 1 && s < steps - 1 && s % 6 == 3)
                        {
                            Vector3 benchPos = basePt + rgt * (roadWidth * 0.5f + 2.45f);
                            if (IsSpotClearForDecor(benchPos, 16.5f))
                            {
                                BuildParkBench(walkwayRoot.transform, benchPos, Quaternion.LookRotation(-rgt, Vector3.up));
                            }
                        }
                    }
                }
            }

            BuildFlankingSidewalks(driveStart, driveEnd, 13.0f, 11.5f, 1);
            BuildFlankingSidewalks(aveStart, aveEnd, 10.5f, 2.5f, 2);

            // 2. Curved Scenic Stepping-Stone Garden Path linking Spawn Plaza -> Alpine Villas -> Alpine Lake
            Vector3 pathP0 = new Vector3(data.characterSpawnPos.x, 0f, data.characterSpawnPos.z) + outDir * 5.0f;
            Vector3 pathP1 = cityCenter - startDir * 4f + outDir * 21f;
            Vector3 pathP2 = lakeCenter + outDir * 10f + startDir * 16f;
            int paverCount = 16;
            for (int i = 0; i <= paverCount; i++)
            {
                float t = i / (float)paverCount;
                float u = 1f - t;
                Vector3 pt = u * u * pathP0 + 2f * u * t * pathP1 + t * t * pathP2;
                Vector3 tangent = (2f * u * (pathP1 - pathP0) + 2f * t * (pathP2 - pathP1)).normalized;
                Vector3 pRight = Vector3.Cross(Vector3.up, tangent).normalized;

                if (!IsSpotClearForDecor(pt, 14.5f)) continue;
                GameObject paver = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(paver.GetComponent<Collider>());
                paver.name = $"ParkStonePaver_{i}";
                paver.transform.SetParent(walkwayRoot.transform, false);
                paver.transform.SetPositionAndRotation(new Vector3(pt.x, -0.06f, pt.z), Quaternion.LookRotation(tangent, Vector3.up));
                paver.transform.localScale = new Vector3(2.35f, 0.08f, 2.25f);
                paver.GetComponent<MeshRenderer>().sharedMaterial = stoneWalkMat;

                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 edgePt = pt + pRight * (side * 1.65f);
                    BuildGrassTuft(walkwayRoot.transform, edgePt, i * 11 + side + 100);
                    if ((i + side) % 2 == 0)
                        BuildWildflowerCluster(walkwayRoot.transform, edgePt + tangent * 0.65f, i * 7 + side + 50);
                }

                if (i == 5 || i == 11)
                {
                    BuildParkBench(walkwayRoot.transform, pt + pRight * 2.15f, Quaternion.LookRotation(-pRight, Vector3.up));
                    BuildWalkwayBollardLantern(walkwayRoot.transform, pt - pRight * 1.65f);
                }
            }

            bool IsClearOfExtraBuildings(Vector3 gp, float radius)
            {
                if (data.extraBuildingClearCenters == null) return true;
                Vector2 gXZ = new Vector2(gp.x, gp.z);
                float rSq = radius * radius;
                for (int i = 0; i < data.extraBuildingClearCenters.Count; i++)
                {
                    if ((gXZ - data.extraBuildingClearCenters[i]).sqrMagnitude < rSq)
                        return false;
                }
                return true;
            }

            // 3. Meadow Grass & Wildflower Carpet across Valley Hub Courtyard & Lawn
            Vector3 lawnCenter = new Vector3(data.characterSpawnPos.x, 0f, data.characterSpawnPos.z);
            for (int g = 0; g < 150; g++)
            {
                float ang = g * 2.39996f;
                float rad = 4.2f + Mathf.Sqrt(g) * 5.2f;
                Vector3 gp = lawnCenter + startDir * (Mathf.Cos(ang) * rad) + outDir * (Mathf.Sin(ang) * rad);
                // Keep directly centered asphalt driveway band clear of grass poking up through asphalt
                Vector3 toDrive = gp - driveStart;
                Vector3 dFwd = (driveEnd - driveStart);
                dFwd.y = 0f;
                if (dFwd.sqrMagnitude > 0.01f)
                {
                    dFwd.Normalize();
                    Vector3 dRgt = Vector3.Cross(Vector3.up, dFwd).normalized;
                    float proj = Vector3.Dot(toDrive, dFwd);
                    float lat = Mathf.Abs(Vector3.Dot(toDrive, dRgt));
                    if (proj >= -4f && proj <= Vector3.Distance(driveStart, driveEnd) + 4f && lat < 7.6f)
                        continue;
                }
                if (!IsSpotClearForDecor(gp, 14.5f) || !IsClearOfExtraBuildings(gp, 11.0f)) continue;

                BuildGrassTuft(walkwayRoot.transform, gp, 300 + g);
                if (g % 3 == 0)
                    BuildWildflowerCluster(walkwayRoot.transform, gp + outDir * 0.9f, 400 + g);
            }

            // 4. Infield Meadow Grass & Wildflower Pass across central track interior
            for (int g = 0; g < 120; g++)
            {
                float ang = g * 2.39996f + 0.5f;
                float rad = Mathf.Lerp(29f, 92f, g / 119f);
                Vector3 gp = new Vector3(Mathf.Cos(ang) * rad, 0f, Mathf.Sin(ang) * rad);
                if (gp.magnitude < 28.5f) continue;
                if (Mathf.Max(Mathf.Abs(gp.x), Mathf.Abs(gp.z)) < 28.5f) continue;
                if (Mathf.Abs(gp.x) < 5.6f && Mathf.Abs(gp.z) < 42.5f) continue;
                if (Mathf.Abs(gp.z) < 5.6f && Mathf.Abs(gp.x) < 42.5f) continue;
                if (IsTooCloseToTrack(data.waypoints, gp, 13.5f)) continue;
                if (!IsClearOfExtraBuildings(gp, 11.0f)) continue;

                BuildGrassTuft(walkwayRoot.transform, gp, 700 + g);
                if (g % 4 == 0)
                    BuildWildflowerCluster(walkwayRoot.transform, gp + new Vector3(0.8f, 0f, -0.7f), 900 + g);
            }
        }

        private static void BuildWalkwayBollardLantern(Transform parent, Vector3 pos)
        {
            GameObject bol = new GameObject("WalkwayBollardLantern");
            bol.transform.SetParent(parent, false);
            bol.transform.position = new Vector3(pos.x, -0.06f, pos.z);
            CreateTexturedBoxPart(bol.transform, "Post", new Vector3(0f, 0.45f, 0f),
                new Vector3(0.24f, 0.90f, 0.24f), LegoPalette.CarbonBlack, SurfacePattern.None, false, false, 0.30f, 0.25f);
            CreateBoxPart(bol.transform, "GlowLens", new Vector3(0f, 0.92f, 0f),
                new Vector3(0.20f, 0.20f, 0.20f), LegoPalette.SunWarmGold, true, false, 0.25f);
            CreateTexturedBoxPart(bol.transform, "Cap", new Vector3(0f, 1.05f, 0f),
                new Vector3(0.30f, 0.08f, 0.30f), LegoPalette.CarbonBlack, SurfacePattern.None, false, false, 0.30f, 0.25f);
        }

        private static void BuildParkBench(Transform parent, Vector3 pos, Quaternion rot)
        {
            GameObject bench = new GameObject("LowPolyParkBench");
            bench.transform.SetParent(parent, false);
            bench.transform.SetPositionAndRotation(new Vector3(pos.x, -0.05f, pos.z), rot);

            CreateTexturedBoxPart(bench.transform, "LegL", new Vector3(-0.78f, 0.24f, 0f),
                new Vector3(0.14f, 0.48f, 0.56f), LegoPalette.CarbonBlack, SurfacePattern.None, false, false, 0.28f, 0.25f);
            CreateTexturedBoxPart(bench.transform, "LegR", new Vector3( 0.78f, 0.24f, 0f),
                new Vector3(0.14f, 0.48f, 0.56f), LegoPalette.CarbonBlack, SurfacePattern.None, false, false, 0.28f, 0.25f);
            CreateTexturedBoxPart(bench.transform, "Seat", new Vector3(0f, 0.46f, 0.04f),
                new Vector3(1.92f, 0.12f, 0.58f), LegoPalette.WorkshopWood, SurfacePattern.WoodPlanks, false, false, 0.18f, 0f, 0.95f, 0.38f);
            GameObject back = CreateTexturedBoxPart(bench.transform, "BackRest", new Vector3(0f, 0.80f, -0.24f),
                new Vector3(1.92f, 0.42f, 0.10f), LegoPalette.WorkshopWood, SurfacePattern.WoodPlanks, false, false, 0.18f, 0f, 0.95f, 0.38f);
            back.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
        }

        private static GameObject CreateBoxPart(Transform parent, string name, Vector3 localPos, Vector3 scale, Color color, bool emissive = false, bool hasCollider = true, float smoothness = 0.18f)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (!hasCollider) Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetLowPolyMaterial(color, emissive, smoothness);
            return go;
        }

        private static GameObject CreateTexturedBoxPart(
            Transform parent,
            string name,
            Vector3 localPos,
            Vector3 scale,
            Color color,
            SurfacePattern pattern,
            bool emissive = false,
            bool hasCollider = true,
            float smoothness = 0.18f,
            float metallic = 0.0f,
            float patternScale = 0.48f,
            float patternStrength = 0.34f)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (!hasCollider) Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetTexturedLowPolyMaterial(
                    color, pattern, smoothness, metallic, patternScale, patternStrength, 0.0f, emissive);
            return go;
        }

        private static Light AddWorkshopPointLight(Transform parent, string name, Vector3 localPos, Color color, float intensity, float range)
        {
            GameObject lObj = new GameObject(name);
            lObj.transform.SetParent(parent, false);
            lObj.transform.localPosition = localPos;
            Light l = lObj.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            return l;
        }

        private static void BuildMotorverseGarage(Transform parent, Vector3 garageCenter, Vector3 garageForward, BuiltTrackData data)
        {
            GameObject garage = new GameObject("Motorverse_RedBrick_Garage");
            garage.transform.SetParent(parent, false);
            garage.transform.position = new Vector3(garageCenter.x, 0f, garageCenter.z);
            garage.transform.rotation = Quaternion.LookRotation(garageForward, Vector3.up);

            // Textured Concrete Floor Bay, Apron & Safety Yellow Markings
            CreateTexturedBoxPart(garage.transform, "GarageFloor", new Vector3(0f, -0.03f, 0f),
                new Vector3(17f, 0.12f, 17f), LegoPalette.WorkshopConcrete, SurfacePattern.StoneAshlar, false, true, 0.16f, 0f, 0.50f, 0.25f);
            CreateTexturedBoxPart(garage.transform, "FrontApronConcrete", new Vector3(0f, -0.04f, 11.4f),
                new Vector3(19.6f, 0.10f, 6.6f), LegoPalette.WorkshopConcrete, SurfacePattern.StoneAshlar, false, true, 0.16f, 0f, 0.48f, 0.25f);
            CreateBoxPart(garage.transform, "BayLineL", new Vector3(-6.2f, 0.04f, 0f), new Vector3(0.22f, 0.03f, 13.5f), LegoPalette.BrightYellow, false, false, 0.16f);
            CreateBoxPart(garage.transform, "BayLineR", new Vector3( 6.2f, 0.04f, 0f), new Vector3(0.22f, 0.03f, 13.5f), LegoPalette.BrightYellow, false, false, 0.16f);
            CreateBoxPart(garage.transform, "BayLineBack", new Vector3(0f, 0.04f, -6.5f), new Vector3(12.6f, 0.03f, 0.22f), LegoPalette.BrightYellow, false, false, 0.16f);

            // Ashlar Stone Plinth Base + Red-Brick Masonry Walls + Limestone Cornices
            Color brickRed = LegoPalette.WorkshopBrickRed;
            Color pilasterRed = brickRed * 0.84f;
            Color stoneCol = LegoPalette.LowPolyRockGrey;
            Color trimStoneCol = LegoPalette.CloudWarmWhite;

            // Stone plinth & cap trim wrapping main perimeter
            CreateTexturedBoxPart(garage.transform, "PlinthBack", new Vector3(0f, 0.58f, -8.0f), new Vector3(17.25f, 1.16f, 1.05f), stoneCol, SurfacePattern.StoneAshlar, false, false, 0.14f, 0f, 0.55f, 0.36f);
            CreateTexturedBoxPart(garage.transform, "PlinthLeft", new Vector3(-8.1f, 0.58f, 0f), new Vector3(1.05f, 1.16f, 16.25f), stoneCol, SurfacePattern.StoneAshlar, false, false, 0.14f, 0f, 0.55f, 0.36f);
            CreateTexturedBoxPart(garage.transform, "PlinthRight", new Vector3( 8.1f, 0.58f, 0f), new Vector3(1.05f, 1.16f, 16.25f), stoneCol, SurfacePattern.StoneAshlar, false, false, 0.14f, 0f, 0.55f, 0.36f);
            CreateTexturedBoxPart(garage.transform, "PlinthFrontL", new Vector3(-7.0f, 0.58f, 8.0f), new Vector3(3.05f, 1.16f, 1.12f), stoneCol, SurfacePattern.StoneAshlar, false, false, 0.14f, 0f, 0.55f, 0.36f);
            CreateTexturedBoxPart(garage.transform, "PlinthFrontR", new Vector3( 7.0f, 0.58f, 8.0f), new Vector3(3.05f, 1.16f, 1.12f), stoneCol, SurfacePattern.StoneAshlar, false, false, 0.14f, 0f, 0.55f, 0.36f);
            CreateTexturedBoxPart(garage.transform, "WaterTableL", new Vector3(-8.12f, 1.20f, 0f), new Vector3(1.16f, 0.14f, 16.4f), trimStoneCol, SurfacePattern.StoneAshlar, false, false, 0.18f);
            CreateTexturedBoxPart(garage.transform, "WaterTableR", new Vector3( 8.12f, 1.20f, 0f), new Vector3(1.16f, 0.14f, 16.4f), trimStoneCol, SurfacePattern.StoneAshlar, false, false, 0.18f);

            // Brick Masonry Walls
            CreateTexturedBoxPart(garage.transform, "BackWall", new Vector3(0f, 4.18f, -8.0f), new Vector3(17.0f, 6.05f, 0.82f), brickRed, SurfacePattern.BrickMasonry, false, true, 0.14f, 0f, 0.62f, 0.38f);
            CreateTexturedBoxPart(garage.transform, "LeftWall", new Vector3(-8.1f, 4.18f, 0f), new Vector3(0.82f, 6.05f, 16.0f), brickRed, SurfacePattern.BrickMasonry, false, true, 0.14f, 0f, 0.62f, 0.38f);
            CreateTexturedBoxPart(garage.transform, "RightWall", new Vector3( 8.1f, 4.18f, 0f), new Vector3(0.82f, 6.05f, 16.0f), brickRed, SurfacePattern.BrickMasonry, false, true, 0.14f, 0f, 0.62f, 0.38f);
            CreateTexturedBoxPart(garage.transform, "FrontPillarL", new Vector3(-7.0f, 4.18f, 8.0f), new Vector3(2.8f, 6.05f, 0.90f), brickRed, SurfacePattern.BrickMasonry, false, true, 0.14f, 0f, 0.62f, 0.38f);
            CreateTexturedBoxPart(garage.transform, "FrontPillarR", new Vector3( 7.0f, 4.18f, 8.0f), new Vector3(2.8f, 6.05f, 0.90f), brickRed, SurfacePattern.BrickMasonry, false, true, 0.14f, 0f, 0.62f, 0.38f);

            // 4 Brick Corner Pilasters with Stone Capitals
            Vector3[] pilPos = { new Vector3(-8.2f, 3.65f, 8.1f), new Vector3(8.2f, 3.65f, 8.1f), new Vector3(-8.2f, 3.65f, -8.1f), new Vector3(8.2f, 3.65f, -8.1f) };
            for (int p = 0; p < pilPos.Length; p++)
            {
                CreateTexturedBoxPart(garage.transform, $"CornerPilaster_{p}", pilPos[p], new Vector3(0.84f, 7.2f, 1.08f), pilasterRed, SurfacePattern.BrickMasonry, false, false, 0.14f, 0f, 0.62f, 0.38f);
                CreateTexturedBoxPart(garage.transform, $"PilasterCap_{p}", new Vector3(pilPos[p].x, 7.22f, pilPos[p].z),
                    new Vector3(1.02f, 0.24f, 1.24f), trimStoneCol, SurfacePattern.StoneAshlar, false, false, 0.18f);
            }

            // Two Mullioned Showroom Windows on Left Wall + Back Clerestory Windows
            float[] leftWinZ = { -2.4f, 2.6f };
            for (int w = 0; w < leftWinZ.Length; w++)
            {
                float wz = leftWinZ[w];
                CreateTexturedBoxPart(garage.transform, $"LeftWinSill_{w}", new Vector3(-8.58f, 2.02f, wz), new Vector3(0.32f, 0.18f, 3.35f), trimStoneCol, SurfacePattern.StoneAshlar, false, false, 0.18f);
                CreateTexturedBoxPart(garage.transform, $"LeftWinFrame_{w}", new Vector3(-8.52f, 3.25f, wz), new Vector3(0.18f, 2.30f, 3.10f), trimStoneCol, SurfacePattern.None, false, false, 0.18f);
                CreateTexturedBoxPart(garage.transform, $"LeftWinGlass_{w}", new Vector3(-8.58f, 3.25f, wz), new Vector3(0.12f, 1.88f, 2.70f), new Color(0.58f, 0.80f, 0.92f), SurfacePattern.None, false, false, 0.86f, 0.28f);
                CreateTexturedBoxPart(garage.transform, $"LeftWinMullionV_{w}", new Vector3(-8.61f, 3.25f, wz), new Vector3(0.10f, 1.92f, 0.10f), LegoPalette.CarbonBlack, SurfacePattern.None, false, false, 0.24f, 0.25f);
                CreateTexturedBoxPart(garage.transform, $"LeftWinMullionH_{w}", new Vector3(-8.61f, 3.25f, wz), new Vector3(0.10f, 0.10f, 2.74f), LegoPalette.CarbonBlack, SurfacePattern.None, false, false, 0.24f, 0.25f);
            }
            for (int bx = -1; bx <= 1; bx += 2)
            {
                CreateTexturedBoxPart(garage.transform, $"BackClerestoryFrame_{bx}", new Vector3(bx * 4.2f, 5.4f, -8.42f), new Vector3(3.4f, 1.25f, 0.18f), trimStoneCol, SurfacePattern.None, false, false, 0.18f);
                CreateTexturedBoxPart(garage.transform, $"BackClerestoryGlass_{bx}", new Vector3(bx * 4.2f, 5.4f, -8.46f), new Vector3(3.0f, 0.92f, 0.12f), new Color(0.62f, 0.82f, 0.94f), SurfacePattern.None, false, false, 0.86f, 0.28f);
            }

            CreateTexturedBoxPart(garage.transform, "FrontHeaderTeal", new Vector3(0f, 5.80f, 8.0f), new Vector3(12.2f, 0.82f, 1.12f), LegoPalette.WorkshopTeal, SurfacePattern.WoodPlanks, false, true, 0.16f, 0f, 0.65f, 0.25f);
            CreateTexturedBoxPart(garage.transform, "FrontCornice", new Vector3(0f, 7.12f, 8.08f), new Vector3(17.4f, 0.28f, 1.18f), trimStoneCol, SurfacePattern.StoneAshlar, false, false, 0.18f);

            // Seamless Triangular Brick Gables matching 16-deg roof pitch
            float garageRoofRise = Mathf.Tan(16f * Mathf.Deg2Rad) * 8.5f;
            Material brickGableMat = LegoMeshAndMaterialFactory.GetTexturedLowPolyMaterial(brickRed, SurfacePattern.BrickMasonry, 0.14f, 0f, 0.62f, 0.38f);
            foreach (float gz in new float[] { 8.0f, -8.0f })
            {
                GameObject gable = new GameObject(gz > 0f ? "FrontPedimentGable" : "BackPedimentGable");
                gable.transform.SetParent(garage.transform, false);
                gable.transform.localPosition = new Vector3(0f, 7.20f, gz);
                gable.transform.localScale = new Vector3(17.0f, garageRoofRise, 0.82f);
                gable.AddComponent<MeshFilter>().sharedMesh = GetGablePrismMesh();
                gable.AddComponent<MeshRenderer>().sharedMaterial = brickGableMat;
            }

            // Cantilevered Steel & Glass Front Entrance Awning + Tie Rods
            CreateTexturedBoxPart(garage.transform, "FrontAwningFrame", new Vector3(0f, 5.38f, 9.35f),
                new Vector3(12.2f, 0.16f, 2.5f), LegoPalette.CarbonBlack, SurfacePattern.None, false, false, 0.30f, 0.35f);
            CreateTexturedBoxPart(garage.transform, "FrontAwningGlass", new Vector3(0f, 5.42f, 9.35f),
                new Vector3(11.6f, 0.08f, 2.2f), new Color(0.55f, 0.78f, 0.90f), SurfacePattern.None, false, false, 0.86f, 0.25f);
            for (int tr = -1; tr <= 1; tr += 2)
            {
                GameObject rod = CreateTexturedBoxPart(garage.transform, $"AwningTieRod_{tr}", new Vector3(tr * 4.8f, 6.22f, 9.15f),
                    new Vector3(0.08f, 0.08f, 2.35f), LegoPalette.SilverMetallic, SurfacePattern.None, false, false, 0.55f, 0.4f);
                rod.transform.localRotation = Quaternion.Euler(42f, 0f, 0f);

                // Stone Entrance Flower Planters flanking main doorway piers
                CreateTexturedBoxPart(garage.transform, $"EntrancePlanter_{tr}", new Vector3(tr * 7.8f, 0.40f, 9.05f),
                    new Vector3(1.55f, 0.78f, 1.05f), stoneCol, SurfacePattern.StoneAshlar, false, false, 0.14f, 0f, 0.65f, 0.35f);
                GameObject flowerBed = new GameObject($"EntrancePlanterFlowers_{tr}");
                flowerBed.transform.SetParent(garage.transform, false);
                flowerBed.transform.localPosition = new Vector3(tr * 7.8f, 0.78f, 9.05f);
                flowerBed.transform.localScale = Vector3.one * 1.45f;
                flowerBed.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetLowPolyFlowerPatchMesh(
                    tr < 0 ? LegoPalette.BrightOrange : LegoPalette.BrightYellow);
                flowerBed.AddComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetSwayingFoliageMaterial(Color.white, 0.68f);
            }

            // Multi-part Gear + Crossed Wrenches Emblem & Gooseneck Lamp above Door
            for (int t = 0; t < 8; t++)
            {
                float ga = (t / 8f) * Mathf.PI * 2f;
                GameObject tooth = CreateBoxPart(garage.transform, $"EmblemTooth_{t}",
                    new Vector3(Mathf.Cos(ga) * 1.15f, 7.65f + Mathf.Sin(ga) * 1.15f, 8.55f),
                    new Vector3(0.42f, 0.42f, 0.16f), LegoPalette.CloudWarmWhite, true, false, 0.18f);
                tooth.transform.localRotation = Quaternion.Euler(0f, 0f, ga * Mathf.Rad2Deg);
            }
            CreateBoxPart(garage.transform, "GearEmblemRing", new Vector3(0f, 7.65f, 8.56f), new Vector3(1.85f, 1.85f, 0.16f), LegoPalette.CloudWarmWhite, true, false, 0.18f);
            CreateBoxPart(garage.transform, "EmblemCenter", new Vector3(0f, 7.65f, 8.64f), new Vector3(1.15f, 1.15f, 0.12f), brickRed, false, false, 0.14f);
            GameObject wrenchA = CreateBoxPart(garage.transform, "WrenchA", new Vector3(0f, 7.65f, 8.74f), new Vector3(1.25f, 0.20f, 0.10f), LegoPalette.CloudWarmWhite, true, false, 0.16f);
            wrenchA.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            GameObject wrenchB = CreateBoxPart(garage.transform, "WrenchB", new Vector3(0f, 7.65f, 8.74f), new Vector3(1.25f, 0.20f, 0.10f), LegoPalette.CloudWarmWhite, true, false, 0.16f);
            wrenchB.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);

            CreateBoxPart(garage.transform, "GooseneckArm", new Vector3(-3.6f, 6.85f, 8.6f), new Vector3(0.18f, 0.18f, 1.2f), LegoPalette.CarbonBlack, false, false, 0.20f);
            CreateBoxPart(garage.transform, "FrontLampShade", new Vector3(-3.6f, 6.65f, 9.15f), new Vector3(0.85f, 0.28f, 0.85f), LegoPalette.CarbonBlack, false, false, 0.20f);
            GameObject frontLampGlow = CreateBoxPart(garage.transform, "FrontLampGlow", new Vector3(-3.6f, 6.48f, 9.15f), new Vector3(0.55f, 0.16f, 0.55f), LegoPalette.SunWarmGold, true, false, 0.25f);
            AddWorkshopPointLight(frontLampGlow.transform, "FrontEntrancePointLight", new Vector3(0f, -0.45f, 0f), new Color(1.0f, 0.82f, 0.48f), 1.0f, 14f);

            // Sloped Terracotta Tile Gable Roof + Timber Fascia Trim + Exposed Interior Steel Trusses
            GameObject roofL = CreateTexturedBoxPart(garage.transform, "RoofPanelL", new Vector3(-4.55f, 8.38f, 0f),
                new Vector3(10.0f, 0.42f, 18.4f), LegoPalette.CanyonTerracotta, SurfacePattern.RoofTiles, false, true, 0.18f, 0f, 0.52f, 0.36f);
            roofL.transform.localRotation = Quaternion.Euler(0f, 0f, 16f);
            GameObject roofR = CreateTexturedBoxPart(garage.transform, "RoofPanelR", new Vector3( 4.55f, 8.38f, 0f),
                new Vector3(10.0f, 0.42f, 18.4f), LegoPalette.CanyonTerracotta, SurfacePattern.RoofTiles, false, true, 0.18f, 0f, 0.52f, 0.36f);
            roofR.transform.localRotation = Quaternion.Euler(0f, 0f, -16f);
            CreateTexturedBoxPart(garage.transform, "RoofRidgeBeam", new Vector3(0f, 9.66f, 0f),
                new Vector3(0.70f, 0.44f, 18.6f), LegoPalette.WorkshopWood, SurfacePattern.WoodPlanks, false, false, 0.16f, 0f, 0.65f, 0.36f);
            CreateTexturedBoxPart(garage.transform, "EaveTrimL", new Vector3(-9.25f, 6.95f, 0f),
                new Vector3(0.24f, 0.34f, 18.5f), trimStoneCol, SurfacePattern.WoodPlanks, false, false, 0.18f);
            CreateTexturedBoxPart(garage.transform, "EaveTrimR", new Vector3( 9.25f, 6.95f, 0f),
                new Vector3(0.24f, 0.34f, 18.5f), trimStoneCol, SurfacePattern.WoodPlanks, false, false, 0.18f);

            foreach (float tz in new float[] { -4.2f, 0f, 4.2f })
            {
                CreateTexturedBoxPart(garage.transform, $"InteriorTrussTie_{tz}", new Vector3(0f, 7.12f, tz),
                    new Vector3(15.4f, 0.22f, 0.24f), LegoPalette.CarbonBlack, SurfacePattern.None, false, false, 0.30f, 0.3f);
                CreateTexturedBoxPart(garage.transform, $"InteriorKingPost_{tz}", new Vector3(0f, 8.25f, tz),
                    new Vector3(0.24f, 2.25f, 0.24f), LegoPalette.CarbonBlack, SurfacePattern.None, false, false, 0.30f, 0.3f);
            }

            // Attached Side Brick Workshop Wing with Stone Plinth, Ribbed Teal Roll-Up Door + Wooden Water Tower
            CreateTexturedBoxPart(garage.transform, "PlinthWing", new Vector3(13.2f, 0.56f, -0.5f), new Vector3(10.05f, 1.12f, 12.25f), stoneCol, SurfacePattern.StoneAshlar, false, false, 0.14f, 0f, 0.55f, 0.36f);
            CreateTexturedBoxPart(garage.transform, "SideWingBody", new Vector3(13.2f, 3.25f, -0.5f), new Vector3(9.8f, 4.5f, 12.0f), brickRed, SurfacePattern.BrickMasonry, false, true, 0.14f, 0f, 0.62f, 0.38f);
            CreateTexturedBoxPart(garage.transform, "SideWingRoof", new Vector3(13.2f, 5.75f, -0.5f), new Vector3(10.8f, 0.45f, 13.0f), LegoPalette.CanyonTerracotta, SurfacePattern.RoofTiles, false, false, 0.18f, 0f, 0.52f, 0.36f);
            CreateTexturedBoxPart(garage.transform, "SideWingFascia", new Vector3(13.2f, 5.75f, 6.0f), new Vector3(10.9f, 0.32f, 0.22f), trimStoneCol, SurfacePattern.None, false, false, 0.20f);
            for (int slat = 0; slat < 5; slat++)
            {
                CreateTexturedBoxPart(garage.transform, $"RollupSlat_{slat}", new Vector3(13.2f, 0.65f + slat * 0.72f, 5.58f),
                    new Vector3(5.6f, 0.56f, 0.18f), LegoPalette.WorkshopTeal, SurfacePattern.WoodPlanks, false, false, 0.22f, 0.1f, 0.9f, 0.26f);
            }
            BuildStiltedWaterTower(garage.transform, new Vector3(13.6f, 0f, -10.5f));

            // Workshop Interior Props: Ceiling Strip Lights, Engine Hoist Crane, Tool Chests, Workbench & Tire Rack
            GameObject ceilL = CreateBoxPart(garage.transform, "CeilingLightL", new Vector3(-3.2f, 6.9f, -1.5f), new Vector3(0.7f, 0.18f, 6.0f), LegoPalette.PureWhite, true, false);
            GameObject ceilR = CreateBoxPart(garage.transform, "CeilingLightR", new Vector3( 3.2f, 6.9f, -1.5f), new Vector3(0.7f, 0.18f, 6.0f), LegoPalette.PureWhite, true, false);
            AddWorkshopPointLight(ceilL.transform, "CeilingPointLightL", new Vector3(0f, -1.1f, 1.0f), new Color(1.0f, 0.95f, 0.86f), 1.15f, 18f);
            AddWorkshopPointLight(ceilR.transform, "CeilingPointLightR", new Vector3(0f, -1.1f, 1.0f), new Color(1.0f, 0.95f, 0.86f), 1.15f, 18f);

            // Red Tool Chests & Wooden Workbench against Back/Right wall
            CreateBoxPart(garage.transform, "ToolChestBack", new Vector3(4.5f, 1.0f, -6.7f), new Vector3(2.2f, 2.0f, 1.2f), LegoPalette.BrightRed, false, false);
            CreateTexturedBoxPart(garage.transform, "WorkbenchBack", new Vector3(0.4f, 0.75f, -6.7f), new Vector3(4.6f, 1.5f, 1.3f), LegoPalette.WorkshopWood, SurfacePattern.WoodPlanks, false, false, 0.16f);
            CreateBoxPart(garage.transform, "ToolPegboard", new Vector3(0.4f, 2.5f, -7.45f), new Vector3(3.8f, 1.5f, 0.15f), LegoPalette.CarbonBlack, false, false);

            // Orange Engine Hoist Crane & Suspended V8 Engine (left rear bay)
            CreateBoxPart(garage.transform, "EngineHoistBase", new Vector3(-4.8f, 0.15f, -4.6f), new Vector3(1.8f, 0.25f, 2.6f), LegoPalette.BrightOrange, false, false);
            CreateBoxPart(garage.transform, "EngineHoistMast", new Vector3(-5.2f, 1.85f, -5.5f), new Vector3(0.32f, 3.4f, 0.32f), LegoPalette.BrightOrange, false, false);
            GameObject hoistBoom = CreateBoxPart(garage.transform, "EngineHoistBoom", new Vector3(-4.2f, 3.45f, -4.6f), new Vector3(0.28f, 0.28f, 3.0f), LegoPalette.BrightOrange, false, false);
            hoistBoom.transform.localRotation = Quaternion.Euler(-12f, 32f, 0f);
            GameObject hangingV8 = new GameObject("SuspendedV8Engine");
            hangingV8.transform.SetParent(garage.transform, false);
            hangingV8.transform.localPosition = new Vector3(-3.6f, 2.15f, -3.7f);
            hangingV8.transform.localScale = Vector3.one * 1.4f;
            hangingV8.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetBrickMesh(2, 2, 2, BrickShape.V8Engine);
            hangingV8.AddComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetLowPolyMaterial(LegoPalette.SilverMetallic, false, 0.45f, 0.4f);

            // Stack of Spare Tires outside Front-Left corner
            CreateBoxPart(garage.transform, "TireRackStand", new Vector3(-6.4f, 0.75f, 6.2f), new Vector3(1.6f, 1.5f, 1.6f), LegoPalette.TireBlack, false, false);

            // 2 Low-Poly Display Cars parked neatly beside the Garage (Blue Coupe & Beige Pickup)
            BuildStaticDisplayCar(garage.transform, new Vector3(11.4f, 0.0f, 1.5f), Quaternion.Euler(0f, -20f, 0f), LegoPalette.BrightBlue, false);
            BuildStaticDisplayCar(garage.transform, new Vector3(11.8f, 0.0f, -4.8f), Quaternion.Euler(0f, -15f, 0f), LegoPalette.TanSand, true);

            // Garage Entry Zone Ring right in front of the open bay door (distinct Warm Orange/Gold glow)
            Vector3 entryLocal = new Vector3(0f, 0.08f, 9.8f);
            data.garageEntryPos = garage.transform.TransformPoint(entryLocal);
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(ring.GetComponent<Collider>());
            ring.name = "GarageEntryZoneRing";
            ring.transform.SetParent(garage.transform, false);
            ring.transform.localPosition = entryLocal;
            ring.transform.localScale = new Vector3(10.5f, 0.08f, 10.5f);
            ring.GetComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetLowPolyMaterial(LegoPalette.BrightOrange, true, 0.30f);

            GameObject signObj = new GameObject("GarageDoorSign");
            signObj.transform.SetParent(garage.transform, false);
            signObj.transform.localPosition = new Vector3(0f, 5.95f, 8.9f);
            signObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            TextMesh garageSign = signObj.AddComponent<TextMesh>();
            garageSign.text = "GARAGE [TAB / E]";
            garageSign.fontSize = 44;
            garageSign.characterSize = 0.14f;
            garageSign.anchor = TextAnchor.MiddleCenter;
            garageSign.alignment = TextAlignment.Center;
            garageSign.color = LegoPalette.SunWarmGold;

            // Car Assembly Staging Position inside Workshop Bay
            data.garageInteriorCarPos = garage.transform.TransformPoint(new Vector3(0.4f, 0.55f, -0.5f));
            data.garageInteriorCarRot = garage.transform.rotation * Quaternion.Euler(0f, 22f, 0f);
        }

        private static void BuildStiltedWaterTower(Transform parent, Vector3 localPos)
        {
            GameObject tower = new GameObject("WoodenWaterTower");
            tower.transform.SetParent(parent, false);
            tower.transform.localPosition = localPos;

            float[] lx = { -1.35f, 1.35f, -1.35f, 1.35f };
            float[] lz = { -1.35f, -1.35f, 1.35f, 1.35f };
            for (int i = 0; i < 4; i++)
            {
                GameObject leg = CreateTexturedBoxPart(tower.transform, $"Leg_{i}", new Vector3(lx[i] * 0.85f, 3.6f, lz[i] * 0.85f),
                    new Vector3(0.32f, 7.4f, 0.32f), LegoPalette.WorkshopWood, SurfacePattern.WoodPlanks, false, false, 0.14f, 0f, 0.8f, 0.32f);
                leg.transform.localRotation = Quaternion.Euler(-lz[i] * 4f, 0f, lx[i] * 4f);
            }
            CreateTexturedBoxPart(tower.transform, "Deck", new Vector3(0f, 7.2f, 0f), new Vector3(3.4f, 0.35f, 3.4f), LegoPalette.WorkshopWood, SurfacePattern.WoodPlanks, false, false, 0.14f, 0f, 0.8f, 0.32f);

            GameObject tank = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(tank.GetComponent<Collider>());
            tank.transform.SetParent(tower.transform, false);
            tank.transform.localPosition = new Vector3(0f, 9.1f, 0f);
            tank.transform.localScale = new Vector3(3.1f, 1.75f, 3.1f);
            tank.GetComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetTexturedLowPolyMaterial(LegoPalette.LowPolyTrunkBrown, SurfacePattern.WoodPlanks, 0.14f, 0f, 0.72f, 0.38f);

            GameObject cap = new GameObject("TowerRoof");
            cap.transform.SetParent(tower.transform, false);
            cap.transform.localPosition = new Vector3(0f, 10.8f, 0f);
            cap.transform.localScale = new Vector3(2.2f, 1.6f, 2.2f);
            cap.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetLowPolyMountainMesh();
            cap.AddComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetTexturedLowPolyMaterial(LegoPalette.WorkshopBrickRed, SurfacePattern.RoofTiles, 0.18f, 0f, 0.65f, 0.34f);
        }

        private static void BuildStaticDisplayCar(Transform parent, Vector3 localPos, Quaternion localRot, Color bodyCol, bool pickupStyle)
        {
            GameObject car = new GameObject("DisplayCarStatic");
            car.transform.SetParent(parent, false);
            car.transform.localPosition = localPos;
            car.transform.localRotation = localRot;

            CreateBoxPart(car.transform, "Chassis", new Vector3(0f, 0.34f, 0f), new Vector3(1.85f, 0.25f, 4.1f), LegoPalette.CarbonBlack, false, false, 0.18f);
            CreateBoxPart(car.transform, "Body", new Vector3(0f, 0.64f, 0f), new Vector3(1.92f, 0.56f, 4.0f), bodyCol, false, true, 0.22f);
            CreateBoxPart(car.transform, "Stripe", new Vector3(0f, 0.94f, 0.45f), new Vector3(0.45f, 0.05f, 2.8f), LegoPalette.CloudWarmWhite, false, false, 0.18f);
            CreateBoxPart(car.transform, "Cabin", new Vector3(0f, 1.15f, pickupStyle ? 0.35f : -0.15f),
                new Vector3(1.55f, 0.52f, pickupStyle ? 1.35f : 1.95f), LegoPalette.CloudWarmWhite, false, false, 0.20f);
            CreateBoxPart(car.transform, "HeadL", new Vector3(-0.62f, 0.66f, 2.02f), new Vector3(0.38f, 0.20f, 0.08f), LegoPalette.SunWarmGold, true, false, 0.25f);
            CreateBoxPart(car.transform, "HeadR", new Vector3( 0.62f, 0.66f, 2.02f), new Vector3(0.38f, 0.20f, 0.08f), LegoPalette.SunWarmGold, true, false, 0.25f);

            Mesh wheelMesh = LegoMeshAndMaterialFactory.GetWheelMeshForTier(WheelSizeTier.Medium);
            Mesh rimMesh = LegoMeshAndMaterialFactory.GetBrickMesh(2, 3, 3, BrickShape.CylinderPart);
            Material tireMat = LegoMeshAndMaterialFactory.GetLowPolyMaterial(LegoPalette.TireBlack, false, 0.18f);
            Material rimMat = LegoMeshAndMaterialFactory.GetLowPolyMaterial(LegoPalette.SilverMetallic, false, 0.42f, 0.3f);

            float[] wx = { -0.98f, 0.98f, -0.98f, 0.98f };
            float[] wz = { 1.25f, 1.25f, -1.25f, -1.25f };
            for (int w = 0; w < 4; w++)
            {
                GameObject wh = new GameObject($"Wheel_{w}");
                wh.transform.SetParent(car.transform, false);
                wh.transform.localPosition = new Vector3(wx[w], 0.42f, wz[w]);
                wh.AddComponent<MeshFilter>().sharedMesh = wheelMesh;
                wh.AddComponent<MeshRenderer>().sharedMaterial = tireMat;

                GameObject rim = new GameObject("Rim");
                rim.transform.SetParent(wh.transform, false);
                rim.transform.localScale = Vector3.one * 0.85f;
                rim.AddComponent<MeshFilter>().sharedMesh = rimMesh;
                rim.AddComponent<MeshRenderer>().sharedMaterial = rimMat;
            }
        }

        private static void BuildFencedTestDriveArena(Transform parent, Vector3 arenaCenter, Vector3 dir, BuiltTrackData data)
        {
            GameObject arena = new GameObject("TestDrive_Arena_Fenced");
            arena.transform.SetParent(parent, false);
            arena.transform.position = new Vector3(arenaCenter.x, 0f, arenaCenter.z);
            arena.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            float size = 34f;
            float half = size * 0.5f;

            GameObject floor = CreateBoxPart(arena.transform, "TestArenaAsphalt", new Vector3(0f, -0.05f, 0f), new Vector3(size, 0.14f, size), LegoPalette.LowPolyAsphalt, false, true, 0.20f);
            floor.AddComponent<TerrainSurfaceTag>().terrainType = TerrainType.Street;

            // Center training circle markings
            CreateBoxPart(arena.transform, "ArenaMarkH", new Vector3(0f, 0.03f, 0f), new Vector3(18f, 0.02f, 0.45f), LegoPalette.BrightYellow, false, false);
            CreateBoxPart(arena.transform, "ArenaMarkV", new Vector3(0f, 0.03f, 0f), new Vector3(0.45f, 0.02f, 18f), LegoPalette.BrightYellow, false, false);

            // Physical barrier fences on all 4 sides
            CreateBoxPart(arena.transform, "FenceN", new Vector3(0f, 0.65f,  half), new Vector3(size, 1.3f, 0.8f), LegoPalette.BrightRed);
            CreateBoxPart(arena.transform, "FenceS", new Vector3(0f, 0.65f, -half), new Vector3(size, 1.3f, 0.8f), LegoPalette.BrightRed);
            CreateBoxPart(arena.transform, "FenceE", new Vector3( half, 0.65f, 0f), new Vector3(0.8f, 1.3f, size), LegoPalette.PureWhite);
            CreateBoxPart(arena.transform, "FenceW", new Vector3(-half, 0.65f, 0f), new Vector3(0.8f, 1.3f, size), LegoPalette.PureWhite);

            // Slalom test cones + gentle test ramp inside arena
            for (int c = -1; c <= 1; c++)
            {
                CreateBoxPart(arena.transform, $"SlalomCone_{c}", new Vector3(c * 5.5f, 0.35f, 6.5f), new Vector3(0.55f, 0.7f, 0.55f), LegoPalette.BrightOrange, false, false);
            }
            GameObject ramp = CreateBoxPart(arena.transform, "TestRamp", new Vector3(0f, 0.30f, -8.5f), new Vector3(6.0f, 0.45f, 6.5f), LegoPalette.WorkshopWood);
            ramp.transform.localRotation = Quaternion.Euler(-9f, 0f, 0f);

            data.testArenaSpawnPos = arena.transform.TransformPoint(new Vector3(0f, 0.65f, 0f));
            data.testArenaSpawnRot = Quaternion.LookRotation(-dir, Vector3.up);
            data.testArenaBounds = new Bounds(new Vector3(arenaCenter.x, 0f, arenaCenter.z), new Vector3(size - 2.5f, 20f, size - 2.5f));
        }

        private static void BuildLowPolyVillageBuilding(Transform parent, Vector3 pos, Quaternion rot, Color wallCol, Color roofCol, float w, float d, float h)
        {
            GameObject house = new GameObject("LowPolyVillageCottage");
            house.transform.SetParent(parent, false);
            house.transform.position = new Vector3(pos.x, 0f, pos.z);
            house.transform.rotation = rot;

            Color stoneCol = LegoPalette.LowPolyRockGrey;
            Color trimWhite = LegoPalette.CloudWarmWhite;
            Color timberCol = LegoPalette.WorkshopWood;
            Color glassCol = new Color(0.56f, 0.78f, 0.90f);

            // 1. Stone Ashlar Foundation Plinth + Water-Table Ledge + 2-Step Entry Stoop
            CreateTexturedBoxPart(house.transform, "StonePlinth", new Vector3(0f, 0.36f, 0f),
                new Vector3(w + 0.45f, 0.88f, d + 0.45f), stoneCol, SurfacePattern.StoneAshlar, false, true, 0.14f, 0f, 0.58f, 0.36f);
            CreateTexturedBoxPart(house.transform, "PlinthLedge", new Vector3(0f, 0.82f, 0f),
                new Vector3(w + 0.54f, 0.10f, d + 0.54f), trimWhite, SurfacePattern.StoneAshlar, false, false, 0.16f, 0f, 0.6f, 0.25f);
            CreateTexturedBoxPart(house.transform, "EntryStoopTop", new Vector3(0f, 0.34f, d * 0.5f + 0.62f),
                new Vector3(2.8f, 0.56f, 1.15f), stoneCol, SurfacePattern.StoneAshlar, false, true, 0.14f, 0f, 0.65f, 0.34f);
            CreateTexturedBoxPart(house.transform, "EntryStepLow", new Vector3(0f, 0.16f, d * 0.5f + 1.32f),
                new Vector3(2.9f, 0.28f, 0.65f), stoneCol, SurfacePattern.StoneAshlar, false, true, 0.14f, 0f, 0.65f, 0.34f);

            // 2. Ground Floor Stone-Tinted Walls + Upper Half-Timbered Story + Corner Timber Posts
            float groundH = (h - 0.85f) * 0.54f;
            float upperH = (h - 0.85f) * 0.46f;
            float groundY = 0.85f + groundH * 0.5f;
            float beltY = 0.85f + groundH;
            float upperY = beltY + upperH * 0.5f;

            CreateTexturedBoxPart(house.transform, "GroundFloorWalls", new Vector3(0f, groundY, 0f),
                new Vector3(w, groundH, d), Color.Lerp(wallCol, trimWhite, 0.28f), SurfacePattern.StoneAshlar, false, true, 0.14f, 0f, 0.55f, 0.28f);
            CreateTexturedBoxPart(house.transform, "TimberBeltCourse", new Vector3(0f, beltY, 0f),
                new Vector3(w + 0.24f, 0.24f, d + 0.24f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.16f, 0f, 0.85f, 0.38f);
            CreateTexturedBoxPart(house.transform, "UpperFloorWalls", new Vector3(0f, upperY, 0f),
                new Vector3(w - 0.08f, upperH, d - 0.08f), wallCol, SurfacePattern.WoodPlanks, false, true, 0.14f, 0f, 0.68f, 0.22f);

            // 4 Structural Corner Timber Posts
            for (int cx = -1; cx <= 1; cx += 2)
            {
                for (int cz = -1; cz <= 1; cz += 2)
                {
                    CreateTexturedBoxPart(house.transform, $"CornerTimber_{cx}_{cz}",
                        new Vector3(cx * (w * 0.5f - 0.04f), 0.85f + (h - 0.85f) * 0.5f, cz * (d * 0.5f - 0.04f)),
                        new Vector3(0.32f, h - 0.82f, 0.32f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.16f, 0f, 0.9f, 0.35f);
                }
            }

            // 3. Recessed Timber Front Entry Door + Brass Handle + Portico Canopy & Warm Porch Sconces
            float frontZ = d * 0.5f;
            CreateTexturedBoxPart(house.transform, "DoorFrame", new Vector3(0f, 1.85f, frontZ + 0.04f),
                new Vector3(1.88f, 2.38f, 0.18f), trimWhite, SurfacePattern.StoneAshlar, false, false, 0.18f);
            CreateTexturedBoxPart(house.transform, "DoorLeaf", new Vector3(0f, 1.80f, frontZ + 0.07f),
                new Vector3(1.48f, 2.18f, 0.16f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.18f, 0f, 1.1f, 0.40f);
            CreateBoxPart(house.transform, "DoorHandle", new Vector3(0.50f, 1.75f, frontZ + 0.18f),
                new Vector3(0.10f, 0.22f, 0.10f), LegoPalette.SunWarmGold, false, false, 0.65f);

            for (int ps = -1; ps <= 1; ps += 2)
            {
                CreateTexturedBoxPart(house.transform, $"PorchPost_{ps}", new Vector3(ps * 1.25f, 1.80f, frontZ + 1.05f),
                    new Vector3(0.22f, 2.40f, 0.22f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.16f);
                CreateBoxPart(house.transform, $"PorchSconce_{ps}", new Vector3(ps * 1.18f, 2.45f, frontZ + 0.15f),
                    new Vector3(0.18f, 0.28f, 0.18f), LegoPalette.SunWarmGold, true, false, 0.25f);
            }
            GameObject porchHood = CreateTexturedBoxPart(house.transform, "PorchCanopyRoof",
                new Vector3(0f, 3.15f, frontZ + 0.68f),
                new Vector3(3.15f, 0.20f, 1.48f), roofCol, SurfacePattern.RoofTiles, false, false, 0.18f, 0f, 0.6f, 0.34f);
            porchHood.transform.localRotation = Quaternion.Euler(14f, 0f, 0f);

            // 4. 2nd-Floor Cantilevered Wooden Balcony over Front Facade
            float balcY = beltY + 0.12f;
            float balcW = Mathf.Min(w * 0.54f, 4.6f);
            CreateTexturedBoxPart(house.transform, "BalconyDeck", new Vector3(0f, balcY, frontZ + 0.56f),
                new Vector3(balcW, 0.18f, 1.18f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.16f, 0f, 0.85f, 0.38f);
            CreateTexturedBoxPart(house.transform, "BalconyFrontRail", new Vector3(0f, balcY + 0.82f, frontZ + 1.08f),
                new Vector3(balcW, 0.12f, 0.12f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.16f);
            for (int bp = -2; bp <= 2; bp++)
            {
                CreateTexturedBoxPart(house.transform, $"BalconyBaluster_{bp}",
                    new Vector3(bp * (balcW * 0.23f), balcY + 0.44f, frontZ + 1.08f),
                    new Vector3(0.10f, 0.72f, 0.10f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.16f);
            }
            CreateTexturedBoxPart(house.transform, "BalconyDoorGlass", new Vector3(0f, balcY + 1.05f, frontZ + 0.04f),
                new Vector3(1.42f, 1.78f, 0.12f), glassCol, SurfacePattern.None, false, false, 0.86f, 0.28f);
            CreateTexturedBoxPart(house.transform, "BalconyDoorMullion", new Vector3(0f, balcY + 1.05f, frontZ + 0.07f),
                new Vector3(0.10f, 1.80f, 0.14f), trimWhite, SurfacePattern.None, false, false, 0.20f);

            // 5. Helper to build Framed, Mullioned, Shuttered Windows with Optional Blooming Flower Box
            void AddArchitecturalWindow(string tag, Vector3 localPos, Quaternion localRot, bool withFlowerBox, int flowerSeed)
            {
                GameObject win = new GameObject(tag);
                win.transform.SetParent(house.transform, false);
                win.transform.SetLocalPositionAndRotation(localPos, localRot);

                CreateTexturedBoxPart(win.transform, "Sill", new Vector3(0f, -0.74f, 0.10f),
                    new Vector3(1.52f, 0.14f, 0.26f), trimWhite, SurfacePattern.StoneAshlar, false, false, 0.18f);
                CreateTexturedBoxPart(win.transform, "Frame", new Vector3(0f, 0f, 0.04f),
                    new Vector3(1.36f, 1.38f, 0.14f), trimWhite, SurfacePattern.None, false, false, 0.20f);
                CreateTexturedBoxPart(win.transform, "Glass", new Vector3(0f, 0f, 0.07f),
                    new Vector3(1.10f, 1.12f, 0.10f), glassCol, SurfacePattern.None, false, false, 0.86f, 0.28f);
                CreateTexturedBoxPart(win.transform, "MullionV", new Vector3(0f, 0f, 0.09f),
                    new Vector3(0.08f, 1.14f, 0.12f), LegoPalette.CarbonBlack, SurfacePattern.None, false, false, 0.24f, 0.25f);
                CreateTexturedBoxPart(win.transform, "MullionH", new Vector3(0f, 0f, 0.09f),
                    new Vector3(1.14f, 0.08f, 0.12f), LegoPalette.CarbonBlack, SurfacePattern.None, false, false, 0.24f, 0.25f);

                for (int sh = -1; sh <= 1; sh += 2)
                {
                    CreateTexturedBoxPart(win.transform, $"Shutter_{sh}", new Vector3(sh * 0.90f, 0f, 0.06f),
                        new Vector3(0.38f, 1.32f, 0.10f), roofCol, SurfacePattern.WoodPlanks, false, false, 0.18f, 0f, 1.2f, 0.30f);
                }

                if (withFlowerBox)
                {
                    CreateTexturedBoxPart(win.transform, "PlanterBox", new Vector3(0f, -0.82f, 0.24f),
                        new Vector3(1.42f, 0.28f, 0.32f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.16f);
                    Color[] boxPetals = { LegoPalette.BrightOrange, LegoPalette.BrightYellow, new Color(0.95f, 0.42f, 0.62f) };
                    GameObject blooms = new GameObject("PlanterBlooms");
                    blooms.transform.SetParent(win.transform, false);
                    blooms.transform.localPosition = new Vector3(0f, -0.68f, 0.24f);
                    blooms.transform.localScale = Vector3.one * 0.95f;
                    blooms.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetLowPolyFlowerPatchMesh(
                        boxPetals[Mathf.Abs(flowerSeed) % boxPetals.Length]);
                    blooms.AddComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetSwayingFoliageMaterial(Color.white, 0.72f);
                }
            }

            float frontWinX = w * 0.32f;
            AddArchitecturalWindow("FrontWin_L", new Vector3(-frontWinX, groundY + 0.12f, frontZ), Quaternion.identity, true, 1);
            AddArchitecturalWindow("FrontWin_R", new Vector3( frontWinX, groundY + 0.12f, frontZ), Quaternion.identity, true, 2);

            float sideX = w * 0.5f;
            Quaternion rotRight = Quaternion.Euler(0f, 90f, 0f);
            Quaternion rotLeft = Quaternion.Euler(0f, -90f, 0f);
            Quaternion rotBack = Quaternion.Euler(0f, 180f, 0f);
            AddArchitecturalWindow("RightWin_1", new Vector3( sideX, groundY + 0.12f,  d * 0.22f), rotRight, true, 3);
            AddArchitecturalWindow("RightWin_2", new Vector3( sideX, upperY, -d * 0.20f), rotRight, false, 0);
            AddArchitecturalWindow("LeftWin_1",  new Vector3(-sideX, groundY + 0.12f, -d * 0.22f), rotLeft,  true, 4);
            AddArchitecturalWindow("LeftWin_2",  new Vector3(-sideX, upperY,  d * 0.20f), rotLeft,  false, 0);
            AddArchitecturalWindow("BackWin_1",  new Vector3(-w * 0.26f, groundY + 0.12f, -frontZ), rotBack, false, 0);
            AddArchitecturalWindow("BackWin_2",  new Vector3( w * 0.26f, upperY, -frontZ), rotBack, false, 0);

            // 6. Seamless Pitched Roof, Triangular Half-Timber Gable, Exposed Rafters, Chimney & Dormer
            float pitchDeg = 26f;
            float roofRise = Mathf.Tan(pitchDeg * Mathf.Deg2Rad) * (w * 0.5f);

            GameObject gable = new GameObject("GableWallPrism");
            gable.transform.SetParent(house.transform, false);
            gable.transform.localPosition = new Vector3(0f, h - 0.02f, 0f);
            gable.transform.localScale = new Vector3(w - 0.08f, roofRise, d - 0.08f);
            gable.AddComponent<MeshFilter>().sharedMesh = GetGablePrismMesh();
            gable.AddComponent<MeshRenderer>().sharedMaterial =
                LegoMeshAndMaterialFactory.GetTexturedLowPolyMaterial(wallCol, SurfacePattern.WoodPlanks, 0.14f, 0f, 0.68f, 0.22f);

            CreateTexturedBoxPart(house.transform, "GableKingPost", new Vector3(0f, h + roofRise * 0.48f, frontZ - 0.02f),
                new Vector3(0.24f, roofRise * 0.90f, 0.20f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.16f);

            float slopeLen = (w * 0.5f + 0.85f) / Mathf.Cos(pitchDeg * Mathf.Deg2Rad);
            float midX = (w * 0.25f) + 0.25f;
            float midY = h + roofRise * 0.5f - 0.04f;
            GameObject r1 = CreateTexturedBoxPart(house.transform, "RoofL", new Vector3(-midX, midY, 0f),
                new Vector3(slopeLen, 0.30f, d + 1.35f), roofCol, SurfacePattern.RoofTiles, false, true, 0.18f, 0f, 0.58f, 0.36f);
            r1.transform.localRotation = Quaternion.Euler(0f, 0f, pitchDeg);
            GameObject r2 = CreateTexturedBoxPart(house.transform, "RoofR", new Vector3( midX, midY, 0f),
                new Vector3(slopeLen, 0.30f, d + 1.35f), roofCol, SurfacePattern.RoofTiles, false, true, 0.18f, 0f, 0.58f, 0.36f);
            r2.transform.localRotation = Quaternion.Euler(0f, 0f, -pitchDeg);

            CreateTexturedBoxPart(house.transform, "RoofRidgeBeam", new Vector3(0f, h + roofRise + 0.08f, 0f),
                new Vector3(0.42f, 0.28f, d + 1.50f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.16f);

            // Exposed timber rafter tails along left & right eaves
            for (int rf = 0; rf < 4; rf++)
            {
                float rz = Mathf.Lerp(-d * 0.42f, d * 0.42f, rf / 3f);
                CreateTexturedBoxPart(house.transform, $"RafterTailL_{rf}", new Vector3(-w * 0.5f - 0.45f, h - 0.18f, rz),
                    new Vector3(0.55f, 0.18f, 0.18f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.16f);
                CreateTexturedBoxPart(house.transform, $"RafterTailR_{rf}", new Vector3( w * 0.5f + 0.45f, h - 0.18f, rz),
                    new Vector3(0.55f, 0.18f, 0.18f), timberCol, SurfacePattern.WoodPlanks, false, false, 0.16f);
            }

            // Brick Masonry Chimney + Terracotta Flue Pot
            CreateTexturedBoxPart(house.transform, "BrickChimney", new Vector3(w * 0.28f, h + roofRise * 0.65f, -d * 0.24f),
                new Vector3(0.95f, roofRise + 1.15f, 0.95f), LegoPalette.WorkshopBrickRed, SurfacePattern.BrickMasonry, false, false, 0.14f, 0f, 0.72f, 0.38f);
            CreateTexturedBoxPart(house.transform, "ChimneyCap", new Vector3(w * 0.28f, h + roofRise + 0.72f, -d * 0.24f),
                new Vector3(1.18f, 0.18f, 1.18f), stoneCol, SurfacePattern.StoneAshlar, false, false, 0.16f);
        }

        private static void BuildScenicAlpineLake(Transform parent, Vector3 center, Vector3 axisDir)
        {
            GameObject lake = new GameObject("ScenicAlpineLake");
            lake.transform.SetParent(parent, false);
            lake.transform.position = new Vector3(center.x, 0f, center.z);
            lake.transform.rotation = Quaternion.LookRotation(axisDir, Vector3.up);

            GameObject water = CreateBoxPart(lake.transform, "LakeSurface", new Vector3(0f, -0.06f, 0f), new Vector3(30f, 0.10f, 22f), LegoPalette.LowPolyLakeCyan, false, true, 0.68f);
            water.AddComponent<TerrainSurfaceTag>().terrainType = TerrainType.OffRoad;

            for (int i = -4; i <= 4; i++)
            {
                BuildLowPolyRock(parent, lake.transform.TransformPoint(new Vector3(i * 3.2f, 0f, 11.2f)), 1.5f + (Mathf.Abs(i) % 2) * 0.6f, i * 45f);
                CreateBoxPart(lake.transform, $"FencePost_{i}", new Vector3(i * 3.4f, 0.55f, -12.2f), new Vector3(0.32f, 1.1f, 0.32f), LegoPalette.WorkshopWood);
            }
            CreateBoxPart(lake.transform, "FenceRailTop", new Vector3(0f, 0.85f, -12.2f), new Vector3(28f, 0.18f, 0.16f), LegoPalette.WorkshopWood, false, false);
            CreateBoxPart(lake.transform, "FenceRailMid", new Vector3(0f, 0.48f, -12.2f), new Vector3(28f, 0.16f, 0.16f), LegoPalette.WorkshopWood, false, false);
        }

        private static void BuildScenicMineTunnel(Transform parent, Vector3 pos, Quaternion rot)
        {
            GameObject mine = new GameObject("ScenicMineTunnelEntrance");
            mine.transform.SetParent(parent, false);
            mine.transform.position = new Vector3(pos.x, 0f, pos.z);
            mine.transform.rotation = rot;

            // Low-poly rock bluff mound behind tunnel portal (visual scenery only)
            GameObject bluff = new GameObject("MineRockBluff");
            bluff.transform.SetParent(mine.transform, false);
            bluff.transform.localPosition = new Vector3(0f, -0.2f, -3.5f);
            bluff.transform.localScale = new Vector3(13.5f, 11.5f, 10.5f);
            bluff.AddComponent<MeshFilter>().sharedMesh = LegoMeshAndMaterialFactory.GetLowPolyMountainMesh();
            bluff.AddComponent<MeshRenderer>().sharedMaterial = LegoMeshAndMaterialFactory.GetLowPolyMaterial(LegoPalette.LowPolyMountainSlate, false, 0.12f);

            // Dark tunnel cavity + Timber beam portal frame
            CreateBoxPart(mine.transform, "TunnelOpening", new Vector3(0f, 2.3f, 0.4f), new Vector3(4.8f, 4.6f, 2.8f), LegoPalette.CarbonBlack);
            CreateBoxPart(mine.transform, "TimberPostL", new Vector3(-2.65f, 2.4f, 1.9f), new Vector3(0.65f, 4.8f, 0.65f), LegoPalette.WorkshopWood);
            CreateBoxPart(mine.transform, "TimberPostR", new Vector3( 2.65f, 2.4f, 1.9f), new Vector3(0.65f, 4.8f, 0.65f), LegoPalette.WorkshopWood);
            CreateBoxPart(mine.transform, "TimberLintel", new Vector3(0f, 5.0f, 1.9f), new Vector3(6.4f, 0.72f, 0.8f), LegoPalette.WorkshopWood);

            // Warm hanging lanterns on portal posts + Ore cart sitting on rail tracks
            CreateBoxPart(mine.transform, "LanternL", new Vector3(-2.1f, 3.6f, 2.35f), new Vector3(0.42f, 0.52f, 0.42f), LegoPalette.BrightOrange, true, false);
            CreateBoxPart(mine.transform, "LanternR", new Vector3( 2.1f, 3.6f, 2.35f), new Vector3(0.42f, 0.52f, 0.42f), LegoPalette.BrightOrange, true, false);
            CreateBoxPart(mine.transform, "MineRailL", new Vector3(-0.75f, 0.08f, 3.5f), new Vector3(0.14f, 0.12f, 7.5f), LegoPalette.SilverMetallic, false, false);
            CreateBoxPart(mine.transform, "MineRailR", new Vector3( 0.75f, 0.08f, 3.5f), new Vector3(0.14f, 0.12f, 7.5f), LegoPalette.SilverMetallic, false, false);
            CreateBoxPart(mine.transform, "OreCartBody", new Vector3(0f, 0.65f, 3.2f), new Vector3(1.8f, 0.95f, 2.3f), LegoPalette.LowPolyRockGrey, false, false);
            CreateBoxPart(mine.transform, "OreRocks", new Vector3(0f, 1.15f, 3.2f), new Vector3(1.4f, 0.38f, 1.8f), LegoPalette.WorkshopConcrete, false, false);
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
