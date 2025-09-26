Shader "Universal Render Pipeline/2D/LatteArtBrush"
{
    Properties
    {
        _MainTex ("Base Texture", 2D) = "white" {}
        _BrushTex ("Brush Texture", 2D) = "white" {}
        _PaintColor ("Paint Color", Color) = (1, 1, 1, 1)
        _BrushPos ("Brush Position", Vector) = (0.5, 0.5, 0.1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BrushTex);
            SAMPLER(sampler_BrushTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _PaintColor;
                float4 _BrushPos;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Sample base texture
                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Calculate brush effect
                float2 brushCenter = _BrushPos.xy;
                float brushSize = _BrushPos.z;
                float brushOpacity = _BrushPos.w;

                // Convert to brush space
                float2 brushUV = (input.uv - brushCenter) / brushSize + 0.5;
                
                // Check bounds
                float inBounds = step(0.0, brushUV.x) * step(brushUV.x, 1.0) * 
                               step(0.0, brushUV.y) * step(brushUV.y, 1.0);

                // Sample brush
                half4 brushSample = SAMPLE_TEXTURE2D(_BrushTex, sampler_BrushTex, brushUV);
                
                // Apply paint
                float brushMask = brushSample.a * brushOpacity * inBounds;
                half4 paintColor = _PaintColor;
                paintColor.a *= brushMask;
                
                // Blend
                return lerp(baseColor, paintColor, paintColor.a);
            }
            ENDHLSL
        }
    }
}
