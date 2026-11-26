// 타이틀 무대의 물건(기둥·바위·나무·성 그림). 도트 그림을 주변광과 해, 뒤에서 비치는 가장자리 빛으로 비춘다.
// 세운 그림(_CARD)은 면이 하나라 해를 받는 정도가 같으므로, 주변광에 가장자리 빛(해 쪽이나 위가 비어 있는 윤곽)만 더한다.
// 두 번째 그림(_GlowTex)은 스스로 빛나는 자리다(창의 불, 새긴 글, 빛나는 잎).
Shader "MiniPandoras/Stage/Model"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Picture", 2D) = "white" {}
        [NoScaleOffset] _GlowTex ("Glow", 2D) = "black" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        [HDR] _Ambient ("Ambient", Color) = (0.3, 0.3, 0.5, 1)
        [HDR] _Sun ("Sun", Color) = (1, 0.9, 0.7, 1)
        _SunDir ("Toward Sun", Vector) = (0, 1, -0.4, 0)
        [HDR] _Rim ("Rim", Color) = (0.5, 0.7, 1, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        [HDR] _Glow ("Glow Color", Color) = (1, 0.8, 0.5, 1)
        _GlowPulse ("Glow Pulse", Range(0, 1)) = 0.15
        _GlowShare ("Follows Story Glow", Range(0, 1)) = 0
        _Cutoff ("Cutoff", Range(0, 1)) = 0.5
        _Steps ("Light Steps", Float) = 5
        _Haze ("Haze", Range(0, 1)) = 0
        [HDR] _HazeColor ("Haze Color (a = at foot)", Color) = (0.7, 0.6, 0.7, 1)
        _HazeFoot ("Haze Height (foot, top)", Vector) = (0, 0, 0, 0)
        _Reveal ("Reveal (outward from center)", Range(0, 1)) = 1
        _RevealFrom ("Shown Before Reveal", Float) = 1.3
        _Unfold ("Widens While Revealed", Range(0, 1)) = 0
        [Toggle(_CARD)] _Card ("Card", Float) = 0
        [Toggle(_SWAY)] _Sway ("Sway", Float) = 0
        _SwayAmount ("Sway Amount", Float) = 0.06
    }

    SubShader
    {
        Tags { "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" "RenderPipeline" = "UniversalPipeline" }
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "StageNoise.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);
        TEXTURE2D(_GlowTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_TexelSize;
            half4 _Color, _Ambient, _Sun, _Rim, _Glow, _HazeColor;
            float4 _SunDir, _HazeFoot;
            float _RimPower, _GlowPulse, _Cutoff, _Steps, _Haze, _SwayAmount, _GlowShare, _Reveal, _RevealFrom, _Unfold;
        CBUFFER_END

        float3 StageSway(float3 positionWS, float3 pivotOS)
        {
            #if defined(_SWAY)
                // 카드의 아랫변(TEXCOORD1)에서 높을수록 크게 흔들린다. 바람은 왼쪽에서 오른쪽으로 물결처럼 지나간다.
                float3 pivotWS = TransformObjectToWorld(pivotOS);
                float height = length(positionWS - pivotWS);
                float gust = sin(_Time.y * 1.5 - pivotWS.x * 0.55 + pivotWS.y * 0.3) + 0.5 * sin(_Time.y * 2.9 - pivotWS.x * 1.7);
                positionWS.x += height * _SwayAmount * (0.6 + gust);
            #endif
            return positionWS;
        }
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma shader_feature_local _CARD
            #pragma shader_feature_local_vertex _SWAY
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float3 pivotOS : TEXCOORD1;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half4 color : TEXCOORD3;
                half fogFactor : TEXCOORD4;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                // 다 펼치기 직전에 조금 넘쳤다 돌아온다.
                float spread = _Reveal + sin(_Reveal * PI) * 0.24 * _Reveal;
                input.positionOS.x *= lerp(1, lerp(0.3, 1, spread), _Unfold);
                output.positionWS = StageSway(TransformObjectToWorld(input.positionOS), input.pivotOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.color = input.color * _Color;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 Fragment(Varyings input) : SV_Target
            {
                half4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                clip(texel.a * RevealMask(input.uv, _Reveal, _RevealFrom) - _Cutoff);
                float3 toSun = normalize(_SunDir.xyz);
                half3 light = _Ambient.rgb;
                #if defined(_CARD)
                    // 해 쪽으로 몇 도트 건너가 비어 있으면 윤곽이다.
                    float2 dots = float2(toSun.x, -toSun.z) * _MainTex_TexelSize.xy * 4;
                    half beyond = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + dots).a;
                    half above = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(0, _MainTex_TexelSize.y * 3)).a;
                    half rim = saturate((1 - beyond) + (1 - above) * 0.5);
                #else
                    float3 normal = normalize(input.normalWS);
                    float facing = saturate(dot(normal, toSun) * 0.5 + 0.5);
                    facing = floor(facing * facing * _Steps + 0.5) / _Steps;
                    light += _Sun.rgb * facing;
                    float3 view = normalize(GetCameraPositionWS() - input.positionWS);
                    float rim = pow(1 - saturate(abs(dot(normal, view))), _RimPower) * saturate(dot(normal, toSun) + 0.6);
                #endif
                half3 lit = texel.rgb * light + _Rim.rgb * rim * (0.35 + texel.rgb);

                half3 glow = SAMPLE_TEXTURE2D(_GlowTex, sampler_MainTex, input.uv).rgb;
                lit += glow * _Glow.rgb * (1 - _GlowPulse + _GlowPulse * sin(_Time.y * 1.3 + input.positionWS.x * 2)) * StageGlowAmount(_GlowShare);

                // 발치 안개: 무대의 위는 -Z다.
                float height = -input.positionWS.z;
                float low = _HazeFoot.y > _HazeFoot.x ? 1 - saturate((height - _HazeFoot.x) / (_HazeFoot.y - _HazeFoot.x)) : 0;
                lit = lerp(lit, _HazeColor.rgb, saturate(_Haze + low * _HazeColor.a));
                return half4(MixFog(lit, input.fogFactor), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ColorMask R

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma shader_feature_local_vertex _SWAY

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 pivotOS : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformWorldToHClip(StageSway(TransformObjectToWorld(input.positionOS), input.pivotOS));
                output.uv = input.uv;
                return output;
            }

            half Fragment(Varyings input) : SV_Target
            {
                clip(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }
    }
}
