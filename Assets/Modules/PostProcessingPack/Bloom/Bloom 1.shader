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
    int _Quality;
    float _Radius;
    TEXTURE2D_X(_InputTexture);

    float4 HorizontalBlur(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		uint2 positionSS = input.texcoord * _ScreenSize.xy;
        float3 outColor = LOAD_TEXTURE2D_X(_InputTexture, positionSS).xyz;

        float steps = _Radius / _Quality;
        float step = _Radius / steps;

        float totalWeight = 1;
		for(int x = -steps; x <= steps; x++)
		{
			for(int y = -steps; y <= steps; y++)
			{
				float posX = x * step;
				float posY = y * step;
				uint2 positionSSleft = uint2(positionSS.x + posX, positionSS.y + posY);

				float3 neighborColor = LOAD_TEXTURE2D_X(_InputTexture, positionSSleft).xyz;

				float lum = pow(saturate(Luminance(neighborColor)), _Curve);
				//float lum = pow(saturate(Max3(neighborColor.x, neighborColor.y, neighborColor.z)), _Curve);
				float distance = sqrt(x*x + y*y);
				//float weight = exp(-0.5 * (x*x + y*y) / (8));
				float weight = saturate(1 - distance / steps);
		        
		        outColor += neighborColor * weight * lum;
		        totalWeight += weight;
			}
		}
		outColor = LOAD_TEXTURE2D_X(_InputTexture, positionSS).xyz + outColor / totalWeight;

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
