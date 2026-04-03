Shader "UI/PropGlow"
{
    Properties
    {
        _MainTex      ("Sprite Texture", 2D)       = "white" {}

        // ── Glow controls (driven at runtime by PropHoverHighlight) ──
        _GlowColor    ("Glow Color",  Color)        = (1, 0.85, 0.3, 1)
        _GlowStrength ("Glow Strength", Range(0,1)) = 0
        // Radius in UV space per ring step. 0.02 ≈ 2% of texture width per ring.
        // Increase for a wider, softer bloom; decrease for a tighter edge.
        _GlowRadius   ("Glow Radius (UV)", Range(0.002, 0.08)) = 0.022
        // How quickly outer rings fall off. Higher = tighter glow.
        _GlowFalloff  ("Glow Falloff", Range(0.5, 6)) = 2.5
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
            #pragma vertex   vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;

            fixed4 _GlowColor;
            float  _GlowStrength;
            float  _GlowRadius;
            float  _GlowFalloff;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;      // Unity UI vertex color (Canvas alpha etc.)
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = TRANSFORM_TEX(v.uv, _MainTex);
                o.color  = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // ── Base sprite ───────────────────────────────────────────
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                // ── Glow mask: sample alpha in three concentric rings ─────
                // 3 rings × 8 directions = 24 taps.
                // Outer rings are weighted less (exponential falloff) so the
                // glow fades smoothly from the sprite edge outward.
                float glowMask  = 0.0;
                float totalW    = 0.0;

                // Pre-compute 8 unit-circle directions
                // (angles: 0, 45, 90, 135, 180, 225, 270, 315 degrees)
                const int   DIRS  = 8;
                const float STEP  = 6.28318530718 / DIRS;

                [unroll]
                for (int ring = 1; ring <= 3; ring++)
                {
                    float ringFrac = ring / 3.0;               // 0.33 .. 1.0
                    float w        = pow(1.0 - ringFrac, _GlowFalloff) + 0.05;
                    float r        = _GlowRadius * ringFrac;

                    [unroll]
                    for (int d = 0; d < DIRS; d++)
                    {
                        float  angle = d * STEP;
                        float2 off   = float2(cos(angle), sin(angle)) * r;
                        float  a     = tex2D(_MainTex, i.uv + off).a;
                        glowMask    += a * w;
                        totalW      += w;
                    }
                }

                glowMask = saturate(glowMask / totalW);

                // Only show glow where the original sprite is transparent —
                // this keeps the sprite crisp and puts the glow purely outside it.
                // The (1 - col.a) factor guarantees col.a + glowAlpha <= 1, no clamping needed.
                float glowAlpha = glowMask * (1.0 - col.a) * _GlowStrength * _GlowColor.a;

                // ── Composite: glow behind sprite ─────────────────────────
                // Pre-multiplied "over" blend: sprite on top of glow layer.
                // When glowAlpha == 0 this reduces to exactly (col.rgb, col.a) —
                // the sprite is pixel-perfect and completely unchanged at rest.
                float  combinedA = col.a + glowAlpha;
                fixed3 rgb = (col.rgb * col.a + _GlowColor.rgb * glowAlpha)
                             / max(combinedA, 0.0001);
                float  a   = combinedA;

                return fixed4(rgb, a);
            }
            ENDHLSL
        }
    }
}
