//
// WaterDistortion.fx — Multi-layer sine-wave UV distortion for water tiles,
// with interactive click-ripple support.
// Compiled by MonoGame Content Pipeline (DesktopGL, Reach profile).
// MojoShader-compatible: no dynamic loops, manually unrolled ripple accumulation.
//

// The scene texture containing water tiles already rendered.
sampler TextureSampler : register(s0);

// Gradient mask for SurfaceReachDistortion technique.
// Alpha encodes per-pixel distortion scale (0 = no distortion, 1 = full).
texture GradientMaskTexture;
sampler GradientMaskSampler = sampler_state
{
    Texture = <GradientMaskTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

// --- Ambient wave parameters ---
float Time;
float Amplitude;
float Frequency;
float Speed;
float2 CameraOffset;

// --- Click-ripple parameters ---
// Each element: xy = ripple centre in screen UV space, z = age in seconds.
// Age < 0 means inactive (masked out with a step() so no branching needed).
float3 Ripple0;
float3 Ripple1;
float3 Ripple2;
float3 Ripple3;
float3 Ripple4;
float3 Ripple5;
float3 Ripple6;
float3 Ripple7;
float RippleAmplitude;  // UV displacement strength (~0.005).
float RippleFrequency;  // Tightness of concentric rings (~40).
float RippleSpeed;      // How fast rings expand outward (~15).
float AspectRatio;      // VirtualWidth / VirtualHeight, keeps rings circular.

// Tint colour applied to surface-reach props at full submersion depth.
// lerp(white, this, gradientFactor) so the top stays untinted.
float3 WaterTintColor;

// Accumulate displacement from a single ripple (no branching).
float2 RippleOffset(float3 ripple, float2 texCoord)
{
    float2 center = ripple.xy;
    float age = ripple.z;

    // step(0, age) = 1 when active (age >= 0), 0 when inactive (age < 0).
    float active = step(0.0, age);

    float2 diff = texCoord - center;
    diff.x *= AspectRatio;
    float dist = length(diff) + 0.0001; // avoid div-by-zero

    float radius = age * RippleSpeed * 0.01;
    float ringDist = dist - radius;
    float ring = exp(-ringDist * ringDist * RippleFrequency * RippleFrequency);

    float timeFade = saturate(1.0 - age * 0.5);

    float2 dir = diff / dist;
    dir.x /= AspectRatio;

    return dir * ring * timeFade * RippleAmplitude * active;
}

float4 MainPS(float2 texCoord : TEXCOORD0, float4 color : COLOR0) : COLOR0
{
    float t = Time * Speed;

    float2 worldUV = texCoord + CameraOffset;

    // Layer 1: broad, slow sway.
    float phase1 = worldUV.y * Frequency + t;
    float wave1X = sin(phase1) * Amplitude;
    float wave1Y = cos(worldUV.x * Frequency * 0.7 + t * 0.5) * Amplitude * 0.4;

    // Layer 2: tighter ripple offset in phase for organic look.
    float phase2 = worldUV.y * Frequency * 2.3 + t * 1.7 + 1.5;
    float wave2X = sin(phase2) * Amplitude * 0.35;

    float2 offset = float2(wave1X + wave2X, wave1Y);

    // Manually unrolled ripple accumulation (MojoShader requires static loops).
    offset += RippleOffset(Ripple0, texCoord);
    offset += RippleOffset(Ripple1, texCoord);
    offset += RippleOffset(Ripple2, texCoord);
    offset += RippleOffset(Ripple3, texCoord);
    offset += RippleOffset(Ripple4, texCoord);
    offset += RippleOffset(Ripple5, texCoord);
    offset += RippleOffset(Ripple6, texCoord);
    offset += RippleOffset(Ripple7, texCoord);

    float2 distortedCoord = texCoord + offset;

    return tex2D(TextureSampler, distortedCoord) * color;
}

technique WaterDistortion
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}

float4 SurfaceReachPS(float2 texCoord : TEXCOORD0, float4 color : COLOR0) : COLOR0
{
    // Read gradient factor from the mask texture (separate from the scene texture).
    // Alpha encodes how deep below the surface this pixel is (0 = surface, 1 = submerged).
    float gradientFactor = tex2D(GradientMaskSampler, texCoord).a;

    // Early-out for pixels outside any prop's mask region.
    if (gradientFactor < 0.004)  // ~1/255 threshold
    {
        return float4(0, 0, 0, 0);
    }

    // Power curve: ramps quickly from 0 — only the very top pixels stay stationary.
    gradientFactor = pow(abs(gradientFactor), 0.70);

    float t = Time * Speed;
    float2 worldUV = texCoord + CameraOffset;

    // Same wave computation as MainPS.
    float phase1 = worldUV.y * Frequency + t;
    float wave1X = sin(phase1) * Amplitude;
    float wave1Y = cos(worldUV.x * Frequency * 0.7 + t * 0.5) * Amplitude * 0.4;

    float phase2 = worldUV.y * Frequency * 2.3 + t * 1.7 + 1.5;
    float wave2X = sin(phase2) * Amplitude * 0.35;

    float2 offset = float2(wave1X + wave2X, wave1Y);

    // Manually unrolled ripple accumulation (same as MainPS).
    offset += RippleOffset(Ripple0, texCoord);
    offset += RippleOffset(Ripple1, texCoord);
    offset += RippleOffset(Ripple2, texCoord);
    offset += RippleOffset(Ripple3, texCoord);
    offset += RippleOffset(Ripple4, texCoord);
    offset += RippleOffset(Ripple5, texCoord);
    offset += RippleOffset(Ripple6, texCoord);
    offset += RippleOffset(Ripple7, texCoord);

    // Scale distortion by the gradient factor — top of prop = 0 distortion, bottom = full.
    offset *= gradientFactor;

    float2 distortedCoord = texCoord + offset;

    float4 texColor = tex2D(TextureSampler, distortedCoord) * color;
    // Depth-based tint: surface pixels stay original colour, submerged pixels shift toward water tint.
    texColor.rgb *= lerp(float3(1, 1, 1), WaterTintColor, gradientFactor);
    return texColor;
}

technique SurfaceReachDistortion
{
    pass P0
    {
        PixelShader = compile ps_3_0 SurfaceReachPS();
    }
}
