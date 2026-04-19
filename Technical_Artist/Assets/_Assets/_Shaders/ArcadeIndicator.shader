Shader "Arcade/IndicatorGlow"
{
    Properties
    {
        [HDR] _BaseColor ("Indicator Color", Color) = (1, 0.5, 0, 1)
        _BlinkSpeed ("Blink Speed", Float) = 15.0
        
        [Header(Outer Glow)]
        _Radius ("Glow Radius", Range(0.001, 1)) = 0.4
        _Softness ("Glow Softness", Range(0.001, 1)) = 0.2
        _GlowIntensity ("Glow Brightness", Range(0.1, 2.0)) = 0.4

        [Header(Inner Core)]
        _CoreRadius ("Core Radius", Range(0.001, 1)) = 0.15
        _CoreSoftness ("Core Softness", Range(0.001, 1)) = 0.05
        _CoreIntensity ("Core Brightness", Range(0.1, 5.0)) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha One // Additive Blending for a glowing light effect
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _BlinkSpeed;
                float _Radius;
                float _Softness;
                float _GlowIntensity;
                float _CoreRadius;
                float _CoreSoftness;
                float _CoreIntensity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Distance from center
                float dist = distance(IN.uv, float2(0.5, 0.5));
                
                // Remap sliders so 1.0 equals the exact maximum edge of the Quad (0.5 UV space)
                float trueGlowRadius = _Radius * 0.5;
                float trueGlowSoftness = _Softness * 0.5;
                float trueCoreRadius = _CoreRadius * 0.5;
                float trueCoreSoftness = _CoreSoftness * 1;
                
                // Outer Glow Circle
                float outerGlow = 1.0 - smoothstep(trueGlowRadius - trueGlowSoftness, trueGlowRadius, dist);
                outerGlow *= _GlowIntensity;

                // Inner Bright Core
                float innerCore = 1.0 - smoothstep(trueCoreRadius - trueCoreSoftness, trueCoreRadius, dist);
                innerCore *= _CoreIntensity;

                // Combine them
                float finalCircle = outerGlow + innerCore;

                // Sharp Blinking Math using Sine wave
                float blink = sin(_Time.y * _BlinkSpeed);
                blink = step(0.0, blink); // Snaps to exactly 0 or 1

                return _BaseColor * finalCircle * blink;
            }
            ENDHLSL
        }
    }
}
