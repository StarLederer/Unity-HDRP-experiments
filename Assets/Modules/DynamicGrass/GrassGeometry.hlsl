// Vertex output from geometry
PackedVaryingsType VertexOutput(
    AttributesMesh source,
    float3 position, float3 prev_position, half3 normal,
    half emission = 0, half random = 0, half3 bary_coord = 0.5
)
{
    // We omit the z component of bary_coord.
    half4 color = half4(bary_coord.xy, emission, random);
    return PackVertexData(source, position, prev_position, normal, color);
}

// Geometry shader function body
[maxvertexcount(6)]
void GrassGeometry(
    uint pid : SV_PrimitiveID,
    point Attributes input[1],
    inout TriangleStream<PackedVaryingsType> outStream
)
{
    // Input vertices
    AttributesMesh v0 = ConvertToAttributesMesh(input[0]);

    float grassSize = 0.12;

    float3 p0_c0 = v0.positionOS - float3(grassSize/2, 0, 0);
    float3 p0_c1 = v0.positionOS + float3(0, grassSize, 0);
    float3 p0_c2 = v0.positionOS + float3(grassSize/2, 0, 0);

    float3 p0_c00 = v0.positionOS - float3(0, 0, grassSize/2);
    float3 p0_c11 = v0.positionOS + float3(0, grassSize, 0);
    float3 p0_c22 = v0.positionOS + float3(0, 0, grassSize/2);


    #if SHADERPASS == SHADERPASS_MOTION_VECTORS
        bool hasDeformation = unity_MotionVectorsParams.x > 0.0;
        float3 p0_p = hasDeformation ? input[0].previousPositionOS : v0.positionOS;
    #else
        float3 p0_p = v0.positionOS;
    #endif

    #ifdef ATTRIBUTES_NEED_NORMAL
        float3 n0 = v0.normalOS;
    #else
        float3 n0 = 0;
    #endif

    outStream.Append(VertexOutput(v0, p0_c0, p0_p, n0));
    outStream.Append(VertexOutput(v0, p0_c1, p0_p, n0));
    outStream.Append(VertexOutput(v0, p0_c2, p0_p, n0));
    outStream.RestartStrip();

    outStream.Append(VertexOutput(v0, p0_c00, p0_p, n0));
    outStream.Append(VertexOutput(v0, p0_c11, p0_p, n0));
    outStream.Append(VertexOutput(v0, p0_c22, p0_p, n0));
    outStream.RestartStrip();
}
