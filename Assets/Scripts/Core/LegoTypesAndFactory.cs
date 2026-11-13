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
        SlopeInverted = 9
    }

    public enum PowerUpType
    {
        None = 0,
        HomingBrickMissile = 1,
        EnergyBrickShield = 2,
        SpiderWebMine = 3,
        HyperWarpBoost = 4
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
                    mesh = BuildSpoilerMesh(studsX, platesY, studsZ);
                    break;
                case BrickShape.FlatTile:
                    mesh = BuildStandardLegoBrickMesh(studsX, platesY, studsZ, false);
                    break;
                case BrickShape.FenderArch:
                    mesh = BuildFenderArchMesh(studsX, platesY, studsZ);
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
            return Mathf.Max(platesY * PLATE_UNIT, studsZ * STUD_UNIT) * 0.52f;
        }

        private static Mesh BuildCenteredTreadWheelMesh(int studsX, int platesY, int studsZ)
        {
            float width = Mathf.Max(studsX * STUD_UNIT * 1.05f, STUD_UNIT * 1.25f);
            float outerRadius = GetWheelRadius(platesY, studsZ);
            float innerRadius = outerRadius * 0.88f;
            float shoulderRadius = outerRadius * 0.84f;
            float rimRecessRadius = outerRadius * 0.54f;

            List<Vector3> verts = new List<Vector3>();
            List<int> tris = new List<int>();

            int segments = 20;
            float halfW = width * 0.5f;
            float treadHalfW = halfW * 0.84f;
            float recessW = halfW * 0.68f;

            for (int i = 0; i < segments; i++)
            {
                float a0 = (i / (float)segments) * Mathf.PI * 2f;
                float a1 = ((i + 1) / (float)segments) * Mathf.PI * 2f;
                float aMid = (a0 + a1) * 0.5f;
                Vector3 radialOut = new Vector3(0f, Mathf.Sin(aMid), Mathf.Cos(aMid));

                float r = (i % 2 == 0) ? outerRadius : innerRadius;

                Vector3 p0L = new Vector3(-treadHalfW, Mathf.Sin(a0) * r, Mathf.Cos(a0) * r);
                Vector3 p1L = new Vector3(-treadHalfW, Mathf.Sin(a1) * r, Mathf.Cos(a1) * r);
                Vector3 p0R = new Vector3(treadHalfW, Mathf.Sin(a0) * r, Mathf.Cos(a0) * r);
                Vector3 p1R = new Vector3(treadHalfW, Mathf.Sin(a1) * r, Mathf.Cos(a1) * r);

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

                // Tread top face (facing radially outward)
                AddQuadWithNormal(verts, tris, p0L, p1L, p1R, p0R, radialOut);

                // Tread lug step wall
                float nextR = ((i + 1) % 2 == 0) ? outerRadius : innerRadius;
                if (Mathf.Abs(r - nextR) > 0.001f)
                {
                    Vector3 n1L = new Vector3(-treadHalfW, Mathf.Sin(a1) * nextR, Mathf.Cos(a1) * nextR);
                    Vector3 n1R = new Vector3(treadHalfW, Mathf.Sin(a1) * nextR, Mathf.Cos(a1) * nextR);
                    Vector3 tang = new Vector3(0f, Mathf.Cos(a1), -Mathf.Sin(a1)) * (r > nextR ? 1f : -1f);
                    AddQuadWithNormal(verts, tris, p1L, n1L, n1R, p1R, tang);
                }

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
            float width = Mathf.Max(studsX * STUD_UNIT * 1.06f, STUD_UNIT * 1.26f);
            float rimRadius = GetWheelRadius(platesY, studsZ) * 0.55f;
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
            return m;
        }
    }
}
