import bmesh
import bpy
import math
import sys

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
GLB_PATH = argv[0] if len(argv) > 0 else "Caps.glb"
PREVIEW_PATH = argv[1] if len(argv) > 1 else ""

MM = 0.001

WHITE = (0.90, 0.90, 0.88)
DARK = (0.03, 0.03, 0.035)

bpy.ops.wm.read_factory_settings(use_empty=True)

materials = {}


def material(name, color, roughness=0.45):
    if name in materials:
        return materials[name]
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    bsdf.inputs["Roughness"].default_value = roughness
    mat.diffuse_color = (*color, 1)
    materials[name] = mat
    return mat


def finish(obj, parent, mat, bevel=0.4):
    obj.data.materials.append(mat)
    for poly in obj.data.polygons:
        poly.use_smooth = True
    if bevel > 0:
        mod = obj.modifiers.new("Bevel", "BEVEL")
        mod.width = bevel * MM
        mod.segments = 3
        mod.limit_method = "ANGLE"
        mod.angle_limit = math.radians(40)
        mod.harden_normals = True
    obj.parent = parent
    return obj


def cylinder(name, radius, depth, location, rotation=(0, 0, 0), vertices=64):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius * MM, depth=depth * MM,
                                        location=tuple(v * MM for v in location), rotation=rotation)
    obj = bpy.context.active_object
    obj.name = name
    return obj


def cone(name, radius1, radius2, depth, location, vertices=64):
    bpy.ops.mesh.primitive_cone_add(vertices=vertices, radius1=radius1 * MM, radius2=radius2 * MM, depth=depth * MM,
                                    location=tuple(v * MM for v in location))
    obj = bpy.context.active_object
    obj.name = name
    return obj


def lathe(name, profile, segments=64):
    mesh = bpy.data.meshes.new(name)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    bm = bmesh.new()
    verts = [bm.verts.new((r * MM, 0, z * MM)) for r, z in profile]
    edges = [bm.edges.new((verts[i], verts[i + 1])) for i in range(len(verts) - 1)]
    bmesh.ops.spin(bm, geom=verts + edges, axis=(0, 0, 1), cent=(0, 0, 0), steps=segments, angle=2 * math.pi, use_merge=True)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-7)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(mesh)
    bm.free()
    return obj


def arc(cx, cz, radius, a0, a1, steps=6):
    return [(cx + radius * math.cos(a), cz + radius * math.sin(a))
            for a in (math.radians(a0 + (a1 - a0) * i / steps) for i in range(steps + 1))]


def box(name, size, location):
    bpy.ops.mesh.primitive_cube_add(size=1, location=tuple(v * MM for v in location))
    obj = bpy.context.active_object
    obj.scale = tuple(v * MM for v in size)
    bpy.ops.object.transform_apply(scale=True)
    obj.name = name
    return obj


def cut(obj, cutter):
    cutter.parent = obj.parent
    mod = obj.modifiers.new("Cut", "BOOLEAN")
    mod.operation = "DIFFERENCE"
    mod.object = cutter
    mod.solver = "EXACT"
    cutter.hide_render = True
    cutter.hide_viewport = True
    cutters.append(cutter)


def scoop(obj, extent, top, drop, sag):
    y0, z0 = -extent, top + 0.2
    y1, z1 = extent, top - drop
    length = math.hypot(y1 - y0, z1 - z0)
    radius = (length * length / 4 + sag * sag) / (2 * sag)
    normal_y, normal_z = (z0 - z1) / length, (y1 - y0) / length
    center = (0, (y0 + y1) / 2 + normal_y * (radius - sag), (z0 + z1) / 2 + normal_z * (radius - sag))
    cutter = cylinder(obj.name + "_Scoop", radius, 60, center, (0, math.radians(90), 0), 192)
    cut(obj, cutter)


def ribs(obj, count, depth):
    mesh = obj.data
    for v in mesh.vertices:
        angle = math.atan2(v.co.y, v.co.x)
        r = math.hypot(v.co.x, v.co.y)
        if r < 1e-6:
            continue
        k = 1 - (depth / (r / MM)) * (0.5 + 0.5 * math.cos(angle * count))
        v.co.x *= k
        v.co.y *= k


def dot(cap, parent, z, body_radius, radius, color, protrude=0.6, boss=None):
    front = body_radius
    if boss is not None:
        boss_radius, boss_depth, boss_mat = boss
        finish(cylinder(cap + "_Boss", boss_radius, boss_depth + 2, (0, -(front + boss_depth / 2 - 1), z), (math.radians(90), 0, 0)), parent, boss_mat, 0.5)
        front += boss_depth
    finish(cylinder(cap + "_Dot", radius, protrude + 1.5, (0, -(front + protrude / 2 - 0.75), z), (math.radians(90), 0, 0)), parent, material(cap + "_Dot", color, 0.35), 0.25)
    finish(cylinder(cap + "_Hole", radius * 0.28, 0.4, (0, -(front + protrude + 0.1), z), (math.radians(90), 0, 0), 32), parent, material("Hole", DARK, 0.6), 0)


def new_cap(name):
    empty = bpy.data.objects.new("Cap_" + name, None)
    bpy.context.scene.collection.objects.link(empty)
    caps.append(empty)
    return empty


