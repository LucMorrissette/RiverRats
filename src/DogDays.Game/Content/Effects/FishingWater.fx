//
// FishingWater.fx — Side-view fishing scene water effects.
// Combines UV distortion, event-driven ripples, underwater caustics,
// and expanding splash highlight rings.
// Compiled by MonoGame Content Pipeline (DesktopGL, Reach profile).
// MojoShader-compatible: no dynamic loops, manually unrolled accumulation.
//

sampler TextureSampler : register(s0);

// --- Timing ---
float Time;

// --- Ambient wave parameters ---
float Amplitude;   // UV displacement strength (~0.003).
float Frequency;   // Wave tightness (~20).
float Speed;       // Animation speed (~1.2).

// --- Water surface threshold ---
// Y position of the water surface in UV space (0–1).
// Effects only apply below this line.
float WaterSurfaceV;

// --- Aspect ratio ---
// VirtualWidth / VirtualHeight — keeps ripple rings circular.
float AspectRatio;

// --- Click-ripple parameters ---
// Each element: xy = centre in screen UV space, z = age in seconds.
// Age < 0 means inactive.
float3 Ripple0;
float3 Ripple1;
float3 Ripple2;
float3 Ripple3;
float3 Ripple4;
float3 Ripple5;
float3 Ripple6;
float3 Ripple7;
float RippleAmplitude;   // UV displacement for event ripples (~0.012).
float RippleFrequency;   // Ring tightness (~35).
float RippleSpeed;       // Ring expansion speed (~20).

// --- Splash highlight parameters ---
// Each element: xy = centre in screen UV, z = age in seconds.
// Age < 0 means inactive. Max 4 simultaneous splash highlights.
float3 Splash0;
float3 Splash1;
float3 Splash2;
float3 Splash3;
float SplashBrightness;  // Additive glow intensity (~0.5).
float SplashRingSpeed;   // How fast the ring expands (~25).

// --- Spook ring parameters ---
// Red-tinted expanding rings for bad casts. Same layout as splashes.
// Max 4 simultaneous spook rings. Age < 0 = inactive.
float3 Spook0;
float3 Spook1;
float3 Spook2;
float3 Spook3;
float SpookBrightness;   // Additive red glow intensity (~0.55).
float SpookRingSpeed;    // Expansion speed (~12, slower than splash).

// --- Caustic parameters ---
float CausticIntensity;  // Brightness of caustic pattern (~0.08).
float CausticScale;      // Pattern scale (~8).

// ---- Helpers ----

// Accumulate UV displacement from a single ripple (no branching).
float2 RippleOffset(float3 ripple, float2 texCoord)
{
    float2 center = ripple.xy;
    float age = ripple.z;

    float active = step(0.0, age);

    float2 diff = texCoord - center;
    diff.x *= AspectRatio;
    float dist = length(diff) + 0.0001;

    float radius = age * RippleSpeed * 0.01;
    float ringDist = dist - radius;
    float ring = exp(-ringDist * ringDist * RippleFrequency * RippleFrequency);

    float timeFade = saturate(1.0 - age * 0.5);

    float2 dir = diff / dist;
    dir.x /= AspectRatio;

    return dir * ring * timeFade * RippleAmplitude * active;
}

// Compute additive brightness from a single splash highlight ring.
float SplashHighlight(float3 splash, float2 texCoord)
{
    float2 center = splash.xy;
    float age = splash.z;

    float active = step(0.0, age);

    float2 diff = texCoord - center;
    diff.x *= AspectRatio;
    float dist = length(diff) + 0.0001;

    float radius = age * SplashRingSpeed * 0.01;
    float ringWidth = 0.008 + age * 0.006; // ring widens as it expands
    float ring = exp(-(dist - radius) * (dist - radius) / (ringWidth * ringWidth));

    float fade = saturate(1.0 - age * 1.2);

    return ring * fade * active;
}

