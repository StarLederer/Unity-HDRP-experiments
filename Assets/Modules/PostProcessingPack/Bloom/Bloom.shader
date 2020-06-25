Shader "Hidden/Shader/Bloom"
{
    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FXAA.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"

    struct Attributes
    {
        uint vertexID : SV_VertexID;
        UNITY_VERTEX_INPUT_INSTANCE_ID
    };

    struct Varyings
    {
        float4 positionCS : SV_POSITION;
        float2 texcoord   : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
    };

    Varyings Vert(Attributes input)
    {
        Varyings output;
        UNITY_SETUP_INSTANCE_ID(input);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
        output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
        output.texcoord = GetFullScreenTriangleTexCoord(input.vertexID);
        return output;
    }

    // List of properties to control your post process effect
    float _Curve;
    int _Steps;
    float _Radius;
    TEXTURE2D_X(_InputTexture);

	float random(float2 uv)
	{
		return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
	}

    float4 HorizontalBlur(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		float2 positionSS = input.texcoord * _ScreenSize.xy;
        float3 outColor = float3(0, 0, 0);//LOAD_TEXTURE2D_X(_InputTexture, positionSS).xyz;
        float totalWeight = 0;

		//for(int i = _Steps; i <= _Steps; i++)
		//{
        	float x = (random(positionSS) - 0.5) * 2 * _Radius;
        	x *= lerp(1, -1, step(_ScreenSize.x, positionSS.x + x));
        	x *= lerp(-1, 1, step(0, positionSS.x + x));
        	float y = ((random(positionSS + 0.1.xx) - 0.5) * 2 * _Radius);
        	y *= lerp(1, -1, step(_ScreenSize.y, + positionSS.y + y));
        	y *= lerp(-1, 1, step(0, positionSS.y + y));
			float2 scatterPos = uint2(positionSS.x + x, positionSS.y + y);

			float3 neighborColor = LOAD_TEXTURE2D_X(_InputTexture, scatterPos).xyz;

			float br = pow(saturate(Max3(neighborColor.x, neighborColor.y, neighborColor.z)), _Curve);
			//float distance = sqrt(x*x + y*y);
			//float weight = saturate(1 - distance / 1);
			float weight = 1;
	        
	        outColor += neighborColor * weight * br;
	        totalWeight += weight;
		//}
		outColor = outColor / totalWeight;

        return float4(outColor.xyz, 1);
    }

    ENDHLSL

    SubShader
    {
        Pass
        {
            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment HorizontalBlur
                #pragma vertex Vert
            ENDHLSL
        }
    }
    Fallback Off
}
