// ============================================================================
// DashyCrashyCarPaint.shader
// Module 2 — Shader 2: Unlit Fake Cel-Shading Car Paint
// ----------------------------------------------------------------------------
// Purpose:
//   Provides a vibrant, high-contrast vehicle material that simulates diffuse
//   lighting mathematically within an Unlit rendering path. By computing a
//   simple N·L dot product against a hardcoded fake light direction and
//   thresholding it via a Step function, the shader produces stark cel-shaded
//   bands of color with near-zero additional fragment cost over a flat color.
//
// Architecture:
//   • Unlit rendering path — completely bypasses URP lighting passes.
//   • SRP Batcher compatible via CBUFFER declarations.
//   • Dot product + Step = 2 ALU instructions for fake lighting.
//   • HDR-compatible main color for optional Bloom interaction.
//
// Shader Graph Equivalent:
//   Normal Vector (World) → Dot Product(FakeLightDir) → Step(Threshold)
//   → Lerp(ShadowTint, MainColor) → Base Color (Unlit Master Stack)
// ============================================================================

Shader "TechArt/DashyCrashyCarPaint"
{
    Properties
    {
        [HDR] _MainColor ("Main Color", Color) = (0.94, 0.26, 0.27, 1)
        _ShadowTint ("Shadow Tint", Color) = (0.45, 0.1, 0.12, 1)
        _FakeLightDir ("Fake Light Direction", Vector) = (0.3, 1.0, 0.2, 0)
        _StepThreshold ("Shadow Threshold", Range(0, 1)) = 0.3

        [Header(Fresnel Rim)]
        [Toggle(_FRESNEL_ON)] _FresnelOn ("Enable Fresnel Rim", Float) = 1
        _FresnelColor ("Fresnel Color", Color) = (1, 1, 1, 1)
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 3.0
        _FresnelIntensity ("Fresnel Intensity", Range(0, 2)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        LOD 100

        Pass
        {
            Name "UnlitCelShade"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _FRESNEL_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ── SRP Batcher Compatibility ────────────────────────────────
            CBUFFER_START(UnityPerMaterial)
                half4  _MainColor;
                half4  _ShadowTint;
                float4 _FakeLightDir;
                half   _StepThreshold;
                half4  _FresnelColor;
                half   _FresnelPower;
                half   _FresnelIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
            #if defined(_FRESNEL_ON)
                float3 viewDirWS  : TEXCOORD1;
            #endif
            };

            // ── Vertex Shader ────────────────────────────────────────────
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);

            #if defined(_FRESNEL_ON)
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.viewDirWS = GetWorldSpaceNormalizeViewDir(posWS);
            #endif

                return OUT;
            }

            // ── Fragment Shader ──────────────────────────────────────────
            half4 frag(Varyings IN) : SV_Target
            {
                // Normalize the interpolated world-space normal.
                float3 normalWS = normalize(IN.normalWS);

                // Normalize the fake light direction (artist-configurable).
                float3 lightDir = normalize(_FakeLightDir.xyz);

                // ── N·L Diffuse Dot Product ──────────────────────────────
                // Computes the cosine of the angle between the surface
                // normal and the fake light direction. Values > 0 face
                // towards the light; values ≤ 0 face away.
                half NdotL = dot(normalWS, lightDir);

                // ── Step Threshold (Cel-Shading) ─────────────────────────
                // step(edge, x) returns 1.0 if x >= edge, else 0.0.
                // This creates a hard binary shadow line — the hallmark
                // of the "Dashy Crashy" cel-shaded aesthetic.
                half lightMask = step(_StepThreshold, NdotL);

                // ── Compose Final Color ──────────────────────────────────
                // Lit areas receive MainColor; shadowed areas receive
                // ShadowTint. lerp(a, b, t): t=0→a, t=1→b.
                half4 finalColor = lerp(_ShadowTint, _MainColor, lightMask);

                // ── Optional Fresnel Rim ─────────────────────────────────
                // Adds a bright rim highlight at glancing angles, enhancing
                // the car's silhouette without any lighting pass cost.
            #if defined(_FRESNEL_ON)
                float3 viewDir = normalize(IN.viewDirWS);
                half fresnel = pow(1.0 - saturate(dot(normalWS, viewDir)), _FresnelPower);
                finalColor.rgb += _FresnelColor.rgb * fresnel * _FresnelIntensity;
            #endif

                return finalColor;
            }

            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
