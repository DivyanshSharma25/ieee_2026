Shader "Custom/VR_LitLiquidFill"
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
        LOD 300

        // ==========================================
        // PASS 1: Forward Lit (Receives light/color)
        // ==========================================
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // VR Optimization
            #pragma multi_compile_instancing

            // URP Light & Shadow Macros
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL; // NEW: Grab the vertex normals
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0; // NEW: World space position for lighting
                float3 normalWS : TEXCOORD1;   // NEW: World space normal for lighting
                float localY : TEXCOORD2;
                
                UNITY_VERTEX_INPUT_INSTANCE_ID 
                UNITY_VERTEX_OUTPUT_STEREO 
            };

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

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // URP Built-in functions to safely transform position and normals to World Space
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionHCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                
                output.localY = input.positionOS.y;

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // 1. LIQUID MATH (Exactly the same as before)
                float normalizedY = (input.localY - _ContainerOffset) / _ContainerHeight;
                half isLiquid = step(normalizedY, _FillLevel);
                half4 albedo = lerp(_EmptyColor, _BaseColor, isLiquid);

                // 2. LIGHTING MATH
                // Get the main directional light
                Light mainLight = GetMainLight();
                
                // Ensure the normal vector is perfectly straight
                float3 normal = normalize(input.normalWS);
                
                // Lambertian reflection (N dot L) - How directly is the light hitting the surface?
                half NdotL = saturate(dot(normal, mainLight.direction));
                
                // Calculate Diffuse (Color * Intensity * Angle)
                half3 diffuse = mainLight.color * mainLight.distanceAttenuation * NdotL;
                
                // Calculate Ambient (Spherical Harmonics) so the dark side of the object isn't pitch black
                half3 ambient = SampleSH(normal);
                
                // Multiply our liquid colors by the final light data
                half3 finalColor = albedo.rgb * (diffuse + ambient);

                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }

        // ==========================================
        // PASS 2: ShadowCaster (Casts shadows)
        // ==========================================
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                
                // Projects the mesh's silhouette into the shadow map
                output.positionHCS = TransformWorldToHClip(positionWS); 
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Shadow pass doesn't need to output color, only depth
                return 0; 
            }
            ENDHLSL
        }
    }
}