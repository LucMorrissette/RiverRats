//
// OcclusionReveal.fx — Circular alpha-fade lens for revealing the player (and follower)
// behind occluding objects.
// Compiled by MonoGame Content Pipeline (WindowsDX, Reach profile).
// MojoShader-compatible: no dynamic loops or branching on non-uniform values.
//

// The occluder render target containing entities drawn in front of the characters.
sampler TextureSampler : register(s0);

// Player centre in screen UV space (0–1). Set each frame by the renderer.
float2 PlayerCenter;

// Follower centre in screen UV space (0–1). Set to (-1, -1) when follower reveal is inactive.
float2 FollowerCenter;

// Radius of the reveal circle in UV space. Adjusted for aspect ratio in shader.
float RevealRadius;

// Width of the soft edge as a fraction of RevealRadius (0 = hard edge, 1 = fully soft).
float EdgeSoftness;

// VirtualWidth / VirtualHeight — keeps the reveal circle round on non-square viewports.
float AspectRatio;

// Minimum alpha inside the reveal circle (0 = fully transparent, 1 = no effect).
float MinAlpha;

// Returns the fade factor (0 = fully revealed, 1 = fully opaque) for a single lens centre.
float LensFade(float2 texCoord, float2 center)
{
    float2 diff = texCoord - center;
    diff.x *= AspectRatio;
    float dist = length(diff);
    float innerRadius = RevealRadius * (1.0 - EdgeSoftness);
    return smoothstep(innerRadius, RevealRadius, dist);
}

float4 MainPS(float2 texCoord : TEXCOORD0, float4 color : COLOR0) : COLOR0
{
    float4 texColor = tex2D(TextureSampler, texCoord);

    // Early-out for empty pixels (no occluder drawn here).
    if (texColor.a < 0.004)
    {
        return float4(0, 0, 0, 0);
    }

    // Player lens: fade is 0 at centre, 1 outside.
    float fade = LensFade(texCoord, PlayerCenter);

    // Follower lens: take the minimum fade (most-revealed) of the two lenses.
    // FollowerCenter.x < 0 signals the follower lens is inactive, so we skip it.
    if (FollowerCenter.x >= 0.0)
    {
        float followerFade = LensFade(texCoord, FollowerCenter);
        fade = min(fade, followerFade);
    }

    // Lerp alpha between MinAlpha (centre of lens) and full opacity (outside lens).
    float alpha = lerp(MinAlpha, 1.0, fade);

    // Multiply ALL channels — textures are premultiplied alpha, so RGB must scale
    // with A to avoid invalid bright pixels.
    texColor *= alpha;
    return texColor * color;
}

technique OcclusionReveal
{
    pass P0
    {
        PixelShader = compile ps_3_0 MainPS();
    }
}