// Compute additive brightness from a single spook ring (red warning wave).
// Slower expansion, wider ring, longer fade than splash highlights.
float SpookRing(float3 spook, float2 texCoord)
{
    float2 center = spook.xy;
    float age = spook.z;

    float active = step(0.0, age);

    float2 diff = texCoord - center;
    diff.x *= AspectRatio;
    float dist = length(diff) + 0.0001;

    float radius = age * SpookRingSpeed * 0.01;
    float ringWidth = 0.012 + age * 0.010; // wider ring for ominous feel
    float ring = exp(-(dist - radius) * (dist - radius) / (ringWidth * ringWidth));

    float fade = saturate(1.0 - age * 0.6); // slower fade than splash

    return ring * fade * active;
}

// Simple procedural caustic pattern using two overlapping sine grids.
float Caustic(float2 uv, float t)
{
    float2 uv1 = uv + float2(t * 0.3, t * 0.2);
    float2 uv2 = uv * 0.75 + float2(-t * 0.25, t * 0.15);

    float c1 = sin(uv1.x) * sin(uv1.y);
    float c2 = sin(uv2.x + 1.5) * sin(uv2.y + 0.7);

    // Combine and bias so output is mostly positive with bright caustic lines.
    return saturate((c1 + c2) * 0.5 + 0.5);
}

float4 MainPS(float2 texCoord : TEXCOORD0, float4 color : COLOR0) : COLOR0
{
    // Water mask: 1.0 below water surface, 0.0 above.
    float waterMask = step(WaterSurfaceV, texCoord.y);

    float t = Time * Speed;

    // --- Ambient wave distortion (only underwater) ---
    float phase1 = texCoord.y * Frequency + t;
    float wave1X = sin(phase1) * Amplitude;
    float wave1Y = cos(texCoord.x * Frequency * 0.7 + t * 0.5) * Amplitude * 0.4;

    float phase2 = texCoord.y * Frequency * 2.3 + t * 1.7 + 1.5;
    float wave2X = sin(phase2) * Amplitude * 0.35;

    float2 offset = float2(wave1X + wave2X, wave1Y) * waterMask;

    // --- Ripple distortion (surface ripples extend slightly above) ---
    // Allow ripples to show 1% above the water line for surface-ring visibility.
    float rippleMask = step(WaterSurfaceV - 0.01, texCoord.y);

    float2 rippleTotal = float2(0.0, 0.0);
    rippleTotal += RippleOffset(Ripple0, texCoord);
    rippleTotal += RippleOffset(Ripple1, texCoord);
    rippleTotal += RippleOffset(Ripple2, texCoord);
    rippleTotal += RippleOffset(Ripple3, texCoord);
    rippleTotal += RippleOffset(Ripple4, texCoord);
    rippleTotal += RippleOffset(Ripple5, texCoord);
    rippleTotal += RippleOffset(Ripple6, texCoord);
    rippleTotal += RippleOffset(Ripple7, texCoord);
    offset += rippleTotal * rippleMask;

    float2 distortedCoord = texCoord + offset;
    float4 scene = tex2D(TextureSampler, distortedCoord) * color;

    // --- Underwater caustic highlights ---
    // Depth factor: caustics intensify further below the surface.
    float depth = saturate((texCoord.y - WaterSurfaceV) / (1.0 - WaterSurfaceV));
    float causticVal = Caustic(texCoord * CausticScale, Time) * CausticIntensity * depth * waterMask;
    scene.rgb += causticVal * scene.a;

    // --- Splash highlight rings (additive glow at surface) ---
    float splashTotal = 0.0;
    splashTotal += SplashHighlight(Splash0, texCoord);
    splashTotal += SplashHighlight(Splash1, texCoord);
    splashTotal += SplashHighlight(Splash2, texCoord);
    splashTotal += SplashHighlight(Splash3, texCoord);
    scene.rgb += splashTotal * SplashBrightness * rippleMask * scene.a;

    // --- Spook rings (red warning wave for bad casts) ---
    float spookTotal = 0.0;
    spookTotal += SpookRing(Spook0, texCoord);
    spookTotal += SpookRing(Spook1, texCoord);
    spookTotal += SpookRing(Spook2, texCoord);
    spookTotal += SpookRing(Spook3, texCoord);
    scene.rgb += float3(spookTotal, spookTotal * 0.15, spookTotal * 0.05) * SpookBrightness * rippleMask * scene.a;

    return scene;
}

technique FishingWater
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}
