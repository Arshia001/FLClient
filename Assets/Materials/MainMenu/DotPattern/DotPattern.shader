Shader "UI/DotPattern"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_RadiusTex("Radius Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

		_BoxSize("Box Size", Float) = 0.1
		_Aspect("Aspect", Float) = 1
		_MaxRadius("Maximum Radius", Float) = 0.05

		_AnimTimeSeed("Animation Time Seed", Float) = 0
		_AnimIntensity("Animation Intensity", Float) = 0.1
		_AnimInterval("Animation Interval", Float) = 2
		_AnimDuration("Animation Duration", Float) = 1
		_AnimSpread("Animation Spread", Float) = 0.1

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
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
		{
			Name "Default"
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0

			#include "UnityCG.cginc"
			#include "UnityUI.cginc"

			#pragma multi_compile_local _ UNITY_UI_CLIP_RECT
			#pragma multi_compile_local _ UNITY_UI_ALPHACLIP

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord  : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
				UNITY_VERTEX_OUTPUT_STEREO
			};

			sampler2D _MainTex;
			sampler2D _RadiusTex;
			fixed4 _Color;
			fixed4 _TextureSampleAdd;
			float4 _ClipRect;
			float4 _MainTex_ST;
			float4 _RadiusTex_ST;

			fixed _BoxSize;
			fixed _Aspect;
			fixed _MaxRadius;

			fixed _AnimTimeSeed;
			fixed _AnimIntensity;
			fixed _AnimInterval;
			fixed _AnimDuration;
			fixed _AnimSpread;

			v2f vert(appdata_t v)
			{
				v2f OUT;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
				OUT.worldPosition = v.vertex;
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

				OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

				OUT.color = v.color * _Color;
				return OUT;
			}

			fixed2 closestBoxCenter(float2 uv)
			{
				fixed _BoxSizeY = _BoxSize * _Aspect;
				return fixed2(uv.x - uv.x % _BoxSize + _BoxSize / 2, uv.y - uv.y % _BoxSizeY + _BoxSizeY / 2);
			}

			fixed smoothStepTwoWay(fixed center, fixed spread, fixed x)
			{
				return smoothstep(spread, 0, abs(center - x));
			}

			fixed animate(fixed radius)
			{
				if (radius <= 0)
					return 0;
				fixed time = (_Time.y + _AnimTimeSeed) % _AnimInterval - (_AnimInterval - _AnimDuration) / 2;
				fixed center = 1 - (time / _AnimDuration);
				fixed anim = smoothStepTwoWay(center * _MaxRadius, _AnimSpread, radius) * _AnimIntensity;
				return radius + (anim * sqrt(radius) / sqrt(_MaxRadius));
			}

			fixed circleRadius(float2 center)
			{
				half intensity = tex2D(_RadiusTex, center).x;
				return animate(intensity * _MaxRadius);
			}

			fixed dist(fixed2 center, float2 uv)
			{
				uv.y = uv.y / _Aspect;
				center.y = center.y / _Aspect;
				return distance(center, uv);
			}

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

				fixed2 center = closestBoxCenter(IN.texcoord);
				fixed radius = circleRadius(center);
				color.a = color.a * (1 - smoothstep(0.8, 1, dist(center, IN.texcoord) / radius));

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
        ENDCG
        }
    }
}
