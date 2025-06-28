Shader "Unlit/toonShaderTest"
{
    Properties
    {
        [MainTexture] _MainTex ("Texture", 2D) = "white" {}
        _textureAffect("Texture Affect", range(0,1)) = 0.5
        //_Normal ("Normal", 2D) = "bump" {}
        
        //光照
        [HDR]_highlightColor("Highlight Color", Color) = (1,1,1,1)
        _highlightRange("Highlight Range", Range(0,1)) = 0.5
        _highlightSmoothness("Highlight Smoothness", Range(0,1)) = 0.1
    	
    	[Space(20)]
    	_shadowCol("Shadow Color", Color) = (0,0,0,1)
    	_shadowIntensity("Shadow Intensity", Range(0,1)) = 0.5
        
        [Space(30)]
        [Toggle(_ENABLE_ASSIGNED_DIFFUSE)] _assignedDiffuseOn("Assigned Diffuse On", float) = 0
        /*_BrightColor("Bright Color", Color) = (1,1,1,1)
        _ShadowColor1("Shadow Color1", Color) = (0.7,0.7,0.9)
        _ShadowColor2("Shadow Color2", Color) = (0.5,0.5,0.5)*/
    	[Gradient] _GradientRamp("Gradient", 2D) = "white" {}
    	_SelfShadingSize ("[]Shading Offset", Range(0, 1.0)) = 0.0
        _ShadowRange("Shadow Range", Range(0,1)) = 0.03
        _ShadowSmooth("Shadow Smooth", Range(0,1)) = 0.02
    	
    	
    	[Space(30)]
    	_rimRange("Rim Range", Range(0,1)) = 0.5
        _rimSmoothness("Rim Smoothness", Range(0,1)) = 0.1
    	_rimBrightCol("Rim Bright Col", Color) = (1,1,1,1)
    	_rimDarkCol("Rim Dark Col", Color) = (0.5,0.5,0.5,1)
        
        //描边
        [Space(30)]
        _OutlineWidth ("Outline Width", range(0.01,3)) = 0.24
        _OutlineColor ("Outline Color", Color) = (0.5,0.5,0.5,1)
        
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"  "RenderPipeline" = "UniversalRenderPipeline" "IgnoreProjector" = "True" }


        Pass //着色pass
        {   
            Tags{"LightMode" = "UniversalForward"}
            Cull Back
            Zwrite On
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            // Universal Pipeline keywords

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" //hlsl核心代码库
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl" //引用光照库函数

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex); 
			TEXTURE2D(_GradientRamp);
            SAMPLER(sampler_GradientRamp);
            
            float4 _MainTex_ST;
            half4 _BrightColor;
            half4 _ShadowColor1;
            half4 _ShadowColor2;
            half4 _highlightColor;
            half4 _shadowCol;
            half4 _rimBrightCol;
            half4 _rimDarkCol;
            half _ShadowSmooth;
            half _ShadowRange;

            half _highlightRange;
            half _highlightSmoothness;

            half _shadowIntensity;

            half _rimRange;
            half _rimSmoothness;

            half _SelfShadingSize;
            half _textureAffect;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 nDirOS : NORMAL;
                float4 vertCol : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                float3 nDirWS : TEXCOORD1;
                float3 posWS : TEXCOORD2;
                float4 vertCol : TEXCOORD3;
            };


            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.nDirWS = TransformObjectToWorldNormal(v.nDirOS.xyz);
                o.posWS = TransformObjectToWorld(v.vertex.xyz);
                o.uv = v.uv;
                o.vertCol = v.vertCol;

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                //向量准备
                Light mainlight = GetMainLight(TransformWorldToShadowCoord(i.posWS));
                real3 lightCol = mainlight.color;
                half3 vDirWS = normalize(_WorldSpaceCameraPos.xyz - i.posWS);
                half3 nDirWS = normalize(i.nDirWS);
                half3 lDirWS = normalize(mainlight.direction);
                half3 hDirWS = normalize(vDirWS + lDirWS);

                //光照模型
                half lambert = saturate(dot(nDirWS,lDirWS));
                half halfLambert = dot(nDirWS,lDirWS) *0.5 + 0.5;
                half blinnphong = saturate(dot(nDirWS,hDirWS));
            	half fresnel = 1-max(0,dot(vDirWS,nDirWS));
                
                half4 var_MainTex = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.uv);
				var_MainTex.rgb = lerp(var_MainTex.rgb,1,_textureAffect);
                
                half3 diffuse;
                half shadow = max(1-_shadowIntensity,mainlight.shadowAttenuation);
            	
                //assigned
                /*half mid = 1-( smoothstep(_ShadowRange-_ShadowSmooth,_ShadowRange+_ShadowSmooth,-dot(nDirWS,lDirWS))+smoothstep(_ShadowRange-_ShadowSmooth,_ShadowRange+_ShadowSmooth,dot(nDirWS,lDirWS)));
                half mask = saturate(smoothstep(_ShadowRange-_ShadowSmooth,_ShadowRange+_ShadowSmooth,-dot(nDirWS,lDirWS))+mid);
                half3 darCol02 = lerp(_ShadowColor1.rgb, _ShadowColor2.rgb, mid+(1-mask));
                half3 diffuseCol =lerp(_BrightColor.rgb,darCol02,mask);
                diffuseCol *= var_MainTex.rgb;
                diffuse = 1-mask;*/
            	half angleDiff = saturate(halfLambert - _SelfShadingSize);
            	float2 gradient_uv = float2(angleDiff, 0.5);
            	half4 Color = SAMPLE_TEXTURE2D(_GradientRamp, sampler_GradientRamp, gradient_uv);
            	Color.rgb *= lightCol;
            	diffuse = Color * var_MainTex.rgb;


            	//rim
            	half rim = smoothstep(_rimRange-_rimSmoothness,_rimRange+_rimSmoothness,Pow4(fresnel));
            	half3 rimCol = lerp(_rimDarkCol.rgb,_rimBrightCol.rgb,lambert)*rim;
                

                //highLight
            	_highlightRange = 1-_highlightRange;
                half spec = smoothstep(_highlightRange-_highlightSmoothness,_highlightRange+_highlightSmoothness,Pow4(blinnphong));
                half3 specCol =lerp(diffuse,_highlightColor.rgb,spec*step(0.5,lambert));
        
                half3 finalRGB= specCol+ rimCol;
					finalRGB =lerp(_shadowCol.rgb,finalRGB,shadow) ;//;
                return float4(finalRGB,1);
            }
            ENDHLSL
        }

        Pass //描边pass
        {   
            Tags{"LightMode" = "SRPDefaultUnlit"}//SRPDefaultUnlit
            Cull Front
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" //hlsl核心代码库
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"


            float _OutlineWidth;
            float4 _OutlineColor;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 nDirOS : NORMAL;
                float4 vertCol : COLOR;
                float4 tangent : TANGENT;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 vertCol : TEXCOORD0;
                float3 nDirWS : TEXCOORD1;
                float3 tDirWS : TEXCOORD2;
                float3 bDirWS : TEXCOORD3;
            };


            v2f vert (appdata v)
            {
                v2f o;

                //v.vertex.xyz = v.vertex.xyz+ v.nDirOS * _OutlineWidth *0.1; //顶点沿着模型法线外扩

                float4 pos = TransformObjectToHClip(v.vertex.xyz);
                float3 vertCol = v.vertCol.xyz * 2 -1;
                float3 nDirVS = mul(UNITY_MATRIX_IT_MV, vertCol);
                float3 ndcNormal = normalize(mul(UNITY_MATRIX_P,nDirVS.xyz)) * pos.w; //法线变换到NDC空间
                float4 nearUpperRight = mul(unity_CameraInvProjection, float4(1,1, UNITY_NEAR_CLIP_VALUE, _ProjectionParams.y));
                float aspect = abs(nearUpperRight.y / nearUpperRight.x);
                ndcNormal *= aspect;
                pos.xy += 0.01* _OutlineWidth * ndcNormal.xy;
                o.pos = pos;


                
                // o.nDirWS = TransformObjectToWorldNormal(v.nDirOS.xyz);
                // o.tDirWS = normalize( mul( unity_ObjectToWorld, float4( v.tangent.xyz,0.0 ) ) .xyz);
                // o.bDirWS = normalize(cross(o.nDirWS, o.tDirWS) * v.tangent.w);

                // float4 vertCol = v.vertCol * 2 -1;
                // half3 smoothNormal = normalize(UnpackNormalmapRGorAG(vertCol));
                // float3x3 TBN = float3x3 (o.tDirWS, o.bDirWS, o.nDirWS);
                // half3 nDirWS = normalize(mul(smoothNormal,TBN));
                // half3 outlineNormal = TransformObjectToHClip(nDirWS) * pos.w;
                // float aspect = _ScreenParams.x / _ScreenParams.y;
                // pos.xy += 0.001 * _OutlineWidth * v.vertCol.a * outlineNormal.xy *aspect ;
                // o.pos = pos;

                /*float3 vertCol = v.vertCol.xyz * 2 -1;
                v.vertex.xyz += vertCol * _OutlineWidth *0.001;
                o.pos = TransformObjectToHClip(v.vertex.xyz);*/

                return o;
            }

            float4 frag (v2f i) : SV_Target
            {   
                
                return _OutlineColor;
            }
            ENDHLSL
        }
        
        pass 
    	{
			Tags{ "LightMode" = "ShadowCaster" }
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 
			struct appdata
			{
				float4 vertex : POSITION;
			};
 
			struct v2f
			{
				float4 pos : SV_POSITION;
			};
 
			sampler2D _MainTex;
			float4 _MainTex_ST;
 
			v2f vert(appdata v)
			{
				v2f o;
				o.pos = mul(UNITY_MATRIX_MVP,v.vertex);
				return o;
			}
			float4 frag(v2f i) : SV_Target
			{
				float4 color;
				color.xyz = float3(0.0, 0.0, 0.0);
				return color;
			}
			ENDHLSL
		}
    	
    	/*Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0
            Cull off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }
    	
    	Pass 
    	{
			Name "DepthNormals"
			Tags {
			    "LightMode" = "DepthNormals"
			}

			HLSLPROGRAM
			#pragma vertex DepthNormalsVertex
			#pragma fragment DepthNormalsFragment


			#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
			ENDHLSL
		}*/


    }
	
	//Fallback "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "Quibli.QuibliEditor"
}
