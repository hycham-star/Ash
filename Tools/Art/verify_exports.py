"""Blender FBX round-trip checks. Does not validate Unity imports or device rendering.
Run: blender -b --threads 4 --python Tools/Art/verify_exports.py -- /path/to/Ash
"""
import bpy
import os
import sys
from mathutils import Vector

root = sys.argv[sys.argv.index('--') + 1] if '--' in sys.argv else os.getcwd()
for name in ('Headquarters', 'Scavenger', 'HeadquartersSurroundings'):
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=os.path.join(root, 'Assets/Ash/Resources/Art', name + '.fbx'))
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    assert meshes, f'{name}: missing mesh'
    triangles = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in meshes)
    assert 0 < triangles < 20000, f'{name}: triangle budget {triangles}'
    assert all(o.data.uv_layers for o in meshes), f'{name}: missing UVs'
    assert all(len(o.data.materials) == 1 for o in meshes), f'{name}: expected single atlas'
    coords = [o.matrix_world @ Vector(v) for o in meshes for v in o.bound_box]
    dims = [max(v[i] for v in coords) - min(v[i] for v in coords) for i in range(3)]
    assert max(dims) < 6, f'{name}: unexpected scale {dims}'
    if name == 'Scavenger':
        rigs = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE']
        assert len(rigs) == 1 and len(rigs[0].data.bones) == 11, 'Invalid skeleton'
        rig = rigs[0]
        assert rig.animation_data and rig.animation_data.action, 'Missing walk animation'
        for obj in meshes:
            for vertex in obj.data.vertices:
                assert vertex.groups and abs(sum(g.weight for g in vertex.groups) - 1) < .001, 'Unnormalized skin weights'
        assert any(len(v.groups) > 1 for o in meshes for v in o.data.vertices), 'Missing blended joints'
        action = rig.animation_data.action
        start, end = action.frame_range
        assert end - start >= 30, 'Truncated walk cycle'
        def pose(frame):
            bpy.context.scene.frame_set(int(frame))
            deps = bpy.context.evaluated_depsgraph_get()
            obj = meshes[0].evaluated_get(deps)
            mesh = obj.to_mesh()
            vertices = [v.co.copy() for v in mesh.vertices]
            obj.to_mesh_clear()
            return vertices
        first = pose(start)
        mid = pose(start + 8)
        last = pose(end)
        assert max((a-b).length for a,b in zip(first, mid)) > .05, 'Walk does not deform mesh'
        assert max((a-b).length for a,b in zip(first, last)) < .005, 'Loop has a pose discontinuity'
        assert 1.5 < dims[2] < 2.1, f'Invalid human height {dims[2]}'
        print('PASS skeletal deformation, blended normalized weights, walk length and loop closure')
    print('PASS FBX round trip', name, 'triangles', triangles, 'dimensions', [round(x, 3) for x in dims])
