# Aluminum Metal Gradient Shader Design

## Overview

Create a customizable Unity shader that renders a smooth aluminum metal gradient effect for grid-based game blocks.

## Requirements

- Vertical gradient (top to bottom)
- Smooth aluminum surface with specular highlights
- Configurable via Inspector
- Applicable to any block (Mover, Wall, etc.)

## Technical Design

### File Structure

```
Assets/
├── Shaders/
│   └── AluminumMetalGradient.shader
├── Materials/
│   └── AluminumMetal.mat (optional)
```

### Shader Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `_TopColor` | Color | Light aluminum | Gradient top color |
| `_BottomColor` | Color | Dark aluminum | Gradient bottom color |
| `_Glossiness` | Range(0-1) | 0.7 | Smoothness (specular intensity) |
| `_Metallic` | Range(0-1) | 0.9 | Metalness |
| `_GradientOffset` | Range(-1-1) | 0 | Gradient offset |

### Implementation Approach

**Shader Type:** Unity Surface Shader

**Gradient Calculation:**
1. Get vertex world position Y coordinate
2. Normalize Y value to 0-1 range for gradient blending
3. Use `lerp()` to interpolate between TopColor and BottomColor

**Lighting Model:**
- Unity Standard Surface Shader
- Metallic = 0.9 (high metal reflection)
- Smoothness = 0.7 (smooth surface highlight)
- Gradient color as Albedo

### Usage

1. Create material: Right-click `Assets/Materials/` → Create → Material
2. Select shader: Choose `Custom/AluminumMetalGradient` in Inspector
3. Adjust parameters: Modify colors, smoothness, metallic properties
4. Apply to blocks: Drag material to any Mover/Wall MeshRenderer

## Testing

- Test material on Crate, Player, Wall in LevelScene
- Verify gradient appearance across different block heights
- Confirm highlight effects interact correctly with scene lighting

## Approval

Design approved on 2026-03-07.
