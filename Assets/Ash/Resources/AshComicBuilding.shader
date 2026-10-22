Shader "Ash/Comic Outpost"
{
    Properties
    {
        _MainTex ("Painted albedo and baked creases", 2D) = "white" {}
        _BumpMap ("Painted surface normal", 2D) = "bump" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Ink silhouette", Color) = (0.075,0.063,0.047,1)
        _OutlineWidth ("Thin ink silhouette in meters", Range(0,0.025)) = 0.012
        _NormalStrength ("Painted relief", Range(0,1)) = 0.25
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "INK"
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "AshAtmosphere.cginc"
            half _OutlineWidth;fixed4 _OutlineColor;
            struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;};
            struct v2f {float4 pos:SV_POSITION;float3 world:TEXCOORD0;UNITY_FOG_COORDS(1)};
            v2f vert(appdata v)
            {
                v2f o;float3 world=mul(unity_ObjectToWorld,v.vertex).xyz;
                world+=normalize(UnityObjectToWorldNormal(v.normal))*_OutlineWidth;
                o.world=world;o.pos=mul(UNITY_MATRIX_VP,float4(world,1));UNITY_TRANSFER_FOG(o,o.pos);return o;
            }
            fixed4 frag(v2f i):SV_Target {fixed4 color=_OutlineColor;color=AshBoundaryHaze(color,i.world);UNITY_APPLY_FOG(i.fogCoord,color);return color;}
            ENDCG
        }
        Pass
        {
            Name "PAINT"
            Tags { "LightMode"="ForwardBase" }
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "AshAtmosphere.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            sampler2D _MainTex,_BumpMap;float4 _MainTex_ST;fixed4 _Color;half _NormalStrength;
            struct appdata {float4 vertex:POSITION;float3 normal:NORMAL;float4 tangent:TANGENT;float2 uv:TEXCOORD0;};
            struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;half3 normal:TEXCOORD1;half3 tangent:TEXCOORD2;half3 bitangent:TEXCOORD3;float3 world:TEXCOORD4;SHADOW_COORDS(5) UNITY_FOG_COORDS(6)};
            v2f vert(appdata v)
            {
                v2f o;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.pos=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);
                o.normal=UnityObjectToWorldNormal(v.normal);o.tangent=UnityObjectToWorldDir(v.tangent.xyz);
                o.bitangent=cross(o.normal,o.tangent)*(v.tangent.w*unity_WorldTransformParams.w);
                TRANSFER_SHADOW(o);UNITY_TRANSFER_FOG(o,o.pos);return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                half3 detail=UnpackNormal(tex2D(_BumpMap,i.uv));detail.xy*=_NormalStrength;detail=normalize(detail);
                half3 n=normalize(i.tangent*detail.x+i.bitangent*detail.y+i.normal*detail.z);
                half lit=saturate(dot(n,normalize(_WorldSpaceLightPos0.xyz))*.8h+.2h);
                // Broad, stable paint bands. Smooth transitions avoid harsh shadow flicker when zooming.
                half paint=.20h+.28h*smoothstep(.12h,.28h,lit)+.25h*smoothstep(.55h,.72h,lit);
                half shadow=SHADOW_ATTENUATION(i);
                half3 ambient=max(ShadeSH9(half4(n,1)),half3(.26h,.25h,.22h));
                half3 albedo=tex2D(_MainTex,i.uv).rgb*_Color.rgb;
                fixed4 color=fixed4(albedo*(ambient+AshFillLight(n)+_LightColor0.rgb*paint*lerp(.45h,1.0h,shadow)),1);
                color=AshBoundaryHaze(color,i.world);UNITY_APPLY_FOG(i.fogCoord,color);return color;
            }
            ENDCG
        }
        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
    Fallback Off
}
