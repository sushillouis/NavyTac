Shader "Custom/FogOfWar_Transparent" {
    Properties {
        _FogColor ("Fog Color", Color) = (0, 0, 0, 0)  
        _FogTex ("Fog Texture", 2D) = "white" {}
    }

    SubShader {
        Tags { 
            "Queue"="Transparent" 
            "RenderType"="Transparent" 
        }
        ZWrite Off  // Disable depth writing
        Blend SrcAlpha OneMinusSrcAlpha  // Standard alpha blending
        LOD 200

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _FogTex;
            fixed4 _FogColor;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // Get fog texture alpha (0 = revealed, 1 = fogged)
                fixed fogAlpha = tex2D(_FogTex, i.uv).a;
    
                fixed4 result = fixed4(_FogColor.rgb, _FogColor.a * fogAlpha);
                return result;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}