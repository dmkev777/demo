using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lego2KDrive
{
    [Serializable]
    public class LegoBrickSpec
    {
        public Vector3Int gridPos;     // Position in stud/plate grid coordinates
        public Vector3Int size;        // Width (studs), Height (plates), Length (studs)
        public Color color;
        public BrickShape shape;
        public bool isCoreChassis;     // Core base chassis + wheels stay intact; custom bricks built on top can detach
        public bool isEmissive;

        public LegoBrickSpec(Vector3Int pos, Vector3Int sz, Color col, BrickShape shp = BrickShape.StandardBrick, bool core = false, bool emissive = false)
        {
            gridPos = pos;
            size = sz;
            color = col;
            shape = shp;
            isCoreChassis = core;
            isEmissive = emissive;
        }

        public LegoBrickSpec Clone()
        {
            return new LegoBrickSpec(gridPos, size, color, shape, isCoreChassis, isEmissive);
        }
    }

    [Serializable]
    public struct VehicleAeroStats
    {
        public int speedRating;       // 1..6 (Image 1 6-bar meter)
        public int weightRating;      // 1..6
        public int aeroRating;        // 1..6
        public float topSpeedMultiplier;
        public float accelMultiplier;
        public float gripMultiplier;
        public float offRoadMultiplier;
        public int brickCount;
    }

    [Serializable]
    public class SerializedLoadoutData
    {
        public List<LegoBrickSpec> street = new List<LegoBrickSpec>();
        public List<LegoBrickSpec> offRoad = new List<LegoBrickSpec>();
        public List<LegoBrickSpec> water = new List<LegoBrickSpec>();
        public int wheelSizeTier = (int)WheelSizeTier.Medium;
    }

    /// <summary>
    /// Stores and generates the 3 vehicle blueprints (Street, OffRoad, Water).
    /// Starts with a SOLID, CHUNKY, COMPACT CHASSIS BASE (Khung gầm nhỏ nhưng dày dặn, chắc chắn)
    /// so every part looks volumetric and sturdy!
    /// </summary>
    public class LegoVehicleLoadout
    {
        private const string SAVE_KEY = "Lego2KDrive_PlayerCustomLoadout_v6";

        public Color primaryColor;
        public Color accentColor;
        public bool useMinimalBaseByDefault;
        public WheelSizeTier wheelSizeTier = WheelSizeTier.Medium;

        public List<LegoBrickSpec> streetBricks = new List<LegoBrickSpec>();
        public List<LegoBrickSpec> offRoadBricks = new List<LegoBrickSpec>();
        public List<LegoBrickSpec> waterBricks = new List<LegoBrickSpec>();

        public LegoVehicleLoadout(Color primary, Color accent, bool startWithSmallBase = true)
        {
            primaryColor = primary;
            accentColor = accent;
            useMinimalBaseByDefault = startWithSmallBase;

            if (startWithSmallBase)
            {
                if (!TryLoadFromPrefs())
                {
                    ResetAllToSmallBareBase();
                }
            }
            else
            {
                ResetAllToFullTemplates();
            }
        }

        // Single unified customizable car (boat & 3-form shapeshifting removed)
        public List<LegoBrickSpec> GetBricksForForm(TerrainType form)
        {
            return streetBricks;
        }

        public VehicleAeroStats CalculateVehicleStats()
        {
            List<LegoBrickSpec> bricks = streetBricks;
            float mass = 12f;
            float power = 0f;
            float aero = 1.5f;
            int count = bricks != null ? bricks.Count : 0;

            if (bricks != null)
            {
                foreach (LegoBrickSpec b in bricks)
                {
                    if (b == null) continue;
                    float vol = Mathf.Max(1, b.size.x * b.size.y * b.size.z);
                    switch (b.shape)
                    {
                        case BrickShape.Wheel:
                            mass += 1.2f;
                            break;
                        case BrickShape.V8Engine:
                            power += 4.2f;
                            mass += 3.0f;
                            break;
                        case BrickShape.JetNozzle:
                            power += 3.5f;
                            aero += 0.8f;
                            mass += 1.5f;
                            break;
                        case BrickShape.SpoilerWing:
                            aero += 3.2f;
                            mass += 1.0f;
                            break;
                        case BrickShape.FrontSplitter:
                        case BrickShape.SideSkirt:
                        case BrickShape.RoofScoop:
                            aero += 2.0f;
                            mass += 0.9f;
                            break;
                        case BrickShape.SlopeFront:
                        case BrickShape.SlopeInverted:
                        case BrickShape.Windshield:
                        case BrickShape.FenderArch:
                            aero += 1.1f;
                            mass += vol * 0.25f;
                            break;
                        case BrickShape.SportSeat:
                            aero += 0.5f;
                            mass += 0.8f;
                            break;
                        default:
                            mass += vol * 0.38f;
                            if (b.gridPos.y >= 5) aero -= 0.25f;
                            break;
                    }
                }
            }

            float wheelSpeedAdj = wheelSizeTier == WheelSizeTier.Large ? -0.02f : (wheelSizeTier == WheelSizeTier.Small ? 0.04f : 0f);
            float wheelAccelAdj = wheelSizeTier == WheelSizeTier.Large ? -0.06f : (wheelSizeTier == WheelSizeTier.Small ? 0.10f : 0f);
            float wheelGripAdj  = wheelSizeTier == WheelSizeTier.Large ? 0.12f  : (wheelSizeTier == WheelSizeTier.Small ? -0.05f : 0f);
            float wheelOffRoad  = wheelSizeTier == WheelSizeTier.Large ? 1.28f  : (wheelSizeTier == WheelSizeTier.Small ? 0.84f : 1.00f);

            float powerRatio = Mathf.Clamp((power * 1.4f + aero * 0.6f) / Mathf.Max(14f, mass * 0.45f), 0.35f, 1.45f);
            float aeroNorm = Mathf.Clamp(aero / 10f, 0.15f, 1.0f);
            float weightNorm = Mathf.Clamp((mass - 12f) / 36f, 0.10f, 1.0f);

            VehicleAeroStats stats = new VehicleAeroStats
            {
                brickCount = count,
                speedRating = Mathf.Clamp(Mathf.RoundToInt(2 + powerRatio * 2.6f + aeroNorm * 1.1f), 1, 6),
                weightRating = Mathf.Clamp(Mathf.RoundToInt(1 + weightNorm * 5f + (wheelSizeTier == WheelSizeTier.Large ? 1f : 0f)), 1, 6),
                aeroRating = Mathf.Clamp(Mathf.RoundToInt(1 + aeroNorm * 5f), 1, 6),
                topSpeedMultiplier = Mathf.Clamp(0.92f + powerRatio * 0.14f + aeroNorm * 0.10f + wheelSpeedAdj, 0.84f, 1.24f),
                accelMultiplier = Mathf.Clamp(1.06f + power * 0.03f - weightNorm * 0.22f + wheelAccelAdj, 0.80f, 1.28f),
                gripMultiplier = Mathf.Clamp(0.90f + aeroNorm * 0.20f + wheelGripAdj, 0.82f, 1.28f),
                offRoadMultiplier = wheelOffRoad
            };
            return stats;
        }

        public string ExportToJson()
        {
            SerializedLoadoutData data = new SerializedLoadoutData
            {
                street = streetBricks,
                offRoad = streetBricks,
                water = streetBricks,
                wheelSizeTier = (int)wheelSizeTier
            };
            return JsonUtility.ToJson(data);
        }

        public bool ImportFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;
            try
            {
                SerializedLoadoutData data = JsonUtility.FromJson<SerializedLoadoutData>(json);
                if (data != null && data.street != null && data.street.Count > 0)
                {
                    streetBricks = data.street;
                    offRoadBricks = streetBricks;
                    waterBricks = streetBricks;
                    wheelSizeTier = (WheelSizeTier)Mathf.Clamp(data.wheelSizeTier, 0, 2);
                    return true;
                }
            }
            catch
            {
                // Ignore malformed json
            }
            return false;
        }

        public void SaveToPrefs()
        {
            if (!useMinimalBaseByDefault) return;
            string json = ExportToJson();
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();

            if (LegoMultiplayerRoomManager.Instance != null)
            {
                LegoMultiplayerRoomManager.Instance.BroadcastMyCustomCarLoadout();
            }
        }

        public bool TryLoadFromPrefs()
        {
            if (!PlayerPrefs.HasKey(SAVE_KEY)) return false;
            string json = PlayerPrefs.GetString(SAVE_KEY, "");
            return ImportFromJson(json);
        }

        public void ResetAllToSmallBareBase()
        {
            streetBricks = CreateMinimalStreetBase(primaryColor, accentColor);
            offRoadBricks = streetBricks;
            waterBricks = streetBricks;
            SaveToPrefs();
        }

        public void ResetFormToSmallBareBase(TerrainType form)
        {
            ResetAllToSmallBareBase();
        }

        public void ResetAllToFullTemplates()
        {
            streetBricks = CreateCompactRivalStreetBlueprint(primaryColor, accentColor);
            offRoadBricks = streetBricks;
            waterBricks = streetBricks;
        }

        public void LoadFullTemplateForForm(TerrainType form)
        {
            streetBricks = CreateCompactRivalStreetBlueprint(primaryColor, accentColor);
            offRoadBricks = streetBricks;
            waterBricks = streetBricks;
            SaveToPrefs();
        }

        // =====================================================================
        // SOLID, CHUNKY SMALL CHASSIS BASES (KHUNG GẦM NHỎ NHƯNG DÀY DẶN & CHẮC CHẮN)
        // =====================================================================

        /// <summary>
        /// Small Street Chassis Base: Thick 2-layer Carbon & Silver Technic Kart Frame + 4 wide wheels + starter decorative blocks.
        /// </summary>
        public static List<LegoBrickSpec> CreateMinimalStreetBase(Color primary, Color accent)
        {
            List<LegoBrickSpec> b = new List<LegoBrickSpec>();

            // Lower Heavy Axle Frame & Studded Chassis Tub (Core chassis)
            b.Add(new LegoBrickSpec(new Vector3Int(0, 1, 0), new Vector3Int(4, 2, 6), LegoPalette.CarbonBlack, BrickShape.StandardBrick, true));
            b.Add(new LegoBrickSpec(new Vector3Int(0, 1, 3), new Vector3Int(4, 2, 1), LegoPalette.SilverMetallic, BrickShape.StandardBrick, true));
            b.Add(new LegoBrickSpec(new Vector3Int(0, 1, -3), new Vector3Int(4, 2, 1), LegoPalette.SilverMetallic, BrickShape.StandardBrick, true));

            // 4 Wide, Chunky Wheels flush with ground
            b.Add(new LegoBrickSpec(new Vector3Int(-3, 0, 2), new Vector3Int(2, 3, 3), LegoPalette.TireBlack, BrickShape.Wheel, true));
            b.Add(new LegoBrickSpec(new Vector3Int(3, 0, 2), new Vector3Int(2, 3, 3), LegoPalette.TireBlack, BrickShape.Wheel, true));
            b.Add(new LegoBrickSpec(new Vector3Int(-3, 0, -2), new Vector3Int(2, 3, 3), LegoPalette.TireBlack, BrickShape.Wheel, true));
            b.Add(new LegoBrickSpec(new Vector3Int(3, 0, -2), new Vector3Int(2, 3, 3), LegoPalette.TireBlack, BrickShape.Wheel, true));

            // Image 1-Style Sleek Orange Sports Car Silhouette (Detachable & customizable parts)
            b.Add(new LegoBrickSpec(new Vector3Int(0, 2, 3), new Vector3Int(4, 1, 2), LegoPalette.DarkGrey, BrickShape.FrontSplitter, false));
            b.Add(new LegoBrickSpec(new Vector3Int(0, 3, 2), new Vector3Int(4, 2, 2), primary, BrickShape.SlopeFront, false));
            b.Add(new LegoBrickSpec(new Vector3Int(-2, 2, 2), new Vector3Int(2, 2, 3), primary, BrickShape.FenderArch, false));
            b.Add(new LegoBrickSpec(new Vector3Int(2, 2, 2), new Vector3Int(2, 2, 3), primary, BrickShape.FenderArch, false));
            b.Add(new LegoBrickSpec(new Vector3Int(-2, 2, -2), new Vector3Int(2, 2, 3), primary, BrickShape.FenderArch, false));
            b.Add(new LegoBrickSpec(new Vector3Int(2, 2, -2), new Vector3Int(2, 2, 3), primary, BrickShape.FenderArch, false));
            b.Add(new LegoBrickSpec(new Vector3Int(-2, 2, 0), new Vector3Int(1, 2, 3), LegoPalette.DarkGrey, BrickShape.SideSkirt, false));
            b.Add(new LegoBrickSpec(new Vector3Int(2, 2, 0), new Vector3Int(1, 2, 3), LegoPalette.DarkGrey, BrickShape.SideSkirt, false));
            b.Add(new LegoBrickSpec(new Vector3Int(0, 3, 0), new Vector3Int(2, 2, 2), LegoPalette.WorkshopWood, BrickShape.SportSeat, false));
            b.Add(new LegoBrickSpec(new Vector3Int(0, 4, 1), new Vector3Int(3, 2, 2), LegoPalette.CyanGlow, BrickShape.Windshield, false));
            b.Add(new LegoBrickSpec(new Vector3Int(0, 3, -2), new Vector3Int(2, 2, 2), accent, BrickShape.V8Engine, false));
            b.Add(new LegoBrickSpec(new Vector3Int(0, 5, -3), new Vector3Int(4, 2, 2), primary, BrickShape.SpoilerWing, false));

            return b;
        }

        /// <summary>
        /// Small Off-Road Chassis Base: Thick raised suspension chassis + 4 chunky knobby tires.
        /// </summary>
        public static List<LegoBrickSpec> CreateMinimalOffRoadBase(Color primary, Color accent)
        {
            List<LegoBrickSpec> b = new List<LegoBrickSpec>();

            // Thick 2-layer Raised Off-Road Frame + Yellow Suspension Blocks
            b.Add(new LegoBrickSpec(new Vector3Int(0, 2, 0), new Vector3Int(4, 2, 6), LegoPalette.DarkGrey, BrickShape.StandardBrick, true));
            b.Add(new LegoBrickSpec(new Vector3Int(0, 2, 3), new Vector3Int(4, 2, 1), LegoPalette.BrightYellow, BrickShape.StandardBrick, true));
            b.Add(new LegoBrickSpec(new Vector3Int(0, 2, -3), new Vector3Int(4, 2, 1), LegoPalette.BrightYellow, BrickShape.StandardBrick, true));

            // 4 Chunky Knobby Off-Road Wheels
            b.Add(new LegoBrickSpec(new Vector3Int(-3, 0, 2), new Vector3Int(2, 4, 3), LegoPalette.TireBlack, BrickShape.Wheel, true));
            b.Add(new LegoBrickSpec(new Vector3Int(3, 0, 2), new Vector3Int(2, 4, 3), LegoPalette.TireBlack, BrickShape.Wheel, true));
            b.Add(new LegoBrickSpec(new Vector3Int(-3, 0, -2), new Vector3Int(2, 4, 3), LegoPalette.TireBlack, BrickShape.Wheel, true));
            b.Add(new LegoBrickSpec(new Vector3Int(3, 0, -2), new Vector3Int(2, 4, 3), LegoPalette.TireBlack, BrickShape.Wheel, true));

            // Chunky Starter Hood & Rollbar Block
            b.Add(new LegoBrickSpec(new Vector3Int(0, 4, 2), new Vector3Int(2, 2, 2), primary, BrickShape.SlopeFront, false));
            b.Add(new LegoBrickSpec(new Vector3Int(0, 4, -2), new Vector3Int(2, 2, 2), accent, BrickShape.StandardBrick, false));

            return b;
        }

        /// <summary>
        /// Small Water Hull Base: Thick, deep 2-layer floating boat hull + solid bow wedge + rear jet thruster.
        /// </summary>
        public static List<LegoBrickSpec> CreateMinimalWaterBase(Color primary, Color accent)
        {
            List<LegoBrickSpec> b = new List<LegoBrickSpec>();

            // Thick 2-layer Floating Hull Base + Sloped Bow
            b.Add(new LegoBrickSpec(new Vector3Int(0, 1, 0), new Vector3Int(4, 2, 6), LegoPalette.PureWhite, BrickShape.StandardBrick, true));
            b.Add(new LegoBrickSpec(new Vector3Int(0, 1, 4), new Vector3Int(4, 2, 2), primary, BrickShape.SlopeFront, true));

            // Chunky Rear Water-Jet Turbine
            b.Add(new LegoBrickSpec(new Vector3Int(0, 3, -2), new Vector3Int(2, 2, 2), LegoPalette.CyanGlow, BrickShape.JetNozzle, false, true));

            return b;
        }

        // =====================================================================
        // COMPACT PRE-BUILT TEMPLATES (DÙNG CHO AI HOẶC KHI BẤM NÚT MẪU CÓ SẴN)
        // =====================================================================

        public static List<LegoBrickSpec> CreateCompactRivalStreetBlueprint(Color bodyColor, Color stripeColor)
        {
            List<LegoBrickSpec> b = CreateMinimalStreetBase(bodyColor, stripeColor);
            b.Add(new LegoBrickSpec(new Vector3Int(0, 3, 3), new Vector3Int(4, 2, 2), bodyColor, BrickShape.SlopeFront, false));
            b.Add(new LegoBrickSpec(new Vector3Int(-1, 3, 0), new Vector3Int(1, 2, 4), bodyColor, BrickShape.StandardBrick, false));
            b.Add(new LegoBrickSpec(new Vector3Int(1, 3, 0), new Vector3Int(1, 2, 4), bodyColor, BrickShape.StandardBrick, false));
            b.Add(new LegoBrickSpec(new Vector3Int(0, 5, 1), new Vector3Int(2, 2, 2), LegoPalette.CyanGlow, BrickShape.Windshield, false));
            b.Add(new LegoBrickSpec(new Vector3Int(0, 5, -1), new Vector3Int(2, 2, 2), LegoPalette.BrightYellow, BrickShape.StandardBrick, true));
            b.Add(new LegoBrickSpec(new Vector3Int(0, 5, -3), new Vector3Int(4, 2, 2), stripeColor, BrickShape.SpoilerWing, false));
            return b;
        }

        public static List<LegoBrickSpec> CreateCompactRivalOffRoadBlueprint(Color bodyColor, Color accentColor)
        {
            List<LegoBrickSpec> b = CreateMinimalOffRoadBase(bodyColor, accentColor);
            b.Add(new LegoBrickSpec(new Vector3Int(0, 4, 2), new Vector3Int(4, 2, 2), bodyColor, BrickShape.SlopeFront, false));
            b.Add(new LegoBrickSpec(new Vector3Int(-1, 4, 0), new Vector3Int(1, 2, 3), bodyColor, BrickShape.StandardBrick, false));
            b.Add(new LegoBrickSpec(new Vector3Int(1, 4, 0), new Vector3Int(1, 2, 3), bodyColor, BrickShape.StandardBrick, false));
            b.Add(new LegoBrickSpec(new Vector3Int(0, 6, -1), new Vector3Int(2, 2, 2), LegoPalette.BrightYellow, BrickShape.StandardBrick, true));
            b.Add(new LegoBrickSpec(new Vector3Int(0, 6, -3), new Vector3Int(4, 2, 2), accentColor, BrickShape.SpoilerWing, false));
            return b;
        }

        public static List<LegoBrickSpec> CreateCompactRivalBoatBlueprint(Color hullColor, Color deckColor)
        {
            List<LegoBrickSpec> b = CreateMinimalWaterBase(hullColor, deckColor);
            b.Add(new LegoBrickSpec(new Vector3Int(-2, 1, 0), new Vector3Int(1, 2, 6), hullColor, BrickShape.StandardBrick, false));
            b.Add(new LegoBrickSpec(new Vector3Int(2, 1, 0), new Vector3Int(1, 2, 6), hullColor, BrickShape.StandardBrick, false));
            b.Add(new LegoBrickSpec(new Vector3Int(0, 3, 1), new Vector3Int(2, 2, 3), LegoPalette.CyanGlow, BrickShape.Windshield, false));
            b.Add(new LegoBrickSpec(new Vector3Int(0, 3, -1), new Vector3Int(2, 2, 2), LegoPalette.BrightYellow, BrickShape.StandardBrick, true));
            return b;
        }
    }
}
