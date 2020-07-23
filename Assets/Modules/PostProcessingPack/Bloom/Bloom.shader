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

    //
    //
    // List of properties to control your post process effect
    float _Intensity;
    TEXTURE2D_X(_InputTexture);
    TEXTURE2D_X(_BloomTexture);

	//
	//
	// Post process fragment shader
    float4 HorizontalBlur(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

		uint2 positionSS = input.texcoord * _ScreenSize.xy;
        float3 outColor;
        outColor = LOAD_TEXTURE2D_X(_InputTexture, positionSS);
        outColor += LOAD_TEXTURE2D_X(_BloomTexture, positionSS).xyz * _Intensity;

        return float4(lerp(LOAD_TEXTURE2D_X(_InputTexture, positionSS).xyz, LOAD_TEXTURE2D_X(_BloomTexture, positionSS), _Intensity).xyz, 1);   // Lerp
        //return float4(outColor.xyz, 1);                                                                                                       // Additive
        //return LOAD_TEXTURE2D_X(_BloomTexture, positionSS);                                                                                   // Only bloom
        //return LOAD_TEXTURE2D_X(_BloomTexture, positionSS) * _Intensity;                                                                      // Only bloom with intensity
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
