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
void FlattenerGeometry(
    uint pid : SV_PrimitiveID,
    triangle Attributes input[3],
    inout TriangleStream<PackedVaryingsType> outStream
)
{
    // Input vertices
    AttributesMesh v0 = ConvertToAttributesMesh(input[0]);
    AttributesMesh v1 = ConvertToAttributesMesh(input[1]);
    AttributesMesh v2 = ConvertToAttributesMesh(input[2]);

    float3 p0_c = v0.positionOS;
    float3 p0_c1 = v0.positionOS + float3(0, 0.1, 0);
    float3 p0_c2 = v0.positionOS + float3(0.1, 0, 0);
    float3 p0_c3 = v0.positionOS + float3(0.1, 0.1, 0);
    float3 p1_c = v1.positionOS;
    float3 p2_c = v2.positionOS;

#if SHADERPASS == SHADERPASS_MOTION_VECTORS
    bool hasDeformation = unity_MotionVectorsParams.x > 0.0;
    float3 p0_p = hasDeformation ? input[0].previousPositionOS : p0_c;
    float3 p1_p = hasDeformation ? input[1].previousPositionOS : p1_c;
    float3 p2_p = hasDeformation ? input[2].previousPositionOS : p2_c;
#else
    float3 p0_p = p0_c;
    float3 p1_p = p1_c;
    float3 p2_p = p2_c;
#endif

#ifdef ATTRIBUTES_NEED_NORMAL
    float3 n0 = v0.normalOS;
    float3 n1 = v1.normalOS;
    float3 n2 = v2.normalOS;
#else
    float3 n0 = 0;
    float3 n1 = 0;
    float3 n2 = 0;
#endif

        outStream.Append(VertexOutput(v0, p0_c, p0_p, n0));
        outStream.Append(VertexOutput(v0, p0_c1, p0_p, n0));
        outStream.Append(VertexOutput(v0, p0_c2, p0_p, n0));
        //outStream.Append(VertexOutput(v1, p1_c, p1_p, n1));
        //outStream.Append(VertexOutput(v2, p2_c, p2_p, n2));
        outStream.RestartStrip();
        return;
}
