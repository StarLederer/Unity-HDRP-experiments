// Vertex output from geometry
PackedVaryingsType VertexOutput(
    AttributesMesh source,
    float3 position, float3 prev_position, half3 normal, float2 uv0 = 0,
    half emission = 0, half random = 0, half3 bary_coord = 0.5
)
{
    // We omit the z component of bary_coord.
    half4 color = half4(bary_coord.xy, emission, random);
    return PackVertexData(source, position, prev_position, normal, uv0, color);
}

// Geometry shader function body
[maxvertexcount(6)]
void Bak(
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

// Geometry shader function body
[maxvertexcount(12)]
void GrassGeometry(
    uint pid : SV_PrimitiveID,
    point Attributes input[1],
    inout TriangleStream<PackedVaryingsType> outStream
)
{
    // Params
    const float grassSize = 0.4;
    //#define USE_CORRECT_GRASS_NORMALS

    // Input vertex
    AttributesMesh inputVertex = ConvertToAttributesMesh(input[0]);

    #if SHADERPASS == SHADERPASS_MOTION_VECTORS
        bool hasDeformation = unity_MotionVectorsParams.x > 0.0;
        float3 previousVPos = hasDeformation ? input[0].previousPositionOS : inputVertex.positionOS;
    #else
        float3 previousVPos = inputVertex.positionOS;
    #endif

    #ifdef ATTRIBUTES_NEED_NORMAL
        float3 normal = inputVertex.normalOS;
    #else
        float3 normal = 0;
    #endif

    // Position calculations
    float3 currentVPos = inputVertex.positionOS;
    float3 grassVPosUp = currentVPos + float3(0, 1, 0) * grassSize;

    float3 grassVPosBL, grassVPosTL, grassVPosTR, grassVPosBR; // B ottom L eft R ight T op
    float3 faceNormal = normal;

    // X plane
    grassVPosBL = currentVPos - float3(grassSize/2, 0, 0);
    grassVPosTL = grassVPosUp - float3(grassSize/2, 0, 0);
    grassVPosTR = grassVPosUp + float3(grassSize/2, 0, 0);
    grassVPosBR = currentVPos + float3(grassSize/2, 0, 0);
    #ifdef USE_CORRECT_GRASS_NORMALS
        faceNormal = cross(normal, float3(1, 0, 0));
    #endif

    outStream.Append(VertexOutput(inputVertex, grassVPosBL, previousVPos, faceNormal, float2(0, 0)));
    outStream.Append(VertexOutput(inputVertex, grassVPosTL, previousVPos, faceNormal, float2(0, 1)));
    outStream.Append(VertexOutput(inputVertex, grassVPosTR, previousVPos, faceNormal, float2(1, 1)));
    outStream.RestartStrip();

    outStream.Append(VertexOutput(inputVertex, grassVPosTR, previousVPos, faceNormal, float2(1, 1)));
    outStream.Append(VertexOutput(inputVertex, grassVPosBR, previousVPos, faceNormal, float2(1, 0)));
    outStream.Append(VertexOutput(inputVertex, grassVPosBL, previousVPos, faceNormal, float2(0, 0)));
    outStream.RestartStrip();

    // Z plane
    grassVPosBL = currentVPos - float3(0, 0, grassSize/2);
    grassVPosTL = grassVPosUp - float3(0, 0, grassSize/2);
    grassVPosTR = grassVPosUp + float3(0, 0, grassSize/2);
    grassVPosBR = currentVPos + float3(0, 0, grassSize/2);
    #ifdef USE_CORRECT_GRASS_NORMALS
        faceNormal = cross(normal, float3(0, 0, 1));
    #endif

    outStream.Append(VertexOutput(inputVertex, grassVPosBL, previousVPos, faceNormal, float2(0, 0)));
    outStream.Append(VertexOutput(inputVertex, grassVPosTL, previousVPos, faceNormal, float2(0, 1)));
    outStream.Append(VertexOutput(inputVertex, grassVPosTR, previousVPos, faceNormal, float2(1, 1)));
    outStream.RestartStrip();

    outStream.Append(VertexOutput(inputVertex, grassVPosTR, previousVPos, faceNormal, float2(1, 1)));
    outStream.Append(VertexOutput(inputVertex, grassVPosBR, previousVPos, faceNormal, float2(1, 0)));
    outStream.Append(VertexOutput(inputVertex, grassVPosBL, previousVPos, faceNormal, float2(0, 0)));
    outStream.RestartStrip();
}
