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


def reconstruct_depth_points(depth_u16, meta, stride):
    depth_width = meta["DepthWidth"]
    depth_height = meta["DepthHeight"]

    grid_width = meta["GridWidth"]
    grid_height = meta["GridHeight"]

    depth = depth_u16.reshape((depth_height, depth_width))

    depth_view = load_matrix(meta["DepthView"])
    depth_proj = load_matrix(meta["DepthProj"])
    depth_view_proj_inv = np.linalg.inv(depth_view @ depth_proj)

    ys, xs = np.mgrid[0:grid_height:stride, 0:grid_width:stride]

    uv_x = xs.astype(np.float64) / (grid_width - 1)
    uv_y = ys.astype(np.float64) / (grid_height - 1)

    dx = np.rint(uv_x * (depth_width - 1)).astype(np.int32)
    dy = np.rint(uv_y * (depth_height - 1)).astype(np.int32)

    raw_d = depth[dy, dx].astype(np.float64)

    valid = (raw_d != 0.0) & (raw_d != 65535.0)

    d = raw_d / 65535.0

    clip = np.stack(
        [
            uv_x * 2.0 - 1.0,
            uv_y * 2.0 - 1.0,
            d * 2.0 - 1.0,
            np.ones_like(d),
        ],
        axis=-1
    ).reshape((-1, 4))

    valid = valid.reshape((-1,))

    world4 = clip @ depth_view_proj_inv

    w = world4[:, 3]
    valid &= w != 0.0

    world = world4[:, :3] / w[:, None]

    return world[valid]


def sample_color(points, color_rgba, meta):
    color_width = meta["ColorWidth"]
    color_height = meta["ColorHeight"]

    color = color_rgba.reshape((color_height, color_width, 4))

    camera_view = load_matrix(meta["CameraView"])
    camera_proj = load_matrix(meta["CameraProj"])
    color_view_proj = camera_view @ camera_proj

    ones = np.ones((points.shape[0], 1), dtype=np.float64)
    p4 = np.concatenate([points, ones], axis=1)

    color_clip = p4 @ color_view_proj

    w = color_clip[:, 3]
    valid = w != 0.0

    inv_w = np.zeros_like(w)
    inv_w[valid] = 1.0 / w[valid]

    uv_x = color_clip[:, 0] * inv_w * 0.5 + 0.5
    uv_y = color_clip[:, 1] * inv_w * 0.5 + 0.5
    uv_y = 1.0 - uv_y

    valid &= (
        (uv_x >= 0.0) & (uv_x <= 1.0) &
        (uv_y >= 0.0) & (uv_y <= 1.0)
    )

    px = np.clip((uv_x * (color_width - 1)).astype(np.int32), 0, color_width - 1)
    py = np.clip((uv_y * (color_height - 1)).astype(np.int32), 0, color_height - 1)

    rgb = color[py, px, :3].astype(np.float64) / 255.0

    return valid, rgb


def load_frame(frame_dir, stride):
    meta_path = frame_dir / "meta.json"
    depth_path = frame_dir / "depth_u16.raw"
    color_path = frame_dir / "color_rgba.raw"

    with open(meta_path, "r", encoding="utf-8") as f:
        meta = json.load(f)

    depth_count = meta["DepthWidth"] * meta["DepthHeight"]
    color_count = meta["ColorWidth"] * meta["ColorHeight"] * 4

    depth = np.fromfile(depth_path, dtype=np.uint16, count=depth_count)
    color = np.fromfile(color_path, dtype=np.uint8, count=color_count)

    points = reconstruct_depth_points(depth, meta, stride)

    if len(points) == 0:
        print(frame_dir.name, "points: 0")
        return o3d.geometry.PointCloud()

    valid, rgb = sample_color(points, color, meta)

    points = points[valid]
    rgb = rgb[valid]

    pcd = o3d.geometry.PointCloud()
    pcd.points = o3d.utility.Vector3dVector(points)
    pcd.colors = o3d.utility.Vector3dVector(rgb)

    print(frame_dir.name, "points:", len(points))

    return pcd


