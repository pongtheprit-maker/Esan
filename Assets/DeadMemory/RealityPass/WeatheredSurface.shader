Shader "DeadMemory/WeatheredSurface"
{
    Properties
    {
        [MainTexture] _BaseMap("Surface scan",2D)="white"{}
        [MainColor] _BaseColor("Tint",Color)=(.8,.8,.8,1)
        _WorldScale("Metres to UV",Vector)=(.22,.40,0,0)
        _Seed("Variation seed",Float)=0
        _Variation("Macro ageing",Range(0,1))=.18
        _BumpStrength("Micro relief",Range(0,.4))=.06
        _Smoothness("Smoothness",Range(0,1))=.09
        _Metallic("Metallic",Range(0,1))=0
    }
    SubShader
    {
        Tags {"RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry"}
        Pass
        {
            Name "ForwardLit" Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST, _BaseColor, _WorldScale;
            float _Seed, _Variation, _BumpStrength, _Smoothness, _Metallic;
            CBUFFER_END
            struct A {float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V {float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float3 normalWS:TEXCOORD1; half fog:TEXCOORD2; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO};
            V vert(A a)
            {
                V o=(V)0; UNITY_SETUP_INSTANCE_ID(a); UNITY_TRANSFER_INSTANCE_ID(a,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p=GetVertexPositionInputs(a.positionOS.xyz); o.positionCS=p.positionCS; o.positionWS=p.positionWS;
                o.normalWS=TransformObjectToWorldNormal(a.normalOS); o.fog=ComputeFogFactor(o.positionCS.z); return o;
            }
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p)
            {
                float2 cell=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(hash(cell),hash(cell+float2(1,0)),f.x),lerp(hash(cell+float2(0,1)),hash(cell+1),f.x),f.y);
            }
            half3 materialScan(float2 uv)
            {
                float index=noise(uv*.19+_Seed)*7.0;float cell=floor(index),f=frac(index);
                float2 a=sin(float2(11,17)*(cell+_Seed+1))*5;
                float2 b=sin(float2(11,17)*(cell+_Seed+2))*5;
                half3 ca=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv+a).rgb;
                half3 cb=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv+b).rgb;
                return lerp(ca,cb,smoothstep(.18,.82,f));
            }
            half4 frag(V i):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i); UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float3 n=normalize(i.normalWS),an=abs(n);float2 p;
                if(an.y>an.x&&an.y>an.z)p=i.positionWS.xz;
                else if(an.x>an.z)p=i.positionWS.zy;
                else p=i.positionWS.xy;
                float2 uv=p*_WorldScale.xy;
                half3 albedo=materialScan(uv)*_BaseColor.rgb;
                float broad=noise(p*.19+_Seed*3),fine=noise(p*1.7+_Seed);
                albedo*=lerp(1-_Variation,1+_Variation*.35,broad);
                float h=dot(albedo,half3(.3,.59,.11))*.1+fine*.025;
                float3 dx=ddx(i.positionWS),dy=ddy(i.positionWS);
                float3 r1=cross(dy,n),r2=cross(n,dx);float det=dot(dx,r1);
                n=normalize(max(abs(det),1e-8)*n-sign(det)*(ddx(h)*r1+ddy(h)*r2)*_BumpStrength);
                InputData data=(InputData)0; data.positionWS=i.positionWS; data.normalWS=n;
                data.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
                data.shadowCoord=TransformWorldToShadowCoord(i.positionWS);data.bakedGI=SampleSH(n);
                data.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);data.shadowMask=half4(1,1,1,1);
                data.vertexLighting=VertexLighting(i.positionWS,n);
                SurfaceData surface=(SurfaceData)0;surface.albedo=albedo;surface.metallic=_Metallic;
                surface.smoothness=_Smoothness;surface.normalTS=half3(0,0,1);surface.occlusion=1;surface.alpha=1;
                half4 c=UniversalFragmentPBR(data,surface);c.rgb=MixFog(c.rgb,i.fog);return c;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
