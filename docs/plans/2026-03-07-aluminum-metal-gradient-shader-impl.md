# Aluminum Metal Gradient Shader Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create a Unity Surface Shader that renders a smooth aluminum metal gradient effect for grid-based game blocks.

**Architecture:** Use Unity Surface Shader with Standard lighting model. Gradient is calculated in vertex shader using world Y position, then interpolated in surface shader with metallic/smoothness properties.

**Tech Stack:** Unity 2019.4.10f1 LTS, HLSL/ShaderLab

---

### Task 1: Create Shaders Directory

**Files:**
- Create: `Assets/Shaders/` directory

**Step 1: Create directory structure**

```bash
mkdir -p Assets/Shaders
```

**Step 2: Verify directory exists**

```bash
ls Assets/Shaders
```
Expected: Directory exists (empty)

---

### Task 2: Create AluminumMetalGradient Shader

**Files:**
- Create: `Assets/Shaders/AluminumMetalGradient.shader`

**Step 1: Create the shader file with complete implementation**

```hlsl
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
```

**Step 2: Verify file was created**

```bash
ls -la Assets/Shaders/AluminumMetalGradient.shader
```
Expected: File exists with content

**Step 3: Commit shader**

```bash
git add Assets/Shaders/AluminumMetalGradient.shader
git add Assets/Shaders/AluminumMetalGradient.shader.meta
git commit -m "feat: add aluminum metal gradient shader"
```

---

### Task 3: Create Example Material

**Files:**
- Create: `Assets/Materials/AluminumMetal.mat` (via Unity Editor)

**Step 1: Document manual steps for Unity Editor**

In Unity Editor:
1. Right-click `Assets/Materials/` folder
2. Select Create → Material
3. Name it "AluminumMetal"
4. In Inspector, click Shader dropdown
5. Select `Custom/AluminumMetalGradient`
6. Adjust properties:
   - Top Color: (217, 222, 230, 255) - light aluminum
   - Bottom Color: (128, 133, 140, 255) - dark aluminum
   - Smoothness: 0.7
   - Metallic: 0.9
   - Gradient Offset: 0

**Step 2: Commit material (after Unity creates it)**

```bash
git add Assets/Materials/AluminumMetal.mat
git add Assets/Materials/AluminumMetal.mat.meta
git commit -m "feat: add aluminum metal gradient material"
```

---

### Task 4: Testing and Verification

**Step 1: Test in Unity Editor**

1. Open `Assets/Scenes/LevelScene.unity`
2. Select a Crate or Wall object in Hierarchy
3. In Inspector, find MeshRenderer component
4. Change material to "AluminumMetal"
5. Verify:
   - [ ] Vertical gradient is visible (lighter on top, darker on bottom)
   - [ ] Metallic reflection is present
   - [ ] Smoothness/highlight responds to scene lighting
   - [ ] Adjusting Inspector properties updates material in real-time

**Step 2: Test gradient across different heights**

1. Create multiple cubes at different Y positions
2. Apply AluminumMetal material to all
3. Verify gradient appears consistent across heights

**Step 3: Document test results**

Update this plan with test results or create a separate test report.

---

## Summary

| Task | Description | Status |
|------|-------------|--------|
| 1 | Create Shaders directory | Pending |
| 2 | Create AluminumMetalGradient.shader | Pending |
| 3 | Create example material | Pending |
| 4 | Testing and verification | Pending |

## Notes

- Shader uses world-space Y coordinate for gradient calculation
- Adjust `gradientRaw` multiplier in vertex shader if gradient appears too subtle/strong
- Material must be created in Unity Editor (cannot be created programmatically in .mat file)
