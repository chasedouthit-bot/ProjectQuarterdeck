Shader "Quarterdeck/PrototypeOcean"
{
    Properties
    {
        [MainTexture] _BaseMap ("Ripple Map", 2D) = "gray" {}
        _BaseColor ("Deep Color", Color) = (0.04, 0.22, 0.34, 0.88)
        _ShallowColor ("Shallow Color", Color) = (0.14, 0.52, 0.50, 0.82)
        _FoamColor ("Foam Color", Color) = (0.92, 0.96, 1.0, 1.0)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.92
        _SpecularStrength ("Specular Strength", Range(0, 2)) = 0.85
        _RippleStrength ("Ripple Normal Strength", Range(0, 1)) = 0.35
        _FoamStrength ("Foam Strength", Range(0, 2)) = 0.75
        _WaveHeight ("Wave Height (m)", Float) = 1
        _WavePhase ("Wave Phase", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShallowColor;
                half4 _FoamColor;
                half _Smoothness;
                half _SpecularStrength;
                half _RippleStrength;
                half _FoamStrength;
                half _WaveHeight;
                half _WavePhase;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionWS = posInputs.positionWS;
                output.positionCS = posInputs.positionCS;
                output.normalWS = normalInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half3 SampleRippleNormal(float2 uvA, float2 uvB)
            {
                half hL = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvA + float2(-0.01, 0)).r;
                half hR = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvA + float2(0.01, 0)).r;
                half hD = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvA + float2(0, -0.01)).r;
                half hU = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvA + float2(0, 0.01)).r;
                half3 nA = normalize(half3(hL - hR, 0.35, hD - hU));

                half hL2 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvB + float2(-0.015, 0)).r;
                half hR2 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvB + float2(0.015, 0)).r;
                half hD2 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvB + float2(0, -0.015)).r;
                half hU2 = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvB + float2(0, 0.015)).r;
                half3 nB = normalize(half3(hL2 - hR2, 0.25, hD2 - hU2));

                return normalize(nA * 0.65 + nB * 0.35);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 meshNormal = normalize(input.normalWS);
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));

                float2 worldUV = input.positionWS.xz * 0.035;
                float phase = _WavePhase;
                float2 uvA = worldUV + float2(phase * 0.04, phase * 0.025);
                float2 uvB = worldUV * 1.85 + float2(-phase * 0.03, phase * 0.05);

                half4 texA = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvA);
                half4 texB = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvB);
                half3 rippleNormal = SampleRippleNormal(uvA, uvB);

                float3 perturbedNormal = normalize(float3(
                    meshNormal.x + rippleNormal.x * _RippleStrength,
                    meshNormal.y,
                    meshNormal.z + rippleNormal.z * _RippleStrength));

                float slope = 1.0 - saturate(meshNormal.y);
                float crest = saturate((input.positionWS.y / max(_WaveHeight, 0.05)) * 0.55 - 0.15);
                float chopFoam = saturate(texA.r * 1.4 + texB.r * 0.6 - 0.75);
                float foam = saturate((slope * 1.35 + crest * 0.65 + chopFoam * slope * 0.8) * _FoamStrength);

                float shallowMask = saturate(0.35 + texA.r * 0.25 + (1.0 - slope) * 0.35);
                half3 waterColor = lerp(_BaseColor.rgb, _ShallowColor.rgb, shallowMask);
                waterColor = lerp(waterColor, _FoamColor.rgb, foam);

                half alpha = lerp(_BaseColor.a, _ShallowColor.a, shallowMask);
                alpha = lerp(alpha, 0.95h, foam * 0.5h);

                Light mainLight = GetMainLight();
                float3 halfDir = normalize(mainLight.direction + viewDirWS);

                float specPower = lerp(48.0, 768.0, _Smoothness);
                float spec = pow(saturate(dot(perturbedNormal, halfDir)), specPower) * _SpecularStrength;
                spec += pow(saturate(dot(meshNormal, halfDir)), specPower * 0.35) * _SpecularStrength * 0.25;

                float ndotl = saturate(dot(perturbedNormal, mainLight.direction));
                half3 ambient = SampleSH(perturbedNormal);

                float F0 = 0.02;
                float cosTheta = saturate(dot(perturbedNormal, viewDirWS));
                float fresnel = F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);

                half3 skyTint = half3(0.55, 0.72, 0.88);
                half3 litColor = waterColor * (ambient * 0.85h + ndotl * 0.55h);
                litColor += (spec + fresnel * 0.55h) * mainLight.color;
                litColor = lerp(litColor, skyTint, fresnel * 0.35h + foam * 0.15h);

                litColor = MixFog(litColor, input.fogFactor);
                return half4(litColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
