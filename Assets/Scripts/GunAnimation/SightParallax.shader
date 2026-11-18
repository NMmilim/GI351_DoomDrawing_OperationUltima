Shader "Custom/SightParallax"
{
    Properties
    {
        _MainTex ("Reticle Texture", 2D) = "white" {}
        [HDR] _Color ("Reticle Color", Color) = (2, 0.1, 0.1, 1)
        _ReticleScale ("Reticle Size", Range(0.1, 5.0)) = 1.0
        _ParallaxDepth ("Infinity Depth", Range(1.0, 50.0)) = 15.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent+1" "RenderType"="Transparent" }
        Blend SrcAlpha One // Additive glow like a real laser/LED sight
        ZWrite Off
        Cull Off

        // Only draw inside the glass stencil mask (Stencil ID = 1)
        Stencil
        {
            Ref 1
            Comp Equal
        }

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
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewDirObj : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _Color;
            float _ReticleScale;
            float _ParallaxDepth;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                // Calculate camera view direction in the sight's local object space
                float3 objCamPos = mul(unity_WorldToObject, float4(_WorldSpaceCameraPos, 1.0)).xyz;
                o.viewDirObj = v.vertex.xyz - objCamPos;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 viewDir = normalize(i.viewDirObj);

                // Offset UVs based on viewing angle to simulate infinity projection
                float2 parallaxOffset = viewDir.xy / max(abs(viewDir.z), 0.001) * _ParallaxDepth;
                float2 centeredUV = (i.uv - 0.5) * _ReticleScale + parallaxOffset + 0.5;

                // Clip anything outside the texture bounds so it doesn't repeat
                if (centeredUV.x < 0.0 || centeredUV.x > 1.0 || centeredUV.y < 0.0 || centeredUV.y > 1.0)
                    discard;

                fixed4 tex = tex2D(_MainTex, centeredUV);
                return tex * _Color;
            }
            ENDCG
        }
    }
}
