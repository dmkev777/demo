using System.Collections.Generic;
using UnityEngine;

namespace Lego2KDrive
{
    public enum TerrainType
    {
        Street = 0,
        OffRoad = 1,
        Water = 2
    }

    public enum WheelSizeTier
    {
        Small = 0,
        Medium = 1,
        Large = 2
    }

    public enum PartCategory
    {
        Chassis = 0,
        Body = 1,
        Aero = 2,
        Interior = 3,
        Engine = 4
    }

    public enum BrickShape
    {
        StandardBrick = 0,
        SlopeFront = 1,
        Wheel = 2,
        JetNozzle = 3,
        Windshield = 4,
        SpoilerWing = 5,
        FlatTile = 6,
        FenderArch = 7,
        CylinderPart = 8,
        SlopeInverted = 9,
        V8Engine = 10,
        SportSeat = 11,
        SideSkirt = 12,
        FrontSplitter = 13,
        RoofScoop = 14
    }

    public enum PowerUpType
    {
        None = 0,
        HomingBrickMissile = 1,
        EnergyBrickShield = 2,
        SpiderWebMine = 3,
        HyperWarpBoost = 4
    }

    public enum SurfacePattern
    {
        None = 0,
        BrickMasonry = 1,
        StoneAshlar = 2,
        RoofTiles = 3,
        WoodPlanks = 4,
        AsphaltGrain = 5,
        FoliageDetail = 6,
        MeadowTerrain = 7,
        AlpineRock = 8,
        TreeBark = 9
    }

    public static class LegoPalette
    {
        public static readonly Color BrightRed = new Color(0.84f, 0.10f, 0.12f);
        public static readonly Color BrightBlue = new Color(0.06f, 0.38f, 0.84f);
        public static readonly Color BrightYellow = new Color(0.96f, 0.76f, 0.04f);
        public static readonly Color BrightGreen = new Color(0.12f, 0.68f, 0.22f);
        public static readonly Color BrightOrange = new Color(0.95f, 0.42f, 0.05f);
        public static readonly Color PureWhite = new Color(0.92f, 0.93f, 0.94f);
        public static readonly Color LightGrey = new Color(0.65f, 0.68f, 0.71f);
        public static readonly Color SilverMetallic = new Color(0.76f, 0.79f, 0.83f);
        public static readonly Color DarkGrey = new Color(0.24f, 0.26f, 0.29f);
        public static readonly Color CarbonBlack = new Color(0.13f, 0.14f, 0.16f);
        public static readonly Color TireBlack = new Color(0.08f, 0.08f, 0.10f);
        public static readonly Color CyanGlow = new Color(0.10f, 0.82f, 0.95f);
        public static readonly Color PurpleRival = new Color(0.55f, 0.15f, 0.82f);
        public static readonly Color LimeRival = new Color(0.42f, 0.82f, 0.10f);
        public static readonly Color GoldChrome = new Color(0.94f, 0.70f, 0.14f);
        public static readonly Color TanSand = new Color(0.80f, 0.68f, 0.46f);
        public static readonly Color CanyonTerracotta = new Color(0.72f, 0.36f, 0.20f);

        // Low-poly palette (Image 2 & 3 style — warm flat-shaded colors, zero outlines)
        public static readonly Color LowPolyGrassBright = new Color(0.34f, 0.58f, 0.22f);
        public static readonly Color LowPolyGrassWarm = new Color(0.44f, 0.62f, 0.24f);
        public static readonly Color LowPolyPineDark = new Color(0.16f, 0.42f, 0.22f);
        public static readonly Color LowPolyPineMid = new Color(0.22f, 0.52f, 0.27f);
        public static readonly Color LowPolyTrunkBrown = new Color(0.45f, 0.29f, 0.17f);
        public static readonly Color LowPolyMountainSlate = new Color(0.46f, 0.52f, 0.62f);
        public static readonly Color LowPolyMountainGreen = new Color(0.36f, 0.50f, 0.34f);
        public static readonly Color LowPolyMountainSnow = new Color(0.84f, 0.88f, 0.93f);
        public static readonly Color LowPolyRockGrey = new Color(0.54f, 0.57f, 0.60f);
        public static readonly Color LowPolyLakeCyan = new Color(0.16f, 0.68f, 0.84f);
        public static readonly Color LowPolySeaBlue = new Color(0.14f, 0.58f, 0.80f);
        public static readonly Color LowPolyAsphalt = new Color(0.24f, 0.26f, 0.30f);
        public static readonly Color LowPolyDirtTrack = new Color(0.66f, 0.46f, 0.28f);
        public static readonly Color LowPolyDirtShoulder = new Color(0.76f, 0.58f, 0.38f);
        public static readonly Color WorkshopBrickRed = new Color(0.76f, 0.33f, 0.24f);
        public static readonly Color WorkshopTeal = new Color(0.15f, 0.56f, 0.54f);
        public static readonly Color WorkshopConcrete = new Color(0.72f, 0.73f, 0.75f);
        public static readonly Color WorkshopWood = new Color(0.70f, 0.50f, 0.29f);
        public static readonly Color CloudWarmWhite = new Color(0.99f, 0.96f, 0.90f);
        public static readonly Color SunWarmGold = new Color(1.00f, 0.92f, 0.62f);

        public static readonly Color[] GarageColors = new Color[]
        {
            BrightRed,
            BrightBlue,
            BrightYellow,
            BrightGreen,
            BrightOrange,
            PureWhite,
            CarbonBlack,
            PurpleRival,
            CyanGlow
        };

        public static readonly string[] GarageColorNames = new string[]
        {
            "Red", "Blue", "Yellow", "Green", "Orange", "White", "Carbon", "Purple", "Cyan"
        };
    }

    public class TerrainSurfaceTag : MonoBehaviour
    {
        public TerrainType terrainType = TerrainType.Street;
    }

    /// <summary>
    /// High-Volume, Chunky, Injection-Molded LEGO Mesh & Material Factory.
    /// Every triangle is mathematically verified to wind outward (Vector3.Cross(b-a, c-a) dot outwardNormal > 0)
    /// so no face can ever render inside-out!
    /// </summary>
    public static class LegoMeshAndMaterialFactory
    {
        public const float STUD_UNIT = 0.40f;   // Plump, solid LEGO stud spacing
        public const float PLATE_UNIT = 0.28f;  // Thick, hefty vertical plate unit

        private static readonly Dictionary<string, Mesh> _meshCache = new Dictionary<string, Mesh>();
        private static readonly Dictionary<string, Material> _matCache = new Dictionary<string, Material>();
        private static Shader _cachedLitShader;

        public static Vector3 GetVelocity(Rigidbody rb)
        {
#if UNITY_6000_0_OR_NEWER
            return rb.linearVelocity;
#else
            return rb.velocity;
#endif
        }

        public static void SetVelocity(Rigidbody rb, Vector3 v)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }

        public static void SetAngularVelocity(Rigidbody rb, Vector3 av)
        {
#if UNITY_6000_0_OR_NEWER
            rb.angularVelocity = av;
#else
            rb.angularVelocity = av;
#endif
        }

        private static readonly Dictionary<string, Material> _holoCache = new Dictionary<string, Material>();
        private static readonly Dictionary<string, Material> _lowPolyMatCache = new Dictionary<string, Material>();
        private static readonly Dictionary<string, Mesh> _lowPolyMeshCache = new Dictionary<string, Mesh>();
        private static readonly Dictionary<SurfacePattern, Texture2D> _patternTexCache = new Dictionary<SurfacePattern, Texture2D>();
        private static Shader _cachedLowPolyShader;

        public static Texture2D GetPatternTexture(SurfacePattern pattern)
        {
            if (pattern == SurfacePattern.None) return Texture2D.whiteTexture;
            if (_patternTexCache.TryGetValue(pattern, out Texture2D cached) && cached != null)
                return cached;

            const int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true, false);
            tex.name = $"Pattern_{pattern}";
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Trilinear;
            tex.anisoLevel = 8;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float v = 1f;
                    int hash = ((x * 73856093) ^ (y * 19349663)) & 0xFFFF;
                    float fineNoise = ((hash % 100) / 100f - 0.5f);

