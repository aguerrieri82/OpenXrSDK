# pip install open3d numpy
import numpy as np
import open3d as o3d

# -------------------------
# EDIT THESE
# -------------------------
XYZ_PATH = r"d:\points.xyz"

# Downsample while reading (critical for performance/memory)
VOXEL_SIZE = 0.02              # meters (example). 0.01=1cm, 0.02=2cm, 0.05=5cm

READ_MB = 256                  # chunk size in MB (64..512 typical)
MAX_RAW_POINTS = 0             # 0 = read all; else stop after N raw points (quick test)

GLOBAL_FLUSH_LIMIT = 2_000_000 # collapse global set if it grows beyond this (trade speed/memory)

OUT_DOWNSAMPLED_PLY = r"d:\points.xyz"
OUT_MESH_PLY = r"d:\mesh_poisson.ply"

SHOW_POINTCLOUD = True
SHOW_MESH = True

# Mesh reconstruction
RUN_POISSON = True
POISSON_DEPTH = 9              # 8-10 typical (higher = more detail, slower)
NORMAL_RADIUS = VOXEL_SIZE * 3.0
NORMAL_MAX_NN = 50
ORIENT_K = 30                  # 0 disables orientation, but Poisson usually needs it
DENSITY_REMOVE_QUANTILE = 0.02 # remove lowest-density vertices (0 disables)

# If True: parse XYZ and (re)generate OUT_DOWNSAMPLED_PLY.
# If False: load OUT_DOWNSAMPLED_PLY and continue from there.
REBUILD_DOWNSAMPLED_FROM_XYZ = False


def voxel_unique(points: np.ndarray, voxel: float) -> np.ndarray:
    """Keep 1 representative point per voxel cell (first occurrence)."""
    if points.size == 0:
        return points
    key = np.floor(points / voxel).astype(np.int32)  # (N,3) int voxel coords
    _, idx = np.unique(key, axis=0, return_index=True)
    return points[idx]