def build_cloud(session_path, stride, voxel_size):
    session = Path(session_path)

    all_pcd = o3d.geometry.PointCloud()

    frame_dirs = sorted(
        p for p in session.iterdir()
        if p.is_dir() and (p / "meta.json").exists()
    )

    for frame_dir in frame_dirs:
        print("loading", frame_dir.name)
        all_pcd += load_frame(frame_dir, stride)

    all_pcd.remove_non_finite_points()

    if voxel_size > 0:
        all_pcd = all_pcd.voxel_down_sample(voxel_size)

    return all_pcd

def make_double_sided(mesh):
    vertices = np.asarray(mesh.vertices)
    triangles = np.asarray(mesh.triangles)

    reversed_triangles = triangles[:, [0, 2, 1]]

    mesh.triangles = o3d.utility.Vector3iVector(
        np.vstack([triangles, reversed_triangles])
    )

    if mesh.has_triangle_uvs():
        uvs = np.asarray(mesh.triangle_uvs)
        rev_uvs = uvs.reshape((-1, 3, 2))[:, [0, 2, 1], :].reshape((-1, 2))
        mesh.triangle_uvs = o3d.utility.Vector2dVector(
            np.vstack([uvs, rev_uvs])
        )

    mesh.compute_vertex_normals()
    return mesh

def make_mesh_poisson(pcd, depth, density_quantile):
    print("estimating normals")

    pcd.estimate_normals(
        search_param=o3d.geometry.KDTreeSearchParamHybrid(
            radius=0.08,
            max_nn=30
        )
    )

    pcd.orient_normals_consistent_tangent_plane(30)

    print("poisson reconstruction")

    mesh, densities = o3d.geometry.TriangleMesh.create_from_point_cloud_poisson(
        pcd,
        depth=depth
    )

    densities = np.asarray(densities)

    if density_quantile > 0:
        threshold = np.quantile(densities, density_quantile)
        mesh.remove_vertices_by_mask(densities < threshold)

    mesh.compute_vertex_normals()

    return mesh

def make_mesh_ball_pivoting(pcd):
    print("estimating normals")

    pcd.estimate_normals(
        search_param=o3d.geometry.KDTreeSearchParamHybrid(
            radius=0.05,
            max_nn=30
        )
    )

    distances = pcd.compute_nearest_neighbor_distance()
    avg_dist = np.mean(distances)

    print("avg nn distance:", avg_dist)

    radii = [
        0.02,
        0.04,
        0.08,
        0.16
    ]

    print("ball pivot radii:", radii)

    mesh = o3d.geometry.TriangleMesh.create_from_point_cloud_ball_pivoting(
        pcd,
        o3d.utility.DoubleVector(radii)
    )

    mesh.compute_vertex_normals()

    return mesh


def main():
    parser = argparse.ArgumentParser()

    parser.add_argument("session")
    parser.add_argument("--stride", type=int, default=2)
    parser.add_argument("--voxel", type=float, default=0.01)
    parser.add_argument("--poisson-depth", type=int, default=9)
    parser.add_argument("--density-cut", type=float, default=0.02)
    parser.add_argument("--out-ply", default="snapshot_cloud.ply")
    parser.add_argument("--out-mesh", default="snapshot_mesh.ply")
    parser.add_argument("--no-mesh", action="store_true")

    args = parser.parse_args()

    pcd = build_cloud(args.session, args.stride, args.voxel)

    print("total points:", len(pcd.points))

    if len(pcd.points) == 0:
        print("no points written")
        return

    o3d.io.write_point_cloud(args.out_ply, pcd)
    print("saved", args.out_ply)

    if not args.no_mesh:
        mesh = make_mesh_ball_pivoting(pcd)
        
        mesh = make_double_sided(mesh)

        o3d.io.write_triangle_mesh(args.out_mesh, mesh)
        print("saved", args.out_mesh)


if __name__ == "__main__":
    main()