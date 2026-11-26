// 빛가루(소울더스트) 묶음. 가루 하나가 카메라를 보는 작은 판이고, 저마다 다른 박자로 떠돌고 반짝인다.
// 타이틀에서 게임으로 뛰어들면(_StageDive) 가운데에서 바깥으로, 그리고 카메라 쪽으로 흩어진다.
// 메시: POSITION = 가루 자리, TEXCOORD0 = 판의 모서리(-1~1), TEXCOORD1 = (박자, 크기, 떠도는 정도), 색 = 가루 색.
Shader "MiniPandoras/Stage/Dust"
{
    Properties
    {
        _Brightness ("Brightness", Float) = 1
        _Size ("Size", Float) = 1
        _Drift ("Drift", Float) = 0.15
        _Rise ("Rise (height of one loop)", Float) = 0
        _Burst ("Burst Distance", Float) = 14
        _Center ("Burst Center", Vector) = (0, 0, 0, 0)
        _GlowShare ("Follows Story Glow", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent+20" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
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
                float4 _Center;
                float _Brightness, _Size, _Drift, _Rise, _Burst, _GlowShare;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 corner : TEXCOORD0;
                float3 mote : TEXCOORD1;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 corner : TEXCOORD0;
                half3 color : TEXCOORD1;
            };

            Varyings Vertex(Attributes input)
            {
                float seed = input.mote.x;
                float t = _Time.y;
                float3 center = input.positionOS;
                float3 sway = float3(sin(t * 0.7 + seed * 41), sin(t * 0.53 + seed * 67), cos(t * 0.61 + seed * 29));
                center += sway * _Drift * input.mote.z;
                float fade = 1;
                if (abs(_Rise) > 0)
                {
                    // 무대의 위는 -Z다. 떠오르다 사라지고 아래에서 다시 나타난다.
                    float loop = frac(t * 0.04 * lerp(0.6, 1.5, frac(seed * 13.7)) + seed);
                    center.z -= loop * _Rise;
                    fade = smoothstep(0, 0.15, loop) * smoothstep(1, 0.7, loop);
                }

                float dive = _StageDive * _StageDive;
                float3 outward = normalize(center - _Center.xyz + float3(0, 0, 1e-4));
                float reach = lerp(0.4, 1.3, frac(seed * 7.13));
                center += (outward * float3(1, 0.4, 1) + float3(0, -1.6, 0)) * dive * _Burst * reach;

                float3 positionVS = TransformWorldToView(TransformObjectToWorld(center));
                positionVS.xy += input.corner * input.mote.y * _Size * (1 + dive * 1.4);

                Varyings output;
                output.positionCS = TransformWViewToHClip(positionVS);
                output.corner = input.corner;
                float twinkle = 0.6 + 0.4 * sin(t * lerp(0.8, 2.6, frac(seed * 5.3)) + seed * 90);
                output.color = input.color.rgb * input.color.a * twinkle * fade * _Brightness * StageGlowAmount(_GlowShare);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                // 도트처럼 각진 심과 부드러운 번짐
                float edge = max(abs(input.corner.x), abs(input.corner.y));
                float core = step(edge, 0.34);
                float glow = pow(saturate(1 - length(input.corner)), 2.4) * 0.45;
                return half4(input.color * (core + glow), 0);
            }
            ENDHLSL
        }
    }
}