def stepped(name, body_color, dot_color, skirt_r=9.5, skirt_h=6.5, body_r=7.0, top=18.5, dot_r=2.6, drop=5, dot_z=None, cylindrical=False):
    cap = new_cap(name)
    body_mat = material(name + "_Body", body_color)
    skirt_top = skirt_r * 0.96
    finish(lathe(name + "_Skirt",
                 [(0, 0), (skirt_r - 0.6, 0)] + arc(skirt_r - 0.6, 0.6, 0.6, -90, 0, 3) +
                 arc(skirt_top - 1.6, skirt_h - 1.6, 1.6, 0, 90) + [(0, skirt_h)]), cap, body_mat, 0)
    shoulder = 2.4
    top_r = body_r * 0.92
    if cylindrical:
        body = finish(cylinder(name + "_Body", body_r, top - skirt_h + 1, (0, 0, (top + skirt_h - 1) / 2)), cap, body_mat, 0.6)
    else:
        body = finish(lathe(name + "_Body",
                            [(0, skirt_h - 1), (body_r * 1.06, skirt_h - 1), (body_r * 1.02, skirt_h + 1.2), (body_r * 0.98, (skirt_h + top) / 2)] +
                            arc(top_r - shoulder, top - shoulder, shoulder, 0, 90) + [(0, top)]), cap, body_mat, 0.4)
    if drop > 0:
        scoop(body, body_r + 1, top, drop, 1.0)
    if dot_color is not None:
        dot(name, cap, dot_z if dot_z is not None else top - 6.5, body_r * 1.0, dot_r, dot_color)
    return cap, body_mat


def banana(name, body_color, dot_color, base_r=10.5, top_r=7.8, height=17.5):
    cap = new_cap(name)
    body_mat = material(name + "_Body", body_color, 0.35)
    shoulder = 3.0
    body = finish(lathe(name + "_Body",
                        [(0, 0), (base_r - 0.6, 0)] + arc(base_r - 0.6, 0.6, 0.6, -90, 0, 3) +
                        [(base_r, 3), (base_r - 0.7, 3.4), (base_r * 0.95, 6), (top_r * 1.06, height - shoulder - 2)] +
                        arc(top_r - shoulder, height - shoulder, shoulder, 0, 90) + [(0, height)]), cap, body_mat, 0.4)
    scoop(body, top_r + 1, height, 7, 1.5)
    dot(name, cap, height - 6.5, top_r + 1.3, 2.4, dot_color, boss=(3.6, 1.6, body_mat))
    return cap


def ny(name, dot_color, body_r=6.8, height=14, stem_h=8):
    cap = new_cap(name)
    body_mat = material(name + "_Body", WHITE)
    finish(cylinder(name + "_Stem", 1.9, stem_h + 1, (0, 0, stem_h / 2)), cap, body_mat, 0.3)
    body = finish(cylinder(name + "_Body", body_r, height, (0, 0, stem_h + height / 2), vertices=96), cap, body_mat, 0.5)
    ribs(body, 32, 0.35)
    dot(name, cap, stem_h + height * 0.55, body_r - 0.2, 2.2, dot_color)
    return cap


caps = []
cutters = []

stepped("SuperSkinny", WHITE, (0.55, 0.55, 0.55))
stepped("Mtn94", WHITE, DARK, skirt_h=6, body_r=7.4, top=17, dot_r=2.4)
banana("Banana", (0.04, 0.04, 0.045), (0.85, 0.88, 0.08))
stepped("Soft", (0.74, 0.75, 0.77), (0.15, 0.48, 0.90), skirt_r=9.8, body_r=7.6, top=17.5, dot_r=2.8)
stepped("FatPink", WHITE, (0.86, 0.42, 0.48))
stepped("AstroFat", (0.93, 0.93, 0.92), DARK, skirt_r=10.2, skirt_h=7.5, body_r=8.4, top=19, dot_r=3.0, drop=5.5)
ny("NY", DARK)

transversal, transversal_mat = stepped("Transversal", WHITE, None, top=19)
finish(box("Transversal_Nozzle", (3.8, 3.0, 3.4), (0, -6.1, 12.5)), transversal, material("Transversal_Nozzle", (0.05, 0.05, 0.055)), 0.4)
finish(box("Transversal_Slit", (2.8, 0.3, 0.45), (0, -7.6, 12.5)), transversal, material("Hole", DARK, 0.6), 0)

needle, needle_mat = stepped("Needle", (0.82, 0.09, 0.07), None, top=18)
finish(cylinder("Needle_Insert", 2.2, 1.5, (0, -7.1, 11.5), (math.radians(90), 0, 0)), needle, material("Needle_Insert", WHITE), 0.2)
finish(cylinder("Needle_Tube", 0.9, 11, (0, -12.8, 11.5), (math.radians(90), 0, 0), 32), needle, material("Needle_Insert", WHITE), 0.2)

for i, cap in enumerate(caps):
    cap.location = ((i - (len(caps) - 1) / 2) * 30 * MM, 0, 0)
    cap.rotation_euler = (0, 0, math.radians(-30))

if PREVIEW_PATH:
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_cavity = True
    scene.display.render_aa = "8"
    scene.render.resolution_x = 2400
    scene.render.resolution_y = 600
    scene.render.film_transparent = False
    scene.world = bpy.data.worlds.new("World")
    scene.world.color = (0.12, 0.12, 0.12)

    cam_data = bpy.data.cameras.new("Camera")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = len(caps) * 30 * MM + 0.01
    cam = bpy.data.objects.new("Camera", cam_data)
    scene.collection.objects.link(cam)
    cam.location = (0, -0.3, 0.13)
    cam.rotation_euler = (math.radians(68), 0, 0)
    scene.camera = cam

    scene.render.filepath = PREVIEW_PATH
    bpy.ops.render.render(write_still=True)

for cap in caps:
    cap.location = (0, 0, 0)
    cap.rotation_euler = (0, 0, 0)

bpy.ops.object.select_all(action="DESELECT")
for cap in caps:
    cap.select_set(True)
    for child in cap.children:
        child.select_set(True)

bpy.ops.export_scene.gltf(filepath=GLB_PATH, export_format="GLB", use_selection=True, export_apply=True, export_yup=True)
