// ============================================================================
// FakeShadowProjector.shader
// Module 1 — Supplementary: Fake Ground Shadow (Blob Shadow)
// ----------------------------------------------------------------------------
// Purpose:
//   Since real-time shadows are disabled globally for mobile performance,
//   this shader renders a blurred shadow texture on a simple quad mesh
//   positioned slightly above the road surface beneath the vehicle.
//   This provides visual grounding without any shadow map computation.
//
// Architecture:
//   • Transparent Unlit — single alpha-blended pass.
//   • Rendered in the Transparent queue to overlay the road.
//   • ZWrite Off to prevent Z-fighting with the road surface.
//   • SRP Batcher compatible.
// ============================================================================

Shader "TechArt/FakeShadowProjector"
{
    Properties
    {
        _ShadowTex ("Shadow Texture", 2D) = "white" {}
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.4)
        _ShadowIntensity ("Shadow Intensity", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        LOD 100

        Pass
        {
            Name "FakeShadow"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            ZWrite Off
            ZTest LEqual

            // Offset to prevent Z-fighting with the road
            Offset -1, -1

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ShadowTex_ST;
                half4  _ShadowColor;
                half   _ShadowIntensity;
            CBUFFER_END

            TEXTURE2D(_ShadowTex);
            SAMPLER(sampler_ShadowTex);

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

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _ShadowTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half shadowMask = SAMPLE_TEXTURE2D(_ShadowTex, sampler_ShadowTex, IN.uv).a;
                half4 col = _ShadowColor;
                col.a *= shadowMask * _ShadowIntensity;
                return col;
            }

            ENDHLSL
        }
    }

    FallBack Off
}
