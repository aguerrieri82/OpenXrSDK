# OpenXrSDK

**A code-first 3D engine specialized for XR/VR, built around OpenXR and designed primarily for Meta Quest.**

OpenXrSDK is a modular real-time 3D engine written in C#/.NET.

Scenes, objects, components, materials and behavior are defined directly in C#. Desktop tooling focuses on inspection, preview and debugging rather than visual authoring.

Meta Quest is the primary runtime target, with Windows also supported for development, debugging, tooling and desktop execution.

---

## Why

OpenXrSDK started from a practical problem: standalone VR leaves little performance headroom, and iteration speed matters as much as runtime efficiency.

General-purpose engines such as Unity and Unreal offer comprehensive tooling, but that generality also brings complexity and longer iteration cycles. This project needed a tighter loop: change code, deploy to the headset, debug directly on the device, profile the actual hardware and optimize against its real constraints.

Meta Quest was chosen as the primary standalone XR platform for its combination of hardware capabilities, broad commercial availability, affordability, OpenXR support and access to platform-specific XR features.

Building the engine directly keeps that loop short and makes low-level rendering and runtime decisions accessible when needed.

---

## Code first

OpenXrSDK treats code as the primary authoring format.

The relationship between code and tooling is similar to web development: visual tools can inspect and manipulate the result without becoming the authored representation.

Scenes, objects, components, materials and behavior are defined through normal C# APIs. The API favors a fluent style where it improves readability, keeping scene construction and behavior concise while remaining ordinary code that can be diffed, searched, refactored and composed.


### Hello world

A complete Quest entry point can stay small:

```csharp
public class MainActivity : XrEngineActivity
{
    protected override void BuildApp(XrEngineAppBuilder builder)
    {
        var app = new EngineApp();
        var scene = new Scene3D();

        scene.ActiveCamera = new PerspectiveCamera
        {
            Near = 0.01f,
            Far = 100f,
            BackgroundColor = new Color(0, 0, 0, 1)
        };

        scene.AddChild(new SunLight
        {
            Intensity = 1f,
            Direction = Vector3.Normalize(new Vector3(-1, -1, -1))
        });

        var cube = scene.AddChild(new TriangleMesh(
            Cube3D.Default,
            (Material)MaterialFactory.CreatePbr("#ff0000")));

        cube.Transform.SetScale(0.15f);
        cube.Transform.Position = new Vector3(0, 1.5f, -1f);

        cube.Animate()
            .Target(x => x.Transform.Rotation)
            .From(Vector3.Zero)
            .ToLinear(new Vector3(0, MathF.PI * 2, 0), 2f)
            .Loop()
            .Create()
            .Play();
        app.OpenScene(scene);

        builder
            .UseApp(app)
            .UseOpenGL()
            .UseOculus()
            .UseMultiView()
            .AddXrRoot()
            .SetRenderQuality(1f, 4);
    }
}
```

No scene file or visual authoring step is involved: the application, XR runtime configuration, scene and animation are all defined directly in C#.

---

## Editor

OpenXrSDK includes a desktop editor focused on inspection, preview and debugging. It is not intended as a visual authoring tool, and project content remains defined in code.

### Live inspection

The editor exposes the live engine state through a visual interface. Developers can navigate the scene hierarchy, select objects, inspect components and properties, preview materials, textures and other assets, and modify exposed values while immediately observing their effect.

### XR preview

The editor can link to a running XR instance, allowing a scene to be inspected and tuned from the desktop while the result is viewed in VR.

### Persistence model

The editor deliberately does not write scene or asset state back into the project. Changes made through the editor are for inspection and experimentation, while C# remains the authored representation.

---

## Supported features

### Rendering, lighting & materials

#### Rendering

OpenXrSDK uses a custom forward renderer built on OpenGL/OpenGL ES. Physically based rendering is the primary material path for native engine materials and imported glTF content.

#### Lighting

The standard PBR lighting path supports:

- Image-based lighting (IBL)
- Directional lights
- Point lights
- Spot lights

#### Materials

The primary material model follows the metallic/roughness PBR workflow, with normal, ambient-occlusion and emissive maps. Specialized material features are documented in the glTF extension support below.

#### Shadows

Shadow maps support PCF and VSM filtering. Shadow support is currently limited to directional lights.

### Animation & deformation

#### Animation

OpenXrSDK includes a general property animation system for animating engine values directly from C#. Targets, interpolation, timing, looping and playback are defined through the fluent animation API.

#### Skeletal animation & morph targets

Imported glTF assets support skeletal animation with GPU skinning and morph target animation.

#### Inverse kinematics

Inverse kinematics supports articulated chains and pose control.

### Physics

Rigid-body physics is provided through PhysX and integrated with the engine scene/component model.

The physics layer covers rigid bodies, colliders, joints and collision handling. XR interaction uses the same simulation, allowing grabbed objects to transition naturally into rigid-body motion when released.

