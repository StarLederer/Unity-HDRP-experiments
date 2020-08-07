Shader "Hidden/Shader/IrisScattering"
{
    HLSLINCLUDE

    #pragma target 4.5
    #pragma only_renderers d3d11 ps4 xboxone vulkan metal switch

    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/UberPostFeatures.cs.hlsl"
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/BloomCommon.hlsl"
    //#include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/FXAA.hlsl"
    //#include "Packages/com.unity.render-pipelines.high-definition/Runtime/PostProcessing/Shaders/RTUpscale.hlsl"

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
    TEXTURE2D_X(_BloomTexture);
    TEXTURE2D_X(_InputTexture);

    CBUFFER_START(cb0)
        //float4 _ChromaParams;
        //float4 _VignetteParams1;
        //float4 _VignetteParams2;
        //float4 _VignetteColor;
        //float4 _DistortionParams1;
        //float4 _DistortionParams2;
        //float4 _LogLut3D_Params;        // x: 1 / lut_size, y: lut_size - 1, z: postexposure, w: enabled
        float4 _BloomParams;
        float4 _BloomThreshold;
        float4 _BloomTint;
        float4 _BloomDirtScaleOffset;
        float4 _BloomBicubicParams;
        //float4 _DebugFlags;
    CBUFFER_END

    #define BloomTint               _BloomTint.xyz
    #define BloomIntensity          _BloomParams.x
    #define DirtIntensity           _BloomParams.y
    #define BloomEnabled            _BloomParams.z
    #define DirtEnabled             _BloomParams.w
    #define DirtScale               _BloomDirtScaleOffset.xy
    #define DirtOffset              _BloomDirtScaleOffset.zw

    SAMPLER(sampler_LinearClamp);

    float4 CustomPostProcess(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

        // Default code
        float2 uv = input.texcoord * _ScreenSize.xy;
        float3 color = LOAD_TEXTURE2D_X(_InputTexture, uv).xyz;

        // Bloom
        #if 0 // Bilinear
            float3 bloom = SAMPLE_TEXTURE2D_X_LOD(_BloomTexture, sampler_LinearClamp, ClampAndScaleUVForBilinear(uv), 0.0).xyz;
        #else
            float3 bloom = LOAD_TEXTURE2D_X(_BloomTexture, uv / _RTHandleScale.xy).xyz;
            // float3 bloom = SampleTexture2DBicubic(
            //         TEXTURE2D_X_ARGS(_BloomTexture, sampler_LinearClamp), uv * _RTHandleScale.xy,
            //         _BloomBicubicParams,
            //         _RTHandleScale.xy,
            //         unity_StereoEyeIndex).xyz;
        #endif

        //float3 thresholdedColor = QuadraticThreshold(color, 0, float3(0, 0, 0));
        //color = lerp(color, (color - thresholdedColor) + (bloom * BloomTint), BloomIntensity); // original
        color = lerp(color, bloom, _Intensity);

        // UNITY_BRANCH
        // if (DirtEnabled)
        // {
        //     // UVs for the dirt texture should be DistortUV(uv * DirtScale + DirtOffset) but
        //     // considering we use a cover-style scale on the dirt texture the difference isn't massive
        //     // so we chose to save a few ALUs here instead in case lens distortion is active
        //     float3 dirt = SAMPLE_TEXTURE2D_LOD(_BloomDirtTexture, sampler_LinearClamp, uvDistorted * DirtScale + DirtOffset, 0.0).xyz;
        //     color += bloom * dirt * DirtIntensity;
        // }

        return float4(color, 1);
    }

    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "IrisScattering"

            ZWrite Off
            ZTest Always
            Blend Off
            Cull Off

            HLSLPROGRAM
                #pragma fragment CustomPostProcess
                #pragma vertex Vert
            ENDHLSL
        }
    }
    Fallback Off
}
