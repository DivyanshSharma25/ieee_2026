Shader "Custom/VR_UnlitLiquidFill"
{
    Properties
    {
        _BaseColor ("Liquid Color", Color) = (0, 0.5, 1, 1)
        _EmptyColor ("Empty Color", Color) = (1, 1, 1, 1)
        _FillLevel ("Fill Level", Range(0.0, 1.0)) = 0.5
        _ContainerHeight ("Container Height", Float) = 1.0
        _ContainerOffset ("Container Offset (Bottom Y)", Float) = 0.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Required for VR Single Pass Instanced rendering
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                
                // VR Macro: Defines the instance ID required for instanced rendering
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float localY : TEXCOORD0;
                
                // VR Macros: Passes the instance ID and stereo target eye from vertex to fragment
                UNITY_VERTEX_INPUT_INSTANCE_ID 
                UNITY_VERTEX_OUTPUT_STEREO 
            };

            // Declare properties in a CBUFFER for SRP Batcher compatibility (another massive performance boost)
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _EmptyColor;
                float _FillLevel;
                float _ContainerHeight;
                float _ContainerOffset;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                // VR Setup: Initializes the instance ID and stereo target for this vertex
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Convert Object Space position to Clip Space
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                
                // Step 1: Read the Local Y-Position and pass it to the fragment shader
                output.localY = input.positionOS.y;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // VR Setup: Allows the fragment shader to use stereo and instance IDs
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Step 2: Normalize the Height (The 0 to 1 Scale)
                float normalizedY = (input.localY - _ContainerOffset) / _ContainerHeight;

                // Step 3: The step() Mask
                // Returns 1.0 if normalizedY <= _FillLevel, otherwise 0.0
                half isLiquid = step(normalizedY, _FillLevel);

                // Step 4: Painting the Colors
                // lerp returns _EmptyColor when isLiquid is 0.0, and _BaseColor when isLiquid is 1.0
                half4 finalColor = lerp(_EmptyColor, _BaseColor, isLiquid);

                return finalColor;
            }
            ENDHLSL
        }
    }
}