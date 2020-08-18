Shader "Custom/DynamicGrassPointCloudLitBAK"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _GrassWidth("Grass width", Float) = 0.25
        _GrassHeight("Grass height", Float) = 0.25
        _AlphaCutoff("Alpha cutoff", Float) = 0.5
    }
    SubShader
    {
        Tags
		{
			"RenderType" = "Opaque"
			"Queue" = "Transparent"
		}

        //CULL OFF

        Pass
        {
        	CGPROGRAM
        
	        #include "UnityCG.cginc"

	        #pragma vertex vert
	        #pragma fragment frag
	        #pragma geometry geom

	        #pragma target 4.0

	        sampler2D _MainTex;

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

	        //
	        //
	        // Parameters
	        half _Glossiness;
	        half _Metallic;
	        fixed4 _Color;
	        half _GrassHeight;
	        half _GrassWidth;
	        half _AlphaCutoff;

	        //
	        //
	        // Vertex shader
	        v2g vert(appdata_full v)
	        {
	        	float3 v0 = v.vertex.xyz;

	        	v2g OUT;
	        	OUT.pos = v.vertex;
	        	OUT.norm = v.normal;
	        	OUT.uv = v.texcoord;
	        	return OUT;
	        }

	        //
	        //
	        // Geometry shader
	        [maxvertexcount(8)]
	        void geom(point v2g IN[1], inout TriangleStream<g2f> triStream)
	        {
	        	float3 v0 = IN[0].pos.xyz;
	        	float3 v1 = IN[0].pos.xyz + IN[0].norm * _GrassHeight;

	        	// adding vertecies to form a quad
	        	g2f OUT;
	        	
	        	float3 perpendicularAngle = float3(1, 0, 0);
	        	float3 faceNormal;

				// one side
				faceNormal = cross(perpendicularAngle, IN[0].norm);

	        	OUT.pos = UnityObjectToClipPos(v0 + perpendicularAngle * 0.5 * _GrassWidth);
	        	OUT.norm  = faceNormal;
	        	OUT.uv = float2(1, 0);
	        	triStream.Append(OUT);

	        	OUT.pos = UnityObjectToClipPos(v0 - perpendicularAngle * 0.5 * _GrassWidth);
	        	OUT.norm  = faceNormal;
	        	OUT.uv = float2(0, 0);
	        	triStream.Append(OUT);

	        	OUT.pos = UnityObjectToClipPos(v1 + perpendicularAngle * 0.5 * _GrassWidth);
	        	OUT.norm = faceNormal;
	        	OUT.uv = float2(1, 1);
	        	triStream.Append(OUT);

	        	OUT.pos = UnityObjectToClipPos(v1 - perpendicularAngle * 0.5 * _GrassWidth);
	        	OUT.norm = faceNormal;
	        	OUT.uv = float2(0, 1);
	        	triStream.Append(OUT);

	        	// otehr side
	        	faceNormal = cross(float3(0, 0, 0), faceNormal);

	        	OUT.pos = UnityObjectToClipPos(v0 + perpendicularAngle * 0.5 * _GrassWidth);
	        	OUT.norm  = faceNormal;
	        	OUT.uv = float2(1, 0);
	        	triStream.Append(OUT);

	        	OUT.pos = UnityObjectToClipPos(v0 - perpendicularAngle * 0.5 * _GrassWidth);
	        	OUT.norm  = faceNormal;
	        	OUT.uv = float2(0, 0);
	        	triStream.Append(OUT);

	        	OUT.pos = UnityObjectToClipPos(v1 + perpendicularAngle * 0.5 * _GrassWidth);
	        	OUT.norm = faceNormal;
	        	OUT.uv = float2(1, 1);
	        	triStream.Append(OUT);

	        	OUT.pos = UnityObjectToClipPos(v1 - perpendicularAngle * 0.5 * _GrassWidth);
	        	OUT.norm = faceNormal;
	        	OUT.uv = float2(0, 1);
	        	triStream.Append(OUT);
	        }

			//
	        //
	        // Fragment shader
	        float4 frag (g2f IN) : COLOR
	        {
	        	fixed4 diffuse = tex2D(_MainTex, IN.uv) * _Color;
	        	clip(diffuse.a - _AlphaCutoff);

				// Direct lighting
				float3 lightColor = float3(1, 1, 1);
				float3 lightDirection = _WorldSpaceLightPos0.xyz;

				float3 normal = IN.norm;

				float3 lighting = max(0, dot(lightDirection, normal));

				float3 light = 0;
				light += lightColor * lighting; // sun

				float4 result = float4(light, 1) * diffuse;
	            return result;
	        }

	        ENDCG
        }
    }
}
