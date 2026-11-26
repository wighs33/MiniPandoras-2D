// 빛으로 된 거대한 형상(프롤로그의 여신). 그림을 빛 색으로 물들여 더해 그리고, 아래로 갈수록 가루로 흩어지며, 옷자락이 일렁인다.
Shader "MiniPandoras/Stage/Spirit"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Picture", 2D) = "white" {}
        [HDR] _Shade ("Shade", Color) = (0.1, 0.15, 0.5, 1)
        [HDR] _Light ("Light", Color) = (1, 0.95, 0.85, 1)
        _Keep ("Keep Original Color", Range(0, 1)) = 0.4
        _Opacity ("Opacity", Range(0, 1)) = 0.7
        _Dissolve ("Dissolve From (height)", Range(0, 1)) = 0.35
        _Flow ("Flow", Float) = 0.006
    }

    SubShader
    {
        Tags { "Queue" = "Transparent+10" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
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
                half4 _Shade, _Light;
                float _Keep, _Opacity, _Dissolve, _Flow;
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
                float away = saturate(abs(uv.x - 0.5) * 2 + (0.7 - uv.y) * 0.8);
                float2 sway = float2(StageNoise(float2(uv.y * 5 - _Time.y * 0.35, _Time.y * 0.13)) - 0.5,
                                     StageNoise(float2(uv.x * 5 + _Time.y * 0.2, _Time.y * 0.17 + 5)) - 0.5);
                half4 texel = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + sway * _Flow * away * 2);
                float luminance = dot(texel.rgb, half3(0.3, 0.55, 0.15));
                half3 spirit = lerp(_Shade.rgb, _Light.rgb, smoothstep(0.1, 0.95, luminance));
                half3 color = lerp(spirit, texel.rgb * _Light.rgb, _Keep);
                float grain = StageNoise(uv * float2(60, 90) + float2(0, -_Time.y * 0.6));
                float solid = smoothstep(0, 1, saturate((uv.y - _Dissolve * 0.35) / max(0.01, _Dissolve)) + (grain - 0.5) * 0.5);
                float gone = saturate(_StageDive * 2.2 - grain * 0.6);
                float alpha = texel.a * _Opacity * saturate(solid) * (1 - gone);
                return half4(color * alpha, alpha * 0.92);
            }
            ENDHLSL
        }
    }
}