def read_xyz_stream_voxel(path: str) -> tuple[np.ndarray, int]:
    block_bytes = READ_MB * 1024 * 1024
    leftover = b""
    global_pts = np.empty((0, 3), dtype=np.float32)
    raw_total = 0
    chunk_id = 0

    with open(path, "rb") as f:
        while True:
            data = f.read(block_bytes)
            if not data:
                break

            buf = leftover + data
            last_nl = buf.rfind(b"\n")
            if last_nl < 0:
                leftover = buf
                continue

            full = buf[: last_nl + 1]
            leftover = buf[last_nl + 1 :]

            arr = np.fromstring(full.decode("ascii", errors="ignore"), sep=" ", dtype=np.float32)
            if arr.size < 3:
                continue

            arr = arr[: (arr.size // 3) * 3].reshape((-1, 3))
            m = np.isfinite(arr).all(axis=1)
            pts = arr[m]

            raw_total += pts.shape[0]
            chunk_id += 1
            print(f"chunk {chunk_id}: parsed {pts.shape[0]:,} pts; raw total {raw_total:,}")

            pts_ds = voxel_unique(pts, VOXEL_SIZE)

            if global_pts.size == 0:
                global_pts = pts_ds
            else:
                global_pts = np.vstack([global_pts, pts_ds])
                if global_pts.shape[0] >= GLOBAL_FLUSH_LIMIT:
                    global_pts = voxel_unique(global_pts, VOXEL_SIZE)
                    print(f"  collapsed global -> {global_pts.shape[0]:,} pts")

            if MAX_RAW_POINTS > 0 and raw_total >= MAX_RAW_POINTS:
                break

    # Flush leftover
    if leftover.strip():
        arr = np.fromstring(leftover.decode("ascii", errors="ignore"), sep=" ", dtype=np.float32)
        if arr.size >= 3:
            arr = arr[: (arr.size // 3) * 3].reshape((-1, 3))
            m = np.isfinite(arr).all(axis=1)
            pts = arr[m]
            raw_total += pts.shape[0]
            pts_ds = voxel_unique(pts, VOXEL_SIZE)
            global_pts = np.vstack([global_pts, pts_ds]) if global_pts.size else pts_ds

    global_pts = voxel_unique(global_pts, VOXEL_SIZE)
    return global_pts, raw_total


def poisson_mesh_from_pcd(pcd: o3d.geometry.PointCloud) -> o3d.geometry.TriangleMesh:
    # Work on a copy to avoid mutating caller's pcd
    pcd = o3d.geometry.PointCloud(pcd)

    # Normals
    pcd.estimate_normals(
        search_param=o3d.geometry.KDTreeSearchParamHybrid(
            radius=float(NORMAL_RADIUS), max_nn=int(NORMAL_MAX_NN)
        )
    )
    pcd.normalize_normals()

    # Orient normals (Poisson wants consistent orientation)
    if ORIENT_K and ORIENT_K > 0:
        pcd.orient_normals_consistent_tangent_plane(int(ORIENT_K))

    # Poisson
    mesh, densities = o3d.geometry.TriangleMesh.create_from_point_cloud_poisson(
        pcd, depth=int(POISSON_DEPTH)
    )
    mesh.compute_vertex_normals()

    # Remove low-density vertices (recommended)
    if DENSITY_REMOVE_QUANTILE and DENSITY_REMOVE_QUANTILE > 0:
        d = np.asarray(densities)
        thr = np.quantile(d, float(DENSITY_REMOVE_QUANTILE))
        mesh.remove_vertices_by_mask(d < thr)
        mesh.remove_unreferenced_vertices()
        mesh.compute_vertex_normals()

    return mesh


def main():
    # 1) Obtain point cloud (either rebuild from huge XYZ, or load the downsampled .ply)
    if REBUILD_DOWNSAMPLED_FROM_XYZ:
        pts_ds, raw_total = read_xyz_stream_voxel(XYZ_PATH)
        if pts_ds.shape[0] == 0:
            raise RuntimeError("0 points after parsing/downsampling.")

        print(f"final: {pts_ds.shape[0]:,} points (voxel={VOXEL_SIZE}) from raw {raw_total:,}")

        pcd = o3d.geometry.PointCloud()
        pcd.points = o3d.utility.Vector3dVector(pts_ds.astype(np.float64, copy=False))

        ok = o3d.io.write_point_cloud(OUT_DOWNSAMPLED_PLY, pcd, write_ascii=False, compressed=True)
        if not ok:
            raise RuntimeError(f"Failed to write point cloud: {OUT_DOWNSAMPLED_PLY}")

        print(f"saved downsampled cloud: {OUT_DOWNSAMPLED_PLY}")
    else:
        pcd = o3d.io.read_point_cloud(OUT_DOWNSAMPLED_PLY)
        if pcd.is_empty():
            raise RuntimeError(f"Loaded point cloud is empty: {OUT_DOWNSAMPLED_PLY}")

    # 2) Preview point cloud
    if SHOW_POINTCLOUD:
        o3d.visualization.draw_geometries([pcd], window_name="Downsampled point cloud")

    # 3) Poisson mesh (from pcd, not from pts array)
    if RUN_POISSON:
        mesh = poisson_mesh_from_pcd(pcd)

        ok = o3d.io.write_triangle_mesh(OUT_MESH_PLY, mesh, write_ascii=False, compressed=True)
        if not ok:
            raise RuntimeError(f"Failed to write mesh: {OUT_MESH_PLY}")

        print(f"saved mesh: {OUT_MESH_PLY}")
        print(f"mesh verts/tris: {len(mesh.vertices):,} / {len(mesh.triangles):,}")

        if SHOW_MESH:
            o3d.visualization.draw_geometries(
                [mesh], window_name="Poisson mesh", mesh_show_back_face=True
            )


if __name__ == "__main__":
    main()
