Shader "Ash/Settlement Mobile"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Weathering ("Surface variation", Range(0,0.5)) = 0.16
        _GroundTex ("Outpost earth", 2D) = "white" {}
        _GroundTextureMix ("Use ground texture", Range(0,1)) = 0
        _Road ("Worn asphalt", Range(0,1)) = 0
        _Ground ("Earth surface", Range(0,1)) = 0
        _Specular ("Worn metal highlight", Range(0,0.3)) = 0.035
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
            fixed4 _Color;
            sampler2D _GroundTex;
            half _Weathering, _Ground, _Specular, _GroundTextureMix, _Road;
            struct appdata { float4 vertex:POSITION; float3 normal:NORMAL; };
            struct v2f { float4 pos:SV_POSITION; float3 world:TEXCOORD0; half3 normal:TEXCOORD1; SHADOW_COORDS(2) UNITY_FOG_COORDS(3) };
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p)
            {
                float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(hash(a),hash(a+float2(1,0)),f.x),lerp(hash(a+float2(0,1)),hash(a+1),f.x),f.y);
            }
            v2f vert(appdata v)
            {
                v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;
                o.normal=UnityObjectToWorldNormal(v.normal);
                TRANSFER_SHADOW(o);UNITY_TRANSFER_FOG(o,o.pos);return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                half3 n=normalize(i.normal),light=normalize(_WorldSpaceLightPos0.xyz);
                half shadow=SHADOW_ATTENUATION(i);
                half diffuse=saturate((dot(n,light)+.06h)/1.06h);
                half3 ambient=max(ShadeSH9(half4(n,1)),half3(.18,.19,.21));
                // Continuous world-space wear avoids the previous square, pixel-like grain.
                float2 surface=i.world.xz+i.world.y*float2(.73,.31);
                half broad=noise(surface*.52),grain=noise(surface*13);
                half wear=1-_Weathering*(.75h*broad+.25h*grain);
                half3 albedo=_Color.rgb*wear;
                half earth=_Ground*saturate(n.y);
                half soil=noise(i.world.xz*2.1);
                half gravel=smoothstep(.82,.94,grain);
                albedo*=lerp(half3(1,1,1),lerp(half3(.78,.74,.65),half3(1.12,1.06,.89),soil)+gravel*.1h,earth);
                if(_GroundTextureMix>.5h)albedo*=tex2D(_GroundTex,i.world.xz*.08).rgb;
                if(_Road>.5h)
                {
                    half crack=1-smoothstep(.012h,.035h,abs(noise(surface*1.8)-.5h));
                    albedo*=1-crack*.48h;albedo=lerp(albedo,half3(.56,.34,.16),smoothstep(.66h,.85h,broad)*.30h);
                }
                half3 view=normalize(_WorldSpaceCameraPos-i.world);
                half spec=pow(saturate(dot(n,normalize(light+view))),20)*_Specular*(1-earth)*shadow;
                fixed4 color=fixed4(albedo*(ambient+AshFillLight(n)+_LightColor0.rgb*diffuse*shadow)+_LightColor0.rgb*spec,_Color.a);
                color=AshBoundaryHaze(color,i.world);UNITY_APPLY_FOG(i.fogCoord,color);return color;
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
