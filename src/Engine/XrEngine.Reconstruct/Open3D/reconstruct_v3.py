import argparse
import json
from pathlib import Path

import numpy as np
import open3d as o3d


def load_matrix(m):
    return np.array([
        [m["M11"], m["M12"], m["M13"], m["M14"]],
        [m["M21"], m["M22"], m["M23"], m["M24"]],
        [m["M31"], m["M32"], m["M33"], m["M34"]],
        [m["M41"], m["M42"], m["M43"], m["M44"]],
    ], dtype=np.float64)


def box_blur_rgba(src, width, height, radius):
    if radius <= 0:
        return src.reshape((height, width, 4)).copy()

    img = src.reshape((height, width, 4)).astype(np.float32)
    d = radius * 2 + 1

    pad = np.pad(img, ((0, 0), (radius, radius), (0, 0)), mode="edge")
    cs = np.cumsum(pad, axis=1)
    cs = np.concatenate([np.zeros_like(cs[:, :1, :]), cs], axis=1)
    img = (cs[:, d:d + width, :] - cs[:, 0:width, :]) / d

    pad = np.pad(img, ((radius, radius), (0, 0), (0, 0)), mode="edge")
    cs = np.cumsum(pad, axis=0)
    cs = np.concatenate([np.zeros_like(cs[:1, :, :]), cs], axis=0)
    img = (cs[d:d + height, :, :] - cs[0:height, :, :]) / d

    return np.clip(img, 0, 255).astype(np.uint8)


def normalize(v):
    l = np.linalg.norm(v)
    if l == 0.0:
        return v
    return v / l


def reconstruct_grid(depth_u16, color_rgba, meta, stride, blur_radius, max_world_edge):
    depth_width = meta["DepthWidth"]
    depth_height = meta["DepthHeight"]
    color_width = meta["ColorWidth"]
    color_height = meta["ColorHeight"]

    grid_width = meta["GridWidth"]
    grid_height = meta["GridHeight"]

    depth = depth_u16.reshape((depth_height, depth_width))
    color = box_blur_rgba(color_rgba, color_width, color_height, blur_radius)

    depth_view = load_matrix(meta["DepthView"])
    depth_proj = load_matrix(meta["DepthProj"])
    depth_view_proj_inv = np.linalg.inv(depth_view @ depth_proj)

    camera_view = load_matrix(meta["CameraView"])
    camera_proj = load_matrix(meta["CameraProj"])
    color_view_proj = camera_view @ camera_proj

    gx = np.arange(0, grid_width, stride, dtype=np.int32)
    gy = np.arange(0, grid_height, stride, dtype=np.int32)

    xs, ys = np.meshgrid(gx, gy)

    uv_x = xs.astype(np.float64) / (grid_width - 1)
    uv_y = ys.astype(np.float64) / (grid_height - 1)

    dx = np.rint(uv_x * (depth_width - 1)).astype(np.int32)
    dy = np.rint(uv_y * (depth_height - 1)).astype(np.int32)

    raw_d = depth[dy, dx].astype(np.float64)
    valid = (raw_d != 0.0) & (raw_d != 65535.0)

    d = raw_d / 65535.0

    clip = np.stack([
        uv_x * 2.0 - 1.0,
        uv_y * 2.0 - 1.0,
        d * 2.0 - 1.0,
        np.ones_like(d)
    ], axis=-1)

    h, w = clip.shape[:2]

    world4 = clip.reshape((-1, 4)) @ depth_view_proj_inv
    ww = world4[:, 3]

    valid = valid.reshape((-1,))
    valid &= ww != 0.0

    world = np.zeros((h * w, 3), dtype=np.float64)
    world[valid] = world4[valid, :3] / ww[valid, None]
    world = world.reshape((h, w, 3))
    valid = valid.reshape((h, w))

    p4 = np.concatenate(
        [world.reshape((-1, 3)), np.ones((h * w, 1), dtype=np.float64)],
        axis=1
    )

    color_clip = p4 @ color_view_proj
    cw = color_clip[:, 3]

    color_valid = cw != 0.0

    inv_cw = np.zeros_like(cw)
    inv_cw[color_valid] = 1.0 / cw[color_valid]

    cu = color_clip[:, 0] * inv_cw * 0.5 + 0.5
    cv = color_clip[:, 1] * inv_cw * 0.5 + 0.5
    cv = 1.0 - cv

    color_valid &= (
        (cu >= 0.0) & (cu <= 1.0) &
        (cv >= 0.0) & (cv <= 1.0)
    )

    valid &= color_valid.reshape((h, w))

    px = np.rint(cu * (color_width - 1)).astype(np.int32)
    py = np.rint(cv * (color_height - 1)).astype(np.int32)

    px = np.clip(px, 0, color_width - 1)
    py = np.clip(py, 0, color_height - 1)

    colors = color[py, px, :3].astype(np.float64) / 255.0
    colors = colors.reshape((h, w, 3))

    normals = np.zeros((h, w, 3), dtype=np.float64)
    used = np.zeros((h, w), dtype=bool)

    max_edge_sq = max_world_edge * max_world_edge

    def keep_triangle(a, b, c):
        if not valid[a] or not valid[b] or not valid[c]:
            return False

        p0 = world[a]
        p1 = world[b]
        p2 = world[c]

        if np.sum((p0 - p1) ** 2) > max_edge_sq:
            return False
        if np.sum((p1 - p2) ** 2) > max_edge_sq:
            return False
        if np.sum((p2 - p0) ** 2) > max_edge_sq:
            return False

        return True

    for y in range(h - 1):
        for x in range(w - 1):
            i0 = (y, x)
            i1 = (y, x + 1)
            i2 = (y + 1, x)
            i3 = (y + 1, x + 1)

            if keep_triangle(i0, i1, i2):
                p0 = world[i0]
                p1 = world[i1]
                p2 = world[i2]

                n = np.cross(p1 - p0, p2 - p0)

                normals[i0] += n
                normals[i1] += n
                normals[i2] += n

                used[i0] = True
                used[i1] = True
                used[i2] = True

            if keep_triangle(i1, i3, i2):
                p1 = world[i1]
                p3 = world[i3]
                p2 = world[i2]

                n = np.cross(p3 - p1, p2 - p1)

                normals[i1] += n
                normals[i3] += n
                normals[i2] += n

                used[i1] = True
                used[i3] = True
                used[i2] = True

    normal_len = np.linalg.norm(normals, axis=2)
    used &= normal_len > 0.0

    normals[used] /= normal_len[used, None]

    points = world[used]
    normals = normals[used]
    colors = colors[used]

    return points, normals, colors


