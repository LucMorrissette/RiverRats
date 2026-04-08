//
// Clouds.fx — Procedural sky clouds for the fishing mini-game.
// Uses hash-based value noise with FBM (fractal Brownian motion) to generate
// soft, volumetric-looking clouds that drift horizontally across the sky.
//
// Compiled by MonoGame Content Pipeline (Reach profile, ps_3_0).
// No dynamic loops — octave count is fixed at 6 for MojoShader compatibility.
//

// Dummy texture sampler — SpriteBatch requires a bound texture.
sampler TextureSampler : register(s0);

// --- Parameters ---
float Time;           // Elapsed time in seconds.
float WindSpeed;      // Horizontal drift in UV units per second (~0.01).
float AspectRatio;    // Width / Height of the draw region for correct proportions.
float CloudScale;     // Base noise frequency multiplier (~4.0). Higher = smaller clouds.
float Coverage;       // Cloud coverage threshold (0–1). Higher = fewer clouds (~0.45).
float Softness;       // Edge softness band width (~0.25).
float Opacity;        // Overall cloud opacity (0–1, ~0.7).
float3 CloudColorA;   // Bright (sunlit) cloud color (top highlights).
float3 CloudColorB;   // Shadow cloud color (darker undersides).

// --- Hash-based 2D value noise (no texture dependency) ---

float hash(float2 p)
{
    return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
}

float valueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);

    // Quintic Hermite interpolation for smoother gradients.
    f = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

    float a = hash(i);
    float b = hash(i + float2(1.0, 0.0));
    float c = hash(i + float2(0.0, 1.0));
    float d = hash(i + float2(1.0, 1.0));

    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

// --- FBM with 6 manually unrolled octaves (MojoShader-safe) ---

float fbm(float2 p)
{
    float value = 0.0;

    // Octave 1
    value += 0.5    * valueNoise(p * 1.0);
    // Octave 2
    value += 0.25   * valueNoise(p * 2.0  + float2(1.7, 9.2));
    // Octave 3
    value += 0.125  * valueNoise(p * 4.0  + float2(5.3, 2.8));
    // Octave 4
    value += 0.0625 * valueNoise(p * 8.0  + float2(8.1, 4.7));
    // Octave 5
    value += 0.03125 * valueNoise(p * 16.0 + float2(3.6, 7.4));
    // Octave 6
    value += 0.015625 * valueNoise(p * 32.0 + float2(6.2, 1.3));

    return value;
}

float4 CloudPS(float2 texCoord : TEXCOORD0, float4 color : COLOR0) : COLOR0
{
    // Map UV to world-proportional coordinates so clouds aren't stretched.
    float2 uv = texCoord;
    uv.x *= AspectRatio;

    // Apply base scale.
    uv *= CloudScale;

    // Wind drift — clouds move left to right.
    uv.x -= Time * WindSpeed;

    // Two FBM samples at different scales for more interesting shapes.
    float n1 = fbm(uv);
    float n2 = fbm(uv * 0.5 + float2(4.3, 2.1) + Time * WindSpeed * 0.3);

    // Blend the two noise layers for billowy cloud shapes.
    float cloud = n1 * 0.6 + n2 * 0.4;

    // Vertical fade: clouds are denser near the top, thin out toward the bottom.
    float verticalFade = 1.0 - smoothstep(0.0, 0.85, texCoord.y);

    // Apply coverage threshold with soft edges.
    float density = smoothstep(Coverage - Softness, Coverage + Softness, cloud) * verticalFade;

    // Shade: brighter on top (sunlit), darker on bottom (shadow).
    // Use noise derivative approximation: compare to a slightly offset sample.
    float shading = smoothstep(0.3, 0.7, n1);
    float3 cloudColor = lerp(CloudColorB, CloudColorA, shading);

    float alpha = density * Opacity;

    return float4(cloudColor * alpha, alpha);
}

technique Clouds
{
    pass P0
    {
        PixelShader = compile ps_3_0 CloudPS();
    }
}
