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
// Each element: xy = ripple centre in screen UV space, z = age in seconds,
// w = scale multiplier (1 = normal click ripple, higher = more pronounced).
// Age < 0 means inactive (masked out with a step() so no branching needed).
float4 Ripple0;
float4 Ripple1;
float4 Ripple2;
float4 Ripple3;
float4 Ripple4;
float4 Ripple5;
float4 Ripple6;
float4 Ripple7;
float RippleAmplitude;  // UV displacement strength (~0.005).
float RippleFrequency;  // Tightness of concentric rings (~40).
float RippleSpeed;      // How fast rings expand outward (~15).
float AspectRatio;      // VirtualWidth / VirtualHeight, keeps rings circular.

// Tint colour applied to surface-reach props at full submersion depth.
// lerp(white, this, gradientFactor) so the top stays untinted.
float3 WaterTintColor;

// --- V-wake parameters (canoe movement trail) ---
// WakeCenter: xy = stern position in screen UV, z = intensity (0 = off, 1 = full speed).
float3 WakeCenter;
// WakeDirection: unit vector of travel direction in aspect-corrected UV space.
float2 WakeDirection;

// Accumulate displacement from a single ripple (no branching).
float2 RippleOffset(float4 ripple, float2 texCoord)
{
    float2 center = ripple.xy;
    float age = ripple.z;
    float scale = ripple.w;

    // step(0, age) = 1 when active (age >= 0), 0 when inactive (age < 0).
    float active = step(0.0, age);

    float2 diff = texCoord - center;
    diff.x *= AspectRatio;
    float dist = length(diff) + 0.0001; // avoid div-by-zero

    float radius = age * RippleSpeed * 0.01 * scale;
    float ringDist = dist - radius;
    // Softer ring falloff for larger ripples — wider visible band.
    float adjustedFreq = RippleFrequency / max(scale, 1.0);
    float ring = exp(-ringDist * ringDist * adjustedFreq * adjustedFreq);

    // Larger ripples fade more slowly so they travel further.
    float fadeRate = 0.5 / max(scale, 1.0);
    float timeFade = saturate(1.0 - age * fadeRate);

    float2 dir = diff / dist;
    dir.x /= AspectRatio;

    return dir * ring * timeFade * RippleAmplitude * scale * active;
}

// Computes V-wake distortion trailing behind a moving watercraft.
// Produces two diagonal lines of UV displacement fanning out from the stern.
float2 WakeDistortion(float2 texCoord)
{
    float intensity = WakeCenter.z;
    if (intensity < 0.01)
        return float2(0, 0);

    float2 center = WakeCenter.xy;
    float2 dir = WakeDirection;

    // Vector from stern to this pixel, aspect-corrected.
    float2 toPixel = texCoord - center;
    toPixel.x *= AspectRatio;

    // Decompose: positive along = ahead of boat, negative = behind.
    float along = dot(toPixel, dir);

    // Only affect pixels behind the stern (along < 0).
    // Gradual ramp so the wake builds up over distance rather than starting at full power.
    float behindAmount = max(-along, 0.0);
    float behindMask = smoothstep(0.0, 0.14, behindAmount);

    // Perpendicular component.
    float2 perpDir = float2(-dir.y, dir.x);
    float perp = dot(toPixel, perpDir);

    // More acute wake half-angle ~14 degrees (tan ≈ 0.25).
    float vArmTarget = 0.25 * behindAmount;

    // Distance from each V arm.
    float distLeft = abs(perp - vArmTarget);
    float distRight = abs(perp + vArmTarget);
    float distFromArm = min(distLeft, distRight);

    // Arm thickness: wide near stern, tapering further back.
    float baseWidth = 0.016 + behindAmount * 0.10;
    float taper = saturate(1.0 - behindAmount * 2.0);
    float armWidth = baseWidth * (0.45 + 0.55 * taper);
    float armProfile = exp(-(distFromArm * distFromArm) / (armWidth * armWidth));

    // Fade with distance behind stern — trail length scales with intensity.
    float trailLen = 0.50 + intensity * 0.60;
    float distFade = saturate(1.0 - behindAmount / trailLen);
    distFade = distFade * distFade * distFade;

    // Gentle wave oscillation along the arms for texture (two soft layers).
    float wave = 0.7 + 0.2 * sin(behindAmount * 80.0 + Time * 6.0)
                     + 0.1 * sin(behindAmount * 150.0 - Time * 4.0);

    // Displacement pushes outward from the center line.
    float side = sign(perp);
    float2 displacement = perpDir * side;
    displacement.x /= AspectRatio;

    float2 vWake = displacement * armProfile * distFade * wave * intensity * RippleAmplitude * 4.0 * behindMask;

    // --- Turbulent swirls between the V arms ---
    // Only active inside the V cone (|perp| < arm target).
    float absPerp = abs(perp);
    float insideMask = saturate(1.0 - absPerp / max(vArmTarget, 0.001));
    insideMask *= insideMask; // sharpen falloff near arms

    // Multi-frequency swirl pattern using world-space-anchored noise.
    // Strong non-linear expansion so swirls grow faster and larger down-trail.
    float expansion = 1.0 + behindAmount * 9.0 + behindAmount * behindAmount * 24.0;
    float freqScale = 1.0 / expansion;
    float2 swirlUV = texCoord + CameraOffset;
    float swirl1 = sin(swirlUV.x * (180.0 * freqScale) + swirlUV.y * (220.0 * freqScale) + Time * 6.0);
    float swirl2 = cos(swirlUV.x * (310.0 * freqScale) - swirlUV.y * (140.0 * freqScale) + Time * 4.5);
    float swirl3 = sin(swirlUV.x * (95.0 * freqScale) + swirlUV.y * (370.0 * freqScale) - Time * 8.0);
    float2 swirlOffset = float2(
        swirl1 * 0.5 + swirl2 * 0.3 + swirl3 * 0.2,
        swirl2 * 0.5 - swirl1 * 0.2 + swirl3 * 0.3);

    // Stronger near the stern, fading toward the tail.
    float swirlFade = distFade * behindMask * insideMask;
    // Earlier ramp + slight near-origin boost for a stronger beginning.
    float startRamp = smoothstep(0.0, 0.05, behindAmount);
    float startBoost = 1.0 + 0.35 * saturate(1.0 - behindAmount / 0.18);
    swirlFade *= startRamp * startBoost;

    // Grow the apparent swirl size further as the wake dissipates.
    float sizeGrowth = 1.0 + behindAmount * 2.2;
    float2 turbulence = swirlOffset * swirlFade * sizeGrowth * intensity * RippleAmplitude * 2.5;

    return vWake + turbulence;
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

    // V-wake trail from moving watercraft.
    offset += WakeDistortion(texCoord);

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

    // V-wake trail from moving watercraft.
    offset += WakeDistortion(texCoord);

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
