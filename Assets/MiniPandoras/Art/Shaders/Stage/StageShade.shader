// 얼굴도 옷도 보이지 않는 거대한 형상(신, 갓슬레이어, 십이장). 몸은 어둠이고 윤곽에만 저마다의 빛이 돈다.
// 그림에서는 알파(윤곽)만 쓴다. 발치는 안개처럼 흩어진다. 빛은 이야기가 차오르게 할 수 있다(_GlowShare).
Shader "MiniPandoras/Stage/Shade"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Picture", 2D) = "white" {}
        _Dark ("Dark", Color) = (0.02, 0.02, 0.05, 1)
        [HDR] _Hue ("Rim Light", Color) = (1, 0.8, 0.4, 1)
        _Rim ("Rim Width (share of height)", Range(0.001, 0.05)) = 0.007
        _Halo ("Outer Glow", Range(0, 2)) = 0.5
        _Foot ("Foot Fade (height)", Range(0, 0.6)) = 0.22
        _Haze ("Haze", Range(0, 1)) = 0
        _HazeColor ("Haze Color", Color) = (0.2, 0.2, 0.4, 1)
        _Opacity ("Opacity", Range(0, 1)) = 1
        _GlowShare ("Follows Story Glow", Range(0, 1)) = 0.6
        _Seed ("Seed", Float) = 0
        _Reveal ("Reveal (outward from center)", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "StageNoise.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_TexelSize;
                half4 _Dark, _Hue, _HazeColor;
                float _Rim, _Halo, _Foot, _Haze, _Opacity, _GlowShare, _Seed, _Reveal;
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

            half Alpha(float2 uv)
            {
                float2 inside = step(0, uv) * step(uv, 1);
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a * inside.x * inside.y;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                // 옷자락이 바람에 일렁인다.
                float loose = saturate(1 - input.uv.y * 1.3);
                float2 uv = input.uv;
                uv.x += (StageNoise(float2(uv.y * 6 - _Time.y * 0.5 + _Seed, _Time.y * 0.2)) - 0.5) * 0.018 * loose;
                half body = smoothstep(0.35, 0.65, Alpha(uv));

                float2 step1 = float2(_MainTex_TexelSize.x / _MainTex_TexelSize.y, 1) * _Rim;
                half near = 0;
                half far = 0;
                static const float2 around[8] = { float2(1, 0), float2(-1, 0), float2(0, 1), float2(0, -1), float2(0.7, 0.7), float2(-0.7, 0.7), float2(0.7, -0.7), float2(-0.7, -0.7) };
                for (int i = 0; i < 8; i++)
                {
                    near += Alpha(uv + around[i] * step1);
                    far += Alpha(uv + around[i] * step1 * 3.2);
                }

                near /= 8;
                far /= 8;
                float flicker = 0.85 + 0.15 * sin(_Time.y * 1.1 + _Seed * 5 + uv.y * 4);
                half glow = StageGlowAmount(_GlowShare) * flicker;
                half rim = pow(saturate(1 - near), 1.3) * 2.8;
                half3 color = lerp(_Dark.rgb, _HazeColor.rgb, _Haze) + _Hue.rgb * rim * glow;
                half halo = saturate(near * 0.6 + far * 0.5) * (1 - body) * _Halo * glow;

                float foot = smoothstep(0, max(0.001, _Foot), input.uv.y + (StageNoise(input.uv * float2(9, 5) + _Time.y * 0.15) - 0.5) * 0.12);
                foot *= RevealMask(input.uv, _Reveal, 0);
                half alpha = body * foot * _Opacity;
                return half4(color * alpha + _Hue.rgb * halo * foot * _Opacity, alpha);
            }
            ENDHLSL
        }
    }
}
