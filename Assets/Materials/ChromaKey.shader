Shader "UI/ChromaKey_HSV"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ChromaKeyColor ("Chroma Key Color", Color) = (0,1,0,1)
        _Threshold ("Threshold", Range(0,1)) = 0.3
        _Softness ("Softness", Range(0,1)) = 0.1
        _SpillAmount ("Spill Suppression Amount", Range(0,2)) = 1.0
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
            Name "ChromaKey_HSV"

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
            float _SpillAmount;
            float _CanvasGroupAlpha;

            // Function to convert RGB to HSV
            float3 RGBtoHSV(float3 c)
            {
                float4 K = float4(0.0, -1.0/3.0, 2.0/3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

                float d = q.x - min(q.w, q.y);
                float e = 1e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

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
                fixed4 color = tex2D(_MainTex, uv);

                // Convert pixel color and chroma key color to HSV
                float3 cHSV = RGBtoHSV(color.rgb);
                float3 keyHSV = RGBtoHSV(_ChromaKeyColor.rgb);

                // Calculate hue difference (accounting for wrap-around)
                float hueDiff = abs(cHSV.x - keyHSV.x);
                hueDiff = min(hueDiff, 1.0 - hueDiff); // Wrap-around

                // Compute alpha using smoothstep for a smooth transition
                float alpha = smoothstep(_Threshold, _Threshold + _Softness, hueDiff);

                // Enhanced Spill Suppression
                if (alpha < 1.0)
                {
                    // Calculate the amount of green spill
                    float spill = color.g - max(color.r, color.b);
                    spill = max(spill, 0.0);

                    // Only proceed if there is actual spill
                    if (spill > 0.0)
                    {
                        // Reduce spill based on alpha and spill amount
                        color.r += spill * _SpillAmount * (1.0 - alpha);
                        color.b += spill * _SpillAmount * (1.0 - alpha);
                        color.g -= spill * _SpillAmount * (1.0 - alpha) * 2.0; // Multiply by 2.0 to be more aggressive
                    }
                }

                // Apply the computed alpha to the color
                color.a *= alpha * _CanvasGroupAlpha;

                return color;
            }
            ENDCG
        }
    }
}
