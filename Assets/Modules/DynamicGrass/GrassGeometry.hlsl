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
[maxvertexcount(3)]
void GrassGeometry(
    uint pid : SV_PrimitiveID,
    point Attributes input[1],
    inout TriangleStream<PackedVaryingsType> outStream
)
{
    // Input vertices
    AttributesMesh v0 = ConvertToAttributesMesh(input[0]);

    float grassSize = 0.2;

    float3 p0_c = v0.positionOS;
    float3 p0_c1 = v0.positionOS + float3(0, grassSize, grassSize/2);
    float3 p0_c2 = v0.positionOS + float3(0, 0, grassSize);

    #if SHADERPASS == SHADERPASS_MOTION_VECTORS
        bool hasDeformation = unity_MotionVectorsParams.x > 0.0;
        float3 p0_p = hasDeformation ? input[0].previousPositionOS : p0_c;
    #else
        float3 p0_p = p0_c;
    #endif

    #ifdef ATTRIBUTES_NEED_NORMAL
        float3 n0 = v0.normalOS;
    #else
        float3 n0 = 0;
    #endif

    outStream.Append(VertexOutput(v0, p0_c, p0_p, n0));
    outStream.Append(VertexOutput(v0, p0_c1, p0_p, n0));
    outStream.Append(VertexOutput(v0, p0_c2, p0_p, n0));
    outStream.RestartStrip();
    return;
}
