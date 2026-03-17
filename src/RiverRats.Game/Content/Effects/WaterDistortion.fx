//
// WaterDistortion.fx — Multi-layer sine-wave UV distortion for water tiles,
// with interactive click-ripple support.
// Compiled by MonoGame Content Pipeline (WindowsDX, Reach profile).
//

#define MAX_RIPPLES 8

// The scene texture containing water tiles already rendered.
sampler TextureSampler : register(s0);

// --- Ambient wave parameters ---
float Time;
float Amplitude;
float Frequency;
float Speed;
float2 CameraOffset;

// --- Click-ripple parameters ---
// Each element: xy = ripple centre in screen UV (texCoord) space, z = age in seconds.
float3 Ripples[MAX_RIPPLES];
int RippleCount;
float RippleAmplitude;  // UV displacement strength (~0.005).
float RippleFrequency;  // Tightness of concentric rings (~40).
float RippleSpeed;      // How fast rings expand outward (~15).
float AspectRatio;      // VirtualWidth / VirtualHeight, keeps rings circular.

float4 MainPS(float4 position : SV_Position, float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float t = Time * Speed;

    // Shift UVs by camera offset so the wave pattern is locked to world space.
    float2 worldUV = texCoord + CameraOffset;

    // Layer 1: broad, slow sway.
    float phase1 = worldUV.y * Frequency + t;
    float wave1X = sin(phase1) * Amplitude;
    float wave1Y = cos(worldUV.x * Frequency * 0.7 + t * 0.5) * Amplitude * 0.4;

    // Layer 2: tighter ripple offset in phase for organic look.
    float phase2 = worldUV.y * Frequency * 2.3 + t * 1.7 + 1.5;
    float wave2X = sin(phase2) * Amplitude * 0.35;

    float2 offset = float2(wave1X + wave2X, wave1Y);

    // --- Click ripples ---
    for (int i = 0; i < RippleCount; i++)
    {
        float2 center = Ripples[i].xy;
        float age = Ripples[i].z;

        // Aspect-corrected distance so ripples are circular on screen.
        float2 diff = texCoord - center;
        diff.x *= AspectRatio;
        float dist = length(diff);

        // Single ring pulse expanding outward (no oscillation).
        float radius = age * RippleSpeed * 0.01;
        float ringDist = dist - radius;
        // Narrow Gaussian centred on the expanding ring edge.
        float ring = exp(-ringDist * ringDist * RippleFrequency * RippleFrequency);

        // Fade with time.
        float timeFade = saturate(1.0 - age * 0.5);

        // Radial displacement direction (safe normalize).
        float2 dir = dist > 0.001 ? diff / dist : float2(0, 0);
        // Convert back from aspect-corrected space to texCoord space.
        dir.x /= AspectRatio;

        offset += dir * ring * timeFade * RippleAmplitude;
    }

    float2 distortedCoord = texCoord + offset;

    return tex2D(TextureSampler, distortedCoord) * color;
}

technique WaterDistortion
{
    pass P0
    {
        PixelShader = compile ps_4_0 MainPS();
    }
}
