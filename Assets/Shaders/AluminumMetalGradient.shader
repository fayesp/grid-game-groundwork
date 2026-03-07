Shader "Custom/AluminumMetalGradient"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.85, 0.87, 0.9, 1)
        _BottomColor ("Bottom Color", Color) = (0.5, 0.52, 0.55, 1)
        _Glossiness ("Smoothness", Range(0,1)) = 0.7
        _Metallic ("Metallic", Range(0,1)) = 0.9
        _GradientOffset ("Gradient Offset", Range(-1,1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        struct Input
        {
            float gradientUV;
        };

        fixed4 _TopColor;
        fixed4 _BottomColor;
        half _Glossiness;
        half _Metallic;
        half _GradientOffset;

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);

            // Get world position
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

            // Calculate gradient UV based on world Y position
            // Normalize to 0-1 range (adjust multiplier as needed for your scale)
            float gradientRaw = worldPos.y * 0.5 + 0.5;
            o.gradientUV = saturate(gradientRaw + _GradientOffset * 0.5);
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Lerp between bottom and top colors
            fixed3 gradientColor = lerp(_BottomColor.rgb, _TopColor.rgb, IN.gradientUV);

            o.Albedo = gradientColor;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1.0;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
