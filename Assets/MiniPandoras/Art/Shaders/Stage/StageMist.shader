// 타이틀 무대의 안개·구름 띠. 천천히 흐르는 잡음이고, 판의 네 가장자리에서 사라진다.
Shader "MiniPandoras/Stage/Mist"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1, 1, 1, 0.5)
        _Scale ("Noise Scale (x, y)", Vector) = (3, 1, 0, 0)
        _Speed ("Speed", Float) = 0.02
        _Cover ("Cover", Range(0, 1)) = 0.5
        _Bottom ("Flat Bottom", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "StageNoise.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _Scale;
                float _Speed, _Cover, _Bottom;
            CBUFFER_END

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float fade : TEXCOORD1;
            };

            Varyings Vertex(float3 positionOS : POSITION, float2 uv : TEXCOORD0)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(positionOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = uv;
                // 뛰어들 때 카메라가 안개를 지나가면 판이 드러나지 않게 가까운 안개를 걷는다.
                output.fade = smoothstep(1.5, 6, length(GetCameraPositionWS() - positionWS));
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 p = input.uv * _Scale.xy + float2(_Time.y * _Speed, 0);
                float n = StageFbm(p + StageFbm(p * 1.7 - _Time.y * _Speed * 0.7) * 0.8);
                float cover = smoothstep(1 - _Cover, 1.25 - _Cover, n + 0.25);
                float2 edge = 1 - abs(input.uv * 2 - 1);
                float sides = smoothstep(0, 0.35, edge.x);
                float upDown = lerp(smoothstep(0, 0.55, edge.y), smoothstep(1, 0.25, input.uv.y) * smoothstep(0, 0.06, input.uv.y), _Bottom);
                return half4(_Color.rgb, _Color.a * cover * sides * upDown * input.fade);
            }
            ENDHLSL
        }
    }
}
