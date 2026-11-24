Shader "NeoVeil/Anime Cel Toon + Outline (URP)"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Texture", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)

        [Header(Anime Cel Shading)]
        _ShadowColor("Shadow Tint", Color) = (0.42, 0.45, 0.62, 1)
        _ShadowThreshold("Shadow Threshold", Range(-0.5, 0.5)) = 0.05
        _ShadowSmoothness("Shadow Edge Smoothness", Range(0.001, 0.25)) = 0.03

        [Header(Anime Rim Light)]
        [HDR] _RimColor("Rim Light Color", Color) = (0.35, 0.9, 1.0, 1)
        _RimPower("Rim Power", Range(0.5, 8.0)) = 3.2
        _RimThreshold("Rim Threshold", Range(0.0, 1.0)) = 0.55

        [Header(Neon Emission for URP Bloom)]
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionMap("Emission Mask", 2D) = "white" {}

        [Header(Anime Outline)]
        _OutlineColor("Outline Color", Color) = (0.06, 0.07, 0.12, 1)
        _OutlineWidth("Outline Width", Range(0.0, 0.05)) = 0.012
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        // =====================================================================
        // PASS 1: Forward / Forward+ Anime Cel-Shading + Rim + HDR Neon Emission
        // =====================================================================
        Pass
        {
            Name "AnimeCelForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float  fogFactor   : TEXCOORD3;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_EmissionMap);
            SAMPLER(sampler_EmissionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half4  _ShadowColor;
                half   _ShadowThreshold;
                half   _ShadowSmoothness;
                half4  _RimColor;
                half   _RimPower;
                half   _RimThreshold;
                half4  _EmissionColor;
                half4  _OutlineColor;
                half   _OutlineWidth;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS   = normInputs.normalWS;
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor  = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(IN.positionWS));

                // Main Directional Light Cel Ramp
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half ndotl = dot(normalWS, mainLight.direction);
                half celStep = smoothstep(
                    _ShadowThreshold - _ShadowSmoothness,
                    _ShadowThreshold + _ShadowSmoothness,
                    ndotl * mainLight.shadowAttenuation
                );

                half3 ambient = SampleSH(normalWS) * 0.45h;
                half3 mainLighting = lerp(_ShadowColor.rgb, half3(1, 1, 1), celStep) * mainLight.color + ambient;

                // Additional Neon Point/Spot Lights (Unity 6 URP Forward+ & Forward)
                half3 additionalLighting = half3(0, 0, 0);
                #if defined(_ADDITIONAL_LIGHTS)
                    InputData inputData = (InputData)0;
                    inputData.positionWS = IN.positionWS;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                    uint pixelLightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(pixelLightCount)
                        Light addLight = GetAdditionalLight(lightIndex, IN.positionWS);
                        half addNdotL = saturate(dot(normalWS, addLight.direction));
                        half addCel = smoothstep(0.05h, 0.2h, addNdotL);
                        additionalLighting += addLight.color * (addLight.distanceAttenuation * addLight.shadowAttenuation * addCel);
                    LIGHT_LOOP_END
                #endif

                // Crisp Anime Rim Light
                half fresnel = pow(1.0h - saturate(dot(normalWS, viewDirWS)), _RimPower);
                half rimStep = smoothstep(_RimThreshold - 0.05h, _RimThreshold + 0.05h, fresnel);
                half3 rimGlow = _RimColor.rgb * rimStep * _RimColor.a;

                // HDR Neon Emission (drives URP Bloom & DayNightNeonController)
                half3 emissionMask = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, IN.uv).rgb;
                half3 emission = _EmissionColor.rgb * emissionMask;

                half3 finalColor = texColor.rgb * (mainLighting + additionalLighting) + rimGlow + emission;
                finalColor = MixFog(finalColor, IN.fogFactor);
                return half4(finalColor, 1.0h);
            }
            ENDHLSL
        }

        // =====================================================================
        // PASS 2: Inverted-Hull Crisp Anime Outline
        // =====================================================================
        Pass
        {
            Name "AnimeOutline"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vertOutline
            #pragma fragment fragOutline
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float  fogFactor  : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half4  _ShadowColor;
                half   _ShadowThreshold;
                half   _ShadowSmoothness;
                half4  _RimColor;
                half   _RimPower;
                half   _RimThreshold;
                half4  _EmissionColor;
                half4  _OutlineColor;
                half   _OutlineWidth;
            CBUFFER_END

            Varyings vertOutline(Attributes IN)
            {
                Varyings OUT;
                float3 extrudedOS = IN.positionOS.xyz + normalize(IN.normalOS) * _OutlineWidth;
                VertexPositionInputs posInputs = GetVertexPositionInputs(extrudedOS);
                OUT.positionCS = posInputs.positionCS;
                OUT.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 fragOutline(Varyings IN) : SV_Target
            {
                half3 col = MixFog(_OutlineColor.rgb, IN.fogFactor);
                return half4(col, 1.0h);
            }
            ENDHLSL
        }

        // =====================================================================
        // PASS 3: Shadow Caster (So anime characters & buildings cast shadows)
        // =====================================================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
