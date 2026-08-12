Shader "Custom/SimpleLiquidFill"
{
    Properties
    {
        _ContainerColor ("Container Color", Color) = (1, 1, 1, 0.2) // Default to semi-transparent white
        _LiquidColor ("Liquid Color", Color) = (0, 0.5, 1, 1)       // Default to opaque blue
        _FillAmount ("Fill Amount", Range(0.0, 1.0)) = 0.5
        
        // These allow you to adjust for meshes of different sizes.
        // A default Unity Cylinder goes from -1 to 1. A default Cube goes from -0.5 to 0.5.
        _MinY ("Mesh Min Y", Float) = -1.0 
        _MaxY ("Mesh Max Y", Float) = 1.0
    }
    SubShader
    {
        // Set up for transparency so the empty part of the container can be seen through
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 localPos : TEXCOORD1; // We add this to pass the local position to the fragment shader
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Declare our properties
            float4 _ContainerColor;
            float4 _LiquidColor;
            float _FillAmount;
            float _MinY;
            float _MaxY;

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                // Convert vertex position to screen space (works with stereo/instanced rendering)
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Grab the raw, un-transformed local vertex position
                o.localPos = v.vertex.xyz;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1. Map the 0-1 FillAmount to the actual height of the 3D model
                float mappedFillLevel = lerp(_MinY, _MaxY, _FillAmount);

                // 2. Check if the current pixel's local Y is above the fill level.
                // step(edge, x) returns 1 if x >= edge, and 0 if x < edge.
                float isAboveLiquid = step(mappedFillLevel, i.localPos.y);

                // 3. Blend between the Liquid color and Container color based on the step result
                fixed4 finalColor = lerp(_LiquidColor, _ContainerColor, isAboveLiquid);

                return finalColor;
            }
            ENDCG
        }
    }
}