# sRGB Rendering Notes

This document records the empirically verified sRGB behavior across the engine's rendering backends and the shader conventions used to keep color handling consistent.

## Backend / Render Target Rules

### Android / Native Quest OpenXR

The OpenXR swapchain **must be sRGB**.

### OpenXR Link

The swapchain can be either linear or sRGB.

A linear swapchain is eventually sRGB-encoded before presentation by the Link presentation path.

### DX Interop Editor

The DX interop render target **must be sRGB**.

Shader-side sRGB encoding must still be performed even when `GL_FRAMEBUFFER_SRGB` is enabled.

This backend therefore uses `GlRenderTargetFlags.ForceSrgbEncode`.

### Normal WGL Window

The render target must be sRGB.

---

# Shader Context Flags

The global shader update handler (`GlProgramGlobal` - `ContextShaderHandler`) emits:

```csharp
if (bld.Context.IsSrgbAutoEncode)
    bld.AddFeature("SRGB_AUTO_ENCODE");

if (bld.Context.IsSrgbTarget)
    bld.AddFeature("SRGB_TARGET");

if (bld.Context.NeedSrgbEncode)
    bld.AddFeature("SRGB_ENCODE");

if (OpenGLRender.Current!.Options.UseHighQualitySrgb)
    bld.AddFeature("HIGH_QUALITY_SRGB");
```

Semantics:

```text
IsSrgbAutoEncode
    GL_FRAMEBUFFER_SRGB is enabled.

IsSrgbTarget
    The render target expects sRGB values.

NeedSrgbEncode
    Shader must perform linear -> sRGB encoding.
```

`NeedSrgbEncode` is:

```csharp
IsSrgbTarget && !IsSrgbAutoEncode
```

## ForceSrgbEncode

If the render target has:

```csharp
GlRenderTargetFlags.ForceSrgbEncode
```

the context is forced to:

```csharp
_updateCtx.IsSrgbTarget = true;
_updateCtx.IsSrgbAutoEncode = false;
```

Meaning:

> Regardless of the actual framebuffer flag or GL-visible format, assume automatic framebuffer sRGB encoding is unavailable and the destination expects sRGB values.

This is required for DX interop.

---

# Color Inputs

All normal engine `Color` values are assumed to be **sRGB by default**.

A plain color has no texture object behind it, so OpenGL cannot automatically decode it the way it can decode an sRGB texture. The shader therefore has to reconcile two independent facts:

```text
1. What color space is the input value expressed in?
2. What color space must be written to the current render target?
```

That is what `toneMapColor` does.

```glsl
#if defined(COLOR_IS_SRGB) && !defined(SRGB_ENCODE)
    color.rgb = sRGBToLinear(color.rgb);
#endif

#if !defined(COLOR_IS_SRGB) && defined(SRGB_ENCODE)
    color.rgb = linearTosRGB(color.rgb);
#endif
```


So the function is effectively a color-space bridge:

| Input | Shader must write | Operation |
|---|---|---|
| sRGB | linear | `sRGBToLinear` |
| linear | sRGB | `linearTosRGB` |
| sRGB | sRGB | pass through |
| linear | linear | pass through |

This is deliberately based on **input semantic vs required shader output**, not simply on whether `GL_FRAMEBUFFER_SRGB` happens to be enabled.

---


# Plain Texture Materials

If the texture must be sRGB-aware:

1. Call `PrepareTexture` during shader update.
2. Bind it through `LoadTextureFixSrgb`.
3. The fragment shader calls `toneMapTex(FragColor)`.

## PrepareTexture

`PrepareTexture` emits:

```text
TEXTURE_IS_SRGB
```

when the texture is sRGB.

## LoadTextureFixSrgb

`LoadTextureFixSrgb` installs a sampler that disables automatic sRGB texture decoding when all three conditions are true:

```text
render target is sRGB
texture is sRGB
GL_FRAMEBUFFER_SRGB is OFF
```

This creates the intended pass-through path:

```text
stored sRGB texture values
    -> no texture decode
    -> no framebuffer encode
    -> same sRGB values written to sRGB target
```

## toneMapTex

For every other plain-texture case, the fragment shader calls:

```glsl
toneMapTex(FragColor);
```

with:

```glsl
#if !defined(TEXTURE_IS_SRGB) && defined(SRGB_ENCODE)
    color.rgb = linearTosRGB(color.rgb);
#endif
```

## Why `toneMapTex` is different from `toneMapColor`

Textures have one extra stage that plain colors do not have: **hardware texture decoding**.

For an sRGB texture, normal sampling already performs:

```text
stored sRGB texel
    -> hardware sRGB decode
    -> linear value returned by texture()
```

Therefore the shader usually must **not** call `sRGBToLinear` again. Doing so would double-decode the texture and make it much too dark.

That is why `toneMapTex` only needs to handle this case:

```text
texture is linear
+
shader must write sRGB
```

which becomes:

```text
linear texture sample
    -> linearTosRGB
    -> sRGB target
```

