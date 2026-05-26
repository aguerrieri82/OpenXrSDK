import open3d as o3d
import numpy as np
import trimesh
import xatlas
import json
import os
import cv2

# ==============================================================================
#                                CONFIGURAZIONE
# ==============================================================================

INPUT_MESH_PATH  = "D:/Projects/XrEditor/Capture/scene.obj"
OUTPUT_MESH_PATH = "D:/Projects/XrEditor/Capture/scene_out.obj"
JSON_DATA_PATH   = "D:/out.json"

DEBUG_FOLDER  = "D:/Projects/XrEditor/Capture/debug_depth"

DEBUG_SAVE_DEPTH = True

#INPUT_MESH_PATH = OUTPUT_MESH_PATH

OVERRIDE_PATH_WITH_LOCAL = False
LOCAL_IMAGES_FOLDER      = "./Frames"

# Mantieni True se i tuoi muri hanno le normali verso l'esterno
FLIP_FACES_NORMALS = False 

# --- SPECIFICHE ---
IMG_WIDTH  = 1280
IMG_HEIGHT = 1280
TEXTURE_SIZE = 4096

FX = 865.9744
FY = 865.9744
CX = 640.0111
CY = 638.1922

model_matrix_flat = [
    -0.9730506, -1.2665987E-07, -0.2305927, 0,
    -0.2305927, 5.96046448E-08, 0.9730505, 0,
    -8.940697E-08, 1.00000012, -3.57627869E-07, 0,
    0.428469419, 1.56310749, 0.530896842, 1
]

# ==============================================================================
#                                MAIN
# ==============================================================================

def read_raw_rgb24(file_path, width, height):
    expected_bytes = width * height * 3
    try:
        raw_data = np.fromfile(file_path, dtype=np.uint8)
    except FileNotFoundError:
        return None
    if raw_data.size != expected_bytes:
        return None
    return o3d.geometry.Image(raw_data.reshape((height, width, 3)))

