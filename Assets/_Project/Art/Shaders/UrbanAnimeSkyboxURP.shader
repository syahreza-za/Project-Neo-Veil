Shader "NeoVeil/Urban Anime Skybox (URP)"
{
    Properties
    {
        _TopColor("Zenith Sky Color", Color) = (0.04, 0.06, 0.16, 1)
        _HorizonColor("Horizon Neon Glow Color", Color) = (0.28, 0.14, 0.42, 1)
        _BottomColor("Ground Reflection Color", Color) = (0.05, 0.06, 0.11, 1)
        _HorizonSharpness("Horizon Band Sharpness", Range(0.5, 10.0)) = 3.2

        [Header(Stylized Night Stars)]
        _StarIntensity("Star Intensity", Range(0.0, 2.0)) = 1.0
        _StarDensity("Star Density", Range(30.0, 200.0)) = 95.0

        [Header(Stylized Sun / Moon Disc)]
        [HDR] _CelestialColor("Sun / Moon Color", Color) = (1.0, 0.92, 0.8, 1)
        _CelestialSize("Sun / Moon Size", Range(0.005, 0.12)) = 0.032
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 dirWS      : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _HorizonColor;
                half4 _BottomColor;
                half  _HorizonSharpness;
                half  _StarIntensity;
                half  _StarDensity;
                half4 _CelestialColor;
                half  _CelestialSize;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.dirWS = IN.positionOS.xyz;
                return OUT;
            }

            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 dir = normalize(IN.dirWS);
                half up = dir.y;

                // Stylized anime sky gradient
                half skyT = pow(saturate(up), 1.0h / max(0.2h, _HorizonSharpness * 0.35h));
                half3 upperSky = lerp(_HorizonColor.rgb, _TopColor.rgb, skyT);
                half3 lowerSky = lerp(_HorizonColor.rgb, _BottomColor.rgb, saturate(-up * 3.5h));
                half3 col = up >= 0.0h ? upperSky : lowerSky;

                // Soft neon horizon band
                half horizonBand = exp(-abs(up) * _HorizonSharpness * 2.5h);
                col += _HorizonColor.rgb * horizonBand * 0.25h;

                // Procedural crisp anime night stars
                if (up > 0.05h && _StarIntensity > 0.01h)
                {
                    float3 gridCell = floor(dir * _StarDensity);
                    float rnd = Hash31(gridCell);
                    if (rnd > 0.985)
                    {
                        float twinkle = 0.65 + 0.35 * sin(_Time.y * 2.5 + rnd * 100.0);
                        half starMask = saturate((up - 0.05h) * 3.0h);
                        col += half3(0.85, 0.93, 1.0) * (twinkle * _StarIntensity * starMask);
                    }
                }

                // Stylized Sun/Moon disc aligned with main directional light
                Light mainLight = GetMainLight();
                float sunDot = dot(dir, mainLight.direction);
                float disc = smoothstep(1.0 - _CelestialSize * 0.08, 1.0 - _CelestialSize * 0.04, sunDot);
                float halo = pow(saturate(sunDot), 32.0) * 0.35;
                col += _CelestialColor.rgb * (disc + halo);

                return half4(col, 1.0h);
            }
            ENDHLSL
        }
    }
}
