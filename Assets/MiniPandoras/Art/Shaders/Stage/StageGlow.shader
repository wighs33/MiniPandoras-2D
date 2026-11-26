// 타이틀 무대의 빛: 가운데가 밝은 빛무리와, 가운데에서 뻗는 빛살(해·달·여신의 후광). 빛처럼 더해 그린다.
Shader "MiniPandoras/Stage/Glow"
{
    Properties
    {
        [HDR] _Color ("Color", Color) = (1, 0.9, 0.7, 1)
        _Softness ("Softness", Range(0.5, 6)) = 2
        _Rays ("Rays", Range(0, 1)) = 0
        _RayCount ("Ray Count", Float) = 9
        _RaySpeed ("Ray Speed", Float) = 0.08
        _Pulse ("Pulse", Range(0, 1)) = 0.08
        _DiveBoost ("Brighter While Diving", Float) = 1
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
                float _Softness, _Rays, _RayCount, _RaySpeed, _Pulse, _DiveBoost, _GlowShare;
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
                output.uv = uv * 2 - 1;
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float reach = length(input.uv);
                float body = pow(saturate(1 - reach), _Softness);
                float angle = atan2(input.uv.y, input.uv.x);
                // 각도의 양 끝이 이어지도록 원 위의 점으로 잡음을 읽는다.
                float2 around = float2(cos(angle), sin(angle)) * _RayCount * 0.5;
                float t = _Time.y * _RaySpeed;
                float rays = StageNoise(around + t) * StageNoise(around * 2.3 - t * 1.7 + 9.1);
                rays = smoothstep(0.08, 0.5, rays);
                float fan = rays * pow(saturate(1 - reach), 1.2) * smoothstep(0.02, 0.2, reach);
                float pulse = 1 + _Pulse * sin(_Time.y * 0.9);
                return half4(_Color.rgb * lerp(body, fan + body * 0.35, _Rays) * pulse * (1 + _StageDive * _DiveBoost) * StageGlowAmount(_GlowShare), 0);
            }
            ENDHLSL
        }
    }
}