For a normal sRGB texture:

```text
stored sRGB texture
    -> automatic texture decode
    -> linear shader value
```

and if the destination is linear, nothing more is required.

If the destination requires sRGB encoding, that normally happens either through the framebuffer or through the shader's output encoding policy.

### Explicit pass-through case

`LoadTextureFixSrgb` exists for the special case where doing the normal decode/encode round-trip is unnecessary:

```text
texture is sRGB
target is sRGB
GL_FRAMEBUFFER_SRGB is OFF
```

In that case the sampler disables automatic texture decode:

```text
stored sRGB texture
    -> no texture decode
    -> raw sRGB value
    -> no framebuffer encode
    -> same sRGB value in destination
```

This is an exact semantic pass-through.

The important distinction is therefore:

```text
Color:
    no automatic decode exists
    -> shader may need to decode it

Texture:
    sRGB decode normally already happened in texture()
    -> shader must not decode it again
```

---


# sRGB Conversion Quality

The global shader update also emits:

```text
HIGH_QUALITY_SRGB
```

When enabled, use the real piecewise sRGB transfer functions instead of the faster gamma approximation.

Approximate:

```glsl
const float gamma = 2.2;
const float inv_gamma = 1.0 / gamma;

vec3 sRGBToLinear(vec3 color)
{
    return pow(color, vec3(gamma));
}

vec3 linearTosRGB(vec3 color)
{
    return pow(color, vec3(inv_gamma));
}
```

High-quality:

```glsl
vec3 sRGBToLinear(vec3 c)
{
    bvec3 cutoff = lessThanEqual(c, vec3(0.04045));

    vec3 low = c / 12.92;
    vec3 high = pow((c + 0.055) / 1.055, vec3(2.4));

    return mix(high, low, cutoff);
}

vec3 linearTosRGB(vec3 c)
{
    c = max(c, vec3(0.0));

    bvec3 cutoff = lessThanEqual(c, vec3(0.0031308));

    vec3 low = c * 12.92;
    vec3 high = 1.055 * pow(c, vec3(1.0 / 2.4)) - 0.055;

    return mix(high, low, cutoff);
}
```

---

# Mental Model

Keep these concepts separate:

```text
1. Input semantic
   - Is this Color or texture defined/stored as sRGB?

2. Texture sampling
   - Is hardware sRGB decode active or explicitly disabled?

3. Shader working space
   - PBR and other lighting math must operate in linear space.

4. Render target semantic
   - Does the destination expect sRGB values?

5. Framebuffer conversion
   - Is GL_FRAMEBUFFER_SRGB actually performing the encode?

6. Backend presentation behavior
   - Native Quest, Link, DX interop and normal WGL do not behave identically.
```

Do not collapse these into a single generic "sRGB on/off" state.

---

# Keep `GL_FRAMEBUFFER_SRGB` Enabled

There is no practical reason in the current engine design to disable `GL_FRAMEBUFFER_SRGB` globally.

The engine already has enough information to decide whether shader-side encoding is required:

```text
IsSrgbAutoEncode
    GL_FRAMEBUFFER_SRGB is enabled.

IsSrgbTarget
    The render target expects sRGB values.

NeedSrgbEncode
    IsSrgbTarget && !IsSrgbAutoEncode
```

So the preferred policy is:

```text
keep GL_FRAMEBUFFER_SRGB enabled
+
describe the real destination semantics through the render context
+
use ForceSrgbEncode only for backends where framebuffer state does not reflect the actual presentation behavior
```

With a normal sRGB render target, keeping framebuffer sRGB enabled gives the expected hardware linear -> sRGB conversion.

With a linear render target, enabling `GL_FRAMEBUFFER_SRGB` does not create an sRGB target by itself; the attachment format/encoding still determines whether the conversion applies.

For DX interop, the special case is already represented explicitly:

```csharp
_updateCtx.IsSrgbTarget = true;
_updateCtx.IsSrgbAutoEncode = false;
```

through:

```csharp
GlRenderTargetFlags.ForceSrgbEncode
```

That tells shaders to encode manually even if the real GL state has `GL_FRAMEBUFFER_SRGB` enabled.

Therefore disabling framebuffer sRGB just to make a particular pass work would only create another global state combination to reason about. The engine should instead keep the state stable and express exceptional behavior through the render-target semantics and shader features.

In other words:

```text
Do not use GL_FRAMEBUFFER_SRGB OFF as a general color-management mode.
Use target/input semantics to decide what conversion is required.
```

---

# Empirically Verified Backend Summary

| Backend | Target / Swapchain | Required behavior |
|---|---|---|
| Native Quest / Android OpenXR | sRGB | Swapchain must be sRGB |
| OpenXR Link | Linear or sRGB | Linear output is eventually encoded before presentation |
| DX interop editor | sRGB | Shader must encode even if framebuffer sRGB is enabled |
| Normal WGL window | sRGB | Use an sRGB render target |
