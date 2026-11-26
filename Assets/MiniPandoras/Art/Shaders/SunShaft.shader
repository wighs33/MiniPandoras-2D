// HD-2D 필드의 햇살 한 줄기(World.SunShafts). 해 쪽에서 땅으로 비스듬히 내리꽂히는 옅은 빛 띠이고, 빛처럼 더해 그린다.
// 띠의 가로는 가운데가 밝고 가장자리로 부드럽게 사라지며, 안에 가는 빛살 몇 가닥이 천천히 일렁인다.
// 세로는 땅에 닿는 끝과 하늘 쪽 끝에서 옅어진다. 먼 곳은 안개만큼 옅어진다.
Shader "MiniPandoras/Sun Shaft"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.95, 0.8, 1)
        _Intensity ("Intensity", Float) = 0.42
        [HideInInspector] _Alpha ("Alpha", Float) = 1
        [HideInInspector] _Seed ("Seed", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent+50" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
                half _Alpha;
                half _Seed;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half fogFactor : TEXCOORD1;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                // uv.x: 띠의 가로(0~1), uv.y: 땅(0)에서 하늘(1)
                half across = 1 - abs(input.uv.x * 2 - 1);
                half body = smoothstep(0, 1, across);
                float t = _Time.y;
                half rays = 0.55 + 0.45 * sin(input.uv.x * 19 + _Seed * 6.28 + t * 0.35) * sin(input.uv.x * 37 - t * 0.23 + _Seed * 3.1);
                half along = smoothstep(0, 0.12, input.uv.y) * smoothstep(1, 0.45, input.uv.y);
                half strength = body * rays * along * _Alpha * _Intensity;
                half3 color = _Color.rgb * strength;
                // 더해 그리므로 안개 속에서는 검은색 쪽으로 섞어 옅게 한다.
                color = MixFogColor(color, half3(0, 0, 0), input.fogFactor);
                return half4(color, 0);
            }
            ENDHLSL
        }
    }
}
