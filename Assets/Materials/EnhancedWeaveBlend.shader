Shader "UI/EnhancedWeaveBlend"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color Tint", Color) = (1,1,1,1)
        _CanvasGroupAlpha ("Canvas Group Alpha", Float) = 1.0
        _WaveStrength ("Wave Strength", Float) = 0.1
        _WaveFrequency ("Wave Frequency", Float) = 10.0
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always

        // Standard Alpha Blending
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _CanvasGroupAlpha;
            float _WaveStrength;
            float _WaveFrequency;

            v2f vert(appdata_t v)
            {
                v2f o;
                // Apply a sine wave to the y-position based on x-coordinate for a waving effect
                float wave = sin(v.vertex.x * _WaveFrequency) * _WaveStrength;
                v.vertex.y += wave;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.texcoord = v.texcoord;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample the main texture
                fixed4 texColor = tex2D(_MainTex, i.texcoord);
                // Multiply texture color by the vertex color (which includes _Color)
                fixed4 col = texColor * i.color;
                // Apply the alpha value from _CanvasGroupAlpha to the final color
                col.a *= _CanvasGroupAlpha;
                return col;
            }
            ENDCG
        }
    }
}
