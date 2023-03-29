Shader "Hidden/OutlineBlurShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurWidth("Blur Width", Int) = 5
        _TextureWidth("Texture Width", Int) = 1024
        _TextureHeight("Texture Height", Int) = 768
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "OutlineBlur.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
            float _BlurWidth;
            float _TextureWidth;
            float _TextureHeight;

            fixed4 frag (v2f i) : SV_Target
            {
                half3 rgb = half3(0.0, 0.0, 0.0);
                half alpha = 0.0;

                GaussianBlur_float(_MainTex, i.uv, _BlurWidth, _TextureWidth, _TextureHeight, rgb, alpha);

                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
}
