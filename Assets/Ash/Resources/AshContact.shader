Shader "Ash/Contact Grounding"
{
    Properties { _Color ("Ground shade", Color) = (.08,.07,.05,.32) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Offset -1,-1
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
            v2f vert(appdata_base v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.texcoord.xy; return o; }
            fixed4 frag(v2f i):SV_Target { float2 p=(i.uv-.5)*2; return fixed4(_Color.rgb,_Color.a*(1-smoothstep(.2,1,dot(p,p)))); }
            ENDCG
        }
    }
    Fallback Off
}
