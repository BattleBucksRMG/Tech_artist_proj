// ============================================================================
// ArcadeGrassScrolling.shader — Procedural Striped Grass (No Texture Needed!)
// ----------------------------------------------------------------------------
// Generates vibrant alternating strips of green directly on the GPU.
// No image file required. Fully arcade-stylized.
// ============================================================================

Shader "TechArt/ArcadeGrassScrolling"
{
    Properties
    {
        [HDR] _GrassColorA ("Grass Strip Color A", Color) = (0.15, 0.75, 0.05, 1)
        [HDR] _GrassColorB ("Grass Strip Color B", Color) = (0.1, 0.55, 0.02, 1)
        [HDR] _GrassColorC ("Grass Highlight Strip", Color) = (0.3, 0.95, 0.15, 1)
        _StripCount ("Number of Strips (along Z)", Float) = 8
        _HighlightWidth ("Highlight Strip Width", Range(0.01, 0.3)) = 0.05
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
            Name "UnlitGrass"
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

            // SRP Batcher Compatibility
            CBUFFER_START(UnityPerMaterial)
                half4  _GrassColorA;
                half4  _GrassColorB;
                half4  _GrassColorC;
                float  _StripCount;
                float  _HighlightWidth;
            CBUFFER_END

            // Global variable set by GameManager
            float4 _GlobalScrollVector;

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

                // Compute World Position for perfect road sync
                float3 worldPos = vertexInput.positionWS;

                // Use worldPos.x for the strip direction (perpendicular to road),
                // and worldPos.z + scroll offset for the scrolling direction.
                float2 worldUV = float2(worldPos.x / 10.0, worldPos.z / 10.0);

                // Apply the exact accumulated offset from GameManager
                OUT.uv = worldUV + _GlobalScrollVector.xy;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Generate procedural strips along the X-axis (perpendicular to the road)
                // This creates long horizontal bands of green that scroll with the road.
                float stripPattern = frac(IN.uv.x * _StripCount);

                // Alternate between Color A and Color B
                half4 baseColor = lerp(_GrassColorA, _GrassColorB, step(0.5, stripPattern));

                // Add a thin highlight strip at the boundary for that arcade pop
                float highlightMask = 1.0 - smoothstep(0.0, _HighlightWidth, abs(stripPattern - 0.5));
                baseColor = lerp(baseColor, _GrassColorC, highlightMask * 0.6);
                
                Light mainLight = GetMainLight(IN.shadowCoord);
                half shadow = mainLight.shadowAttenuation;

                // Make shadow dark by adjusting the lerp
                half shadowStrength = lerp(0.3, 1.0, shadow);

                return baseColor * shadowStrength;
            }

            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
