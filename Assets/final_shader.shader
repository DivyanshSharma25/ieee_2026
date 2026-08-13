Shader "Custom/VR_LitLiquidFill_DoubleSided"
{
    Properties
    {
        _BaseColor ("Liquid Color (Bottom)", Color) = (0, 0.5, 1, 1)
        _EmptyColor ("Empty Color (Top)", Color) = (1, 1, 1, 0.2) 
        
        _FillLevel ("Fill Level", Range(0.0, 1.0)) = 0.5
        _ContainerHeight ("Container Height", Float) = 1.0
        _ContainerOffset ("Container Offset (Bottom Y)", Float) = 0.0

        // NEW: Creates a dropdown in the inspector to control Face Culling
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Render Faces (Culling)", Float) = 2 // Default to 2 (Back)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            
            // NEW: Applies the culling mode selected in the material inspector
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0; 
                float3 normalWS : TEXCOORD1;   
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
                // Note: _Cull doesn't need to be in the CBUFFER because it's a pipeline state, not a math variable
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                output.positionHCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                
                output.localY = input.positionOS.y;

                return output;
            }

            half4 frag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float normalizedY = (input.localY - _ContainerOffset) / _ContainerHeight;
                half isLiquid = step(normalizedY, _FillLevel);
                
                half4 albedo = lerp(_EmptyColor, _BaseColor, isLiquid);

                Light mainLight = GetMainLight();
                float3 normal = normalize(input.normalWS);
                
                // NEW: If we are rendering the inside of the mesh, flip the normal so lighting still works correctly
                if (!isFrontFace)
                {
                    normal = -normal;
                }

                half NdotL = saturate(dot(normal, mainLight.direction));
                half3 diffuse = mainLight.color * mainLight.distanceAttenuation * NdotL;
                half3 ambient = SampleSH(normal);
                
                half3 finalColor = albedo.rgb * (diffuse + ambient);

                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }
    }
}