// ============================================================================
// ArcadeRoadScrolling.shader — Simple Scrolling Road (Texture Only)
// ----------------------------------------------------------------------------
// Scrolls the road texture using the accumulated offset from GameManager.
// All procedural effects removed — just clean texture movement.
// ============================================================================

Shader "TechArt/ArcadeRoadScrolling"
{
    Properties
    {
        [MainTexture] _BaseMap ("Road Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint Color", Color) = (1, 1, 1, 1)
        _Tiling ("Tiling", Vector) = (1, 4, 0, 0)
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
            Name "UnlitRoad"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Shadow receiving keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                float4 _Tiling;
            CBUFFER_END

            // Global variable set by GameManager
            float4 _GlobalScrollVector;

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 shadowCoord : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = vertexInput.positionCS;
                
                OUT.shadowCoord = GetShadowCoord(vertexInput);

                // World-space Z for scroll syncing
                float3 worldPos = vertexInput.positionWS;
                float2 worldUV = float2(IN.uv.x, worldPos.z / 10.0);

                // Add accumulated offset from GameManager
                float2 scrolledUV = worldUV + _GlobalScrollVector.xy;

                // Apply tiling
                OUT.uv = (scrolledUV * _Tiling.xy) + _BaseMap_ST.zw;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                
                Light mainLight = GetMainLight(IN.shadowCoord);
                half shadow = mainLight.shadowAttenuation;

                // Adjust this lerp to make the shadow perfectly dark (e.g. 0.3)
                half shadowStrength = lerp(0.3, 1.0, shadow);

                return texColor * _BaseColor * shadowStrength;
            }

            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