### Spatial audio

OpenXrSDK uses OpenAL for scene-based spatial audio.

Audio emitters are attached to scene objects and positioned in world space. The listener follows the active listener or camera state, providing positional playback and distance-based spatialization.

### UI

OpenXrSDK provides two complementary UI paths.

#### Web / browser UI

Rich XR interfaces can be built with web technologies and hosted through an embedded browser surface. On Android and Quest, a WebView is rendered into an OpenXR surface/quad layer and receives XR pointer and button input as browser events.

The browser integration includes a bidirectional C# / JavaScript bridge for communication between application code and the web UI. Requests can also be intercepted by the engine, allowing UI assets and application-specific resources to be served without an external web server.

#### Basic UI framework

For interfaces that do not require a browser, the engine also includes a lightweight UI framework for simple in-engine and editor interfaces, with buttons, toggle buttons, check boxes, sliders, text, icons, content views and plotting support.

### OpenXR

**Tested runtimes:** Meta Quest 3 (standalone), Meta Quest Link, Monado, and Meta XR Simulator through the ANGLE backend.

Core OpenXR integration includes actions, spaces, swapchains, projection/quad layers and session lifecycle, plus the following extensions when exposed by the runtime:

| Extension | Support |
| --- | --- |
| `XR_EXT_performance_settings` | CPU/GPU performance levels |
| `XR_EXT_hand_tracking` | Hand joints and tracking |
| `XR_EXT_hand_interaction` | Standard hand interaction profile |
| `XR_EXT_debug_utils` | Runtime debug messages |
| `XR_KHR_visibility_mask` | Per-eye hidden/visible area mesh |
| `XR_KHR_composition_layer_depth` | Projection depth submission |
| `XR_KHR_locate_spaces` | Batched space location |
| `XR_KHR_convert_timespec_time` | XR/system time conversion |
| `XR_KHR_win32_convert_performance_counter_time` | XR/Win32 time conversion |

#### Platform and graphics bindings

| Extension | Support |
| --- | --- |
| `XR_KHR_loader_init` | OpenXR loader initialization |
| `XR_KHR_android_thread_settings` | Android XR thread roles |
| `XR_KHR_android_create_instance` | Android instance bootstrap |
| `XR_KHR_android_surface_swapchain` | Android `Surface` swapchains |
| `XR_FB_android_surface_swapchain_create` | Meta Android surface creation |
| `XR_FB_composition_layer_image_layout` | Layer image orientation |
| `XR_KHR_opengl_enable` | Desktop OpenGL binding |
| `XR_KHR_opengl_es_enable` | Android OpenGL ES binding |
| `XR_KHR_vulkan_enable` | Vulkan graphics binding |
| `XR_META_vulkan_swapchain_create_info` | Extra Vulkan swapchain flags |
| `XR_KHR_swapchain_usage_input_attachment_bit` | Vulkan input-attachment swapchains |

ANGLE uses the Vulkan OpenXR graphics binding while exposing the engine's OpenGL ES renderer through ANGLE.

#### Meta Quest

| Extension | Support |
| --- | --- |
| `XR_FB_scene` | Scene semantics and bounds |
| `XR_FB_scene_capture` | Room capture requests |
| `XR_FB_triangle_mesh` | Scene triangle meshes |
| `XR_FB_spatial_entity` | Spatial entities / anchors |
| `XR_FB_spatial_entity_container` | Entity containment |
| `XR_FB_spatial_entity_storage` | Persistent spatial entities |
| `XR_FB_spatial_entity_query` | Spatial entity queries |
| `XR_META_spatial_entity_discovery` | Spatial entity discovery |
| `XR_META_spatial_entity_mesh` | Spatial entity meshes |
| `XR_FB_hand_tracking_mesh` | Runtime hand mesh |
| `XR_FB_hand_tracking_capsules` | Hand collision capsules |
| `XR_FB_hand_tracking_aim` | Aim and pinch state |
| `XR_EXT_hand_tracking_data_source` | Hand tracking source selection |
| `XR_META_hand_tracking_wide_motion_mode` | Wide-motion hand tracking |
| `XR_META_hand_tracking_frequency_hint` | Hand tracking frequency hint |
| `XR_META_hand_tracking_unextrapolated_poses` | Unextrapolated hand poses |
| `XR_META_simultaneous_hands_and_controllers` | Hands + controllers together |
| `XR_META_touch_controller_plus` | Touch Plus interaction profile |
| `XR_FB_haptic_pcm` | PCM haptics |
| `XR_FB_display_refresh_rate` | Refresh-rate control |
| `XR_FB_foveation` | Foveated rendering |
| `XR_FB_foveation_configuration` | Foveation configuration |
| `XR_FB_swapchain_update_state` | Swapchain state updates |
| `XR_FB_swapchain_update_state_opengl_es` | GLES swapchain updates |
| `XR_FB_space_warp` | Application SpaceWarp |
| `XR_FB_composition_layer_depth_test` | Compositor depth testing |
| `XR_FB_color_space` | Headset color-space control |
| `XR_FB_passthrough` | Passthrough composition |
| `XR_FB_passthrough_keyboard_hands` | Passthrough keyboard/hand support |
| `XR_META_environment_depth` | Environment depth |
| `XR_META_recommended_layer_resolution` | Runtime resolution recommendation |

