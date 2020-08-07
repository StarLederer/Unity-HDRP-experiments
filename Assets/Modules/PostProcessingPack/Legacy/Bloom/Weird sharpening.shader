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
    float _Intensity;
    float _Steps;
    TEXTURE2D_X(_InputTexture);

    float4 HorizontalBlur(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		uint2 positionSS = input.texcoord * _ScreenSize.xy;
        float3 outColor = LOAD_TEXTURE2D_X(_InputTexture, positionSS).xyz;

        float totalWeight = 1;
		for(int x = -_Steps; x <= _Steps; x++)
		{
			for(int y = -_Steps; y <= _Steps; y++)
			{
				//float sig = 0.1;
				//float weight = exp(-0.5 * i*i * sig*sig) * _Intensity;
				float weight = (1 - (sqrt(x*x + y*y) / _Steps));
				//float weight = 1;

		        uint2 positionSSleft = uint2(positionSS.x + x, positionSS.y + y);
		        outColor += LOAD_TEXTURE2D_X(_InputTexture, positionSSleft).xyz * weight;
		        totalWeight += weight;
			}
		}
		outColor = outColor / totalWeight;


        return float4(outColor, 1);
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
