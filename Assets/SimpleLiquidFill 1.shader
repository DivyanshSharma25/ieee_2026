Shader "Universal Render Pipeline/Unlit/SimpleLiquidFillURP"
{
    Properties
    {
        _ContainerColor ("Container Color", Color) = (1, 1, 1, 0.2)
        _LiquidColor ("Liquid Color", Color) = (0, 0.5, 1, 1)
        _FillAmount ("Fill Amount", Range(0.0, 1.0)) = 0.5
        _MinY ("Mesh Min Y", Float) = -1.0
        _MaxY ("Mesh Max Y", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma exclude_renderers gles xboxone ps3

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD1;
            };

            float4 _ContainerColor;
            float4 _LiquidColor;
            float _FillAmount;
            float _MinY;
            float _MaxY;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionOS = IN.positionOS;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Compute per-pixel world Y from the interpolated object-space position
                float3 worldPos = TransformObjectToWorld(IN.positionOS);
                float worldY = worldPos.y;

                // Map object-space _MinY/_MaxY into world-space Y
                float worldMinY = TransformObjectToWorld(float3(0, _MinY, 0)).y;
                float worldMaxY = TransformObjectToWorld(float3(0, _MaxY, 0)).y;
                float mappedFillLevel = lerp(worldMinY, worldMaxY, _FillAmount);

                float isAboveLiquid = step(mappedFillLevel, worldY);

                float4 finalColor = lerp(_LiquidColor, _ContainerColor, isAboveLiquid);
                return finalColor;
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/BlitCopy"
}
