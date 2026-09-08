# Oculus.Platform

Standalone .NET bindings for Meta XR Platform SDK **205.0.0** and Meta Avatars SDK **40.0.1**, in the same assembly.
No Unity, XrEngine, OpenXR, or engine generator references. A local
`Directory.Build.props` isolates this project from the repository's build rules.
Targets `net11.0` and `net11.0-android`; the managed dependency is Newtonsoft.Json 13.0.3.

## Usage

```csharp
using Oculus.Platform;

// Windows, using the installed Meta PC runtime/account:
Core.Initialize(appId);
// For standalone PC testing of a mobile app:
// Core.Initialize(appId, "standalone", accessToken);

Users.GetLoggedInUser().OnComplete(message =>
{
    if (message.IsError)
        Console.Error.WriteLine(message.GetError().Message);
    else
        Console.WriteLine(message.Data.ID);
});

// Call regularly from the application's own update loop:
Request.RunCallbacks();
```

`Users.GetAccessToken()` provides the token needed by the separate Avatar SDK.
Do not log or persist that token. The Avatar bindings below consume that token
and expose native mesh, texture, skeleton and animation data.

On Android, initialize with `Core.Initialize(activity, appId)` on the Activity
thread. The bridge accesses the Horizon OS `HorizonPlatformCore` supplement using
.NET for Android Java reflection. The library manifest declares that supplement;
verify its declaration survives merging into the consuming application's APK.
This requires a compatible Meta Horizon OS device, not generic Android.

Requests support `OnComplete` and `await`. Queue pumping is explicit: awaiting a
request does not pump it. Create requests, register handlers, and pump callbacks
on one application-owned thread. Do not block that thread awaiting a request.
No background callback runner, telemetry initialization, or Unity settings assets
are created. Initialization is process-wide; switching accounts/app IDs within
one initialized process is not supported.

## Native files and provenance

- `Bindings/`: 207 C# files ported from `com.meta.xr.sdk.platform` 205.0.0, including
  generated clients, models, enums, options, converters and compatibility APIs.
- `Native/win-x64/LibOVRPlatformImpl64_1.dll`: copied from the local Meta PC runtime
  (`C:\Program Files\Oculus\Support\oculus-runtime`), file version **1.117.0.0**.
  The Unity package itself does not ship this DLL. It is a local runtime snapshot;
  a compatible installed Meta runtime/service is still required. This copy does
  not constitute a standalone runtime installer.
- `Native/win-x64/*.lib`: copied from the native SDK in
  Meta Quest Developer Hub's `odh/packages/lib/oculus-platform-sdk/Windows` folder.
  C# uses P/Invoke and does not link these import libraries. Windows execution is
  supported only in an x64 process in this port.
- `Native/android-arm64/libovrplatformloader.so`: copied from Unity Platform SDK 205.0.0.
  The current Java supplement transport is supplied by Horizon OS.
- `Native/win-x64/libovravatar2.dll` and `Native/android-arm64/libovravatar2.so`,
  plus the matching body, tracking and GPU skinning native libraries, come from
  the Avatar SDK 40.0.1 Unity package. Only Windows x64 and Android ARM64 binaries
  are included; no 32-bit targets are supported.
- `Licenses/`: native SDK license and third-party notices. Original source notices
  have been retained. Review Meta's terms before redistributing vendor binaries.

The Windows DLL is copied to build/publish output and the Android loaders are
included as native library items. The imported JSON model assembly is rooted for
Android trimming because its members are populated through reflection.

## Port differences and limitations

- Unity imports and preservation attributes removed; optional `PlatformLog.Logger`
  replaces Unity logging. `Core.LogMessages` is off by default.
- Explicit initialization replaces Unity editor/project settings and GameObjects.
- Windows initialization failures propagate; failed probes do not cache success.
- Error responses are not parsed as successful model JSON; malformed successful
  responses fault awaiting requests instead of leaving them pending.
- `Users.SendAuthUrl` returns `Request<JToken>` because the transport returns JSON,
  whereas the upstream generated declaration used Unity's `AndroidJavaObject`.
- Windows notification sessions throw `PlatformNotSupportedException` instead of
  the upstream silent no-op. Android has the supplement session implementation.
- Deprecated synchronous compatibility methods retain their upstream timeout
  behavior. Prefer asynchronous requests and explicit callback pumping.
- No live account request or Quest device execution has been verified yet.

## Verification

```text
dotnet build Oculus.Platform.csproj
dotnet run --project Tests/Oculus.Platform.Tests.csproj
```

The executable checks JSON user IDs/Unicode, error responses, await/callback
delivery, native export availability, and absence of engine assembly references.
It neither initializes an account nor sends network requests.

## Avatar native binding

`Oculus.Avatar2.CAPI` and `Oculus.Avatar2.Experimental.CAPI` are in **Oculus.Platform.dll**.
`Avatar/CAPI` contains 265 native declarations ported from the Unity package.
Native field order, layouts, enum values, calling conventions and marshaling
attributes are retained; Unity convenience methods, conversions and allocators
are omitted. Previously private native entry points are public so a caller can
use them directly. Experimental APIs retain their separate namespace.

`AvatarDefaults.CreateInitializeInfo` supplies the SDK's version and network
defaults, with native coordinates (+X right, +Y up, -Z forward). Set callback
delegates on the returned structure before passing it to `CAPI.ovrAvatar2_Initialize`.
Keep those delegates rooted until after `CAPI.ovrAvatar2_Shutdown` returns.

The native flow is:

1. Initialize the Platform API and retrieve the user ID and access token.
2. Initialize the Avatar runtime, supplying resource and request callbacks.
3. Call `AvatarDefaults.SetAccessToken(token)` (Oculus graph by default).
4. Create an entity using `ovrAvatar2Entity_Create`; select feature flags and
   render filters explicitly (`AvatarDefaults.FullBodyFilters` is available).
5. Call `AvatarDefaults.LoadUser(entityId, userId, out requestId)`. `Pending`
   indicates an asynchronous load, not a failure.
6. Call `ovrAvatar2_Update(deltaSeconds)` on the initialization thread. Inspect
   load status with `ovrAvatar2Asset_GetLoadRequestInfo` and handle resource events.
7. Read resource primitives/images with `ovrAvatar2Asset_GetPrimitiveByIndex`,
   `ovrAvatar2Asset_GetImageByIndex` and `ovrAvatar2Asset_GetImageDataByIndex`.
   Read vertices, UVs, indices, joints, weights and morph targets through the
   `ovrAvatar2VertexBuffer_*` and `ovrAvatar2Primitive_*` entry points.
8. Query `ovrAvatar2Render_QueryRenderState`, primitive render states,
   `ovrAvatar2Render_GetSkinTransforms` and `ovrAvatar2Render_GetMorphTargetWeights`
   for data to feed to the caller's renderer.
9. Destroy entities, then shut down the Avatar runtime.

These are low-level bindings: the caller owns buffer allocation, callback
lifetime, resource-ready/release acknowledgements, tracking input and runtime
lifetime. Buffer lengths are bytes where indicated by the original signature;
do not confuse them with element counts. Pointers returned by the runtime are
borrowed; copy required data before releasing resources or advancing the runtime.
Call `ovrAvatar2Asset_ResourceReadyToRender` after processing a loaded resource
and follow the SDK resource lifecycle before calling `ovrAvatar2Asset_ReleaseResource`.
Animation may additionally require the matching SDK behavior/rig assets and
tracking setup. The binding does not implement a renderer or an avatar export tool.

All 265 declarations were checked against the included Windows library's exports.
This verifies entry point availability, not a successful live avatar download.
