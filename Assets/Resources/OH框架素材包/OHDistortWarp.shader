Shader "OHEffect/OHDistortWarp"
{
    Properties
    {
        _MainTex       ("主贴图",      2D)   = "white" {}
        _DistortStrength ("扭曲强度",  Range(0, 0.1)) = 0.03
        _DistortSpeed   ("扭曲速度",   Range(0, 10))  = 3.0
        _DistortFrequency ("扭曲频率", Range(1, 50))  = 15.0
    }
    SubShader
    {
        // 后处理：在所有内容之上渲染
        Tags { "RenderType"="Opaque" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _DistortStrength;
            float _DistortSpeed;
            float _DistortFrequency;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y * _DistortSpeed;

                // 用 sin 波对 UV 进行双向扭曲，模拟反胃效果
                float2 distortedUV = i.uv;
                distortedUV.x += sin(i.uv.y * _DistortFrequency + t) * _DistortStrength;
                distortedUV.y += cos(i.uv.x * _DistortFrequency + t * 0.8) * _DistortStrength;

                return tex2D(_MainTex, distortedUV);
            }
            ENDCG
        }
    }
    Fallback Off
}