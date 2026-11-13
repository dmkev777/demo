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
    public class SerializedLoadoutData
    {
        public List<LegoBrickSpec> street = new List<LegoBrickSpec>();
        public List<LegoBrickSpec> offRoad = new List<LegoBrickSpec>();
        public List<LegoBrickSpec> water = new List<LegoBrickSpec>();
    }

    /// <summary>
    /// Stores and generates the 3 vehicle blueprints (Street, OffRoad, Water).
    /// Starts with a SOLID, CHUNKY, COMPACT CHASSIS BASE (Khung gầm nhỏ nhưng dày dặn, chắc chắn)
    /// so every part looks volumetric and sturdy!
    /// </summary>
    public class LegoVehicleLoadout
    {
        private const string SAVE_KEY = "Lego2KDrive_PlayerCustomLoadout_v3";

        public Color primaryColor;
        public Color accentColor;
        public bool useMinimalBaseByDefault;

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

        public List<LegoBrickSpec> GetBricksForForm(TerrainType form)
        {
            switch (form)
            {
                case TerrainType.OffRoad: return offRoadBricks;
                case TerrainType.Water: return waterBricks;
                default: return streetBricks;
            }
        }

        public void SaveToPrefs()
        {
            if (!useMinimalBaseByDefault) return;
            SerializedLoadoutData data = new SerializedLoadoutData
            {
                street = streetBricks,
                offRoad = offRoadBricks,
                water = waterBricks
            };
            string json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(SAVE_KEY, json);
            PlayerPrefs.Save();
        }

        public bool TryLoadFromPrefs()
        {
            if (!PlayerPrefs.HasKey(SAVE_KEY)) return false;
            string json = PlayerPrefs.GetString(SAVE_KEY, "");
            if (string.IsNullOrEmpty(json)) return false;

            try
            {
                SerializedLoadoutData data = JsonUtility.FromJson<SerializedLoadoutData>(json);
                if (data != null && data.street != null && data.street.Count > 0)
                {
                    streetBricks = data.street;
                    offRoadBricks = data.offRoad;
                    waterBricks = data.water;
                    return true;
                }
            }
            catch
            {
                // Ignore corrupted save
            }
            return false;
        }

        public void ResetAllToSmallBareBase()
        {
            streetBricks = CreateMinimalStreetBase(primaryColor, accentColor);
            offRoadBricks = CreateMinimalOffRoadBase(primaryColor, accentColor);
            waterBricks = CreateMinimalWaterBase(primaryColor, accentColor);
            SaveToPrefs();
        }

        public void ResetFormToSmallBareBase(TerrainType form)
        {
            switch (form)
            {
                case TerrainType.Street:
                    streetBricks = CreateMinimalStreetBase(primaryColor, accentColor);
                    break;
                case TerrainType.OffRoad:
                    offRoadBricks = CreateMinimalOffRoadBase(primaryColor, accentColor);
                    break;
                case TerrainType.Water:
                    waterBricks = CreateMinimalWaterBase(primaryColor, accentColor);
                    break;
            }
            SaveToPrefs();
        }

        public void ResetAllToFullTemplates()
        {
            streetBricks = CreateCompactRivalStreetBlueprint(primaryColor, accentColor);
            offRoadBricks = CreateCompactRivalOffRoadBlueprint(primaryColor, accentColor);
            waterBricks = CreateCompactRivalBoatBlueprint(primaryColor, accentColor);
        }

        public void LoadFullTemplateForForm(TerrainType form)
        {
            switch (form)
            {
                case TerrainType.Street:
                    streetBricks = CreateCompactRivalStreetBlueprint(primaryColor, accentColor);
                    break;
                case TerrainType.OffRoad:
                    offRoadBricks = CreateCompactRivalOffRoadBlueprint(primaryColor, accentColor);
                    break;
                case TerrainType.Water:
                    waterBricks = CreateCompactRivalBoatBlueprint(primaryColor, accentColor);
                    break;
            }
            SaveToPrefs();
        }

        // =====================================================================
        // SOLID, CHUNKY SMALL CHASSIS BASES (KHUNG GẦM NHỎ NHƯNG DÀY DẶN & CHẮC CHẮN)
        // =====================================================================

        /// <summary>
        /// Small Street Chassis Base: Thick 2-layer Carbon & Silver Technic Kart Frame + 4 wide wheels + solid bumpers.
        /// </summary>
        public static List<LegoBrickSpec> CreateMinimalStreetBase(Color primary, Color accent)
        {
            List<LegoBrickSpec> b = new List<LegoBrickSpec>();

            // Lower Heavy Axle Frame & Upper Studded Chassis Tub (Thick 2-layer base!)
            b.Add(new LegoBrickSpec(new Vector3Int(0, 1, 0), new Vector3Int(4, 2, 6), LegoPalette.CarbonBlack, BrickShape.StandardBrick, true));
            b.Add(new LegoBrickSpec(new Vector3Int(0, 1, 3), new Vector3Int(4, 2, 1), LegoPalette.SilverMetallic, BrickShape.StandardBrick, true));
            b.Add(new LegoBrickSpec(new Vector3Int(0, 1, -3), new Vector3Int(4, 2, 1), LegoPalette.SilverMetallic, BrickShape.StandardBrick, true));

            // 4 Wide, Chunky Kart Wheels flush with ground
            b.Add(new LegoBrickSpec(new Vector3Int(-3, 0, 2), new Vector3Int(2, 3, 3), LegoPalette.TireBlack, BrickShape.Wheel, true));
            b.Add(new LegoBrickSpec(new Vector3Int(3, 0, 2), new Vector3Int(2, 3, 3), LegoPalette.TireBlack, BrickShape.Wheel, true));
            b.Add(new LegoBrickSpec(new Vector3Int(-3, 0, -2), new Vector3Int(2, 3, 3), LegoPalette.TireBlack, BrickShape.Wheel, true));
            b.Add(new LegoBrickSpec(new Vector3Int(3, 0, -2), new Vector3Int(2, 3, 3), LegoPalette.TireBlack, BrickShape.Wheel, true));

            // Chunky Starter Nose Block & Rear Engine Block (Detachable starter pieces)
            b.Add(new LegoBrickSpec(new Vector3Int(0, 3, 2), new Vector3Int(2, 2, 2), primary, BrickShape.SlopeFront, false));
            b.Add(new LegoBrickSpec(new Vector3Int(0, 3, -2), new Vector3Int(2, 2, 2), accent, BrickShape.StandardBrick, false));

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
