Shader "Ash/Settlement Mobile"
{
    Properties { _Color ("Color", Color) = (1,1,1,1) _Weathering ("Surface variation", Range(0,0.3)) = 0.08 }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            fixed4 _Color;
            half _Weathering;
            struct Input { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct Output { float4 position : SV_POSITION; half3 light : TEXCOORD0; UNITY_FOG_COORDS(1) float3 world : TEXCOORD2; };
            Output vert(Input v)
            {
                Output o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld,v.vertex).xyz;
                half3 normal = UnityObjectToWorldNormal(v.normal);
                half diffuse = saturate((dot(normal, normalize(_WorldSpaceLightPos0.xyz))+.12h)/1.12h);
                o.light = max(UNITY_LIGHTMODEL_AMBIENT.rgb, half3(.28,.28,.28)) + _LightColor0.rgb * diffuse;
                UNITY_TRANSFER_FOG(o, o.position);
                return o;
            }
            fixed4 frag(Output i) : SV_Target
            {
                float3 cell=floor(i.world*6);
                half grain=frac(sin(dot(cell,float3(12.9898,39.346,78.233)))*43758.5453);
                half broad=sin(i.world.x*.63+sin(i.world.z*.47))*.5h+.5h;
                half surface=1-_Weathering*(.65h*grain+.35h*broad);
                fixed4 color = fixed4(_Color.rgb * i.light * surface, _Color.a);
                UNITY_APPLY_FOG(i.fogCoord, color);
                return color;
            }
            ENDCG
        }
    }
    Fallback Off
}
