// 타이틀 무대의 하늘. 멀리 세운 큰 판 하나에 그러데이션, 별(도트), 성운·은하수, 오로라, 해·달과 빛무리를 그린다.
// 판의 가로는 가운데가 0이고, 세로는 아래(지평선)가 0, 위가 1이다. 움직임은 모두 시간으로 계산한다.
Shader "MiniPandoras/Stage/Sky"
{
    Properties
    {
        [HDR] _Top ("Top", Color) = (0.02, 0.03, 0.1, 1)
        [HDR] _Middle ("Middle", Color) = (0.1, 0.07, 0.3, 1)
        [HDR] _Horizon ("Horizon", Color) = (0.4, 0.5, 0.9, 1)
        _MiddleAt ("Middle At", Range(0.05, 0.9)) = 0.4
        _Falloff ("Horizon Falloff", Range(0.3, 4)) = 1.2
        _Aspect ("Aspect (width over height)", Float) = 2
        [HDR] _NebulaA ("Nebula A", Color) = (0.3, 0.1, 0.6, 1)
        [HDR] _NebulaB ("Nebula B", Color) = (0.1, 0.4, 0.8, 1)
        _NebulaAmount ("Nebula Amount", Range(0, 2)) = 0.8
        _NebulaScale ("Nebula Scale", Float) = 2.2
        _BandAngle ("Milky Band Angle", Float) = 0.5
        _BandWidth ("Milky Band Width", Float) = 0.45
        _BandShift ("Milky Band Shift", Float) = 0.4
        [HDR] _AuroraA ("Aurora A", Color) = (0.1, 0.9, 0.6, 1)
        [HDR] _AuroraB ("Aurora B", Color) = (0.4, 0.2, 0.9, 1)
        _AuroraAmount ("Aurora Amount", Range(0, 2)) = 0
        [HDR] _StarColor ("Star Color", Color) = (1, 1, 1, 1)
        _StarGrid ("Star Grid", Float) = 150
        _StarAmount ("Star Amount", Range(0, 0.2)) = 0.05
        [HDR] _OrbColor ("Orb Color", Color) = (1, 0.95, 0.8, 1)
        [HDR] _OrbHalo ("Orb Halo", Color) = (1, 0.7, 0.4, 1)
        _Orb ("Orb (x, y, radius, halo size)", Vector) = (0, 0.4, 0, 0.5)
        _OrbCraters ("Orb Craters", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
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
                half4 _Top, _Middle, _Horizon, _NebulaA, _NebulaB, _AuroraA, _AuroraB, _StarColor, _OrbColor, _OrbHalo;
                float4 _Orb;
                float _MiddleAt, _Falloff, _Aspect, _NebulaAmount, _NebulaScale, _BandAngle, _BandWidth, _BandShift;
                float _AuroraAmount, _StarGrid, _StarAmount, _OrbCraters;
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

            half3 Stars(float2 p, float seedShift)
            {
                float2 cell = floor(p * _StarGrid);
                float seed = StageHash(cell + seedShift);
                float shown = step(1 - _StarAmount, seed);
                float2 inside = frac(p * _StarGrid) - 0.5;
                float size = lerp(0.12, 0.34, StageHash(cell + 7.3 + seedShift));
                float dotMask = step(max(abs(inside.x), abs(inside.y)), size);
                float twinkle = 0.55 + 0.45 * sin(_Time.y * lerp(0.6, 2.2, StageHash(cell + 3.1)) + seed * 400);
                return _StarColor.rgb * shown * dotMask * twinkle * lerp(0.25, 1.5, StageHash(cell + 11.9));
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 p = float2((uv.x * 2 - 1) * _Aspect * 0.5, uv.y);

                float low = saturate(uv.y / _MiddleAt);
                float high = saturate((uv.y - _MiddleAt) / (1 - _MiddleAt));
                half3 color = lerp(_Horizon.rgb, _Middle.rgb, pow(low, 1 / _Falloff));
                color = lerp(color, _Top.rgb, smoothstep(0, 1, high));

                // 은하수: 비스듬한 띠 둘레에만 성운과 별이 짙다.
                float2 along = float2(cos(_BandAngle), sin(_BandAngle));
                float across = dot(p - float2(0, _BandShift), float2(-along.y, along.x));
                float band = exp(-across * across / max(0.001, _BandWidth * _BandWidth));
                float2 drift = float2(_Time.y * 0.004, _Time.y * 0.0015);
                float cloud = StageFbm(p * _NebulaScale + drift);
                float veil = StageFbm(p * _NebulaScale * 2.1 - drift * 2 + cloud * 1.3);
                float nebula = saturate(cloud * 1.5 - 0.35) * band;
                color += _NebulaA.rgb * nebula * _NebulaAmount;
                color += _NebulaB.rgb * saturate(veil * 1.6 - 0.7) * band * _NebulaAmount;
                color *= lerp(1, 0.72, saturate(veil * 2.2 - 1.25) * band * saturate(_NebulaAmount));

                // 오로라: 세로로 길게 끌린 커튼
                if (_AuroraAmount > 0)
                {
                    float wave = p.x * 1.3 + 0.25 * sin(p.x * 3.1 + _Time.y * 0.11) + 0.4 * StageFbm(float2(p.x * 1.7, _Time.y * 0.03));
                    float foot = 0.42 + 0.16 * sin(wave * 2.3 + _Time.y * 0.07);
                    float rise = saturate((uv.y - foot) / 0.5);
                    float curtain = smoothstep(0, 0.05, uv.y - foot) * pow(1 - rise, 2.2);
                    float strands = 0.55 + 0.45 * StageNoise(float2(wave * 26 + _Time.y * 0.25, uv.y * 1.2));
                    color += lerp(_AuroraA.rgb, _AuroraB.rgb, rise) * curtain * strands * _AuroraAmount;
                }

                float starsHere = saturate(uv.y * 2.2) * (0.55 + band * 1.1);
                color += (Stars(p, 0) + Stars(p * 0.53 + 3.7, 41) * 1.3) * starsHere;

                float2 toOrb = p - _Orb.xy;
                float reach = length(toOrb);
                color += _OrbHalo.rgb * exp(-reach / max(0.001, _Orb.w));
                if (_Orb.z > 0)
                {
                    float disc = smoothstep(_Orb.z, _Orb.z * 0.985, reach);
                    float2 onDisc = toOrb / _Orb.z;
                    float sea = smoothstep(0.42, 0.66, StageFbm(onDisc * 1.9 + 4.2));
                    float limb = sqrt(saturate(1 - dot(onDisc, onDisc)));
                    half3 face = _OrbColor.rgb * (0.82 + 0.18 * limb) * (1 - sea * 0.28 * _OrbCraters);
                    color = lerp(color, face, disc);
                }

                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
