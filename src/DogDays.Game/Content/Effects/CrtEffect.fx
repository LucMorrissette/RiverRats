//
// CrtEffect.fx — Full-screen CRT post-process: barrel distortion, scanlines, and vignette.
// Compiled by MonoGame Content Pipeline (DesktopGL, Reach profile).
// MojoShader-compatible: no dynamic loops or branching on non-uniform values.
//

// The final scene render target. Bound automatically by SpriteBatch as sampler s0.
sampler TextureSampler : register(s0);

// Resolution of the scene render target in pixels (e.g., 480, 270).
float2 TextureSize;

// Barrel distortion strength. 0 = no distortion, 0.1–0.2 = subtle CRT curve.
float DistortionAmount;

// Scanline darkness. 0 = no scanlines, 1 = fully dark every other row.
float ScanlineIntensity;

// Vignette strength. 0 = no darkening, higher = more corner darkening.
float VignetteStrength;

// Dark slate colour for out-of-bounds pixels (CRT bezel / powered-off screen).
float3 BorderColor;

/// <summary>
/// Applies barrel distortion to UV coordinates, simulating a curved CRT screen.
/// Pixels outside the curved area are filled with BorderColor.
/// </summary>
float2 BarrelDistort(float2 uv)
{
    // Centre UVs around origin (-0.5 to 0.5).
    float2 centered = uv - 0.5;

    // Radial distance squared from centre.
    float r2 = dot(centered, centered);

    // Push pixels outward proportional to distance squared.
    centered *= 1.0 + DistortionAmount * r2;

    return centered + 0.5;
}

float4 MainPS(float2 texCoord : TEXCOORD0, float4 color : COLOR0) : COLOR0
{
    // Apply barrel distortion to get curved-screen UVs.
    float2 uv = BarrelDistort(texCoord);

    // Black outside the curved screen boundary.
    // step(0, x) = 1 when x >= 0; step(x, 1) = 1 when x <= 1.
    float inBounds = step(0.0, uv.x) * step(uv.x, 1.0)
                   * step(0.0, uv.y) * step(uv.y, 1.0);

    float4 texColor = tex2D(TextureSampler, uv);

    // --- Scanlines ---
    // Use the distorted UV's Y position mapped to pixel rows.
    // sin() produces a smooth wave along scanline rows.
    float scanlinePos = uv.y * TextureSize.y * 3.14159;
    float scanline = sin(scanlinePos) * 0.5 + 0.5;
    // lerp between full brightness and the scanline wave by intensity.
    texColor.rgb *= lerp(1.0, scanline, ScanlineIntensity);

    // --- Vignette ---
    // Distance from centre in -1 to 1 range.
    float2 d = (uv - 0.5) * 2.0;
    float vignette = 1.0 - dot(d, d) * VignetteStrength;
    // Clamp to avoid negative brightness.
    vignette = saturate(vignette);
    texColor.rgb *= vignette;

    // Fill out-of-bounds pixels with the border colour.
    float3 finalRgb = lerp(BorderColor, texColor.rgb, inBounds);
    float finalA = lerp(1.0, texColor.a, inBounds);

    return float4(finalRgb, finalA) * color;
}

technique CrtEffect
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}
