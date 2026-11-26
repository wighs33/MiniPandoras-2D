// 무대의 빛줄기와 폭포: 위에서 아래로 흐르는 결이 있는 띠. 빛처럼 더해 그린다.
// 판의 가로는 가운데가 짙고, 세로는 두 끝에서 옅어진다(_Ends). 이야기가 차오르게 할 수 있다(_GlowShare).
Shader "MiniPandoras/Stage/Beam"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1, 0.9, 0.6, 1)
        _Streaks ("Streaks", Float) = 7
        _Speed ("Speed (down)", Float) = 0.3
        _Contrast ("Streak Contrast", Range(0, 1)) = 0.6
        _Ends ("End Fade (bottom, top)", Vector) = (0.15, 0.3, 0, 0)
        _Edge ("Edge Softness", Range(0.2, 4)) = 1.4
        _GlowShare ("Follows Story Glow", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One One
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
                float4 _Ends;
                float _Streaks, _Speed, _Contrast, _Edge, _GlowShare;
            CBUFFER_END

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vertex(float3 positionOS : POSITION, float2 uv : TEXCOORD0)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(positionOS);
                output.uv = uv;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float across = pow(saturate(1 - abs(uv.x * 2 - 1)), _Edge);
                float streak = StageNoise(float2(uv.x * _Streaks, uv.y * 2.2 + _Time.y * _Speed)) * 0.6 + StageNoise(float2(uv.x * _Streaks * 2.7, uv.y * 4.1 + _Time.y * _Speed * 1.7)) * 0.4;
                float along = smoothstep(0, max(0.001, _Ends.x), uv.y) * smoothstep(1, 1 - max(0.001, _Ends.y), uv.y);
                return half4(_Color.rgb * across * along * lerp(1, streak * 1.6, _Contrast) * StageGlowAmount(_GlowShare), 0);
            }
            ENDHLSL
        }
    }
}
