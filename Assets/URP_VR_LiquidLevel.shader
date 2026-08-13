Shader "Custom/URP_VR_LiquidLevel"
{
    Properties
    {
        [Header(Liquid Colors)]
        _BaseColor ("Liquid Wall Color", Color) = (0.0, 0.4, 0.8, 0.7)
        _TopColor ("Liquid Flat Surface Cap", Color) = (0.0, 0.6, 0.9, 0.95)

        [Header(Empty Container Glass Colors)]
        _EmptyColor ("Glass Outside Base Tint", Color) = (0.9, 0.9, 1.0, 0.05)
        _EmptyInsideColor ("Glass Inside Base Tint", Color) = (0.7, 0.8, 0.9, 0.08)

        [Header(PBR Glass Settings (Normal Material Look))]
        _GlassSmoothness ("Glass Smoothness", Range(0.0, 1.0)) = 0.95
        _ReflectionStrength ("Environment Reflection Strength", Range(0.0, 3.0)) = 1.5

        [Header(3D Depth and Highlights)]
        _OpenTopCutoff ("Open Top Cutoff (Clips Roof)", Range(0.5, 1.05)) = 0.98
        _FresnelPower ("Fresnel Edge Power", Range(0.5, 5.0)) = 2.5
        _SpecularSmoothness ("Main Light Glint Size", Range(8.0, 128.0)) = 64.0
        _SurfaceBandWidth ("Liquid Meniscus Thickness", Range(0.001, 0.05)) = 0.015
        _SurfaceBandColor ("Liquid Meniscus Highlight", Color) = (0.4, 0.8, 1.0, 0.9)

        [Header(Level Settings)]
        _FillLevel ("Fill Level", Range(0.0, 1.0)) = 0.5
        _ContainerHeight ("Container Height", Float) = 1.0
        _ContainerOffset ("Container Bottom Y Offset", Float) = -0.5
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        // PASS 1: Inside the Container (Liquid Volume Cap & Back Glass)
        Pass
        {
            Name "Container_Inside"
            Tags { "LightMode" = "UniversalForward" }

            Cull Front
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _TopColor;
                half4 _EmptyColor;
                half4 _EmptyInsideColor;
                half4 _SurfaceBandColor;
                half _GlassSmoothness;
                half _ReflectionStrength;
                half _OpenTopCutoff;
                half _FresnelPower;
                half _SpecularSmoothness;
                half _SurfaceBandWidth;
                half _FillLevel;
                float _ContainerHeight;
                float _ContainerOffset;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionOS = input.positionOS.xyz;
                
                output.normalWS = TransformObjectToWorldNormal(-input.normalOS);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float normalizedY = (input.positionOS.y - _ContainerOffset) / _ContainerHeight;
                clip(_OpenTopCutoff - normalizedY);

                // Liquid Surface Raycast
                float3 cameraPosWS = GetCameraPositionWS();
                float3 cameraPosOS = TransformWorldToObject(cameraPosWS);
                float3 rayDirOS = normalize(input.positionOS - cameraPosOS);
                
                float liquidY = _FillLevel * _ContainerHeight + _ContainerOffset;
                float rayDirYSafe = abs(rayDirOS.y) < 0.00001 ? 0.00001 * sign(rayDirOS.y) : rayDirOS.y;
                float t = (liquidY - cameraPosOS.y) / rayDirYSafe;
                float distToFrag = length(input.positionOS - cameraPosOS);
                
                half hitSurface = step(0.0001, t) * step(t, distToFrag);

                // Wall Colors
                half isLiquidWall = step((half)normalizedY, _FillLevel);
                half4 wallCol = lerp(_EmptyInsideColor, _BaseColor, isLiquidWall);

                // Inside Cavity Shadow
                half cavityAO = lerp(0.3, 1.0, normalizedY); 
                wallCol.rgb *= lerp(1.0, cavityAO, 1.0 - isLiquidWall);

                half4 col = lerp(wallCol, _TopColor, hitSurface);

                // Edge Fresnel
                float NdotV = saturate(dot(normalize(input.normalWS), normalize(input.viewDirWS)));
                half fresnel = pow(1.0 - NdotV, _FresnelPower);
                col.a = saturate(col.a + fresnel * 0.5);

                return col;
            }
            ENDHLSL
        }

        // PASS 2: Outside the Container (Front Glass, Reflections, Specular)
        Pass
        {
            Name "Container_Outside"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _TopColor;
                half4 _EmptyColor;
                half4 _EmptyInsideColor;
                half4 _SurfaceBandColor;
                half _GlassSmoothness;
                half _ReflectionStrength;
                half _OpenTopCutoff;
                half _FresnelPower;
                half _SpecularSmoothness;
                half _SurfaceBandWidth;
                half _FillLevel;
                float _ContainerHeight;
                float _ContainerOffset;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.positionOS = input.positionOS.xyz;
                
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(positionWS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);
                float normalizedY = (input.positionOS.y - _ContainerOffset) / _ContainerHeight;
                
                clip(_OpenTopCutoff - normalizedY);

                half isLiquidWall = step((half)normalizedY, _FillLevel);
                half4 col = lerp(_EmptyColor, _BaseColor, isLiquidWall);

                // 1. PBR Environment Reflections (Makes it look like standard Unity material)
                float3 reflectVector = reflect(-viewDirWS, normalWS);
                half perceptualRoughness = 1.0 - _GlassSmoothness;
                half3 envReflections = GlossyEnvironmentReflection(reflectVector, perceptualRoughness, 1.0);
                
                // Apply reflection heavily to the empty glass part, gently to the liquid part
                half reflectionMask = lerp(1.0, 0.4, isLiquidWall); 
                col.rgb += envReflections * _ReflectionStrength * reflectionMask;

                // 2. Main Light Specular Glint
                Light mainLight = GetMainLight();
                float3 halfDir = normalize(mainLight.direction + viewDirWS);
                float NdotH = saturate(dot(normalWS, halfDir));
                half spec = pow(NdotH, _SpecularSmoothness) * mainLight.distanceAttenuation;
                col.rgb += spec * mainLight.color * 0.8;

                // 3. Fresnel Edge Glass Thickness
                float NdotV = saturate(dot(normalWS, viewDirWS));
                half fresnel = pow(1.0 - NdotV, _FresnelPower);
                col.rgb += fresnel * 0.2;
                col.a = saturate(col.a + fresnel * 0.7);

                // 4. Liquid Meniscus Edge
                float distToSurface = abs(normalizedY - _FillLevel);
                half surfaceLine = smoothstep(_SurfaceBandWidth, 0.0, distToSurface);
                col = lerp(col, _SurfaceBandColor, surfaceLine);

                return col;
            }
            ENDHLSL
        }
    }
}