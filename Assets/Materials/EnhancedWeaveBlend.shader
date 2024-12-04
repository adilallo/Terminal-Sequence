Shader "UI/EnhancedWeaveBlend"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color Tint", Color) = (1,1,1,1)
        _WaveStrength ("Wave Strength", Float) = 0.02
        _WaveFrequency ("Wave Frequency", Float) = 20.0
        _WaveSpeed ("Wave Speed", Float) = 1.0
        _EdgeFadeStart ("Edge Fade Start", Float) = 0.4
        _EdgeFadeEnd ("Edge Fade End", Float) = 0.5
        _CanvasGroupAlpha ("Canvas Group Alpha", Float) = 1.0
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
            float _WaveStrength;
            float _WaveFrequency;
            float _WaveSpeed;
            float _EdgeFadeStart;
            float _EdgeFadeEnd;
            float _CanvasGroupAlpha;

            v2f vert(appdata_t v)
            {
                v2f o;

                // Apply sine wave to vertex position for subtle vertical movement
                float waveOffset = sin(v.vertex.x * _WaveFrequency + _Time.y * _WaveSpeed) * _WaveStrength;
                v.vertex.y += waveOffset;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.texcoord = v.texcoord;

                // Apply sine wave to UV coordinates to create a weaving effect at the pixel level
                o.texcoord.y += sin(v.vertex.x * _WaveFrequency + _Time.y * _WaveSpeed) * _WaveStrength;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample the main texture
                fixed4 texColor = tex2D(_MainTex, i.texcoord);

                // Calculate distance from the center of the image
                float2 center = float2(0.5, 0.5);
                float distanceFromCenter = distance(i.texcoord, center);

                // Calculate alpha fade based on distance
                float alphaFade = smoothstep(_EdgeFadeStart, _EdgeFadeEnd, distanceFromCenter);

                // Apply the alpha fade to the texture's alpha
                texColor.a *= (1.0 - alphaFade);

                // Apply the Canvas Group alpha
                texColor.a *= _CanvasGroupAlpha;

                // Multiply texture color by the vertex color (which includes _Color)
                fixed4 col = texColor * i.color;

                return col;
            }
            ENDCG
        }
    }
}
