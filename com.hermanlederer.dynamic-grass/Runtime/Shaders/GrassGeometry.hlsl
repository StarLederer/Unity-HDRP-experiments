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

float _GrassSize;

[maxvertexcount(12)]
void GrassGeometry(
    uint pid : SV_PrimitiveID,
    point Attributes input[1],
    inout TriangleStream<PackedVaryingsType> outStream
)
{
    // Params
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
    float3 grassVPosUp = currentVPos + float3(0, 1, 0) * _GrassSize;

    float3 grassVPosBL, grassVPosTL, grassVPosTR, grassVPosBR; // B ottom L eft R ight T op
    float3 faceNormal = normal;

    // X plane
    grassVPosBL = currentVPos - float3(_GrassSize/2, 0, 0);
    grassVPosTL = grassVPosUp - float3(_GrassSize/2, 0, 0);
    grassVPosTR = grassVPosUp + float3(_GrassSize/2, 0, 0);
    grassVPosBR = currentVPos + float3(_GrassSize/2, 0, 0);
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
    grassVPosBL = currentVPos - float3(0, 0, _GrassSize/2);
    grassVPosTL = grassVPosUp - float3(0, 0, _GrassSize/2);
    grassVPosTR = grassVPosUp + float3(0, 0, _GrassSize/2);
    grassVPosBR = currentVPos + float3(0, 0, _GrassSize/2);
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