def make_basis_from_normal(n):
    if abs(n[1]) < 0.95:
        ref = np.array([0.0, 1.0, 0.0], dtype=np.float64)
    else:
        ref = np.array([1.0, 0.0, 0.0], dtype=np.float64)

    t = normalize(np.cross(ref, n))
    b = np.cross(n, t)

    return t, b


def append_quad_splats(vertices, colors, triangles, points, normals, point_colors, radius):
    corners = [
        (-1.0, -1.0),
        (1.0, -1.0),
        (1.0, 1.0),
        (-1.0, 1.0),
    ]

    for p, n, c in zip(points, normals, point_colors):
        t, b = make_basis_from_normal(n)

        base = len(vertices)

        for cx, cy in corners:
            vertices.append(p + t * cx * radius + b * cy * radius)
            colors.append(c)

        triangles.append((base + 0, base + 1, base + 2))
        triangles.append((base + 0, base + 2, base + 3))


def append_disc_splats(vertices, colors, triangles, points, normals, point_colors, radius, segments):
    angles = np.linspace(0.0, np.pi * 2.0, segments, endpoint=False)

    for p, n, c in zip(points, normals, point_colors):
        t, b = make_basis_from_normal(n)

        base = len(vertices)

        vertices.append(p)
        colors.append(c)

        for a in angles:
            vertices.append(p + t * np.cos(a) * radius + b * np.sin(a) * radius)
            colors.append(c)

        for i in range(segments):
            i0 = base
            i1 = base + 1 + i
            i2 = base + 1 + ((i + 1) % segments)
            triangles.append((i0, i1, i2))


def build_splat_mesh(session_path, stride, radius, blur_radius, max_world_edge, shape, segments):
    session = Path(session_path)

    vertices = []
    colors = []
    triangles = []

    frame_dirs = sorted(
        p for p in session.iterdir()
        if p.is_dir() and (p / "meta.json").exists()
    )

    for frame_dir in frame_dirs:
        print("loading", frame_dir.name)

        with open(frame_dir / "meta.json", "r", encoding="utf-8") as f:
            meta = json.load(f)

        depth_count = meta["DepthWidth"] * meta["DepthHeight"]
        color_count = meta["ColorWidth"] * meta["ColorHeight"] * 4

        depth = np.fromfile(frame_dir / "depth_u16.raw", dtype=np.uint16, count=depth_count)
        color = np.fromfile(frame_dir / "color_rgba.raw", dtype=np.uint8, count=color_count)

        points, normals, point_colors = reconstruct_grid(
            depth,
            color,
            meta,
            stride,
            blur_radius,
            max_world_edge
        )

        print("splats:", len(points))

        if shape == "disc":
            append_disc_splats(
                vertices,
                colors,
                triangles,
                points,
                normals,
                point_colors,
                radius,
                segments
            )
        else:
            append_quad_splats(
                vertices,
                colors,
                triangles,
                points,
                normals,
                point_colors,
                radius
            )

    mesh = o3d.geometry.TriangleMesh()

    mesh.vertices = o3d.utility.Vector3dVector(np.asarray(vertices, dtype=np.float64))
    mesh.vertex_colors = o3d.utility.Vector3dVector(np.asarray(colors, dtype=np.float64))
    mesh.triangles = o3d.utility.Vector3iVector(np.asarray(triangles, dtype=np.int32))

    mesh.remove_duplicated_vertices()
    mesh.remove_duplicated_triangles()
    mesh.remove_degenerate_triangles()
    mesh.compute_vertex_normals()

    return mesh


def main():
    parser = argparse.ArgumentParser()

    parser.add_argument("session")
    parser.add_argument("--stride", type=int, default=2)
    parser.add_argument("--radius", type=float, default=0.012)
    parser.add_argument("--blur", type=int, default=2)
    parser.add_argument("--max-edge", type=float, default=0.10)
    parser.add_argument("--shape", choices=["quad", "disc"], default="quad")
    parser.add_argument("--segments", type=int, default=8)
    parser.add_argument("--out", default="snapshot_splats.ply")

    args = parser.parse_args()

    mesh = build_splat_mesh(
        args.session,
        stride=args.stride,
        radius=args.radius,
        blur_radius=args.blur,
        max_world_edge=args.max_edge,
        shape=args.shape,
        segments=args.segments
    )

    print("vertices:", len(mesh.vertices))
    print("triangles:", len(mesh.triangles))

    o3d.io.write_triangle_mesh(args.out, mesh)
    print("saved", args.out)


if __name__ == "__main__":
    main()