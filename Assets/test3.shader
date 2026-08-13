Shader "Custom/VR_LitLiquidFill_Transparent"
{
    Properties
    {
        _BaseColor ("Liquid Color (Bottom)", Color) = (0, 0.5, 1, 1)
        
        // The Alpha (A) value here now controls the transparency of the top part
        _EmptyColor ("Empty Color (Top)", Color) = (1, 1, 1, 0.2) 
        
        _FillLevel ("Fill Level", Range(0.0, 1.0)) = 0.5
        _ContainerHeight ("Container Height", Float) = 1.0
        _ContainerOffset ("Container Offset (Bottom Y)", Float) = 0.0
    }
    SubShader
    {
        // 1. CHANGED: Tell Unity this is a transparent object so it renders after opaque objects
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            // 2. CHANGED: Enable Alpha Blending 
            Blend SrcAlpha OneMinusSrcAlpha
            
            // 3. CHANGED: Turn off Depth Writing to prevent transparency sorting glitches
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // VR Optimization
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

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Calculate the split between top and bottom
                float normalizedY = (input.localY - _ContainerOffset) / _ContainerHeight;
                half isLiquid = step(normalizedY, _FillLevel);
                
                // This lerp now seamlessly handles blending the Alpha channel as well as RGB
                half4 albedo = lerp(_EmptyColor, _BaseColor, isLiquid);

                Light mainLight = GetMainLight();
                float3 normal = normalize(input.normalWS);
                half NdotL = saturate(dot(normal, mainLight.direction));
                
                half3 diffuse = mainLight.color * mainLight.distanceAttenuation * NdotL;
                half3 ambient = SampleSH(normal);
                
                half3 finalColor = albedo.rgb * (diffuse + ambient);

                // Returns the computed lighting with the dynamically calculated Alpha
                return half4(finalColor, albedo.a);
            }
            ENDHLSL
        }
    }
}