                    switch (pattern)
                    {
                        case SurfacePattern.BrickMasonry:
                        {
                            int brickH = 32;
                            int brickW = 64;
                            int row = y / brickH;
                            int ly = y % brickH;
                            int xShift = (row % 2 == 1) ? brickW / 2 : 0;
                            int col = (x + xShift) / brickW;
                            int lx = (x + xShift) % brickW;
                            bool isMortar = ly < 4 || lx < 4;
                            if (isMortar)
                                v = 0.62f + fineNoise * 0.06f;
                            else
                            {
                                int bHash = ((row * 374761393) ^ (col * 668265263)) & 0xFF;
                                float bVar = (bHash / 255f - 0.5f) * 0.16f;
                                float edgeShade = (ly < 6 || lx < 6) ? 0.94f : 1.02f;
                                v = (0.94f + bVar + fineNoise * 0.08f) * edgeShade;
                            }
                            break;
                        }
                        case SurfacePattern.StoneAshlar:
                        {
                            int courseH = 42;
                            int row = y / courseH;
                            int ly = y % courseH;
                            int stoneW = (row % 3 == 0) ? 85 : 64;
                            int shift = (row % 2 == 1) ? stoneW / 2 : 18;
                            int col = (x + shift) / stoneW;
                            int lx = (x + shift) % stoneW;
                            if (ly < 3 || lx < 3)
                                v = 0.58f + fineNoise * 0.05f;
                            else
                            {
                                int sHash = ((row * 1299709) ^ (col * 2750159)) & 0xFF;
                                float tone = (sHash / 255f - 0.5f) * 0.18f;
                                float bevel = (ly > courseH - 5) ? 1.06f : ((ly < 6) ? 0.88f : 1.0f);
                                v = (0.94f + tone + fineNoise * 0.10f) * bevel;
                            }
                            break;
                        }
                        case SurfacePattern.RoofTiles:
                        {
                            int tileH = 32;
                            int tileW = 32;
                            int row = y / tileH;
                            int ly = y % tileH;
                            int xShift = (row % 2 == 1) ? tileW / 2 : 0;
                            int lx = (x + xShift) % tileW;
                            float arch = Mathf.Sin((lx / (float)tileW) * Mathf.PI);
                            float overlapDark = ly < 5 ? 0.60f : (0.86f + 0.18f * (ly / (float)tileH));
                            float seamDark = (lx < 3 || lx > tileW - 3) ? 0.78f : 1.0f;
                            v = overlapDark * seamDark * (0.92f + arch * 0.12f + fineNoise * 0.06f);
                            break;
                        }
                        case SurfacePattern.WoodPlanks:
                        {
                            int plankW = 32;
                            int pIdx = x / plankW;
                            int lx = x % plankW;
                            if (lx < 2)
                                v = 0.58f;
                            else
                            {
                                int pHash = (pIdx * 2654435761u.GetHashCode()) & 0xFF;
                                float plankTone = (pHash / 255f - 0.5f) * 0.14f;
                                float grain = Mathf.Sin(y * 0.14f + lx * 1.15f + pIdx * 2.1f) * 0.07f;
                                v = 0.94f + plankTone + grain + fineNoise * 0.04f;
                            }
                            break;
                        }
                        case SurfacePattern.AsphaltGrain:
                        {
                            int h2 = ((x * 1619) ^ (y * 31337)) & 0xFF;
                            float grit = (h2 / 255f - 0.5f) * 0.22f;
                            v = 0.95f + grit;
                            break;
                        }
                        case SurfacePattern.FoliageDetail:
                        {
                            float tauX = (x / (float)size) * Mathf.PI * 2f;
                            float tauY = (y / (float)size) * Mathf.PI * 2f;
                            float clusterWave = 0.5f + 0.5f * Mathf.Sin(tauX * 3f + Mathf.Cos(tauY * 2f)) * Mathf.Cos(tauY * 3f - Mathf.Sin(tauX * 2f));
                            float sprayA = Mathf.Abs(Mathf.Sin(tauX * 8f + tauY * 5f + Mathf.Sin(tauY * 4f)));
                            float sprayB = Mathf.Abs(Mathf.Cos(tauX * 13f - tauY * 11f + Mathf.Cos(tauX * 6f)));
                            float needles = Mathf.Clamp01(sprayA * 0.58f + sprayB * 0.42f);

                            Color deepShadow = new Color(0.55f, 0.72f, 0.50f);
                            Color richEmerald = new Color(0.76f, 1.00f, 0.60f);
                            Color sunlitTip = new Color(1.12f, 1.16f, 0.76f);

                            Color c = Color.Lerp(deepShadow, richEmerald, Mathf.SmoothStep(0.18f, 0.78f, needles * 0.72f + clusterWave * 0.28f));
                            float tipMask = Mathf.SmoothStep(0.62f, 0.94f, needles * clusterWave + fineNoise * 0.10f);
                            c = Color.Lerp(c, sunlitTip, tipMask * 0.85f);
                            pixels[y * size + x] = new Color(
                                Mathf.Clamp(c.r + fineNoise * 0.06f, 0.44f, 1.24f),
                                Mathf.Clamp(c.g + fineNoise * 0.06f, 0.52f, 1.26f),
                                Mathf.Clamp(c.b + fineNoise * 0.04f, 0.38f, 1.10f),
                                1f);
                            continue;
                        }
                        case SurfacePattern.AlpineRock:
                        {
                            float tauX = (x / (float)size) * Mathf.PI * 2f;
                            float tauY = (y / (float)size) * Mathf.PI * 2f;

                            // Tilted geological cliff strata & jagged granite fracture lines
                            float warp = 0.45f * Mathf.Sin(tauX * 2f + tauY) + 0.25f * Mathf.Cos(tauX - tauY * 3f);
                            float strata = 0.5f + 0.5f * Mathf.Sin(tauY * 7f + tauX * 2f + warp * 2.4f);
                            float ridge = Mathf.Abs(Mathf.Sin(tauX * 5f - tauY * 6f + Mathf.Cos(tauY * 4f) * 1.4f));
                            float fissureRaw = Mathf.Min(Mathf.Abs(Mathf.Sin(tauX * 4f + tauY * 5f + warp)), Mathf.Abs(Mathf.Cos(tauX * 7f - tauY * 3f - warp)));
                            float crackDark = Mathf.SmoothStep(0.02f, 0.22f, fissureRaw);

                            Color graniteShadow = new Color(0.48f, 0.52f, 0.60f);
                            Color slateFacet    = new Color(0.74f, 0.82f, 0.94f);
                            Color limeStreak    = new Color(1.04f, 1.00f, 0.90f);
                            Color mossHollow    = new Color(0.60f, 0.80f, 0.52f);

                            Color rock = Color.Lerp(slateFacet, graniteShadow, (1f - ridge) * 0.65f);
                            rock = Color.Lerp(rock, limeStreak, Mathf.SmoothStep(0.55f, 0.92f, strata) * 0.68f);

                            float mossWave = 0.5f + 0.5f * Mathf.Sin(tauX * 3f - tauY * 2f + 1.1f * Mathf.Cos(tauX * 2f));
                            float mossMask = Mathf.SmoothStep(0.68f, 0.90f, mossWave * (1f - ridge * 0.45f));
                            rock = Color.Lerp(rock, mossHollow, mossMask * 0.45f);

                            float shade = Mathf.Lerp(0.48f, 1.08f, crackDark * (0.76f + strata * 0.24f));
                            pixels[y * size + x] = new Color(
                                Mathf.Clamp(rock.r * shade + fineNoise * 0.08f, 0.34f, 1.20f),
                                Mathf.Clamp(rock.g * shade + fineNoise * 0.08f, 0.38f, 1.20f),
                                Mathf.Clamp(rock.b * shade + fineNoise * 0.08f, 0.40f, 1.22f),
                                1f);
                            continue;
                        }
                        case SurfacePattern.TreeBark:
                        {
                            float tauX = (x / (float)size) * Mathf.PI * 2f;
                            float tauY = (y / (float)size) * Mathf.PI * 2f;

                            float sway = 0.45f * Mathf.Sin(tauY * 3f + Mathf.Cos(tauX * 2f));
                            float furrowWave = Mathf.Abs(Mathf.Sin(tauX * 9f + sway));
                            float subGrain = 0.5f + 0.5f * Mathf.Sin(tauY * 14f + tauX * 3f + fineNoise * 0.6f);
                            float knotDist = Mathf.Abs(Mathf.Sin(tauX * 2f + 0.4f)) + Mathf.Abs(Mathf.Sin(tauY * 2f - 0.7f));
                            float knotRing = Mathf.SmoothStep(0.55f, 0.12f, knotDist);

                            Color fissureCol = new Color(0.46f, 0.34f, 0.26f);
                            Color ridgeCol   = new Color(1.08f, 0.86f, 0.68f);
                            Color barkMoss   = new Color(0.54f, 0.70f, 0.44f);

                            float ridgeMask = Mathf.SmoothStep(0.16f, 0.72f, furrowWave);
                            Color bark = Color.Lerp(fissureCol, ridgeCol, ridgeMask * (0.78f + subGrain * 0.22f));
                            bark = Color.Lerp(bark, fissureCol * 0.88f, knotRing * 0.60f);

                            float mossSignal = Mathf.SmoothStep(0.76f, 0.96f, 0.5f + 0.5f * Mathf.Cos(tauX * 3f + tauY * 2f)) * (1f - ridgeMask);
                            bark = Color.Lerp(bark, barkMoss, mossSignal * 0.65f);

                            pixels[y * size + x] = new Color(
                                Mathf.Clamp(bark.r + fineNoise * 0.07f, 0.32f, 1.18f),
                                Mathf.Clamp(bark.g + fineNoise * 0.06f, 0.26f, 1.04f),
                                Mathf.Clamp(bark.b + fineNoise * 0.05f, 0.20f, 0.88f),
                                1f);
                            continue;
                        }
                        case SurfacePattern.MeadowTerrain:
                        {
                            // Full-color seamlessly tileable grass/clover/sunlit-lime/soil meadow texture
                            float tauX = (x / (float)size) * Mathf.PI * 2f;
                            float tauY = (y / (float)size) * Mathf.PI * 2f;

                            float patchWave = 0.5f + 0.5f * Mathf.Sin(tauX + Mathf.Cos(tauY)) * Mathf.Cos(tauY * 2f - Mathf.Sin(tauX * 2f));
                            float warmWave = 0.5f + 0.5f * Mathf.Sin(tauX * 2f - tauY + 0.8f * Mathf.Sin(tauX * 3f + tauY * 2f));
                            float blade = Mathf.Abs(Mathf.Sin(tauX * 14f + Mathf.Sin(tauY * 6f)) * Mathf.Cos(tauY * 22f + Mathf.Cos(tauX * 5f)));
                            float rootcrevice = Mathf.Pow(blade, 1.35f);

                            Color lushEmerald = new Color(0.68f, 1.06f, 0.58f);
                            Color sunlitLime  = new Color(1.04f, 1.18f, 0.54f);
                            Color oliveClover = new Color(0.76f, 0.96f, 0.56f);
                            Color earthyLoam  = new Color(1.12f, 0.88f, 0.56f);

                            Color turf = Color.Lerp(lushEmerald, oliveClover, patchWave);
                            turf = Color.Lerp(turf, sunlitLime, Mathf.SmoothStep(0.35f, 0.85f, warmWave) * 0.72f);

                            float soilRaw = 0.5f + 0.5f * Mathf.Sin(tauX * 3f + tauY * 2f + 1.4f * Mathf.Cos(tauX * 2f - tauY * 3f));
                            float soilMask = Mathf.SmoothStep(0.66f, 0.88f, soilRaw + fineNoise * 0.16f);
                            Color baseRgb = Color.Lerp(turf, earthyLoam, soilMask * 0.82f);

                            float contrast = 0.62f + rootcrevice * 0.56f + fineNoise * 0.14f;
                            pixels[y * size + x] = new Color(
                                Mathf.Clamp(baseRgb.r * contrast, 0.45f, 1.24f),
                                Mathf.Clamp(baseRgb.g * contrast, 0.48f, 1.26f),
                                Mathf.Clamp(baseRgb.b * contrast, 0.36f, 1.12f),
                                1f);
                            continue;
                        }
                    }

