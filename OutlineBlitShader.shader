Shader "Hidden/OutlineBlitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _CameraColorTex ("Camera Color Tex", 2D) = "white" {}
        _BlitStrength ("BlitStrength", Range(1,10)) = 1.0
    }
    SubShader
    {
        // No culling or depth
        Blend One One
        ZWrite Off
        ZTest Off

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
            sampler2D _CameraColorTex;
            float _BlitStrength;

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col1 = tex2D(_MainTex, i.uv);
                fixed4 col2 = tex2D(_CameraColorTex, i.uv);
                
                float grey = (col1.r + col1.g + col1.b) / 3.0 * _BlitStrength;

                fixed4 col = lerp(col2, col1, grey);

                return col;
            }
            ENDCG
        }
    }
}
