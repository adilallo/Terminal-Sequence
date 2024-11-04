Shader "UI/ChromaKey"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ChromaKeyColor ("Chroma Key Color", Color) = (0,1,0,1)
        _Threshold ("Threshold", Range(0,1)) = 0.3
        _Softness ("Softness", Range(0,1)) = 0.1
        _CanvasGroupAlpha ("Canvas Group Alpha", Range(0,1)) = 1.0
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off
        Fog { Mode Off }

        Pass
        {
            Name "ChromaKey"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ChromaKeyColor;
            float _Threshold;
            float _Softness;
            float _CanvasGroupAlpha;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float4 color = tex2D(_MainTex, uv);

                // Calculate the difference between the pixel color and the chroma key color
                float diff = distance(color.rgb, _ChromaKeyColor.rgb);

                // Compute alpha using smoothstep for a smooth transition
                float alpha = smoothstep(_Threshold, _Threshold + _Softness, diff);

                // Apply the computed alpha to the color
                color.a *= alpha * _CanvasGroupAlpha;

                return color;
            }
            ENDCG
        }
    }
}