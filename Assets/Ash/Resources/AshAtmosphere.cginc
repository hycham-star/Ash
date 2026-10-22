#ifndef ASH_ATMOSPHERE_INCLUDED
#define ASH_ATMOSPHERE_INCLUDED
// Enabled only while the settlement camera renders; never obscures the playable interior.
float4 _AshBoundaryFog;
fixed4 _AshFogColor;
half3 AshFillLight(half3 normal)
{
    return saturate(dot(normal,normalize(half3(-.5h,.35h,.6h)))) * half3(.06h,.075h,.09h) * _AshBoundaryFog.w;
}
fixed4 AshBoundaryHaze(fixed4 color,float3 world)
{
    float outside=max(abs(world.x)-_AshBoundaryFog.x,abs(world.z)-_AshBoundaryFog.y)-.8;
    float haze=smoothstep(0,max(1,_AshBoundaryFog.z),outside)*_AshBoundaryFog.w;
    color.rgb=lerp(color.rgb,_AshFogColor.rgb,haze);
    return color;
}
#endif
