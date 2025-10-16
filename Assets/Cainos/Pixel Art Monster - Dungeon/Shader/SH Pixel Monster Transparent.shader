Shader "Cainos/Pixel Art Monster - Dungeon/Transparent"
{
	Properties
	{
		_MainTex ("Main Texture", 2D) = "white" {}
		_Color("Color" , Color) = (1.0,1.0,1.0,1.0)
	}

	SubShader
	{
		Tags
		{
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
			"PreviewType" = "Plane"
		}

		Cull Off
		ZWrite On
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 2.0
				#pragma multi_compile_fog

				#include "UnityCG.cginc"

				struct appdata_t
		{
					float4 vertex : POSITION;
					float2 texcoord : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f
				{
					float4 vertex : SV_POSITION;
					float2 texcoord : TEXCOORD0;
					UNITY_VERTEX_OUTPUT_STEREO
				};

				sampler2D _MainTex;
				float4 _MainTex_ST;

				float4 _Color;

				v2f vert (appdata_t v)
				{
					v2f o;

					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

					o.vertex = UnityObjectToClipPos(v.vertex);
					o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
					return o;
				}

				fixed4 frag (v2f i) : SV_Target
				{
					fixed4 f4Color = tex2D(_MainTex, i.texcoord);
					f4Color = f4Color * _Color;

					clip(f4Color.a - 0.01f);

					return f4Color;
				}
			ENDCG
		}
	}

}
