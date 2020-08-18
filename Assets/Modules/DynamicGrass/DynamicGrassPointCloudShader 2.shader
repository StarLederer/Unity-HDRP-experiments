Shader "Custom/DynamicGrassPointCloudShaderLit"
{
    Properties
    {
		[Header(Shading)]
        _TopColor("Top Color", Color) = (1, 1, 1, 1)
		_BottomColor("Bottom Color", Color) = (1, 1, 1, 1)
		_TranslucentGain("Translucent Gain", Range(0, 1)) = 0.5
    }

	CGINCLUDE
	#include "UnityCG.cginc"
	#include "Autolight.cginc"

	// Simple noise function, sourced from http://answers.unity.com/answers/624136/view.html
	// Returns a number in the 0...1 range.
	float rand(float3 co)
	{
		return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 53.539))) * 43758.5453);
	}

	// Construct a rotation matrix that rotates around the provided axis, sourced from:
	// https://gist.github.com/keijiro/ee439d5e7388f3aafc5296005c8c3f33
	float3x3 AngleAxis3x3(float angle, float3 axis)
	{
		float c, s;
		sincos(angle, s, c);

		float t = 1 - c;
		float x = axis.x;
		float y = axis.y;
		float z = axis.z;

		return float3x3(
			t * x * x + c, t * x * y - s * z, t * x * z + s * y,
			t * x * y + s * z, t * y * y + c, t * y * z - s * x,
			t * x * z - s * y, t * y * z + s * x, t * z * z + c
			);
	}

	float4 vert(float4 vertex : POSITION) : SV_POSITION
	{
		return UnityObjectToClipPos(vertex);
	}

	//
    //
    // Data structures
	struct v2g
	{
		float4 pos : SV_POSITION;
		float3 norm : NORMAL;
		float2 uv : TEXCOORD0;

	};

	struct g2f
	{
		float4 pos : SV_POSITION;
		float3 norm : NORMAL;
		float2 uv : TEXCOORD0;
	};

	[maxvertexcount(8)]
	void geom(point v2g IN[1], inout TriangleStream<g2f> triStream)
	{
	}
	ENDCG

    SubShader
    {
		Cull Off
		Tags
		{
			"RenderPipeline" = "HDRenderPipeline"
			"RenderType" = "HDLitShader"
		}

        Pass
        {
			Tags
			{
				"RenderType" = "Opaque"
				"LightMode" = "GBuffer"
			}

            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

			#pragma target 4.6
            
			#include "Lighting.cginc"

			float4 _TopColor;
			float4 _BottomColor;
			float _TranslucentGain;

			float4 frag (float4 vertex : SV_POSITION, fixed facing : VFACE) : SV_Target
            {	
				return float4(1, 1, 1, 1);
            }
            ENDCG
        }
    }
}