// Vertex output from geometry
PackedVaryingsType VertexOutput(
    AttributesMesh source,
    float3 position, float3 position_prev, half3 normal,
    half emission = 0, half random = 0, half2 qcoord = -1
)
{
    half4 color = half4(qcoord, emission, random);
    return PackVertexData(source, position, position_prev, normal, color);
}

// Geometry shader function body
[maxvertexcount(4)]
void GrassGeometry(
    uint primitiveID : SV_PrimitiveID,
    triangle Attributes input[3],
    inout TriangleStream<PackedVaryingsType> outStream
)
{
    // Input vertices
    AttributesMesh v0 = ConvertToAttributesMesh(input[0]);
    AttributesMesh v1 = ConvertToAttributesMesh(input[1]);
    AttributesMesh v2 = ConvertToAttributesMesh(input[2]);

    float3 p0 = v0.positionOS;
    float3 p1 = v1.positionOS;// + float3(0, 1, 0);
    float3 p2 = v2.positionOS;

    #if SHADERPASS == SHADERPASS_MOTION_VECTORS
        bool hasDeformation = unity_MotionVectorsParams.x > 0.0;
        float3 p0_prev = hasDeformation ? input[0].previousPositionOS : p0;
        float3 p1_prev = hasDeformation ? input[1].previousPositionOS : p1;
        float3 p2_prev = hasDeformation ? input[2].previousPositionOS : p2;
    #else
        float3 p0_prev = p0;
        float3 p1_prev = p1;
        float3 p2_prev = p2;
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

    outStream.Append(VertexOutput(v0, p0, p0_prev, n0));
    outStream.Append(VertexOutput(v1, p1, p1_prev, n1));
    outStream.Append(VertexOutput(v2, p2, p2_prev, n2));
    outStream.RestartStrip();
}