### Assets

`Read` and `Write` refer to functionality implemented in the current codebase. Write support does not necessarily imply lossless round-trip preservation of every source feature.

#### 3D and materials

| Format | Read | Write | Source / notes |
| --- | :---: | :---: | --- |
| glTF 2.0 (`.gltf`, `.glb`) | ✅ | WIP | `glTFLoader` schema/parser + custom engine conversion. The exporter scaffold exists, but geometry export is not complete |
| Wavefront OBJ (`.obj`) | ✅ | ✅ | Native engine reader/writer for mesh geometry, normals and UVs |
| Quixel / Megascans material JSON | ✅ | — | Native material importer that maps Quixel PBR textures into engine materials |

glTF import covers scene hierarchy, meshes, cameras, punctual lights, metallic/roughness PBR materials, skins, morph targets, animations, material variants and node visibility.

##### glTF extensions

| Extension | Status | Support / source |
| --- | :---: | --- |
| `KHR_texture_transform` | ✅ | Texture UV transforms |
| `KHR_draco_mesh_compression` | ✅ | Draco mesh decoding through `Draco.Native` |
| `EXT_texture_webp` | ✅ | WebP texture sources decoded through SkiaSharp |
| `KHR_texture_basisu` | ✅ | BasisU/KTX2 texture sources, including supercompression |
| `KHR_lights_punctual` | ✅ | Directional, point and spot lights |
| `KHR_materials_clearcoat` | ✅ | Clearcoat factor, roughness and normal textures |
| `KHR_materials_ior` | ✅ | Index of refraction |
| `KHR_materials_transmission` | ✅ | Transmission factor and texture |
| `KHR_materials_volume` | ✅ | Thickness and attenuation, including thickness texture |
| `KHR_materials_sheen` | ✅ | Sheen color and roughness factors and textures |
| `KHR_materials_iridescence` | ✅ | Factor, IOR, thickness range and textures |
| `KHR_materials_emissive_strength` | ✅ | Emissive intensity scaling |
| `KHR_materials_specular` | ✅ | Specular factor/color and textures |
| `KHR_materials_dispersion` | ✅ | Chromatic dispersion |
| `KHR_materials_anisotropy` | ✅ | Anisotropy strength, rotation and texture |
| `KHR_materials_variants` | ✅ | Material variants and primitive mappings |
| `KHR_node_visibility` | ✅ | Per-node visibility |
| `KHR_materials_pbrSpecularGlossiness` | ◐ | Recognized by the loader, but material conversion is incomplete |

#### Images and textures

| Format | Read | Write | Source / notes |
| --- | :---: | :---: | --- |
| PNG (`.png`) | ✅ | ✅* | Native PNG bridge with RGBA8 and Gray16 encoding |
| JPEG (`.jpg`) | ✅ | ✅* | TurboJPEG |
| WebP (`.webp`) | ✅ | — | SkiaSharp |
| BMP (`.bmp`) | ✅ | — | SkiaSharp |
| Radiance HDR (`.hdr`) | ✅ | — | Native RGBE parser |
| OpenEXR (`.exr`) | ✅ | — | SharpEXR |
| TIFF (`.tif`) | ✅ | — | libtiff bridge |
| DDS (`.dds`) | ✅ | — | Native parser for BC1/BC3/BC7 and selected uncompressed/float formats |
| KTX (`.ktx`) | ✅ | — | Native parser with the current ETC2 subset |
| KTX2 (`.ktx2`) | ✅ | — | Native parser with supercompression support |
| PKM (`.pkm`) | ✅ | — | Native ETC2 reader |
| PVR (`.pvr`) | ✅ | ✅ | Native reader/writer for ETC1/ETC2, ASTC and selected uncompressed/float formats |

\* Available as direct encoding utilities rather than a generic asset-export pipeline.

#### Texture compression

| Codec | Encode | Source |
| --- | :---: | --- |
| ASTC | ✅ | `astcencoder-native` / ASTC Encoder integration |
| ETC2 | ✅ | `etcpack` native integration |

---

## Project status & contributing

OpenXrSDK is under active development and is not yet intended as a stable public SDK. APIs may change significantly as the architecture evolves. Backward compatibility and migration guarantees are not currently design goals.

Contributions, bug reports and alternative approaches are welcome. The project is open to substantial changes when they improve the engine, while overall technical direction and final integration decisions remain maintainer-led to preserve architectural coherence.

