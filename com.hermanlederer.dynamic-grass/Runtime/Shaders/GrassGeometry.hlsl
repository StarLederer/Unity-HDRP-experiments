//#include "SimplexNoise2D.hlsl"

//
//
// Parameters
float _GrassSize;
float4 _LodCenter; // x y z lod center, w discarded
float4 _LodCascades; // x lod0, y lod1, z lod2, w discarded
float4 _WindParams; // x xz strength, y y strength, z speed, w scale

//
//
// Defines
//#define USE_CORRECT_GRASS_NORMALS // Grass looks better with normals just facing up. If you enable this don't forget to change normal mode in material settings

#define Lod0Radius       _LodCascades.x
#define Lod1Radius       _LodCascades.y
#define Lod2Radius       _LodCascades.z

#define WindStrengthXZ  _WindParams.x
#define WindStrengthY   _WindParams.y
#define WindSpeed       _WindParams.z
#define WindScale       _WindParams.w

//
//
// Random
float random(float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
}

//
//
// Wind helper function
float GetWind(float2 uv)
{
    //return snoise(uv); // live simplex noise
    return sin(uv + _TimeParameters.x);
}

//
//
// Vertex output from geometry
PackedVaryingsType VertexOutput(
    AttributesMesh source,
    float3 position, float3 prev_position, half3 normal, float2 uv0 = 0,
    half emission = 0, half rnd = 0, half3 bary_coord = 0.5)
{
    // We omit the z component of bary_coord
    half4 color = half4(bary_coord.xy, emission, rnd);

    // Wind animation
    float3 positionWithWind = position;
    positionWithWind += float3(uv0.y, uv0.y, uv0.y) * float3(WindStrengthXZ, WindStrengthY, WindStrengthXZ) * GetWind(position * WindScale + _TimeParameters.xx * WindSpeed);

    return PackVertexData(source, positionWithWind, prev_position, normal, uv0, color);
}

