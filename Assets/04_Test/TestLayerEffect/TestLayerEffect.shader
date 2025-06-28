Shader "Unlit/TestLayerEffect"
{
    Properties
    {
        _MainTexA ("TextureA", 2D) = "white" {}
        _MainTexB ("TextureB", 2D) = "white" {}
        [EffectDrawer] _BlendMode ("Blend Mode", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float3 RGB2HSV(float3 c)
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float3 HSV2RGB(float3 c)
            {
                float3 rgb = clamp(abs(fmod(c.x * 6.0 + float3(0.0, 4.0, 2.0), 6) - 3.0) - 1.0, 0, 1);
                rgb = rgb * rgb * (3.0 - 2.0 * rgb);
                return c.z * lerp(float3(1, 1, 1), rgb, c.y);
            }

            sampler2D _MainTexA;
            float4 _MainTexA_ST;
            sampler2D _MainTexB;
            float4 _MainTexB_ST;
            float _BlendMode;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv.xy = TRANSFORM_TEX(v.uv.xy, _MainTexA);
                o.uv.zw = TRANSFORM_TEX(v.uv.xy, _MainTexB);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float4 colorA = tex2D(_MainTexA, i.uv.xy);
                float4 colorB = tex2D(_MainTexB, i.uv.zw);
                float4 finalRGBA;

                switch ((int)_BlendMode)
                {
                    case 0:
                        // Normal
                        finalRGBA = float4(colorA.xyz, 1);
                        break;
                    case 1:
                        // Opacity / Multiply 正片叠底
                        finalRGBA = float4(colorA.xyz * colorB.xyz, 1);
                        break;
                    case 2:
                        // Screen 滤色
                        finalRGBA = float4(1 - (1 - colorA.xyz) * (1 - colorB.xyz), 1);
                        break;
                    case 3:
                        // Color Dodge 颜色减淡
                        finalRGBA = float4(colorA.xyz * (rcp(1 - colorB.xyz)), 1);
                        break;
                    case 4:
                        // Color Burn 颜色加深
                        finalRGBA = float4(1 - rcp(colorB.xyz) * (1 - colorA.xyz), 1);
                        break;
                    case 5:
                        // Linear Dodge 线形减淡
                        finalRGBA = float4(colorB.xyz + colorA.xyz, 1);
                        break;
                    case 6:
                        // Linear Burn 线形加深
                        finalRGBA = float4(colorB.xyz + colorA.xyz - 1, 1);
                        break;
                    case 7:
                        // Overlay 叠加
                        {
                            float3 OA = step(colorA.xyz, 0.5);
                            float3 c1 = colorA.xyz * colorB.xyz * 2 * OA;
                            float3 c2 = (1 - 2 * (1 - colorA.xyz) * (1 - colorB.xyz)) * (1 - OA);
                            finalRGBA = float4(c1 + c2, 1);
                        }
                        break;
                    case 8:
                        // Hard Light 强光
                        {
                            float3 OB = step(colorB.xyz, 0.5);
                            float3 c1 = colorA.xyz * colorB.xyz * 2 * OB;
                            float3 c2 = (1 - 2 * (1 - colorA.xyz) * (1 - colorB.xyz)) * (1 - OB);
                            finalRGBA = float4(c1 + c2, 1);
                        }
                        break;
                    case 9:
                        // Soft Light 柔光
                        {
                            float3 OB = step(colorB.xyz, 0.5);
                            float3 c1 = ((2 * colorB - 1) * (colorA - colorA * colorA) + colorA) * OB;
                            float3 c2 = ((2 * colorB - 1) * (sqrt(colorA) - colorA) + colorA) * (1 - OB);
                            finalRGBA = float4(c1 + c2, 1);
                        }
                        break;
                    case 10:
                        // Vivid Light 亮光
                        {
                            float3 c1 = colorB - (1 - colorB) * (1 - 2 * colorA) / (2 * colorA);
                            float3 c2 = colorB + colorB * (2 * colorA - 1) / (2 * (1 - colorA));
                            float3 OB = step(colorA.xyz, 0.5);
                            c1 *= OB;
                            c2 *= (1 - OB);
                            finalRGBA = float4(c1 + c2, 1);
                        }
                        break;
                    case 11:
                        // Linear Light 线性光
                        finalRGBA = float4(colorA.xyz + 2 * colorB.xyz - 1, 1);
                        break;
                    case 12:
                        // Pin Light 点光
                        {
                            float3 OB = step(colorA, 2 * colorB - 1);
                            float3 c1 = (2 * colorB - 1) * OB;
                            float3 OB1 = (1 - OB) * step(colorA, 2 * colorB);
                            float3 c2 = colorA * OB1;
                            float3 OB2 = 1 - step(colorA, 2 * colorB);
                            float3 c3 = 2 * colorB * OB2;
                            finalRGBA = float4(c1 + c2 + c3, 1);
                        }
                        break;
                    case 13:
                        // Hard Mix 混合实色
                        {
                            float3 OA = step(colorB, 1 - colorA);
                            float3 c1 = 0 * OA;
                            float3 OA1 = step(1 - colorA, colorB);
                            float3 c2 = 1 * OA1;
                            finalRGBA = float4(c1 + c2, 1);
                        }
                        break;
                    case 14:
                        // Difference 差值
                        finalRGBA = float4(abs(colorB.xyz - colorA.xyz), 1);
                        break;
                    case 15:
                        // Exclusion 排除
                        finalRGBA = float4(colorB.xyz + colorA.xyz - 2 * colorB.xyz * colorA.xyz, 1);
                        break;
                    case 16:
                        // Hue 色相
                        {
                            float3 colorAHSV = RGB2HSV(colorA.xyz);
                            float3 colorBHSV = RGB2HSV(colorB.xyz);
                            float3 c1 = float3(colorBHSV.x, colorAHSV.yz);
                            c1 = HSV2RGB(c1);
                            finalRGBA = half4(c1, 1);
                        }
                        break;
                    default:
                        finalRGBA = float4(colorA.xyz, 1);
                        break;
                }

                return finalRGBA;
            }
            ENDCG
        }
    }
}
