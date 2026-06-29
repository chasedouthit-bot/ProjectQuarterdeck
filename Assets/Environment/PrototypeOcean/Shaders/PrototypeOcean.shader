Shader "Quarterdeck/PrototypeOcean"
{
    Properties
    {
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Deep Color", Color) = (0.04, 0.22, 0.34, 0.88)
        _ShallowColor ("Shallow Color", Color) = (0.14, 0.52, 0.50, 0.82)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.92
        _WaveScale ("Wave Scale", Float) = 0.12
        _WaveStrength ("Wave Strength", Range(0, 1)) = 0.35
        _WavePhase ("Wave Phase", Float) = 0
        _SpecularStrength ("Specular Strength", Range(0, 2)) = 0.65
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ShallowColor;
                half _Smoothness;
                half _WaveScale;
                half _WaveStrength;
                half _WavePhase;
                half _SpecularStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float S = _WaveScale;
                float2 worldXZ = input.positionWS.xz * S;
                
                float u = worldXZ.x + _WavePhase;
                float v = worldXZ.y - _WavePhase * 0.7f;
                float w = (worldXZ.x + worldXZ.y) * 0.65f + _WavePhase * 1.3f;

                float sin_u = sin(u), cos_u = cos(u);
                float sin_v = sin(v), cos_v = cos(v);
                float sin_w = sin(w), cos_w = cos(w);

                float waveA = sin_u * sin_v;
                float waveB = sin_w;
                float waveMix = saturate(0.5f + 0.5f * (waveA * 0.6f + waveB * 0.4f));

                // Partial spatial derivatives of the wave function
                float dwA_dx = cos_u * sin_v * S;
                float dwA_dz = sin_u * cos_v * S;

                float dwB_dx = cos_w * 0.65f * S;
                float dwB_dz = cos_w * 0.65f * S;

                float dwMix_dx = 0.5f * (dwA_dx * 0.6f + dwB_dx * 0.4f) * _WaveStrength;
                float dwMix_dz = 0.5f * (dwA_dz * 0.6f + dwB_dz * 0.4f) * _WaveStrength;

                // High frequency micro-ripple scrolling maps for organic texture detail
                half4 texA = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half4 texB = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv * 2.5f + _WavePhase * 0.05f);

                float rippleHeight_x = (texA.r - 0.5f) * 0.08f + (texB.r - 0.5f) * 0.03f;
                float rippleHeight_z = (texA.g - 0.5f) * 0.08f + (texB.g - 0.5f) * 0.03f;

                // Calculate the final normal vector perturbed by both large waves and ripples
                float3 normalWS = normalize(float3(-dwMix_dx * 4.0f - rippleHeight_x, 1.0f, -dwMix_dz * 4.0f - rippleHeight_z));
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));

                // Base water color blending based on ocean depth and texture ripples
                half3 waterColor = lerp(_BaseColor.rgb, _ShallowColor.rgb, waveMix * _WaveStrength + texA.r * 0.15f);
                half alpha = lerp(_BaseColor.a, _ShallowColor.a, waveMix);

                // Lighting calculation
                Light mainLight = GetMainLight();
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                
                // Specular (Glossy sparkles)
                float specPower = lerp(32.0f, 1024.0f, _Smoothness);
                float spec = pow(saturate(dot(normalWS, halfDir)), specPower) * _SpecularStrength;

                // Diffuse lighting
                float ndotl = saturate(dot(normalWS, mainLight.direction));

                // Spherical Harmonics Ambient (Matches background/sky colors perfectly)
                half3 ambient = SampleSH(normalWS);

                // Fresnel Reflection
                float F0 = 0.02f;
                float cosTheta = saturate(dot(normalWS, viewDirWS));
                float fresnel = F0 + (1.0f - F0) * pow(1.0f - cosTheta, 5.0f);

                // Final color composition with ambient, diffuse, specular, and sky specular
                half3 litColor = waterColor * (ambient + ndotl * 0.65h) + (spec + fresnel * 0.4f) * mainLight.color;

                return half4(litColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