                    v = Mathf.Clamp(v, 0.40f, 1.12f);
                    pixels[y * size + x] = new Color(v, v, v, 1f);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(true, false);
            _patternTexCache[pattern] = tex;
            return tex;
        }

        public static Material GetLowPolyMaterial(Color color, bool emissive = false, float smoothness = 0.14f, float metallic = 0.0f)
        {
            return GetTexturedLowPolyMaterial(color, SurfacePattern.None, smoothness, metallic, 0.45f, 0.0f, 0.0f, emissive);
        }

        public static Material GetSwayingFoliageMaterial(Color color, float windSway = 0.78f)
        {
            return GetTexturedLowPolyMaterial(color, SurfacePattern.FoliageDetail, 0.12f, 0.0f, 1.45f, 0.48f, windSway, false);
        }

        public static Material GetTexturedLowPolyMaterial(
            Color color,
            SurfacePattern pattern,
            float smoothness = 0.16f,
            float metallic = 0.0f,
            float patternScale = 0.45f,
            float patternStrength = 0.34f,
            float windSway = 0.0f,
            bool emissive = false)
        {
            float clampedSmooth = emissive ? Mathf.Clamp01(smoothness) : (smoothness <= 0.30f ? Mathf.Min(smoothness, 0.28f) : Mathf.Clamp(smoothness, 0.31f, 0.85f));
            bool matteStyle = !emissive && metallic < 0.15f && clampedSmooth <= 0.30f;
            float effStrength = pattern == SurfacePattern.None ? 0f : Mathf.Clamp01(patternStrength);
            string key = $"lp_{color.r:F3}_{color.g:F3}_{color.b:F3}_{emissive}_{clampedSmooth:F2}_{metallic:F2}_{matteStyle}_{pattern}_{patternScale:F2}_{effStrength:F2}_{windSway:F2}";
            if (_lowPolyMatCache.TryGetValue(key, out Material cached) && cached != null)
                return cached;

            if (_cachedLowPolyShader == null)
            {
                _cachedLowPolyShader = Resources.Load<Shader>("Shaders/MotorverseLowPolyLit");
                if (_cachedLowPolyShader == null) _cachedLowPolyShader = Shader.Find("Custom/MotorverseLowPolyLit");
                if (_cachedLowPolyShader == null) _cachedLowPolyShader = Shader.Find("Standard");
                if (_cachedLowPolyShader == null) _cachedLowPolyShader = Shader.Find("Diffuse");
            }

            Material mat = new Material(_cachedLowPolyShader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", GetPatternTexture(pattern));
            if (mat.HasProperty("_PatternScale")) mat.SetFloat("_PatternScale", patternScale);
            if (mat.HasProperty("_PatternStrength")) mat.SetFloat("_PatternStrength", effStrength);
            if (mat.HasProperty("_WindSway")) mat.SetFloat("_WindSway", windSway);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", clampedSmooth);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", clampedSmooth);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

            if (matteStyle)
            {
                if (mat.HasProperty("_SpecularHighlights")) mat.SetFloat("_SpecularHighlights", 0f);
                if (mat.HasProperty("_GlossyReflections")) mat.SetFloat("_GlossyReflections", 0f);
                if (mat.HasProperty("_EnvironmentReflections")) mat.SetFloat("_EnvironmentReflections", 0f);
                mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                mat.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
                mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            }

            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * 1.28f);
            }
            else if (mat.HasProperty("_EmissionColor"))
            {
                mat.SetColor("_EmissionColor", Color.black);
            }

            mat.enableInstancing = true;
            _lowPolyMatCache[key] = mat;
            return mat;
        }

        public static Material GetHologramMaterial(bool isValid)
        {
            string key = isValid ? "holo_valid" : "holo_invalid";
            if (_holoCache.TryGetValue(key, out Material cached) && cached != null)
                return cached;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");

            Material mat = new Material(shader);
            Color baseCol = isValid ? new Color(0.18f, 0.85f, 1.0f, 0.42f) : new Color(1.0f, 0.24f, 0.22f, 0.44f);
            Color emitCol = isValid ? new Color(0.12f, 0.78f, 1.0f) * 1.15f : new Color(1.0f, 0.18f, 0.16f) * 1.15f;

            mat.color = baseCol;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseCol);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.55f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.55f);
            if (mat.HasProperty("_Mode"))
            {
                mat.SetFloat("_Mode", 3f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }
            if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emitCol);
            _holoCache[key] = mat;
            return mat;
        }

        public static float GetWheelRadiusForTier(WheelSizeTier tier)
        {
            switch (tier)
            {
                case WheelSizeTier.Small: return 0.34f;
                case WheelSizeTier.Large: return 0.56f;
                default: return 0.44f;
            }
        }

        public static float GetWheelScaleForTier(WheelSizeTier tier)
        {
            switch (tier)
            {
                case WheelSizeTier.Small: return 0.82f;
                case WheelSizeTier.Large: return 1.22f;
                default: return 1.00f;
            }
        }

        public static Mesh GetWheelMeshForTier(WheelSizeTier tier)
        {
            string key = $"wheel_tier_{tier}";
            if (_lowPolyMeshCache.TryGetValue(key, out Mesh cached) && cached != null)
                return cached;

            float radius = GetWheelRadiusForTier(tier);
            float width = tier == WheelSizeTier.Large ? 0.50f : (tier == WheelSizeTier.Small ? 0.28f : 0.38f);
            Mesh m = BuildFlatWheelMesh(radius, width, 12);
            _lowPolyMeshCache[key] = m;
            return m;
        }

        private static Mesh LoadObjMeshOrNull(string resourceSubPath)
        {
            Mesh[] loaded = Resources.LoadAll<Mesh>(resourceSubPath);
            if (loaded != null && loaded.Length > 0 && loaded[0] != null && loaded[0].isReadable && loaded[0].vertexCount > 0)
            {
                return loaded[0];
            }
            return null;
        }

        private static Mesh CloneFlatShadedWithContrast(Mesh source)
        {
            Vector3[] srcVerts = source.vertices;
            int[] srcTris = source.triangles;
            Vector3[] flatVerts = new Vector3[srcTris.Length];
            int[] flatTris = new int[srcTris.Length];
            Vector2[] flatUvs = new Vector2[srcTris.Length];

            for (int i = 0; i < srcTris.Length; i++)
            {
                Vector3 v = srcVerts[srcTris[i]];
                flatVerts[i] = v;
                flatTris[i] = i;
                flatUvs[i] = new Vector2(v.x, v.z);
            }

            Mesh m = new Mesh { name = source.name + "_Flat" };
            m.vertices = flatVerts;
            m.triangles = flatTris;
            m.uv = flatUvs;
            m.RecalculateNormals();
            ApplyFacetVertexContrast(m);
            m.RecalculateBounds();
            return m;
        }

        private static void ApplyFacetVertexContrast(Mesh mesh)
        {
            Vector3[] verts = mesh.vertices;
            Vector3[] normals = mesh.normals;
            if (verts == null || normals == null || verts.Length != normals.Length || verts.Length == 0)
                return;

            Bounds b = mesh.bounds;
            if (b.size.y < 0.001f) mesh.RecalculateBounds();
            b = mesh.bounds;
            float invH = 1f / Mathf.Max(0.05f, b.size.y);
            Vector3 sunDir = new Vector3(-0.35f, 0.65f, 0.67f).normalized;

            Color[] colors = new Color[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                float hNorm = Mathf.Clamp01((verts[i].y - b.min.y) * invH);
                float ndl = Vector3.Dot(normals[i], sunDir) * 0.5f + 0.5f;
                float shade = Mathf.Lerp(0.72f, 1.08f, ndl * 0.62f + hNorm * 0.38f);
                colors[i] = new Color(shade * 1.01f, shade * 0.99f, shade * 0.95f, 1f);
            }
            mesh.colors = colors;
        }

        private static Mesh CloneAndFitMeshToBounds(Mesh source, Bounds targetBounds)
        {
            Bounds sb = source.bounds;
            Vector3 sSize = new Vector3(
                Mathf.Max(0.001f, sb.size.x),
                Mathf.Max(0.001f, sb.size.y),
                Mathf.Max(0.001f, sb.size.z));

            Vector3[] srcVerts = source.vertices;
            int[] srcTris = source.triangles;
            Vector3[] flatVerts = new Vector3[srcTris.Length];
            int[] flatTris = new int[srcTris.Length];
            Vector2[] flatUvs = new Vector2[srcTris.Length];

            for (int i = 0; i < srcTris.Length; i++)
            {
                Vector3 v = srcVerts[srcTris[i]];
                float nx = ((v.x - sb.center.x) / sSize.x) * targetBounds.size.x + targetBounds.center.x;
                float ny = ((v.y - sb.min.y) / sSize.y) * targetBounds.size.y + targetBounds.min.y;
                float nz = ((v.z - sb.center.z) / sSize.z) * targetBounds.size.z + targetBounds.center.z;
                flatVerts[i] = new Vector3(nx, ny, nz);
                flatTris[i] = i;
                flatUvs[i] = new Vector2(nx, nz);
            }

            Mesh m = new Mesh();
            m.name = source.name + "_Fit";
            m.vertices = flatVerts;
            m.triangles = flatTris;
            m.uv = flatUvs;
            m.RecalculateNormals();
            m.RecalculateBounds();
            ApplyFacetVertexContrast(m);
            return m;
        }

        private static void AppendFlatBox(List<Vector3> verts, List<int> tris, Vector3 center, Vector3 size)
        {
            Vector3 h = size * 0.5f;
            Vector3[] corners = new Vector3[]
            {
                center + new Vector3(-h.x, -h.y, -h.z),
                center + new Vector3( h.x, -h.y, -h.z),
                center + new Vector3( h.x,  h.y, -h.z),
                center + new Vector3(-h.x,  h.y, -h.z),
                center + new Vector3(-h.x, -h.y,  h.z),
                center + new Vector3( h.x, -h.y,  h.z),
                center + new Vector3( h.x,  h.y,  h.z),
                center + new Vector3(-h.x,  h.y,  h.z)
            };
            int[,] faces = new int[,]
            {
                {0, 2, 1, 0, 3, 2},
                {4, 5, 6, 4, 6, 7},
                {3, 6, 2, 3, 7, 6},
                {0, 1, 5, 0, 5, 4},
                {0, 4, 7, 0, 7, 3},
                {1, 2, 6, 1, 6, 5}
            };
            for (int f = 0; f < 6; f++)
            {
                int baseIdx = verts.Count;
                verts.Add(corners[faces[f, 0]]);
                verts.Add(corners[faces[f, 1]]);
                verts.Add(corners[faces[f, 2]]);
                tris.Add(baseIdx);
                tris.Add(baseIdx + 1);
                tris.Add(baseIdx + 2);

                int baseIdx2 = verts.Count;
                verts.Add(corners[faces[f, 3]]);
                verts.Add(corners[faces[f, 4]]);
                verts.Add(corners[faces[f, 5]]);
                tris.Add(baseIdx2);
                tris.Add(baseIdx2 + 1);
                tris.Add(baseIdx2 + 2);
            }
        }

        private static void AppendFlatQuadOutward(List<Vector3> verts, List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 interior)
        {
            AppendFlatTriOutward(verts, tris, a, b, c, interior);
            AppendFlatTriOutward(verts, tris, a, c, d, interior);
        }

        private static void AppendFlatTriOutward(List<Vector3> verts, List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 interior)
        {
            Vector3 center = (a + b + c) / 3f - interior;
            Vector3 normal = Vector3.Cross(b - a, c - a);
            int idx = verts.Count;
            verts.Add(a);
            if (Vector3.Dot(normal, center) >= 0f)
            {
                verts.Add(b);
                verts.Add(c);
            }
            else
            {
                verts.Add(c);
                verts.Add(b);
            }
            tris.Add(idx);
            tris.Add(idx + 1);
            tris.Add(idx + 2);
        }

        private static Mesh FinalizeFlatMesh(string name, List<Vector3> verts, List<int> tris)
        {
            Mesh m = new Mesh();
            m.name = name;
            Vector2[] uvs = new Vector2[verts.Count];
            for (int i = 0; i < verts.Count; i++)
                uvs[i] = new Vector2(verts[i].x, verts[i].z);
            m.vertices = verts.ToArray();
            m.triangles = tris.ToArray();
            m.uv = uvs;
            m.RecalculateNormals();
            m.RecalculateBounds();
            ApplyFacetVertexContrast(m);
            return m;
        }

        private static Bounds GetTileBounds(int studsX, int platesY, int studsZ)
        {
            Mesh tile = BuildStandardLegoBrickMesh(Mathf.Max(1, studsX), Mathf.Max(1, platesY), Mathf.Max(1, studsZ), false);
            Bounds b = tile.bounds;
            UnityEngine.Object.DestroyImmediate(tile);
            return b;
        }

        private static Mesh BuildFlatWheelMesh(float radius, float width, int sides)
        {
            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            float hw = width * 0.5f;
            Vector3 interior = Vector3.zero;
            for (int i = 0; i < sides; i++)
            {
                float a0 = (i / (float)sides) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)sides) * Mathf.PI * 2f;
                Vector3 l0 = new Vector3(-hw, Mathf.Sin(a0) * radius, Mathf.Cos(a0) * radius);
                Vector3 l1 = new Vector3(-hw, Mathf.Sin(a1) * radius, Mathf.Cos(a1) * radius);
                Vector3 r0 = new Vector3( hw, Mathf.Sin(a0) * radius, Mathf.Cos(a0) * radius);
                Vector3 r1 = new Vector3( hw, Mathf.Sin(a1) * radius, Mathf.Cos(a1) * radius);
                AppendFlatQuadOutward(verts, tris, l0, l1, r1, r0, interior);
                AppendFlatTriOutward(verts, tris, new Vector3(-hw, 0f, 0f), l0, l1, interior);
                AppendFlatTriOutward(verts, tris, new Vector3( hw, 0f, 0f), r1, r0, interior);
            }
            return FinalizeFlatMesh("WheelFlat", verts, tris);
        }

        private static Mesh BuildCurvedFenderArchMesh(int studsX, int platesY, int studsZ)
        {
            Bounds b = GetTileBounds(studsX, platesY, studsZ);
            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            int segs = 6;
            float outerR = b.size.z * 0.50f;
            float innerR = b.size.z * 0.30f;
            float yBase = b.min.y;
            Vector3 center = new Vector3(b.center.x, yBase + b.size.y * 0.2f, b.center.z);
            float hx = b.size.x * 0.5f;
            float hScale = b.size.y / Mathf.Max(0.05f, outerR);

            for (int i = 0; i < segs; i++)
            {
                float a0 = (i / (float)segs) * Mathf.PI;
                float a1 = ((i + 1) / (float)segs) * Mathf.PI;
                float c0 = Mathf.Cos(a0), s0 = Mathf.Sin(a0);
                float c1 = Mathf.Cos(a1), s1 = Mathf.Sin(a1);

                Vector3 oL0 = new Vector3(b.center.x - hx, yBase + s0 * outerR * hScale, b.center.z + c0 * outerR);
                Vector3 oR0 = new Vector3(b.center.x + hx, yBase + s0 * outerR * hScale, b.center.z + c0 * outerR);
                Vector3 oL1 = new Vector3(b.center.x - hx, yBase + s1 * outerR * hScale, b.center.z + c1 * outerR);
                Vector3 oR1 = new Vector3(b.center.x + hx, yBase + s1 * outerR * hScale, b.center.z + c1 * outerR);

                Vector3 iL0 = new Vector3(b.center.x - hx, yBase + s0 * innerR * hScale, b.center.z + c0 * innerR);
                Vector3 iR0 = new Vector3(b.center.x + hx, yBase + s0 * innerR * hScale, b.center.z + c0 * innerR);
                Vector3 iL1 = new Vector3(b.center.x - hx, yBase + s1 * innerR * hScale, b.center.z + c1 * innerR);
                Vector3 iR1 = new Vector3(b.center.x + hx, yBase + s1 * innerR * hScale, b.center.z + c1 * innerR);

                Vector3 mid = (oL0 + oR1 + iL0 + iR1) * 0.25f;
                Vector3 segIn = Vector3.Lerp(mid, center, 0.1f);
                AppendFlatQuadOutward(verts, tris, oL0, oL1, oR1, oR0, segIn);
                AppendFlatQuadOutward(verts, tris, iR0, iR1, iL1, iL0, segIn);
                AppendFlatQuadOutward(verts, tris, iL0, iL1, oL1, oL0, segIn);
                AppendFlatQuadOutward(verts, tris, oR0, oR1, iR1, iR0, segIn);
            }
            return FinalizeFlatMesh("CurvedFenderArch", verts, tris);
        }

        private static Mesh GetLowPolySpoilerMesh(int studsX, int platesY, int studsZ)
        {
            Bounds b = GetTileBounds(studsX, platesY, studsZ);
            Mesh obj = LoadObjMeshOrNull("Models/AeroSpoilerWing");
            if (obj != null) return CloneAndFitMeshToBounds(obj, b);

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            AppendFlatBox(verts, tris, b.center + new Vector3(-b.size.x * 0.28f, -b.size.y * 0.12f, 0f), new Vector3(b.size.x * 0.10f, b.size.y * 0.72f, b.size.z * 0.56f));
            AppendFlatBox(verts, tris, b.center + new Vector3( b.size.x * 0.28f, -b.size.y * 0.12f, 0f), new Vector3(b.size.x * 0.10f, b.size.y * 0.72f, b.size.z * 0.56f));
            AppendFlatBox(verts, tris, b.center + new Vector3(0f, b.size.y * 0.28f, 0f), new Vector3(b.size.x, b.size.y * 0.20f, b.size.z * 0.78f));
            return FinalizeFlatMesh("AeroSpoilerFallback", verts, tris);
        }

        private static Mesh GetLowPolyV8EngineMesh(int studsX, int platesY, int studsZ)
        {
            Bounds b = GetTileBounds(studsX, platesY, studsZ);
            Mesh obj = LoadObjMeshOrNull("Models/V8EngineModule");
            if (obj != null) return CloneAndFitMeshToBounds(obj, b);

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            AppendFlatBox(verts, tris, b.center + new Vector3(0f, -b.size.y * 0.1f, 0f), new Vector3(b.size.x * 0.75f, b.size.y * 0.58f, b.size.z * 0.85f));
            AppendFlatBox(verts, tris, b.center + new Vector3(-b.size.x * 0.28f, b.size.y * 0.18f, 0f), new Vector3(b.size.x * 0.30f, b.size.y * 0.34f, b.size.z * 0.76f));
            AppendFlatBox(verts, tris, b.center + new Vector3( b.size.x * 0.28f, b.size.y * 0.18f, 0f), new Vector3(b.size.x * 0.30f, b.size.y * 0.34f, b.size.z * 0.76f));
            AppendFlatBox(verts, tris, b.center + new Vector3(0f, b.size.y * 0.34f, b.size.z * 0.10f), new Vector3(b.size.x * 0.38f, b.size.y * 0.24f, b.size.z * 0.54f));
            return FinalizeFlatMesh("V8EngineFallback", verts, tris);
        }

        private static Mesh BuildSportSeatMesh(int studsX, int platesY, int studsZ)
        {
            Bounds b = GetTileBounds(studsX, platesY, studsZ);
            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            AppendFlatBox(verts, tris, new Vector3(b.center.x, b.min.y + b.size.y * 0.16f, b.center.z + b.size.z * 0.08f), new Vector3(b.size.x * 0.90f, b.size.y * 0.32f, b.size.z * 0.78f));
            AppendFlatBox(verts, tris, new Vector3(b.center.x, b.min.y + b.size.y * 0.55f, b.min.z + b.size.z * 0.18f), new Vector3(b.size.x * 0.84f, b.size.y * 0.74f, b.size.z * 0.26f));
            AppendFlatBox(verts, tris, new Vector3(b.center.x, b.max.y - b.size.y * 0.08f, b.min.z + b.size.z * 0.20f), new Vector3(b.size.x * 0.56f, b.size.y * 0.22f, b.size.z * 0.22f));
            return FinalizeFlatMesh("SportSeat", verts, tris);
        }

        private static Mesh BuildSideSkirtMesh(int studsX, int platesY, int studsZ)
        {
            Bounds b = GetTileBounds(studsX, platesY, studsZ);
            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            AppendFlatBox(verts, tris, new Vector3(b.center.x, b.min.y + b.size.y * 0.35f, b.center.z), new Vector3(b.size.x * 0.94f, b.size.y * 0.52f, b.size.z * 0.98f));
            AppendFlatBox(verts, tris, new Vector3(b.center.x, b.min.y + b.size.y * 0.75f, b.center.z + b.size.z * 0.28f), new Vector3(b.size.x * 0.75f, b.size.y * 0.42f, b.size.z * 0.36f));
            return FinalizeFlatMesh("SideSkirt", verts, tris);
        }

        private static Mesh BuildFrontSplitterMesh(int studsX, int platesY, int studsZ)
        {
            Bounds b = GetTileBounds(studsX, platesY, studsZ);
            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            AppendFlatBox(verts, tris, new Vector3(b.center.x, b.min.y + b.size.y * 0.24f, b.center.z + b.size.z * 0.12f), new Vector3(b.size.x, b.size.y * 0.38f, b.size.z * 0.95f));
            AppendFlatBox(verts, tris, new Vector3(b.min.x + b.size.x * 0.10f, b.center.y + b.size.y * 0.14f, b.center.z), new Vector3(b.size.x * 0.12f, b.size.y * 0.70f, b.size.z * 0.75f));
            AppendFlatBox(verts, tris, new Vector3(b.max.x - b.size.x * 0.10f, b.center.y + b.size.y * 0.14f, b.center.z), new Vector3(b.size.x * 0.12f, b.size.y * 0.70f, b.size.z * 0.75f));
            return FinalizeFlatMesh("FrontSplitter", verts, tris);
        }

        private static Mesh BuildRoofScoopMesh(int studsX, int platesY, int studsZ)
        {
            Bounds b = GetTileBounds(studsX, platesY, studsZ);
            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            AppendFlatBox(verts, tris, new Vector3(b.center.x, b.min.y + b.size.y * 0.38f, b.center.z), new Vector3(b.size.x * 0.85f, b.size.y * 0.72f, b.size.z * 0.92f));
            AppendFlatBox(verts, tris, new Vector3(b.center.x, b.min.y + b.size.y * 0.78f, b.center.z - b.size.z * 0.16f), new Vector3(b.size.x * 0.65f, b.size.y * 0.36f, b.size.z * 0.56f));
            return FinalizeFlatMesh("RoofScoop", verts, tris);
        }

        public static Mesh GetLowPolyPineTreeMesh()
        {
            const string key = "scenery_pine_tree";
            if (_lowPolyMeshCache.TryGetValue(key, out Mesh cached) && cached != null) return cached;

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            float[] y0s = { 0.0f, 0.78f, 1.55f, 2.30f, 3.00f };
            float[] y1s = { 1.15f, 1.92f, 2.68f, 3.38f, 4.35f };
            float[] r0s = { 1.68f, 1.44f, 1.18f, 0.90f, 0.58f };
            float[] r1s = { 0.42f, 0.34f, 0.26f, 0.18f, 0.02f };
            const int sides = 14;

            for (int t = 0; t < y0s.Length; t++)
            {
                float yBot = y0s[t];
                float yTop = y1s[t];
                float yMid = Mathf.Lerp(yBot, yTop, 0.52f);
                Vector3 interior = new Vector3(0f, (yBot + yTop) * 0.5f, 0f);
                Vector3 cBot = new Vector3(0f, yBot + 0.04f, 0f);
                Vector3 cTop = new Vector3(0f, yTop, 0f);

                for (int i = 0; i < sides; i++)
                {
                    int next = (i + 1) % sides;
                    float a0 = (i / (float)sides) * Mathf.PI * 2f + t * 0.41f;
                    float a1 = ((i + 1) / (float)sides) * Mathf.PI * 2f + t * 0.41f;

                    // Alternate outer drooping bough tips (1.12x, -0.08Y) and recessed valleys (0.78x, +0.06Y)
                    float sc0 = (i % 2 == 0) ? 1.12f : 0.78f;
                    float sc1 = (next % 2 == 0) ? 1.12f : 0.78f;
                    float dy0 = (i % 2 == 0) ? -0.08f : 0.06f;
                    float dy1 = (next % 2 == 0) ? -0.08f : 0.06f;
                    float mSc0 = Mathf.Lerp(sc0, 1.0f, 0.42f);
                    float mSc1 = Mathf.Lerp(sc1, 1.0f, 0.42f);

                    Vector3 b0 = new Vector3(Mathf.Cos(a0) * r0s[t] * sc0, Mathf.Max(0f, yBot + dy0), Mathf.Sin(a0) * r0s[t] * sc0);
                    Vector3 b1 = new Vector3(Mathf.Cos(a1) * r0s[t] * sc1, Mathf.Max(0f, yBot + dy1), Mathf.Sin(a1) * r0s[t] * sc1);
                    float rMidVal = Mathf.Lerp(r0s[t], r1s[t], 0.52f);
                    Vector3 m0 = new Vector3(Mathf.Cos(a0 + 0.06f) * rMidVal * mSc0, yMid + dy0 * 0.4f, Mathf.Sin(a0 + 0.06f) * rMidVal * mSc0);
                    Vector3 m1 = new Vector3(Mathf.Cos(a1 + 0.06f) * rMidVal * mSc1, yMid + dy1 * 0.4f, Mathf.Sin(a1 + 0.06f) * rMidVal * mSc1);
                    Vector3 t0 = new Vector3(Mathf.Cos(a0) * r1s[t], yTop, Mathf.Sin(a0) * r1s[t]);
                    Vector3 t1 = new Vector3(Mathf.Cos(a1) * r1s[t], yTop, Mathf.Sin(a1) * r1s[t]);

                    AppendFlatQuadOutward(verts, tris, b0, b1, m1, m0, interior);
                    AppendFlatQuadOutward(verts, tris, m0, m1, t1, t0, interior);
                    AppendFlatTriOutward(verts, tris, cBot, b0, b1, interior);
                    AppendFlatTriOutward(verts, tris, cTop, t1, t0, interior);
                }
            }

            Mesh m = FinalizeFlatMesh("PineTreeDetailed", verts, tris);
            Vector3[] mv = m.vertices;
            Vector3[] mn = m.normals;
            Color[] cols = new Color[mv.Length];
            Vector3 sunDir = new Vector3(-0.35f, 0.65f, 0.67f).normalized;
            Color shadeGreen = new Color(0.56f, 0.68f, 0.52f);
            Color tipGreen = new Color(1.08f, 1.12f, 0.84f);
            for (int i = 0; i < mv.Length; i++)
            {
                float hNorm = Mathf.Clamp01(mv[i].y / 4.35f);
                float maxEnvR = Mathf.Lerp(1.88f, 0.22f, hNorm);
                float radial = Mathf.Clamp01(Mathf.Sqrt(mv[i].x * mv[i].x + mv[i].z * mv[i].z) / maxEnvR);
                float ndl = Mathf.Clamp01(Vector3.Dot(mn[i], sunDir) * 0.5f + 0.5f);
                float underMask = mn[i].y < -0.22f ? 0.25f : 1.0f;
                float tMix = Mathf.Clamp01(radial * 0.52f + hNorm * 0.22f + ndl * 0.34f) * underMask;
                cols[i] = Color.Lerp(shadeGreen, tipGreen, tMix);
            }
            m.colors = cols;
            _lowPolyMeshCache[key] = m;
            return m;
        }

        public static Mesh GetLowPolyRockMesh()
        {
            const string key = "scenery_rock";
            if (_lowPolyMeshCache.TryGetValue(key, out Mesh cached) && cached != null) return cached;
            Mesh obj = LoadObjMeshOrNull("Models/FacetedRock_LowPoly");
            if (obj != null) { Mesh flat = CloneFlatShadedWithContrast(obj); _lowPolyMeshCache[key] = flat; return flat; }

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            int sides = 6;
            Vector3 interior = new Vector3(0f, 0.4f, 0f);
            Vector3 top = new Vector3(0.08f, 0.95f, -0.05f);
            Vector3 bot = Vector3.zero;
            for (int i = 0; i < sides; i++)
            {
                float a0 = (i / (float)sides) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)sides) * Mathf.PI * 2f;
                float r0 = 0.85f + (i % 2) * 0.22f;
                float r1 = 0.85f + ((i + 1) % 2) * 0.22f;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * r0, 0f, Mathf.Sin(a0) * r0);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * r1, 0f, Mathf.Sin(a1) * r1);
                Vector3 m0 = new Vector3(Mathf.Cos(a0) * r0 * 0.65f, 0.65f, Mathf.Sin(a0) * r0 * 0.65f);
                Vector3 m1 = new Vector3(Mathf.Cos(a1) * r1 * 0.65f, 0.65f, Mathf.Sin(a1) * r1 * 0.65f);
                AppendFlatQuadOutward(verts, tris, p0, p1, m1, m0, interior);
                AppendFlatTriOutward(verts, tris, m0, m1, top, interior);
                AppendFlatTriOutward(verts, tris, bot, p0, p1, interior);
            }
            Mesh m = FinalizeFlatMesh("RockFallback", verts, tris);
            _lowPolyMeshCache[key] = m;
            return m;
        }

        public static Mesh GetLowPolyMountainMesh()
        {
            const string key = "scenery_mountain";
            if (_lowPolyMeshCache.TryGetValue(key, out Mesh cached) && cached != null) return cached;

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();

            void AddCraggyPeak(Vector3 offset, float baseR, float peakH, int rings, float[] yBands, float[] rScaleBands, float seedPhase)
            {
                Vector3 peak = offset + new Vector3(0.04f * baseR, peakH, -0.03f * baseR);
                Vector3 bot = offset;
                Vector3 interior = offset + new Vector3(0f, peakH * 0.34f, 0f);

                Vector3 RingPt(int rIdx, int bIdx)
                {
                    float a = (rIdx / (float)rings) * Mathf.PI * 2f + bIdx * 0.14f + seedPhase;
                    float spur = 1.0f
                                 + Mathf.Sin(a * 3f + seedPhase + bIdx * 0.55f) * 0.19f
                                 + Mathf.Cos(a * 5f - seedPhase * 1.4f + bIdx * 0.8f) * 0.11f;
                    float rr = baseR * rScaleBands[bIdx] * spur;
                    float yJit = (bIdx > 0 && bIdx < yBands.Length - 1) ? Mathf.Sin(a * 4f + seedPhase) * 0.025f * peakH : 0f;
                    return offset + new Vector3(Mathf.Cos(a) * rr, peakH * yBands[bIdx] + yJit, Mathf.Sin(a) * rr);
                }

                for (int i = 0; i < rings; i++)
                {
                    int next = (i + 1) % rings;
                    Vector3 b0 = RingPt(i, 0);
                    Vector3 b1 = RingPt(next, 0);
                    AppendFlatTriOutward(verts, tris, bot, b0, b1, interior);

                    for (int b = 0; b < yBands.Length - 1; b++)
                    {
                        Vector3 low0 = RingPt(i, b);
                        Vector3 low1 = RingPt(next, b);
                        Vector3 hi0 = RingPt(i, b + 1);
                        Vector3 hi1 = RingPt(next, b + 1);
                        AppendFlatQuadOutward(verts, tris, low0, low1, hi1, hi0, interior);
                    }

                    int lastBand = yBands.Length - 1;
                    Vector3 top0 = RingPt(i, lastBand);
                    Vector3 top1 = RingPt(next, lastBand);
                    AppendFlatTriOutward(verts, tris, top0, top1, peak, interior);
                }
            }

            // Main towering alpine spire (16 radial sectors x 5 elevation bands)
            AddCraggyPeak(
                Vector3.zero, 1.02f, 1.0f, 16,
                new float[] { 0.00f, 0.22f, 0.46f, 0.70f, 0.88f },
                new float[] { 1.00f, 0.78f, 0.54f, 0.31f, 0.14f },
                0.2f);

            // Two flanking rocky sub-peaks / buttress spurs
            AddCraggyPeak(
                new Vector3(0.42f, 0f, 0.28f), 0.56f, 0.64f, 10,
                new float[] { 0.00f, 0.34f, 0.68f },
                new float[] { 1.00f, 0.62f, 0.28f },
                1.7f);
            AddCraggyPeak(
                new Vector3(-0.42f, 0f, -0.28f), 0.52f, 0.56f, 10,
                new float[] { 0.00f, 0.36f, 0.70f },
                new float[] { 1.00f, 0.60f, 0.26f },
                3.1f);

            Mesh m = FinalizeFlatMesh("AlpinePeakDetailed", verts, tris);
            Vector3[] mv = m.vertices;
            Vector3[] mn = m.normals;
            Color[] cols = new Color[mv.Length];
            Vector3 sunDir = new Vector3(-0.35f, 0.65f, 0.67f).normalized;
            for (int i = 0; i < mv.Length; i++)
            {
                float y = Mathf.Clamp01(mv[i].y);
                float ndl = Vector3.Dot(mn[i], sunDir) * 0.5f + 0.5f;
                float gShade = Mathf.Lerp(0.72f, 1.06f, ndl * 0.74f + y * 0.26f);
                Color granite = new Color(gShade * 0.99f, gShade * 1.00f, gShade * 1.03f);
                Color mossBase = new Color(0.58f, 0.72f, 0.52f) * Mathf.Lerp(0.84f, 1.06f, ndl);
                Color snowCap = new Color(1.08f, 1.10f, 1.14f) * Mathf.Lerp(0.88f, 1.04f, ndl);

                Color c = granite;
                if (y < 0.22f)
                {
                    c = Color.Lerp(mossBase, granite, Mathf.SmoothStep(0.04f, 0.22f, y));
                }
                else if (y > 0.68f)
                {
                    float ledge = Mathf.Clamp01((mn[i].y + 0.16f) * 1.25f);
                    float sMix = Mathf.SmoothStep(0.68f, 0.88f, y) * (0.42f + 0.58f * ledge);
                    c = Color.Lerp(granite, snowCap, sMix);
                }
                cols[i] = c;
            }
            m.colors = cols;
            _lowPolyMeshCache[key] = m;
            return m;
        }

        public static Mesh GetLowPolyCloudMesh()
        {
            const string key = "scenery_cloud";
            if (_lowPolyMeshCache.TryGetValue(key, out Mesh cached) && cached != null) return cached;
            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            Vector3[] centers = { new Vector3(-0.75f, 0.0f, 0f), new Vector3(0.0f, 0.25f, 0.1f), new Vector3(0.75f, -0.05f, -0.05f), new Vector3(0.25f, 0.12f, -0.35f) };
            float[] radii = { 0.65f, 0.88f, 0.62f, 0.55f };
            for (int c = 0; c < centers.Length; c++)
            {
                int sides = 6;
                float r = radii[c];
                Vector3 ctr = centers[c];
                Vector3 top = ctr + new Vector3(0f, r * 0.72f, 0f);
                Vector3 bot = ctr - new Vector3(0f, r * 0.42f, 0f);
                for (int i = 0; i < sides; i++)
                {
                    float a0 = (i / (float)sides) * Mathf.PI * 2f;
                    float a1 = ((i + 1) / (float)sides) * Mathf.PI * 2f;
                    Vector3 p0 = ctr + new Vector3(Mathf.Cos(a0) * r, 0f, Mathf.Sin(a0) * r * 0.78f);
                    Vector3 p1 = ctr + new Vector3(Mathf.Cos(a1) * r, 0f, Mathf.Sin(a1) * r * 0.78f);
                    AppendFlatTriOutward(verts, tris, p0, p1, top, ctr);
                    AppendFlatTriOutward(verts, tris, bot, p0, p1, ctr);
                }
            }
            Mesh m = FinalizeFlatMesh("LowPolyCloud", verts, tris);
            _lowPolyMeshCache[key] = m;
            return m;
        }

        public static Mesh GetDetailedLeafClusterMesh()
        {
            const string key = "scenery_leaf_cluster";
            if (_lowPolyMeshCache.TryGetValue(key, out Mesh cached) && cached != null) return cached;

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            // Build 9 multi-layered 3D diamond leaflet sprays (inner upper crown + outer crown ring)
            for (int l = 0; l < 9; l++)
            {
                bool inner = l < 4;
                float ang = inner ? ((l / 4f) * Mathf.PI * 2f + 0.45f) : (((l - 4) / 5f) * Mathf.PI * 2f + 0.15f);
                Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                Vector3 perp = new Vector3(-dir.z, 0f, dir.x);
                float lift = inner ? (0.34f + (l % 2) * 0.12f) : (0.18f + (l % 3) * 0.08f);
                float reach = inner ? (0.34f + (l % 2) * 0.06f) : (0.50f + (l % 3) * 0.08f);

                Vector3 root = Vector3.up * (inner ? 0.08f : 0.02f);
                Vector3 midTop = dir * (reach * 0.48f) + Vector3.up * (lift + 0.08f);
                Vector3 midBot = dir * (reach * 0.48f) + Vector3.up * (lift - 0.05f);
                Vector3 sideL = dir * (reach * 0.46f) - perp * 0.15f + Vector3.up * lift;
                Vector3 sideR = dir * (reach * 0.46f) + perp * 0.15f + Vector3.up * lift;
                Vector3 tip = dir * reach + Vector3.up * (lift * 0.90f);
                Vector3 interior = (root + midTop + midBot + tip) * 0.25f;

                AppendFlatTriOutward(verts, tris, root, sideL, midTop, interior);
                AppendFlatTriOutward(verts, tris, root, midTop, sideR, interior);
                AppendFlatTriOutward(verts, tris, midTop, sideL, tip, interior);
                AppendFlatTriOutward(verts, tris, midTop, tip, sideR, interior);
                AppendFlatTriOutward(verts, tris, root, midBot, sideL, interior);
                AppendFlatTriOutward(verts, tris, root, sideR, midBot, interior);
                AppendFlatTriOutward(verts, tris, midBot, tip, sideL, interior);
                AppendFlatTriOutward(verts, tris, midBot, sideR, tip, interior);
            }

            Mesh m = FinalizeFlatMesh("DetailedLeafCluster", verts, tris);
            Vector3[] mv = m.vertices;
            Color[] cols = new Color[mv.Length];
            Color leafShadow = new Color(0.56f, 0.72f, 0.50f, 1f);
            Color leafSunTip = new Color(1.10f, 1.16f, 0.80f, 1f);
            for (int i = 0; i < mv.Length; i++)
            {
                float d = Mathf.Clamp01((Mathf.Sqrt(mv[i].x * mv[i].x + mv[i].z * mv[i].z) + mv[i].y * 0.8f) / 0.85f);
                cols[i] = Color.Lerp(leafShadow, leafSunTip, d);
            }
            m.colors = cols;
            _lowPolyMeshCache[key] = m;
            return m;
        }

        public static Mesh GetSwayingGrassTuftMesh()
        {
            const string key = "scenery_grass_tuft";
            if (_lowPolyMeshCache.TryGetValue(key, out Mesh cached) && cached != null) return cached;

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            const int blades = 28;
            for (int b = 0; b < blades; b++)
            {
                float ang = b * 2.39996f;
                float rOff = Mathf.Lerp(0.05f, 0.48f, b / (float)(blades - 1));
                Vector3 basePos = new Vector3(Mathf.Cos(ang) * rOff, 0f, Mathf.Sin(ang) * rOff);
                Vector3 outDir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                Vector3 perp = new Vector3(-outDir.z, 0f, outDir.x);

                float h = 0.38f + ((b % 5) / 4f) * 0.40f;
                float arch = 0.16f + (b % 3) * 0.08f;
                float bw = 0.072f;

                Vector3 pL0 = basePos - perp * bw;
                Vector3 pR0 = basePos + perp * bw;
                Vector3 pM = basePos + outDir * (arch * 0.45f) + Vector3.up * (h * 0.56f);
                Vector3 pBack = basePos + outDir * (arch * 0.35f) + perp * 0.015f + Vector3.up * (h * 0.52f);
                Vector3 tip = basePos + outDir * arch + Vector3.up * h;
                Vector3 interior = (pL0 + pR0 + pM + pBack) * 0.25f;

                AppendFlatTriOutward(verts, tris, pL0, pR0, pM, interior);
                AppendFlatTriOutward(verts, tris, pL0, pM, tip, interior);
                AppendFlatTriOutward(verts, tris, pR0, tip, pM, interior);
                AppendFlatTriOutward(verts, tris, pR0, pL0, pBack, interior);
                AppendFlatTriOutward(verts, tris, pL0, tip, pBack, interior);
                AppendFlatTriOutward(verts, tris, pR0, pBack, tip, interior);
            }

            Mesh m = FinalizeFlatMesh("SwayingGrassTuft", verts, tris);
            // Rich emerald-base to sunlit-lime-tip gradient
            Vector3[] mv = m.vertices;
            Color[] cols = new Color[mv.Length];
            for (int i = 0; i < mv.Length; i++)
            {
                float t = Mathf.Clamp01(mv[i].y / 0.72f);
                cols[i] = Color.Lerp(new Color(0.46f, 0.68f, 0.42f, 1f), new Color(1.06f, 1.12f, 0.78f, 1f), t);
            }
            m.colors = cols;
            _lowPolyMeshCache[key] = m;
            return m;
        }

        public static Mesh GetLowPolyFlowerPatchMesh()
        {
            return GetLowPolyFlowerPatchMesh(LegoPalette.BrightYellow);
        }

        public static Mesh GetLowPolyFlowerPatchMesh(Color petalColor)
        {
            string key = $"scenery_flower_{petalColor.r:F2}_{petalColor.g:F2}_{petalColor.b:F2}";
            if (_lowPolyMeshCache.TryGetValue(key, out Mesh cached) && cached != null) return cached;

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();
            List<Color> vCols = new List<Color>();

            void AddColoredBox(Vector3 center, Vector3 size, Color col)
            {
                int start = verts.Count;
                AppendFlatBox(verts, tris, center, size);
                for (int i = start; i < verts.Count; i++) vCols.Add(col);
            }

            Color stemGreen = new Color(0.24f, 0.56f, 0.20f);
            Color leafGreen = new Color(0.30f, 0.66f, 0.24f);
            Color centerGold = new Color(0.98f, 0.80f, 0.18f);

            for (int k = 0; k < 5; k++)
            {
                float angle = k * 1.256f + 0.3f;
                float dist = (k == 0) ? 0.04f : (0.16f + (k % 2) * 0.08f);
                Vector3 off = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                float stemH = 0.28f + (k % 3) * 0.06f;

                // Slender green stem + two small basal leaves
                AddColoredBox(off + new Vector3(0f, stemH * 0.5f, 0f), new Vector3(0.032f, stemH, 0.032f), stemGreen);
                int leafStart = verts.Count;
                Vector3 leafDir = new Vector3(Mathf.Cos(angle + 0.8f), 0f, Mathf.Sin(angle + 0.8f));
                Vector3 lp0 = off + new Vector3(0f, stemH * 0.32f, 0f);
                Vector3 lp1 = lp0 + leafDir * 0.14f + Vector3.up * 0.06f;
                Vector3 lPerp = new Vector3(-leafDir.z, 0f, leafDir.x) * 0.045f;
                AppendFlatTriOutward(verts, tris, lp0, lp0 + lPerp + Vector3.up * 0.02f, lp1, lp0 + Vector3.down);
                AppendFlatTriOutward(verts, tris, lp0, lp1, lp0 - lPerp + Vector3.up * 0.02f, lp0 + Vector3.down);
                for (int i = leafStart; i < verts.Count; i++) vCols.Add(leafGreen);

                // 6-petal flower blossom + golden center disc
                Vector3 headCenter = off + new Vector3(0f, stemH + 0.02f, 0f);
                AddColoredBox(headCenter, new Vector3(0.065f, 0.042f, 0.065f), centerGold);
                for (int p = 0; p < 6; p++)
                {
                    float pa = (p / 6f) * Mathf.PI * 2f + k * 0.4f;
                    Vector3 pOff = new Vector3(Mathf.Cos(pa) * 0.065f, 0.01f, Mathf.Sin(pa) * 0.065f);
                    AddColoredBox(headCenter + pOff, new Vector3(0.055f, 0.028f, 0.055f), petalColor);
                }
            }

            Mesh m = FinalizeFlatMesh($"LowPolyFlowerPatch_{petalColor}", verts, tris);
            m.colors = vCols.ToArray();
            _lowPolyMeshCache[key] = m;
            return m;
        }

        public static Material GetPlasticMaterial(Color color, bool emissive = false, float smoothness = 0.62f, float metallic = 0.0f)
        {
            string key = $"{color.r:F3}_{color.g:F3}_{color.b:F3}_{emissive}_{smoothness:F2}_{metallic:F2}";
            if (_matCache.TryGetValue(key, out Material cached) && cached != null)
            {
                return cached;
            }

            if (_cachedLitShader == null)
            {
                _cachedLitShader = Shader.Find("Universal Render Pipeline/Lit");
                if (_cachedLitShader == null)
                    _cachedLitShader = Shader.Find("Standard");
                if (_cachedLitShader == null)
                    _cachedLitShader = Shader.Find("Diffuse");
            }

            Material mat = new Material(_cachedLitShader);
            mat.color = color;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);

            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness"))
                mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", metallic);

            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                if (mat.HasProperty("_EmissionColor"))
                    mat.SetColor("_EmissionColor", color * 1.35f);
            }

            mat.enableInstancing = true;
            _matCache[key] = mat;
            return mat;
        }

        public static Mesh GetBrickMesh(int studsX, int platesY, int studsZ, BrickShape shape = BrickShape.StandardBrick)
        {
            studsX = Mathf.Max(1, studsX);
            platesY = Mathf.Max(1, platesY);
            studsZ = Mathf.Max(1, studsZ);

            string key = $"{studsX}_{platesY}_{studsZ}_{(int)shape}";
            if (_meshCache.TryGetValue(key, out Mesh cached) && cached != null)
            {
                return cached;
            }

            Mesh mesh;
            switch (shape)
            {
                case BrickShape.SlopeFront:
                case BrickShape.Windshield:
                    mesh = BuildSlopeMesh(studsX, platesY, studsZ, false);
                    break;
                case BrickShape.SlopeInverted:
                    mesh = BuildSlopeMesh(studsX, platesY, studsZ, true);
                    break;
                case BrickShape.Wheel:
                    mesh = BuildCenteredTreadWheelMesh(studsX, platesY, studsZ);
                    break;
                case BrickShape.CylinderPart:
                    mesh = BuildWheelRimHubMesh(studsX, platesY, studsZ);
                    break;
                case BrickShape.JetNozzle:
                    mesh = BuildJetNozzleMesh(studsX, platesY, studsZ);
                    break;
                case BrickShape.SpoilerWing:
                    mesh = GetLowPolySpoilerMesh(studsX, platesY, studsZ);
                    break;
                case BrickShape.FlatTile:
                    mesh = BuildStandardLegoBrickMesh(studsX, platesY, studsZ, false);
                    break;
                case BrickShape.FenderArch:
                    mesh = BuildCurvedFenderArchMesh(studsX, platesY, studsZ);
                    break;
                case BrickShape.V8Engine:
                    mesh = GetLowPolyV8EngineMesh(studsX, platesY, studsZ);
                    break;
                case BrickShape.SportSeat:
                    mesh = BuildSportSeatMesh(studsX, platesY, studsZ);
                    break;
                case BrickShape.SideSkirt:
                    mesh = BuildSideSkirtMesh(studsX, platesY, studsZ);
                    break;
                case BrickShape.FrontSplitter:
                    mesh = BuildFrontSplitterMesh(studsX, platesY, studsZ);
                    break;
                case BrickShape.RoofScoop:
                    mesh = BuildRoofScoopMesh(studsX, platesY, studsZ);
                    break;
                default:
                    mesh = BuildStandardLegoBrickMesh(studsX, platesY, studsZ, true);
                    break;
            }

            mesh.name = $"LegoMesh_{key}";
            _meshCache[key] = mesh;
            return mesh;
        }

        private static Mesh BuildStandardLegoBrickMesh(int studsX, int platesY, int studsZ, bool includeTopStuds)
        {
            float seam = 0.014f;
            float w = studsX * STUD_UNIT - seam;
            float h = platesY * PLATE_UNIT;
            float l = studsZ * STUD_UNIT - seam;

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();

            // Clean, flat-topped 3D beveled block (no studs/núm on top)
            AddTrueBeveledBox(verts, tris, new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, l), 0.026f);

            return FinalizeFlatShadedMesh(verts, tris);
        }

        private static Mesh BuildSlopeMesh(int studsX, int platesY, int studsZ, bool inverted)
        {
            float w = studsX * STUD_UNIT - 0.014f;
            float h = platesY * PLATE_UNIT;
            float l = studsZ * STUD_UNIT - 0.014f;

            float hw = w * 0.5f;
            float hl = l * 0.5f;
            float noseH = h * 0.34f;
            float backFlatLen = studsZ >= 2 ? STUD_UNIT * 0.85f : l * 0.25f;

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();

            if (!inverted)
            {
                Vector3 interiorCenter = new Vector3(0f, h * 0.35f, -hl * 0.2f);

                Vector3 bBL = new Vector3(-hw, 0f, -hl);
                Vector3 bBR = new Vector3(hw, 0f, -hl);
                Vector3 bML = new Vector3(-hw, 0f, -hl + backFlatLen);
                Vector3 bMR = new Vector3(hw, 0f, -hl + backFlatLen);
                Vector3 bFL = new Vector3(-hw, 0f, hl);
                Vector3 bFR = new Vector3(hw, 0f, hl);

                Vector3 tBL = new Vector3(-hw, h, -hl);
                Vector3 tBR = new Vector3(hw, h, -hl);
                Vector3 tML = new Vector3(-hw, h, -hl + backFlatLen);
                Vector3 tMR = new Vector3(hw, h, -hl + backFlatLen);
                Vector3 tFL = new Vector3(-hw, noseH, hl);
                Vector3 tFR = new Vector3(hw, noseH, hl);

                // Every face is wound using explicit outward normals so none can ever invert!
                AddQuadOutward(verts, tris, bBL, bBR, bFR, bFL, interiorCenter); // Bottom (-Y)
                AddQuadOutward(verts, tris, bBR, bBL, tBL, tBR, interiorCenter); // Back (-Z)
                AddQuadOutward(verts, tris, bFL, bFR, tFR, tFL, interiorCenter); // Front toe (+Z)
                AddQuadOutward(verts, tris, tBL, tML, tMR, tBR, interiorCenter); // Flat top ridge (+Y)
                AddQuadOutward(verts, tris, tML, tFL, tFR, tMR, interiorCenter); // Angled slope (+Y/+Z)

                // Left side (-X)
                AddQuadOutward(verts, tris, bBL, bML, tML, tBL, interiorCenter);
                AddQuadOutward(verts, tris, bML, bFL, tFL, tML, interiorCenter);

                // Right side (+X)
                AddQuadOutward(verts, tris, bMR, bBR, tBR, tMR, interiorCenter);
                AddQuadOutward(verts, tris, bFR, bMR, tMR, tFR, interiorCenter);
            }
            else
            {
                Vector3 interiorCenter = new Vector3(0f, h * 0.65f, -hl * 0.2f);

                Vector3 bBL = new Vector3(-hw, 0f, -hl);
                Vector3 bBR = new Vector3(hw, 0f, -hl);
                Vector3 bFL = new Vector3(-hw, h - noseH, hl);
                Vector3 bFR = new Vector3(hw, h - noseH, hl);
                Vector3 tBL = new Vector3(-hw, h, -hl);
                Vector3 tBR = new Vector3(hw, h, -hl);
                Vector3 tFL = new Vector3(-hw, h, hl);
                Vector3 tFR = new Vector3(hw, h, hl);

                AddQuadOutward(verts, tris, bBL, bBR, bFR, bFL, interiorCenter);
                AddQuadOutward(verts, tris, bBR, bBL, tBL, tBR, interiorCenter);
                AddQuadOutward(verts, tris, bFL, bFR, tFR, tFL, interiorCenter);
                AddQuadOutward(verts, tris, tBL, tFL, tFR, tBR, interiorCenter);
                AddQuadOutward(verts, tris, bBL, bFL, tFL, tBL, interiorCenter);
                AddQuadOutward(verts, tris, bFR, bBR, tBR, tFR, interiorCenter);
            }

            return FinalizeFlatShadedMesh(verts, tris);
        }

        public static float GetWheelRadius(int platesY, int studsZ)
        {
            return Mathf.Clamp(Mathf.Min(platesY * PLATE_UNIT, studsZ * STUD_UNIT) * 0.45f, 0.32f, 0.42f);
        }

        private static Mesh BuildCenteredTreadWheelMesh(int studsX, int platesY, int studsZ)
        {
            float width = Mathf.Clamp(studsX * STUD_UNIT * 0.82f, 0.28f, 0.46f);
            float outerRadius = GetWheelRadius(platesY, studsZ);
            float shoulderRadius = outerRadius * 0.90f;
            float rimRecessRadius = outerRadius * 0.58f;

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();

            int segments = 16;
            float halfW = width * 0.5f;
            float treadHalfW = halfW * 0.84f;
            float recessW = halfW * 0.70f;

            for (int i = 0; i < segments; i++)
            {
                float a0 = (i / (float)segments) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)segments) * Mathf.PI * 2f;
                float aMid = (a0 + a1) * 0.5f;
                Vector3 radialOut = new Vector3(0f, Mathf.Sin(aMid), Mathf.Cos(aMid));

                Vector3 p0L = new Vector3(-treadHalfW, Mathf.Sin(a0) * outerRadius, Mathf.Cos(a0) * outerRadius);
                Vector3 p1L = new Vector3(-treadHalfW, Mathf.Sin(a1) * outerRadius, Mathf.Cos(a1) * outerRadius);
                Vector3 p0R = new Vector3(treadHalfW, Mathf.Sin(a0) * outerRadius, Mathf.Cos(a0) * outerRadius);
                Vector3 p1R = new Vector3(treadHalfW, Mathf.Sin(a1) * outerRadius, Mathf.Cos(a1) * outerRadius);

                Vector3 sh0L = new Vector3(-halfW, Mathf.Sin(a0) * shoulderRadius, Mathf.Cos(a0) * shoulderRadius);
                Vector3 sh1L = new Vector3(-halfW, Mathf.Sin(a1) * shoulderRadius, Mathf.Cos(a1) * shoulderRadius);
                Vector3 sh0R = new Vector3(halfW, Mathf.Sin(a0) * shoulderRadius, Mathf.Cos(a0) * shoulderRadius);
                Vector3 sh1R = new Vector3(halfW, Mathf.Sin(a1) * shoulderRadius, Mathf.Cos(a1) * shoulderRadius);

                Vector3 s0L = new Vector3(-halfW, Mathf.Sin(a0) * rimRecessRadius, Mathf.Cos(a0) * rimRecessRadius);
                Vector3 s1L = new Vector3(-halfW, Mathf.Sin(a1) * rimRecessRadius, Mathf.Cos(a1) * rimRecessRadius);
                Vector3 s0R = new Vector3(halfW, Mathf.Sin(a0) * rimRecessRadius, Mathf.Cos(a0) * rimRecessRadius);
                Vector3 s1R = new Vector3(halfW, Mathf.Sin(a1) * rimRecessRadius, Mathf.Cos(a1) * rimRecessRadius);

                Vector3 cL = new Vector3(-recessW, 0f, 0f);
                Vector3 cR = new Vector3(recessW, 0f, 0f);

                // Clean low-poly tire tread top face (facing radially outward)
                AddQuadWithNormal(verts, tris, p0L, p1L, p1R, p0R, radialOut);

                // Left & Right tire shoulders
                AddQuadWithNormal(verts, tris, sh0L, sh1L, p1L, p0L, radialOut + Vector3.left);
                AddQuadWithNormal(verts, tris, p0R, p1R, sh1R, sh0R, radialOut + Vector3.right);

                // Left & Right sidewalls (facing -X and +X)
                AddQuadWithNormal(verts, tris, s0L, s1L, sh1L, sh0L, Vector3.left);
                AddQuadWithNormal(verts, tris, sh0R, sh1R, s1R, s0R, Vector3.right);

                // Inner wheel well caps (facing -X and +X)
                AddTriWithNormal(verts, tris, cL, s1L, s0L, Vector3.left);
                AddTriWithNormal(verts, tris, cR, s0R, s1R, Vector3.right);
            }

            return FinalizeFlatShadedMesh(verts, tris);
        }

        private static Mesh BuildWheelRimHubMesh(int studsX, int platesY, int studsZ)
        {
            float width = Mathf.Clamp(studsX * STUD_UNIT * 0.82f, 0.28f, 0.46f) * 1.03f;
            float rimRadius = GetWheelRadius(platesY, studsZ) * 0.58f;
            float halfW = width * 0.5f;

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();

            int sides = 12;
            for (int i = 0; i < sides; i++)
            {
                float a0 = (i / (float)sides) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)sides) * Mathf.PI * 2f;

                Vector3 p0L = new Vector3(-halfW, Mathf.Sin(a0) * rimRadius, Mathf.Cos(a0) * rimRadius);
                Vector3 p1L = new Vector3(-halfW, Mathf.Sin(a1) * rimRadius, Mathf.Cos(a1) * rimRadius);
                Vector3 p0R = new Vector3(halfW, Mathf.Sin(a0) * rimRadius, Mathf.Cos(a0) * rimRadius);
                Vector3 p1R = new Vector3(halfW, Mathf.Sin(a1) * rimRadius, Mathf.Cos(a1) * rimRadius);

                AddQuadOutward(verts, tris, p0L, p1L, p1R, p0R, Vector3.zero);
                AddTriWithNormal(verts, tris, new Vector3(-halfW, 0f, 0f), p1L, p0L, Vector3.left);
                AddTriWithNormal(verts, tris, new Vector3(halfW, 0f, 0f), p0R, p1R, Vector3.right);
            }

            AddBox(verts, tris, Vector3.zero, new Vector3(width * 1.06f, rimRadius * 0.45f, rimRadius * 0.45f));
            for (int s = 0; s < 6; s++)
            {
                float angle = s * 60f * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(0f, Mathf.Sin(angle), Mathf.Cos(angle));
                AddBox(verts, tris, new Vector3(-halfW * 0.98f, dir.y * rimRadius * 0.5f, dir.z * rimRadius * 0.5f),
                    new Vector3(0.08f, rimRadius * 0.28f, rimRadius * 0.28f));
                AddBox(verts, tris, new Vector3(halfW * 0.98f, dir.y * rimRadius * 0.5f, dir.z * rimRadius * 0.5f),
                    new Vector3(0.08f, rimRadius * 0.28f, rimRadius * 0.28f));
            }

            return FinalizeFlatShadedMesh(verts, tris);
        }

        private static Mesh BuildFenderArchMesh(int studsX, int platesY, int studsZ)
        {
            float w = studsX * STUD_UNIT - 0.014f;
            float h = platesY * PLATE_UNIT;
            float l = studsZ * STUD_UNIT - 0.014f;

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();

            AddTrueBeveledBox(verts, tris, new Vector3(0f, h * 0.72f, 0f), new Vector3(w, h * 0.56f, l), 0.022f);
            AddTrueBeveledBox(verts, tris, new Vector3(0f, h * 0.28f, l * 0.36f), new Vector3(w, h * 0.56f, l * 0.28f), 0.02f);
            AddTrueBeveledBox(verts, tris, new Vector3(0f, h * 0.28f, -l * 0.36f), new Vector3(w, h * 0.56f, l * 0.28f), 0.02f);

            return FinalizeFlatShadedMesh(verts, tris);
        }

        private static Mesh BuildJetNozzleMesh(int studsX, int platesY, int studsZ)
        {
            float w = studsX * STUD_UNIT * 0.94f;
            float h = platesY * PLATE_UNIT;
            float l = studsZ * STUD_UNIT * 0.96f;

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();

            AddTrueBeveledBox(verts, tris, new Vector3(0f, h * 0.5f, 0.12f * l), new Vector3(w, h, l * 0.70f), 0.025f);
            AddTrueBeveledBox(verts, tris, new Vector3(0f, h * 0.5f, -l * 0.36f), new Vector3(w * 0.80f, h * 0.80f, l * 0.44f), 0.02f);
            AddTrueBeveledBox(verts, tris, new Vector3(0f, h * 0.5f, -l * 0.62f), new Vector3(w * 0.60f, h * 0.60f, l * 0.26f), 0.015f);

            return FinalizeFlatShadedMesh(verts, tris);
        }

        private static Mesh BuildSpoilerMesh(int studsX, int platesY, int studsZ)
        {
            float w = studsX * STUD_UNIT;
            float h = platesY * PLATE_UNIT;
            float l = studsZ * STUD_UNIT;

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();

            float strutX = w * 0.28f;
            AddTrueBeveledBox(verts, tris, new Vector3(-strutX, h * 0.45f, 0f), new Vector3(STUD_UNIT * 0.45f, h * 0.9f, l * 0.62f), 0.02f);
            AddTrueBeveledBox(verts, tris, new Vector3(strutX, h * 0.45f, 0f), new Vector3(STUD_UNIT * 0.45f, h * 0.9f, l * 0.62f), 0.02f);
            AddTrueBeveledBox(verts, tris, new Vector3(0f, h * 0.90f, -l * 0.06f), new Vector3(w, PLATE_UNIT * 0.75f, l), 0.022f);
            AddTrueBeveledBox(verts, tris, new Vector3(-w * 0.48f, h * 0.82f, -l * 0.05f), new Vector3(STUD_UNIT * 0.34f, h * 0.65f, l * 1.12f), 0.02f);
            AddTrueBeveledBox(verts, tris, new Vector3(w * 0.48f, h * 0.82f, -l * 0.05f), new Vector3(STUD_UNIT * 0.34f, h * 0.65f, l * 1.12f), 0.02f);

            return FinalizeFlatShadedMesh(verts, tris);
        }

        /// <summary>
        /// Watertight 26-face 3D beveled box (6 main faces + 12 edge chamfers + 8 corner triangles)
        /// where every single face is automatically wound outward from `center`!
        /// </summary>
        private static void AddTrueBeveledBox(List<Vector3> verts, List<int> tris, Vector3 center, Vector3 size, float bevel)
        {
            float bx = Mathf.Min(bevel, size.x * 0.20f);
            float by = Mathf.Min(bevel, size.y * 0.20f);
            float bz = Mathf.Min(bevel, size.z * 0.20f);

            float hx = size.x * 0.5f;
            float hy = size.y * 0.5f;
            float hz = size.z * 0.5f;

            float ix = hx - bx;
            float iy = hy - by;
            float iz = hz - bz;

            // Bottom face corners (y = -hy)
            Vector3 b00 = center + new Vector3(-ix, -hy, -iz);
            Vector3 b10 = center + new Vector3(ix, -hy, -iz);
            Vector3 b11 = center + new Vector3(ix, -hy, iz);
            Vector3 b01 = center + new Vector3(-ix, -hy, iz);

            // Top face corners (y = +hy)
            Vector3 t00 = center + new Vector3(-ix, hy, -iz);
            Vector3 t10 = center + new Vector3(ix, hy, -iz);
            Vector3 t11 = center + new Vector3(ix, hy, iz);
            Vector3 t01 = center + new Vector3(-ix, hy, iz);

            // Front face corners (z = +hz)
            Vector3 f00 = center + new Vector3(-ix, -iy, hz);
            Vector3 f10 = center + new Vector3(ix, -iy, hz);
            Vector3 f11 = center + new Vector3(ix, iy, hz);
            Vector3 f01 = center + new Vector3(-ix, iy, hz);

            // Back face corners (z = -hz)
            Vector3 k00 = center + new Vector3(-ix, -iy, -hz);
            Vector3 k10 = center + new Vector3(ix, -iy, -hz);
            Vector3 k11 = center + new Vector3(ix, iy, -hz);
            Vector3 k01 = center + new Vector3(-ix, iy, -hz);

            // Left face corners (x = -hx)
            Vector3 l00 = center + new Vector3(-hx, -iy, -iz);
            Vector3 l01 = center + new Vector3(-hx, -iy, iz);
            Vector3 l11 = center + new Vector3(-hx, iy, iz);
            Vector3 l10 = center + new Vector3(-hx, iy, -iz);

            // Right face corners (x = +hx)
            Vector3 r00 = center + new Vector3(hx, -iy, -iz);
            Vector3 r01 = center + new Vector3(hx, -iy, iz);
            Vector3 r11 = center + new Vector3(hx, iy, iz);
            Vector3 r10 = center + new Vector3(hx, iy, -iz);

            // 1. 6 Primary Faces
            AddQuadOutward(verts, tris, t00, t01, t11, t10, center); // Top (+Y)
            AddQuadOutward(verts, tris, b00, b10, b11, b01, center); // Bottom (-Y)
            AddQuadOutward(verts, tris, f00, f10, f11, f01, center); // Front (+Z)
            AddQuadOutward(verts, tris, k10, k00, k01, k11, center); // Back (-Z)
            AddQuadOutward(verts, tris, l00, l01, l11, l10, center); // Left (-X)
            AddQuadOutward(verts, tris, r01, r00, r10, r11, center); // Right (+X)

            // 2. 4 Top Edge Bevels
            AddQuadOutward(verts, tris, f01, f11, t11, t01, center); // Top-Front
            AddQuadOutward(verts, tris, k11, k01, t00, t10, center); // Top-Back
            AddQuadOutward(verts, tris, l10, l11, t01, t00, center); // Top-Left
            AddQuadOutward(verts, tris, r11, r10, t10, t11, center); // Top-Right

            // 3. 4 Bottom Edge Bevels
            AddQuadOutward(verts, tris, b01, b11, f10, f00, center); // Bottom-Front
            AddQuadOutward(verts, tris, b10, b00, k00, k10, center); // Bottom-Back
            AddQuadOutward(verts, tris, b00, b01, l01, l00, center); // Bottom-Left
            AddQuadOutward(verts, tris, b11, b10, r00, r01, center); // Bottom-Right

            // 4. 4 Vertical Corner Edge Bevels
            AddQuadOutward(verts, tris, l01, f00, f01, l11, center); // Front-Left
            AddQuadOutward(verts, tris, f10, r01, r11, f11, center); // Front-Right
            AddQuadOutward(verts, tris, k00, l00, l10, k01, center); // Back-Left
            AddQuadOutward(verts, tris, r00, k10, k11, r10, center); // Back-Right

            // 5. 8 Corner Triangles (seals the mesh 100% watertight!)
            AddTriOutward(verts, tris, t01, l11, f01, center);
            AddTriOutward(verts, tris, t11, f11, r11, center);
            AddTriOutward(verts, tris, t00, k01, l10, center);
            AddTriOutward(verts, tris, t10, r10, k11, center);

            AddTriOutward(verts, tris, b01, f00, l01, center);
            AddTriOutward(verts, tris, b11, r01, f10, center);
            AddTriOutward(verts, tris, b00, l00, k00, center);
            AddTriOutward(verts, tris, b10, k10, r00, center);
        }

        private static void AddBox(List<Vector3> verts, List<int> tris, Vector3 center, Vector3 size)
        {
            Vector3 hs = size * 0.5f;
            Vector3 p000 = center + new Vector3(-hs.x, -hs.y, -hs.z);
            Vector3 p100 = center + new Vector3(hs.x, -hs.y, -hs.z);
            Vector3 p010 = center + new Vector3(-hs.x, hs.y, -hs.z);
            Vector3 p110 = center + new Vector3(hs.x, hs.y, -hs.z);
            Vector3 p001 = center + new Vector3(-hs.x, -hs.y, hs.z);
            Vector3 p101 = center + new Vector3(hs.x, -hs.y, hs.z);
            Vector3 p011 = center + new Vector3(-hs.x, hs.y, hs.z);
            Vector3 p111 = center + new Vector3(hs.x, hs.y, hs.z);

            AddQuadOutward(verts, tris, p001, p101, p111, p011, center);
            AddQuadOutward(verts, tris, p100, p000, p010, p110, center);
            AddQuadOutward(verts, tris, p010, p011, p111, p110, center);
            AddQuadOutward(verts, tris, p000, p100, p101, p001, center);
            AddQuadOutward(verts, tris, p000, p001, p011, p010, center);
            AddQuadOutward(verts, tris, p101, p100, p110, p111, center);
        }

        private static void AddBeveledStud(List<Vector3> verts, List<int> tris, Vector3 bottomCenter, float radius, float height, int sides)
        {
            float bevelH = height * 0.24f;
            float mainH = height - bevelH;
            float topR = radius * 0.84f;
            Vector3 topCenter = bottomCenter + Vector3.up * height;
            Vector3 studInterior = bottomCenter + Vector3.up * (height * 0.45f);

            for (int i = 0; i < sides; i++)
            {
                float a0 = (i / (float)sides) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)sides) * Mathf.PI * 2f;

                float c0 = Mathf.Cos(a0), s0 = Mathf.Sin(a0);
                float c1 = Mathf.Cos(a1), s1 = Mathf.Sin(a1);

                Vector3 b0 = bottomCenter + new Vector3(c0 * radius, 0f, s0 * radius);
                Vector3 b1 = bottomCenter + new Vector3(c1 * radius, 0f, s1 * radius);
                Vector3 m0 = bottomCenter + new Vector3(c0 * radius, mainH, s0 * radius);
                Vector3 m1 = bottomCenter + new Vector3(c1 * radius, mainH, s1 * radius);
                Vector3 t0 = bottomCenter + new Vector3(c0 * topR, height, s0 * topR);
                Vector3 t1 = bottomCenter + new Vector3(c1 * topR, height, s1 * topR);

                AddQuadOutward(verts, tris, b0, b1, m1, m0, studInterior);
                AddQuadOutward(verts, tris, m0, m1, t1, t0, studInterior);
                AddTriWithNormal(verts, tris, topCenter, t1, t0, Vector3.up);
            }
        }

        /// <summary>
        /// Automatically winds quad (a,b,c,d) so its normal points strictly away from `interiorCenter`.
        /// </summary>
        private static void AddQuadOutward(List<Vector3> verts, List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 interiorCenter)
        {
            Vector3 quadCenter = (a + b + c + d) * 0.25f;
            Vector3 expectedOutward = quadCenter - interiorCenter;
            AddQuadWithNormal(verts, tris, a, b, c, d, expectedOutward);
        }

        private static void AddQuadWithNormal(List<Vector3> verts, List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 expectedNormal)
        {
            int idx = verts.Count;
            verts.Add(a);
            verts.Add(b);
            verts.Add(c);
            verts.Add(d);

            Vector3 n = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(n, expectedNormal) >= 0f)
            {
                tris.Add(idx + 0);
                tris.Add(idx + 1);
                tris.Add(idx + 2);

                tris.Add(idx + 0);
                tris.Add(idx + 2);
                tris.Add(idx + 3);
            }
            else
            {
                tris.Add(idx + 0);
                tris.Add(idx + 2);
                tris.Add(idx + 1);

                tris.Add(idx + 0);
                tris.Add(idx + 3);
                tris.Add(idx + 2);
            }
        }

        private static void AddTriOutward(List<Vector3> verts, List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 interiorCenter)
        {
            Vector3 triCenter = (a + b + c) / 3f;
            AddTriWithNormal(verts, tris, a, b, c, triCenter - interiorCenter);
        }

        private static void AddTriWithNormal(List<Vector3> verts, List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 expectedNormal)
        {
            int idx = verts.Count;
            verts.Add(a);
            verts.Add(b);
            verts.Add(c);

            Vector3 n = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(n, expectedNormal) >= 0f)
            {
                tris.Add(idx + 0);
                tris.Add(idx + 1);
                tris.Add(idx + 2);
            }
            else
            {
                tris.Add(idx + 0);
                tris.Add(idx + 2);
                tris.Add(idx + 1);
            }
        }

        private static Mesh FinalizeFlatShadedMesh(List<Vector3> verts, List<int> tris)
        {
            Mesh m = new Mesh();
            m.SetVertices(verts);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            ApplyFacetVertexContrast(m);
            return m;
        }
    }
}