//
//
// Generate quad helper function
void GenerateGrassQuad(AttributesMesh inputVertex,
    float3 grassVPosBL,
    float3 grassVPosTL,
    float3 grassVPosTR,
    float3 grassVPosBR,
    float3 previousVPos,
    float3 faceNormal,
    inout TriangleStream<PackedVaryingsType> outStream)
{
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
// LOD 0
void Lod0Geometry(
    AttributesMesh inputVertex,
    float3 currentVPos,
    float3 currentVPosUp,
    float3 previousVPos,
    float3 normal,
    float scale,
    inout TriangleStream<PackedVaryingsType> outStream)
{
    float3 grassVPosBL, grassVPosTL, grassVPosTR, grassVPosBR; // B ottom L eft R ight T op
    float3 faceNormal = normal;
    float3 grassDirection;

    // Plane 1
    grassDirection = float3(1, 0, 0);
    grassDirection = cross(grassDirection, normal);

    grassVPosBL = currentVPos - grassDirection * scale;
    grassVPosTL = currentVPosUp - grassDirection * scale;
    grassVPosTR = currentVPosUp + grassDirection * scale;
    grassVPosBR = currentVPos + grassDirection * scale;
    #ifdef USE_CORRECT_GRASS_NORMALS
        faceNormal = cross(normal, grassDirection);
    #endif
    GenerateGrassQuad(inputVertex, grassVPosBL, grassVPosTL, grassVPosTR, grassVPosBR, previousVPos, faceNormal, outStream);

    // Plane 2
    grassDirection = float3(0.7071067811865475, 0, 0.7071067811865475);
    grassDirection = cross(grassDirection, normal);

    grassVPosBL = currentVPos - grassDirection * scale;
    grassVPosTL = currentVPosUp - grassDirection * scale;
    grassVPosTR = currentVPosUp + grassDirection * scale;
    grassVPosBR = currentVPos + grassDirection * scale;
    #ifdef USE_CORRECT_GRASS_NORMALS
        faceNormal = cross(normal, grassDirection);
    #endif
    GenerateGrassQuad(inputVertex, grassVPosBL, grassVPosTL, grassVPosTR, grassVPosBR, previousVPos, faceNormal, outStream);

    // Plane 3
    grassDirection = float3(-0.7071067811865475, 0, 0.7071067811865475);
    grassDirection = cross(grassDirection, normal);

    grassVPosBL = currentVPos - grassDirection * scale;
    grassVPosTL = currentVPosUp - grassDirection * scale;
    grassVPosTR = currentVPosUp + grassDirection * scale;
    grassVPosBR = currentVPos + grassDirection * scale;
    #ifdef USE_CORRECT_GRASS_NORMALS
        faceNormal = cross(normal, grassDirection);
    #endif
    GenerateGrassQuad(inputVertex, grassVPosBL, grassVPosTL, grassVPosTR, grassVPosBR, previousVPos, faceNormal, outStream);
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
    inout TriangleStream<PackedVaryingsType> outStream)
{
    float3 grassVPosBL, grassVPosTL, grassVPosTR, grassVPosBR; // B ottom L eft R ight T op
    float3 faceNormal = normal;
    float3 grassDirection;

    // X plane
    grassDirection = float3(1, 0, 0);
    grassDirection = cross(grassDirection, normal);

    grassVPosBL = currentVPos - grassDirection * scale;
    grassVPosTL = currentVPosUp - grassDirection * scale;
    grassVPosTR = currentVPosUp + grassDirection * scale;
    grassVPosBR = currentVPos + grassDirection * scale;
    #ifdef USE_CORRECT_GRASS_NORMALS
        faceNormal = cross(normal, grassDirection);
    #endif
    GenerateGrassQuad(inputVertex, grassVPosBL, grassVPosTL, grassVPosTR, grassVPosBR, previousVPos, faceNormal, outStream);

    // Z plane
    grassDirection = float3(0, 0, 1);
    grassDirection = cross(grassDirection, normal);

    grassVPosBL = currentVPos - grassDirection * scale;
    grassVPosTL = currentVPosUp - grassDirection * scale;
    grassVPosTR = currentVPosUp + grassDirection * scale;
    grassVPosBR = currentVPos + grassDirection * scale;
    #ifdef USE_CORRECT_GRASS_NORMALS
        faceNormal = cross(normal, grassDirection);
    #endif
    GenerateGrassQuad(inputVertex, grassVPosBL, grassVPosTL, grassVPosTR, grassVPosBR, previousVPos, faceNormal, outStream);
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
    inout TriangleStream<PackedVaryingsType> outStream)
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
    GenerateGrassQuad(inputVertex, grassVPosBL, grassVPosTL, grassVPosTR, grassVPosBR, previousVPos, faceNormal, outStream);
}

//
//
// Main
[maxvertexcount(18)]
void GrassGeometry(
    uint pid : SV_PrimitiveID,
    point Attributes input[1],
    inout TriangleStream<PackedVaryingsType> outStream)
{
    // Input data
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

    // Grass scale calculation with distance fading
    float3 currentVPos = inputVertex.positionOS;
    float posDiffX = currentVPos.x - _LodCenter.x;
    float posDiffY = currentVPos.y - _LodCenter.y;
    float posDiffZ = currentVPos.z - _LodCenter.z;
    float distance = sqrt(posDiffX*posDiffX + posDiffY*posDiffY + posDiffZ*posDiffZ);
    distance += random(currentVPos) * 8 - 4;


    float distanceFade = distance / Lod2Radius;
    currentVPos -= normal * _GrassSize * pow(distanceFade, 2);

    // Grass growth direction
    //float3 currentVPosUp = currentVPos + normal;                                  // surface normal
    float3 currentVPosUp = currentVPos + (normal + float3(0, _GrassSize, 0)) / 2;        // average
    //float3 currentVPosUp = currentVPos + float3(0, _GrassSize, 0);                     // up

    // Generating geometry using LODs
    UNITY_BRANCH
    if (distance < Lod0Radius) Lod0Geometry(inputVertex, currentVPos, currentVPosUp, previousVPos, normal, _GrassSize, outStream);
    else if (distance < Lod1Radius) Lod1Geometry(inputVertex, currentVPos, currentVPosUp, previousVPos, normal, _GrassSize, outStream);
    else if (distance < Lod2Radius) Lod2Geometry(inputVertex, currentVPos, currentVPosUp, previousVPos, normal, _GrassSize, outStream);
}
