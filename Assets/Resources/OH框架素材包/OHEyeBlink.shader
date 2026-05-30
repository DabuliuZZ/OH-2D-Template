Shader "OHTools/UI/EyeBlink"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (0, 0, 0, 1)

        // 眼睛睁开程度：1=完全睁开，0=完全闭合
        _EyeOpen ("Eye Open", Range(0, 1)) = 1

        // 椭圆水平半径（0~1，占屏幕宽度比例）
        _EyeWidth ("Eye Width", Range(0.1, 1)) = 0.35

        // 边缘柔化程度
        _EdgeSmooth ("Edge Smooth", Range(0.001, 0.1)) = 0.02

        // 径向模糊范围（边缘附近的影响区域宽度）
        _BlurRange ("Blur Range", Range(0.01, 0.5)) = 0.15

        // 径向模糊强度（0=无模糊，1=最大模糊）
        _BlurStrength ("Blur Strength", Range(0, 1)) = 0.5

        // 混合模式
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _UnityUIWorldToClip[4];

            float _EyeOpen;
            float _EyeWidth;
            float _EdgeSmooth;
            float _BlurRange;
            float _BlurStrength;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 将 UV 居中（0,0 在屏幕中心，范围 -1~1）
                float2 centered = i.uv * 2.0 - 1.0;
                float openScale = max(_EyeOpen, 0.0);

                // 椭圆半径：宽高都随 _EyeOpen 变化
                // 闭合时(0)：高度=0, 宽度=_EyeWidth
                // 全开时(1)：高度=1.5, 宽度=1.5（覆盖UV四角，√2≈1.414）
                float heightRadius = openScale * 1.5;
                float widthRadius = lerp(_EyeWidth, 1.5, smoothstep(0.3, 1.0, openScale));

                float2 ellipseRadius = float2(max(widthRadius, 0.001), max(heightRadius, 0.001));
                float2 scaled = centered / ellipseRadius;
                float ellipseDist = length(scaled);

                // 椭圆内部透明，外部显示遮罩颜色（边缘柔化）
                float eyeMask = smoothstep(1.0 - _EdgeSmooth, 1.0 + _EdgeSmooth, ellipseDist);

                // === 径向渐变模糊 ===
                // 沿从中心向外的方向多次采样，越靠近椭圆边缘模糊越强
                float edgeDist = ellipseDist - 1.0;
                float blurFactor = exp(-edgeDist * edgeDist / max(_BlurRange * _BlurRange, 0.0001)) * _BlurStrength;

                float2 radialDir = length(centered) > 0.001 ? normalize(centered) : float2(1, 0);

                float maskAccum = eyeMask;
                float weightAccum = 1.0;

                [unroll]
                for (int s = 1; s <= 6; s++)
                {
                    float t = float(s) / 6.0;
                    // 沿径向方向偏移采样，偏移量由模糊强度控制
                    float2 samplePos = centered + radialDir * t * blurFactor * 0.15;
                    float2 sampleScaled = samplePos / ellipseRadius;
                    float sampleMask = smoothstep(1.0 - _EdgeSmooth, 1.0 + _EdgeSmooth, length(sampleScaled));
                    // 高斯权重：中心权重高，边缘权重低
                    float weight = exp(-t * t * 2.0);
                    maskAccum += sampleMask * weight;
                    weightAccum += weight;
                }

                eyeMask = maskAccum / weightAccum;

                // 闭合补偿：确保 _EyeOpen=0 时完全闭合
                // 椭圆SDF在高度为0时沿水平轴仍有内部区域，需平滑过渡消除
                float closeMask = smoothstep(0.0, 0.15, _EyeOpen);
                eyeMask = lerp(1.0, eyeMask, closeMask);

                fixed4 color = i.color;
                color.a *= eyeMask;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
