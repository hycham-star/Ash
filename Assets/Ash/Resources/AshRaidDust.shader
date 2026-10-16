Shader "Ash/Raid Dust"
{
    Properties { _Color ("Dust color", Color) = (.68,.57,.39,.92) }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct Output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; };
            Output vert(Input v) { Output o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; return o; }
            fixed4 frag(Output i):SV_Target
            {
                float2 p=(i.uv-.5)*2;
                float alpha=saturate(1-dot(p,p));
                return fixed4(_Color.rgb,_Color.a*alpha*alpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
