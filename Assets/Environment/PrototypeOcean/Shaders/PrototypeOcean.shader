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
                float2 worldXZ = input.positionWS.xz * _WaveScale;
                float waveA = sin(worldXZ.x + _WavePhase) * sin(worldXZ.y - _WavePhase * 0.7h);
                float waveB = sin((worldXZ.x + worldXZ.y) * 0.65h + _WavePhase * 1.3h);
                float waveMix = saturate(0.5h + 0.5h * (waveA * 0.6h + waveB * 0.4h));

                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 waterColor = lerp(_BaseColor.rgb, _ShallowColor.rgb, waveMix * _WaveStrength + tex.r * 0.25h);
                half alpha = lerp(_BaseColor.a, _ShallowColor.a, waveMix);

                Light mainLight = GetMainLight();
                float3 normalWS = float3(0.0h, 1.0h, 0.0h);
                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float specPower = lerp(16.0h, 256.0h, _Smoothness);
                float spec = pow(saturate(dot(normalWS, halfDir)), specPower) * _SpecularStrength;
                float ndotl = saturate(dot(normalWS, mainLight.direction));
                half3 litColor = waterColor * (0.35h + ndotl * 0.65h) + spec * mainLight.color;

                return half4(litColor, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
