#!/bin/bash
set -e

ROOT="$HOME/src"
ANGLE_DIR="$ROOT/angle"
DEPOT_TOOLS_DIR="$ROOT/depot_tools"
OUT_DIR="out/android-arm64"
DIST_DIR="$1"

ANGLE_REPO="https://github.com/aguerrieri82/angle.git"
ANGLE_BRANCH="${2:-main}"

export GCLIENT_SUPPRESS_GIT_VERSION_WARNING=1

mkdir -p "$ROOT"

#
# depot_tools
#

if [ ! -d "$DEPOT_TOOLS_DIR/.git" ]; then
    echo "Cloning depot_tools..."

    git clone \
        https://chromium.googlesource.com/chromium/tools/depot_tools.git \
        "$DEPOT_TOOLS_DIR"
else
    echo "Updating depot_tools..."

    git -C "$DEPOT_TOOLS_DIR" fetch origin
    git -C "$DEPOT_TOOLS_DIR" checkout -f origin/main
fi

#
# ANGLE
#

if [ ! -d "$ANGLE_DIR/.git" ]; then
    echo "Cloning ANGLE..."

    git clone \
        --branch "$ANGLE_BRANCH" \
        "$ANGLE_REPO" \
        "$ANGLE_DIR"
else
    echo "Fetching ANGLE..."

    git -C "$ANGLE_DIR" fetch origin
fi

echo "ANGLE ready."

export PATH="$DEPOT_TOOLS_DIR:$PATH"

#
# Always write a valid .gclient
#

cat > "$ROOT/.gclient" <<EOF
solutions = [
  {
    "name": "angle",
    "url": "$ANGLE_REPO",
    "deps_file": "DEPS",
    "managed": False,
    "custom_deps": {},
    "custom_vars": {
      "checkout_angle_cl_deps": False,
    },
  },
]

target_os = ["android"]
EOF

#
# Sync dependencies from the directory containing .gclient
#

cd "$ROOT"

if [ ! -f "$ROOT/.gclient_entries" ]; then
    echo "Initial dependency sync..."
    gclient sync -f -D -R --no-history --shallow
else
    echo "Incremental dependency sync..."
    gclient sync
fi

#
# Generate Android ARM64 build
#

cd "$ANGLE_DIR"

GN_ARGS='
target_os="android"
target_cpu="arm64"

arm_control_flow_integrity="none"

angle_build_all=false

is_debug=false
is_component_build=false
treat_warnings_as_errors=false
symbol_level=2
strip_debug_info=false
angle_has_frame_capture=false

angle_enable_gl=false
angle_enable_vulkan=true
angle_enable_vulkan_validation_layers=true
angle_enable_wgpu=false
angle_enable_null=false

use_siso=false
'

echo "Generating build..."

gn gen "$OUT_DIR" --args="$GN_ARGS"

#
# Build
#

echo "Building ANGLE..."

autoninja --offline -C "$OUT_DIR" libEGL libGLESv2

#
# Copy artifacts
#

echo "Copying artifacts to:"
echo "$DIST_DIR"

mkdir -p "$DIST_DIR"

cp -f "$ANGLE_DIR/$OUT_DIR/libEGL_angle.so" "$DIST_DIR/"
cp -f "$ANGLE_DIR/$OUT_DIR/libGLESv2_angle.so" "$DIST_DIR/"
cp -f "$ANGLE_DIR/$OUT_DIR/libVkLayer_khronos_validation.so" "$DIST_DIR/"

echo
echo "ANGLE Android ARM64 build complete"