def main():
    print("[1/6] Generazione UV...")
    if not os.path.exists(INPUT_MESH_PATH):
        print(f"File non trovato: {INPUT_MESH_PATH}")
        return

 
    if (INPUT_MESH_PATH != OUTPUT_MESH_PATH):
 
        raw_mesh = trimesh.load(INPUT_MESH_PATH, process=False)
    
        vmapping, indices, uvs = xatlas.parametrize(raw_mesh.vertices, raw_mesh.faces)
        
        mesh = o3d.geometry.TriangleMesh()
        mesh.vertices = o3d.utility.Vector3dVector(raw_mesh.vertices[vmapping])
        mesh.triangles = o3d.utility.Vector3iVector(indices)
        mesh.triangle_uvs = o3d.utility.Vector2dVector(uvs)
        
        if FLIP_FACES_NORMALS:
            mesh.triangles = o3d.utility.Vector3iVector(np.asarray(mesh.triangles)[:, ::-1])

        model_transform = np.array(model_matrix_flat, dtype=np.float64).reshape(4, 4).T
        mesh.transform(model_transform)
        mesh.compute_vertex_normals()

        print(f"[2/6] Creazione Texture {TEXTURE_SIZE}x{TEXTURE_SIZE}...")
        tex_img = np.zeros((TEXTURE_SIZE, TEXTURE_SIZE, 3), dtype=np.uint8)
        mesh.textures = [o3d.geometry.Image(tex_img)]

    else:
        mesh = o3d.io.read_triangle_model(INPUT_MESH_PATH)
        mesh = mesh.meshes[0].mesh
   
    # --- DEBUG INFO ---
    print(f"MESH EXTENT: {mesh.get_axis_aligned_bounding_box().get_extent()}")

    print("[3/6] Raycasting...")

    mesh_t = o3d.t.geometry.TriangleMesh.from_legacy(mesh)
    scene = o3d.t.geometry.RaycastingScene()
    scene.add_triangles(mesh_t)

    print("[4/6] Caricamento Dati...")
    with open(JSON_DATA_PATH, 'r') as f:
        data_entries = json.load(f)

    intrinsic_matrix = o3d.core.Tensor([[FX, 0, CX], [0, FY, CY], [0, 0, 1]], dtype=o3d.core.Dtype.Float64)
    intrinsic_legacy = o3d.camera.PinholeCameraIntrinsic(IMG_WIDTH, IMG_HEIGHT, FX, FY, CX, CY)
    
    camera_trajectory = o3d.camera.PinholeCameraTrajectory()
    rgbd_images = []
    
    valid_count = 0
    for i, entry in enumerate(data_entries):
        path = entry["FramePath"]
        if OVERRIDE_PATH_WITH_LOCAL:
            path = os.path.join(LOCAL_IMAGES_FOLDER, os.path.basename(path))

        color_img = read_raw_rgb24(path, IMG_WIDTH, IMG_HEIGHT)
        if color_img is None: continue

        mat = np.array(entry["PoseMatrix"], dtype=np.float64).reshape(4,4)
        pose_matrix = mat.T 
        extrinsic = np.linalg.inv(pose_matrix)

        rays = scene.create_rays_pinhole(intrinsic_matrix, o3d.core.Tensor(extrinsic), width_px=IMG_WIDTH, height_px=IMG_HEIGHT)
        ans = scene.cast_rays(rays)
        depth_tensor = ans['t_hit'].numpy()
        
        # FIX FLIP
        #depth_tensor = np.flipud(depth_tensor)
        #depth_tensor = np.fliplr(depth_tensor)
        depth_tensor[np.isinf(depth_tensor)] = 0.0
        
        depth_mm = (depth_tensor * 1000.0).astype(np.uint16)
        depth_img = o3d.geometry.Image(depth_mm)

        if DEBUG_SAVE_DEPTH and i < 150: 

            debug_viz = (depth_tensor / 5.0 * 255).astype(np.uint8)
            cv2.imwrite(f"{DEBUG_FOLDER}/debug_depth_{i}.png", debug_viz)
            if np.max(debug_viz) == 0:
                print(f"[WARNING] Frame {i}: Depth Map completamente NERA! La camera sta guardando il vuoto.")

        # Depth trunc largo
        rgbd = o3d.geometry.RGBDImage.create_from_color_and_depth(
            color_img, depth_img, depth_scale=1000.0, depth_trunc=1000.0, convert_rgb_to_intensity=False
        )
        rgbd_images.append(rgbd)
        
        param = o3d.camera.PinholeCameraParameters()
        param.intrinsic = intrinsic_legacy
        param.extrinsic = extrinsic
        camera_trajectory.parameters.append(param)
        valid_count += 1

    if valid_count == 0: return

    print(f"[5/6] Baking (FORCE MODE) su {valid_count} frames...")
    
    # 1. Cartella Debug
    optimizer_debug_dir = "D:/Projects/XrEditor/Capture/debug_opt"
    if not os.path.exists(optimizer_debug_dir):
        os.makedirs(optimizer_debug_dir)
    
    # 2. PARAMETRI "BOCA LARGA" (Accetta tutto)
    option = o3d.pipelines.color_map.NonRigidOptimizerOption(
        maximum_allowable_depth=100.0,
        maximum_iteration=300,
        
        # TOLLERANZA ESTREMA
        depth_threshold_for_visibility_check=1.0,      # Tolleranza 1 metro!
        depth_threshold_for_discontinuity_check=5.0,   # Ignora discontinuità
        half_dilation_kernel_size_for_discontinuity_map=0, # Nessuna dilatazione
        image_boundary_margin=0,                       # Usa anche i pixel sui bordi
        
        invisible_vertex_color_knn=0,                  # No crash se vuoto
        debug_output_dir=optimizer_debug_dir  
    )
    
    mesh, camera_trajectory = o3d.pipelines.color_map.run_non_rigid_optimizer(mesh, rgbd_images, camera_trajectory, option)
   
    # o3d.visualization.draw_geometries([mesh],
    #                               zoom=0.5399,
    #                               front=[0.0665, -0.1107, -0.9916],
    #                               lookat=[0, 0, 0],
    #                               up=[0, 1, 0])
    
    print(f"[6/6] Salvataggio...")
    o3d.io.write_triangle_mesh(OUTPUT_MESH_PATH, mesh)
    print("Finito.")

if __name__ == "__main__":
    main()