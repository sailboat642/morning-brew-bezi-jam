Shader "URP/2D/LatteArtBrush"
{
    Properties
    {
        _MainTex ("Canvas Texture", 2D) = "white" {}
        _BrushTex ("Brush Texture", 2D) = "white" {}
        _PaintColor ("Paint Color", Color) = (1,1,1,1)
        _BrushPos ("Brush Position (XY=Pos, Z=Size, W=Opacity)", Vector) = (0.5, 0.5, 0.1, 1)
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
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BrushTex);
            SAMPLER(sampler_BrushTex);

            CBUFFER_START(UnityPerMaterial)
            float4 _PaintColor;
            float4 _BrushPos;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Sample base canvas
                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                
                // Calculate brush effect
                float2 brushCenter = _BrushPos.xy;
                float brushSize = _BrushPos.z;
                float brushOpacity = _BrushPos.w;
                
                // Calculate distance from brush center
                float2 brushUV = (IN.uv - brushCenter) / brushSize + 0.5;
                
                // Check if we're within brush bounds
                float inBounds = step(0.0, brushUV.x) * step(brushUV.x, 1.0) * 
                               step(0.0, brushUV.y) * step(brushUV.y, 1.0);
                
                // Sample brush texture
                half4 brushSample = SAMPLE_TEXTURE2D(_BrushTex, sampler_BrushTex, brushUV);
                
                // Create brush mask
                float brushMask = brushSample.r * brushOpacity * inBounds;
                
                // Apply paint
                half4 paintColor = _PaintColor;
                paintColor.a *= brushMask;
                
                // Blend
                return lerp(baseColor, paintColor, paintColor.a);
            }
            ENDHLSL
        }
    }
}
