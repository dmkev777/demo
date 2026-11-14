Shader "Hidden/MotorversePostProcess"
{
    Properties
    {
        _MainTex ("Source Texture", 2D) = "white" {}
        _AoStrength ("Contact AO Strength", Range(0, 1)) = 0.38
        _BloomStrength ("Sunrise Bloom Strength", Range(0, 1)) = 0.08
        _BloomThreshold ("Bloom Threshold", Range(0.4, 1.2)) = 0.96
        _ExposureBoost ("Post Exposure", Range(0.8, 1.5)) = 0.90
        _SharpenStrength ("Sharpness Strength", Range(0, 1)) = 0.36
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            sampler2D _CameraDepthNormalsTexture;

            half _AoStrength;
            half _BloomStrength;
            half _BloomThreshold;
            half _ExposureBoost;
            half _SharpenStrength;

            half3 ACESFilm(half3 x)
            {
                half a = 2.51;
                half b = 0.03;
                half c = 2.43;
                half d = 0.59;
                half e = 0.14;
                return saturate((x * (a * x + b)) / (x * (c * x + d) + e));
            }

            half ComputeContactAO(float2 uv, half centerDepth, half3 centerNormal)
            {
                if (centerDepth > 0.94)
                    return 1.0;

                float2 offsets[6] = {
                    float2( 1.0,  0.0),
                    float2(-1.0,  0.0),
                    float2( 0.0,  1.0),
                    float2( 0.0, -1.0),
                    float2( 0.7,  0.7),
                    float2(-0.7, -0.7)
                };

                float radiusScale = clamp(1.2 / (centerDepth * 14.0 + 0.35), 1.0, 2.8);
                float2 stepUv = _MainTex_TexelSize.xy * (2.2 * radiusScale);

                half occ = 0.0;
                for (int i = 0; i < 6; i++)
                {
                    float sampleD;
                    float3 sampleN;
                    DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, uv + offsets[i] * stepUv), sampleD, sampleN);

                    float depthDiff = centerDepth - (half)sampleD;
                    float normalDiff = 1.0 - saturate(dot(centerNormal, (half3)sampleN));
                    // Darken interior creases and surface contacts while ignoring distant silhouette edges
                    half sameBand = step(abs(depthDiff), 0.012);
                    half crease = smoothstep(0.0002, 0.0045, depthDiff) * sameBand;
                    occ += saturate(crease * 0.75 + normalDiff * 0.25 * sameBand);
                }

                occ = saturate(occ / 6.0);
                return 1.0 - occ * _AoStrength;
            }

            half3 SampleSoftBloom(float2 uv)
            {
                float2 offsets[8] = {
                    float2( 2.0,  0.0), float2(-2.0,  0.0),
                    float2( 0.0,  2.0), float2( 0.0, -2.0),
                    float2( 4.0,  4.0), float2(-4.0,  4.0),
                    float2( 4.0, -4.0), float2(-4.0, -4.0)
                };

                half3 bloomSum = half3(0.0, 0.0, 0.0);
                for (int i = 0; i < 8; i++)
                {
                    half3 s = tex2D(_MainTex, uv + offsets[i] * _MainTex_TexelSize.xy).rgb;
                    half luma = dot(s, half3(0.2126, 0.7152, 0.0722));
                    half weight = smoothstep(_BloomThreshold, _BloomThreshold + 0.35, luma);
                    bloomSum += s * weight;
                }
                // Warm golden sunrise tint on bloom glow
                return (bloomSum / 8.0) * half3(1.06, 0.98, 0.88);
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                half4 col = tex2D(_MainTex, i.uv);

                // Contrast-adaptive sharpening for razor-sharp foliage, brick mortar, and road lines
                float2 px = _MainTex_TexelSize.xy;
                half3 cN = tex2D(_MainTex, i.uv + float2(0, px.y)).rgb;
                half3 cS = tex2D(_MainTex, i.uv - float2(0, px.y)).rgb;
                half3 cE = tex2D(_MainTex, i.uv + float2(px.x, 0)).rgb;
                half3 cW = tex2D(_MainTex, i.uv - float2(px.x, 0)).rgb;
                half3 blurAvg = (cN + cS + cE + cW) * 0.25;
                half3 sharpCol = max(col.rgb + (col.rgb - blurAvg) * _SharpenStrength, half3(0, 0, 0));

                float depth;
                float3 normal;
                DecodeDepthNormal(tex2D(_CameraDepthNormalsTexture, i.uv), depth, normal);

                half ao = ComputeContactAO(i.uv, (half)depth, (half3)normal);
                half3 bloom = SampleSoftBloom(i.uv) * _BloomStrength;

                half3 combined = sharpCol * ao + bloom;

                // Subtle warm color vibrancy + ACES Filmic curve in gamma-friendly domain
                half3 linearCol = GammaToLinearSpace(max(combined * _ExposureBoost, half3(0, 0, 0)));
                half3 graded = ACESFilm(linearCol * 0.88) * half3(1.02, 0.99, 0.95);
                half3 finalGamma = LinearToGammaSpace(graded);

                // Gentle saturation boost for crisp stylized colors
                half luma = dot(finalGamma, half3(0.2126, 0.7152, 0.0722));
                finalGamma = lerp(half3(luma, luma, luma), finalGamma, 1.05);

                // Very soft edge vignette
                float2 centered = i.uv - 0.5;
                half vig = 1.0 - dot(centered, centered) * 0.26;
                finalGamma *= saturate(vig);

                return fixed4(saturate(finalGamma), col.a);
            }
            ENDCG
        }
    }

    Fallback Off
}
