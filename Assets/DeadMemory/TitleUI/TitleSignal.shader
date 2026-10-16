Shader "DeadMemory/TitleSignal"
{
    Properties { _MainTex("Background", 2D) = "white" {} _SignalTime("Time", Float) = 0 _Glitch("Glitch", Float) = 0 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _MainTex; float _SignalTime, _Glitch;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            fixed4 frag(v2f i):SV_Target
            {
                float t=_SignalTime; float2 uv=i.uv;
                float band=step(.78,hash(float2(floor(uv.y*42),floor(t*16))));
                uv.x+=_Glitch*band*(hash(float2(floor(uv.y*42),floor(t*20)))-.5)*.025;
                fixed3 col=tex2D(_MainTex,uv).rgb;
                float split=_Glitch*.0025;
                col.r=tex2D(_MainTex,uv+float2(split,0)).r;
                col.b=tex2D(_MainTex,uv-float2(split,0)).b;
                float noise=hash(floor(i.uv*float2(1280,720))+floor(t*24));
                col+=(noise-.5)*.018;
                col*=.975+.025*sin(i.uv.y*1600);
                float roll=1-smoothstep(.0,.018,abs(i.uv.y-frac(t*.065)));
                col+=roll*.012;
                col*=.975+.025*sin(t*1.1);
                float2 edge=(i.uv-.5)*float2(1.1,1.0);
                col*=1-saturate(dot(edge,edge))*.24;
                return fixed4(max(col,0),1)*i.color;
            }
            ENDCG
        }
    }
}
