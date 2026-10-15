Shader "Ash/Settlement Mobile"
{
    Properties { _Color ("Color", Color) = (1,1,1,1) }
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
            struct Input { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct Output { float4 position : SV_POSITION; half3 light : TEXCOORD0; UNITY_FOG_COORDS(1) };
            Output vert(Input v)
            {
                Output o;
                o.position = UnityObjectToClipPos(v.vertex);
                half3 normal = UnityObjectToWorldNormal(v.normal);
                half diffuse = saturate(dot(normal, normalize(_WorldSpaceLightPos0.xyz)));
                o.light = max(UNITY_LIGHTMODEL_AMBIENT.rgb, half3(.28,.28,.28)) + _LightColor0.rgb * diffuse;
                UNITY_TRANSFER_FOG(o, o.position);
                return o;
            }
            fixed4 frag(Output i) : SV_Target
            {
                fixed4 color = fixed4(_Color.rgb * i.light, _Color.a);
                UNITY_APPLY_FOG(i.fogCoord, color);
                return color;
            }
            ENDCG
        }
    }
    Fallback Off
}
