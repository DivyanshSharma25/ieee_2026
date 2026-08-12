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
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ UNITY_SINGLE_PASS_STEREO

            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float3 localPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Shader properties
            float4 _ContainerColor;
            float4 _LiquidColor;
            float _FillAmount;
            float _MinY;
            float _MaxY;

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(Varyings, o);

                o.positionCS = UnityObjectToClipPos(v.positionOS);
                o.localPos = v.positionOS.xyz;
                o.uv = v.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float mappedFillLevel = lerp(_MinY, _MaxY, _FillAmount);
                float isAboveLiquid = step(mappedFillLevel, i.localPos.y);
                float4 finalColor = lerp(_LiquidColor, _ContainerColor, isAboveLiquid);
                return finalColor;
            }

            ENDHLSL
        }
    }
}