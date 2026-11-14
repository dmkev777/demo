Shader "Custom/MotorverseLowPolyLit"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Surface Pattern (RGB)", 2D) = "white" {}
        _PatternScale ("World Pattern Scale", Float) = 0.45
        _PatternStrength ("Pattern Contrast", Range(0, 1)) = 0.0
        _WindSway ("Foliage Wind Sway", Range(0, 1)) = 0.0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.16
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        _EmissionColor ("Emission Color", Color) = (0,0,0,1)
        _ShadowTint ("Cool Shadow Tint", Color) = (0.34, 0.48, 0.72, 1)
        _RimColor ("Golden Sun Rim", Color) = (1.0, 0.82, 0.52, 1)
        _RimStrength ("Rim Strength", Range(0, 1)) = 0.07
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf StylizedLowPoly fullforwardshadows addshadow vertex:vert
        #pragma target 3.0
        #include "UnityCG.cginc"
        #include "UnityPBSLighting.cginc"

        fixed4 _Color;
        sampler2D _MainTex;
        float _PatternScale;
        half _PatternStrength;
        half _WindSway;
        half _Smoothness;
        half _Metallic;
        fixed4 _EmissionColor;
        fixed4 _ShadowTint;
        fixed4 _RimColor;
        half _RimStrength;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
            float4 color : COLOR;
        };

        void vert(inout appdata_full v)
        {
            if (_WindSway > 0.001)
            {
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float swayMask = smoothstep(0.01, 0.24, max(0.0, v.vertex.y));
                float gust = sin(_Time.y * 2.1 + worldPos.x * 0.42 + worldPos.z * 0.36);
                float flutter = sin(_Time.y * 5.8 + worldPos.x * 2.7 + worldPos.y * 3.1 + worldPos.z * 2.9);
                float dispX = (gust * 0.065 + flutter * 0.025) * _WindSway * swayMask;
                float dispZ = (cos(_Time.y * 1.85 + worldPos.z * 0.48 + worldPos.x * 0.22) * 0.055 + flutter * 0.020) * _WindSway * swayMask;
                float dispY = flutter * 0.015 * _WindSway * swayMask;
                v.vertex.xyz += mul((float3x3)unity_WorldToObject, float3(dispX, dispY, dispZ));
            }
        }

        inline half4 LightingStylizedLowPoly(SurfaceOutput s, half3 viewDir, UnityGI gi)
        {
            half3 L = normalize(gi.light.dir);
            half3 V = normalize(viewDir);
            half3 N = normalize(s.Normal);
            half NdotL = dot(N, L);

            // Stylized low-poly diffuse ramp: crisp lit facet transition + gentle wrap for shaded planes
            half ramp = smoothstep(-0.08, 0.16, NdotL) * 0.68 + saturate(NdotL) * 0.32;

            // Glossy catchlights only for automotive paint / glass / alloy rims (_Smoothness > 0.30)
            half glossMask = saturate((_Smoothness - 0.30) * 1.45);
            half3 H = normalize(L + V);
            half specPow = lerp(14.0, 96.0, saturate(_Smoothness));
            half specTerm = pow(saturate(dot(N, H)), specPow) * glossMask * lerp(0.08, 0.42, saturate(_Smoothness));
            half3 specCol = gi.light.color * specTerm * lerp(half3(1.0, 1.0, 1.0), s.Albedo, _Metallic);

        #ifndef UNITY_PASS_FORWARDADD
            half skyFactor = saturate(N.y * 0.5 + 0.5);
            half3 ambientShadow = s.Albedo * lerp(gi.indirect.diffuse * _ShadowTint.rgb * 1.15, gi.indirect.diffuse * 0.78, skyFactor * 0.5);
            half3 sunlitSurface = s.Albedo * (gi.light.color * 0.86 + gi.indirect.diffuse * 0.16);

            half fresnel = pow(1.0 - saturate(dot(N, V)), 3.8);
            half sunBackscatter = saturate(dot(-V, L) * 0.5 + 0.5);
            half3 rimCol = _RimColor.rgb * (_RimStrength * (1.0 - _Metallic * 0.5)) * fresnel * (0.30 + 0.70 * sunBackscatter) * (0.45 + 0.55 * skyFactor);

            half4 c = half4(lerp(ambientShadow, sunlitSurface, ramp) + specCol + rimCol, s.Alpha);
        #else
            half3 litCol = s.Albedo * gi.light.color * ramp * 0.55;
            half4 c = half4(litCol + specCol * 0.55, s.Alpha);
        #endif

            return c;
        }

        inline void LightingStylizedLowPoly_GI(SurfaceOutput s, UnityGIInput data, inout UnityGI gi)
        {
            gi = UnityGlobalIllumination(data, 1.0, s.Normal);
        }

        void surf(Input IN, inout SurfaceOutput o)
        {
            // Primitives without vertex colors may provide (0,0,0) on some GPU drivers; guard cleanly to white
            half3 vCol = (dot(IN.color.rgb, half3(1.0, 1.0, 1.0)) < 0.01) ? half3(1.0, 1.0, 1.0) : IN.color.rgb;
            half3 albedo = _Color.rgb * vCol * (1.0 - _Metallic * 0.25);

            half3 bf = abs(normalize(IN.worldNormal));
            bf *= bf;
            bf *= bf;
            bf /= max(dot(bf, half3(1.0, 1.0, 1.0)), 0.0001);

            float3 wpMicro = IN.worldPos * _PatternScale;
            half3 cx = tex2D(_MainTex, wpMicro.zy).rgb;
            half3 cy = tex2D(_MainTex, wpMicro.xz).rgb;
            half3 cz = tex2D(_MainTex, wpMicro.xy).rgb;
            half3 patMicro = cx * bf.x + cy * bf.y + cz * bf.z;

            float3 wpMacro = IN.worldPos * (_PatternScale * 0.14) + float3(0.37, 0.19, 0.73);
            half3 mx = tex2D(_MainTex, wpMacro.zy).rgb;
            half3 my = tex2D(_MainTex, wpMacro.xz).rgb;
            half3 mz = tex2D(_MainTex, wpMacro.xy).rgb;
            half3 patMacro = mx * bf.x + my * bf.y + mz * bf.z;

            half3 pat = lerp(patMicro, patMicro * lerp(half3(1.0, 1.0, 1.0), patMacro, 0.65), 0.5);
            half reliefWave = sin(IN.worldPos.x * 0.65 + patMacro.r * 3.1) * cos(IN.worldPos.z * 0.65 + patMacro.b * 3.1);
            half microRelief = clamp((patMicro.g - patMicro.r) * 0.90 + reliefWave * 0.10, -0.20, 0.20);

            albedo *= lerp(half3(1.0, 1.0, 1.0), pat * (1.0 + microRelief), _PatternStrength);
            half smoothOut = _Smoothness * lerp(1.0, pat.g, _PatternStrength * 0.45);

            o.Albedo = albedo;
            o.Specular = smoothOut;
            o.Gloss = smoothOut;
            o.Emission = _EmissionColor.rgb;
            o.Alpha = _Color.a;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
