#include "Uniforms.hlsl"
#include "Samplers.hlsl"
#include "Transform.hlsl"
#include "ScreenPos.hlsl"

void VS(float4 iPos : POSITION,
    out float2 oScreenPos : TEXCOORD0,
    out float4 oPos : OUTPOSITION)
{
    float4x3 modelMatrix = iModelMatrix;
    float3 worldPos = GetWorldPos(modelMatrix);
    oPos = GetClipPos(worldPos);
    oScreenPos = GetScreenPosPreDiv(oPos);
}

void PS(float2 iScreenPos : TEXCOORD0,
    out float4 oColor : OUTCOLOR0)
{
	float y = Sample2D(DiffMap, iScreenPos);
	float v = Sample2D(NormalMap, iScreenPos); // TODO: may need to div v and u by 2
	float u = Sample2D(SpecMap, iScreenPos);

	y = 1.1643*(y - 0.0625);
	u = u - 0.5;
	v = v - 0.5;

	float r = y + 1.5958*v;
	float g = y - 0.39173*u - 0.81290*v;
	float b = y + 2.017*u;

	// TODO: switch to normalized color space
	oColor.b = clamp(y + 2.017*u, 0, 1);
	oColor.g = clamp(y - 0.39173*u - 0.81290*v, 0, 1);
	oColor.r = clamp(y + 1.5958*v, 0, 1);
	oColor.a = 1;
}
