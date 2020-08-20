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

//
//
// Defines
//#define USE_CORRECT_GRASS_NORMALS // grass looks better with normals just facing up

//
//
// Parameters
float _GrassSize;
float3 _LodCenter; // x y z are user, w discarded

//
//
// LOD 0
void Lod0Geometry(
    AttributesMesh inputVertex,
    float3 currentVPos,
    float3 currentVPosUp,
    float3 previousVPos,
    float3 normal,
    float scale,
    inout TriangleStream<PackedVaryingsType> outStream
    )
{
    float3 grassVPosBL, grassVPosTL, grassVPosTR, grassVPosBR; // B ottom L eft R ight T op
    float3 faceNormal = normal;

    float3 grassDirection;

    // Plane 1
    grassDirection = float3(1, 0, 0);

    grassVPosBL = currentVPos - grassDirection * scale;
    grassVPosTL = currentVPosUp - grassDirection * scale;
    grassVPosTR = currentVPosUp + grassDirection * scale;
    grassVPosBR = currentVPos + grassDirection * scale;
    #ifdef USE_CORRECT_GRASS_NORMALS
        faceNormal = cross(normal, grassDirection);
    #endif

    outStream.Append(VertexOutput(inputVertex, grassVPosBL, previousVPos, faceNormal, float2(0, 0)));
    outStream.Append(VertexOutput(inputVertex, grassVPosTL, previousVPos, faceNormal, float2(0, 1)));
    outStream.Append(VertexOutput(inputVertex, grassVPosTR, previousVPos, faceNormal, float2(1, 1)));
    outStream.RestartStrip();

    outStream.Append(VertexOutput(inputVertex, grassVPosTR, previousVPos, faceNormal, float2(1, 1)));
    outStream.Append(VertexOutput(inputVertex, grassVPosBR, previousVPos, faceNormal, float2(1, 0)));
    outStream.Append(VertexOutput(inputVertex, grassVPosBL, previousVPos, faceNormal, float2(0, 0)));
    outStream.RestartStrip();

    // Plane 2
    grassDirection = float3(0.7071067811865475, 0, 0.7071067811865475);

    grassVPosBL = currentVPos - grassDirection * scale;
    grassVPosTL = currentVPosUp - grassDirection * scale;
    grassVPosTR = currentVPosUp + grassDirection * scale;
    grassVPosBR = currentVPos + grassDirection * scale;
    #ifdef USE_CORRECT_GRASS_NORMALS
        faceNormal = cross(normal, grassDirection);
    #endif

    outStream.Append(VertexOutput(inputVertex, grassVPosBL, previousVPos, faceNormal, float2(0, 0)));
    outStream.Append(VertexOutput(inputVertex, grassVPosTL, previousVPos, faceNormal, float2(0, 1)));
    outStream.Append(VertexOutput(inputVertex, grassVPosTR, previousVPos, faceNormal, float2(1, 1)));
    outStream.RestartStrip();

    outStream.Append(VertexOutput(inputVertex, grassVPosTR, previousVPos, faceNormal, float2(1, 1)));
    outStream.Append(VertexOutput(inputVertex, grassVPosBR, previousVPos, faceNormal, float2(1, 0)));
    outStream.Append(VertexOutput(inputVertex, grassVPosBL, previousVPos, faceNormal, float2(0, 0)));
    outStream.RestartStrip();

    // Plane 3
    grassDirection = float3(-0.7071067811865475, 0, 0.7071067811865475);

    grassVPosBL = currentVPos - grassDirection * scale;
    grassVPosTL = currentVPosUp - grassDirection * scale;
    grassVPosTR = currentVPosUp + grassDirection * scale;
    grassVPosBR = currentVPos + grassDirection * scale;
    #ifdef USE_CORRECT_GRASS_NORMALS
        faceNormal = cross(normal, grassDirection);
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

//
//
// LOD 1
void Lod1Geometry(
    AttributesMesh inputVertex,
    float3 currentVPos,
    float3 currentVPosUp,
    float3 previousVPos,
    float3 normal,
    float scale,
    inout TriangleStream<PackedVaryingsType> outStream
    )
{
    float3 grassVPosBL, grassVPosTL, grassVPosTR, grassVPosBR; // B ottom L eft R ight T op
    float3 faceNormal = normal;

    // X plane
    grassVPosBL = currentVPos - float3(scale, 0, 0);
    grassVPosTL = currentVPosUp - float3(scale, 0, 0);
    grassVPosTR = currentVPosUp + float3(scale, 0, 0);
    grassVPosBR = currentVPos + float3(scale, 0, 0);
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
    grassVPosBL = currentVPos - float3(0, 0, scale);
    grassVPosTL = currentVPosUp - float3(0, 0, scale);
    grassVPosTR = currentVPosUp + float3(0, 0, scale);
    grassVPosBR = currentVPos + float3(0, 0, scale);
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

//
//
// LOD 2
void Lod2Geometry(
    AttributesMesh inputVertex,
    float3 currentVPos,
    float3 currentVPosUp,
    float3 previousVPos,
    float3 normal,
    float scale,
    inout TriangleStream<PackedVaryingsType> outStream
    )
{
    float3 grassVPosBL, grassVPosTL, grassVPosTR, grassVPosBR; // B ottom L eft R ight T op
    float3 faceNormal = normal;

    float3 viewDir = mul((float3x3) _ViewMatrix, float3(1, 0, 0));
    float3 grassDirection = viewDir;
    grassVPosBL = currentVPos - grassDirection * scale;
    grassVPosTL = currentVPosUp - grassDirection * scale;
    grassVPosTR = currentVPosUp + grassDirection * scale;
    grassVPosBR = currentVPos + grassDirection * scale;
    #ifdef USE_CORRECT_GRASS_NORMALS
        faceNormal = cross(normal, grassDirection);
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

//
//
// Main
[maxvertexcount(18)]
void GrassGeometry(
    uint pid : SV_PrimitiveID,
    point Attributes input[1],
    inout TriangleStream<PackedVaryingsType> outStream
    )
{
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
        float3 normal = float(0, 1, 0);
    #endif

    // Position calculations
    float3 currentVPos = inputVertex.positionOS;
    float posDiffX = currentVPos.x - _LodCenter.x;
    float posDiffY = currentVPos.y - _LodCenter.y;
    float posDiffZ = currentVPos.z - _LodCenter.z;
    float distance = sqrt(posDiffX*posDiffX + posDiffY*posDiffY + posDiffZ*posDiffZ);

    float scale = 1 - max(8, distance) / 32;
    scale = scale * scale;
    scale = _GrassSize * scale;

    float3 currentVPosUp = currentVPos + normal * scale;

    UNITY_BRANCH
    if (distance < 8) Lod0Geometry(inputVertex, currentVPos, currentVPosUp, previousVPos, normal, scale, outStream);
    else if (distance < 16) Lod1Geometry(inputVertex, currentVPos, currentVPosUp, previousVPos, normal, scale, outStream);
    else if (distance < 32) Lod2Geometry(inputVertex, currentVPos, currentVPosUp, previousVPos, normal, scale, outStream);
}
