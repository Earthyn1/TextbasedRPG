Shader "UI/HoverReveal"
{
    Properties
    {
        _MainTex ("Visual Texture", 2D) = "white" {}
        _HitTex ("Hitmask Texture", 2D) = "black" {}
        _HoveredId ("Hovered ID", Float) = 0
        _Reveal ("Reveal Amount", Float) = 0
        _Tint ("Tint", Color) = (1,0.9,0.6,1)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _HitTex;
            float4 _MainTex_ST;

            float _HoveredId;
            float _Reveal;
            float4 _Tint;

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
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float4 visual = tex2D(_MainTex, uv);
                float4 hit = tex2D(_HitTex, uv);

                float hitId = hit.r * 255.0;

                float mask = step(abs(hitId - _HoveredId), 0.1);

                float alpha = visual.r * mask * _Reveal;

                return float4(_Tint.rgb, alpha * _Tint.a);
            }
            ENDHLSL
        }
    }
}