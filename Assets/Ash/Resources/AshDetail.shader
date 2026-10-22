Shader "Ash/Weathered Detail"
{
    Properties
    {
        _MainTex ("Albedo with baked occlusion", 2D) = "white" {}
        _BumpMap ("Tangent detail normal", 2D) = "bump" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Specular ("Broad surface highlight", Range(0,0.3)) = 0.08
        _NormalStrength ("Fine detail strength", Range(0,1)) = 0.8
        _Wrap ("Soft diffuse wrap", Range(0,0.3)) = 0.07
        _Rim ("Miniature edge light", Range(0,0.15)) = 0.015
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
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
            sampler2D _MainTex, _BumpMap;
            float4 _MainTex_ST;
            fixed4 _Color;
            half _Specular, _NormalStrength, _Wrap, _Rim;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; float4 tangent:TANGENT; float2 uv:TEXCOORD0; };
            struct v2f {
                float4 pos:SV_POSITION; float2 uv:TEXCOORD0;
                half3 normal:TEXCOORD1; half3 tangent:TEXCOORD2; half3 bitangent:TEXCOORD3;
                float3 world:TEXCOORD4; SHADOW_COORDS(5) UNITY_FOG_COORDS(6)
            };
            v2f vert(appdata v)
            {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=TRANSFORM_TEX(v.uv,_MainTex);
                o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.normal=UnityObjectToWorldNormal(v.normal); o.tangent=UnityObjectToWorldDir(v.tangent.xyz);
                o.bitangent=cross(o.normal,o.tangent)*(v.tangent.w*unity_WorldTransformParams.w);
                TRANSFER_SHADOW(o); UNITY_TRANSFER_FOG(o,o.pos); return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                half3 detail=UnpackNormal(tex2D(_BumpMap,i.uv));
                detail.xy*=_NormalStrength;detail=normalize(detail);
                half3 n=normalize(detail.x*i.tangent+detail.y*i.bitangent+detail.z*i.normal);
                half3 light=normalize(_WorldSpaceLightPos0.xyz);
                half diffuse=saturate((dot(n,light)+_Wrap)/(1+_Wrap)); half shadow=SHADOW_ATTENUATION(i);
                half3 ambient=max(ShadeSH9(half4(n,1)),half3(.18,.19,.21));
                half3 albedo=tex2D(_MainTex,i.uv).rgb*_Color.rgb;
                half3 view=normalize(_WorldSpaceCameraPos-i.world);
                // Baked occlusion in the albedo also restrains highlights inside cracks and cloth folds.
                half occlusion=saturate(dot(albedo,half3(.2126,.7152,.0722))*3+.12h);
                half spec=pow(saturate(dot(n,normalize(light+view))),28)*_Specular*shadow*occlusion;
                half rim=pow(1-saturate(dot(n,view)),3)*_Rim*saturate(n.y*.5h+.5h);
                fixed4 color=fixed4(albedo*(ambient+AshFillLight(n)+_LightColor0.rgb*diffuse*shadow+rim)+_LightColor0.rgb*spec,1);
                color=AshBoundaryHaze(color,i.world);UNITY_APPLY_FOG(i.fogCoord,color); return color;
            }
            ENDCG
        }
        Pass
        {
            Name "SHADOWCASTER"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual Cull Back
            CGPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma target 3.0
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            struct shadowInput { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct shadowOutput { V2F_SHADOW_CASTER; };
            shadowOutput shadowVert(shadowInput v)
            {
                shadowOutput o;TRANSFER_SHADOW_CASTER_NORMALOFFSET(o);return o;
            }
            float4 shadowFrag(shadowOutput i):SV_Target { SHADOW_CASTER_FRAGMENT(i) }
            ENDCG
        }
    }
    Fallback Off
}
