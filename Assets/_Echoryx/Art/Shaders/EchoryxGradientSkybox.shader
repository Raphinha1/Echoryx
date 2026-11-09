Shader "Echoryx/Gradient Skybox"
{
    // Stylized space sky: three-color vertical gradient plus a sparse star field. No textures.
    Properties
    {
        _TopColor ("Top", Color) = (0.07, 0.05, 0.22, 1)
        _HorizonColor ("Horizon", Color) = (0.45, 0.25, 0.60, 1)
        _BottomColor ("Bottom", Color) = (0.05, 0.04, 0.14, 1)
        _Exponent ("Gradient Exponent", Range(0.2, 4)) = 0.7
        _StarScale ("Star Scale", Range(20, 400)) = 160
        _StarDensity ("Star Density", Range(0, 0.2)) = 0.035
        _StarBrightness ("Star Brightness", Range(0, 3)) = 1.4
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _HorizonColor;
                half4 _BottomColor;
                half _Exponent;
                half _StarScale;
                half _StarDensity;
                half _StarBrightness;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.dir = input.positionOS.xyz;
                return output;
            }

            float Hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 d = normalize(input.dir);
                half h = d.y;
                half up = pow(saturate(h), _Exponent);
                half down = pow(saturate(-h), _Exponent);
                half3 color = lerp(_HorizonColor.rgb, _TopColor.rgb, up);
                color = lerp(color, _BottomColor.rgb, down);

                float3 cell = floor(d * _StarScale);
                float3 local = frac(d * _StarScale) - 0.5;
                float rnd = Hash(cell);
                float3 jitter = float3(Hash(cell + 7.1), Hash(cell + 13.7), Hash(cell + 19.3)) - 0.5;
                float star = step(1.0 - _StarDensity, rnd) * smoothstep(0.16, 0.0, length(local - jitter * 0.5));
                color += star * _StarBrightness * lerp(half3(1, 1, 1), half3(1.0, 0.8, 0.6), Hash(cell + 3.3));
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
