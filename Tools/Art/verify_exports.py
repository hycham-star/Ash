"""Blender FBX round-trip checks. Does not validate Unity imports or device rendering.
Run: blender -b --threads 4 --python Tools/Art/verify_exports.py -- /path/to/Ash
"""
import bpy
import os
import sys
from mathutils import Vector

root = sys.argv[sys.argv.index('--') + 1] if '--' in sys.argv else os.getcwd()
characters = ('Engineer', 'Sentry', 'MaraFlint', 'JonasReed', 'SeraVale', 'BramHolt', 'IlyraVoss', 'KadeRook', 'AshaSol', 'OrinAshcroft')
for name in ('Headquarters', 'Scavenger', 'HeadquartersSurroundings', 'Workshop', 'Barracks', 'WaterStation', 'FoodGarden', 'OutpostHut', 'OutpostTree', 'OutpostWatchtower', 'Hospital', 'Market', 'SalvageTruck', 'CavalryMount','InfantryCenter','BikerCenter','ArmyCenter','Scrapyard','EnemyCamp','LumberYard') + characters:
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    for action in list(bpy.data.actions):
        bpy.data.actions.remove(action)
    bpy.ops.import_scene.fbx(filepath=os.path.join(root, 'Assets/Ash/Resources/Art', name + '.fbx'))
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    assert meshes, f'{name}: missing mesh'
    triangles = sum(sum(len(p.vertices) - 2 for p in o.data.polygons) for o in meshes)
    assert 0 < triangles < 20000, f'{name}: triangle budget {triangles}'
    assert all(o.data.uv_layers for o in meshes), f'{name}: missing UVs'
    if name in ('Headquarters','Workshop','Barracks','Hospital','Market','FoodGarden','WaterStation','OutpostHut','OutpostWatchtower','InfantryCenter','BikerCenter','ArmyCenter','Scrapyard','EnemyCamp','LumberYard'):
        assert triangles < 4000, f'{name}: comic building exceeds 4000 triangle budget'
    if name == 'CavalryMount':
        assert all(len(o.data.uv_layers) == 1 for o in meshes), 'Mounted atlas must be UV channel zero'
        assert bpy.data.objects.get('FaceAimOrigin') and bpy.data.objects.get('FaceAimForward'), 'Missing mounted facing markers'
    assert all(len(o.data.materials) == 1 for o in meshes), f'{name}: expected single atlas'
    coords = [o.matrix_world @ Vector(v) for o in meshes for v in o.bound_box]
    dims = [max(v[i] for v in coords) - min(v[i] for v in coords) for i in range(3)]
    assert max(dims) < 6, f'{name}: unexpected scale {dims}'
    limits = {'Headquarters':(2.3,2.1), 'Workshop':(1.7,1.6), 'Barracks':(1.5,1.3)}
    if name in limits:
        assert max(abs(v.x) for v in coords)<=limits[name][0]+.001 and max(abs(v.y) for v in coords)<=limits[name][1]+.001, f'{name}: decorative geometry exceeds saved building footprint'
        print('PASS authored details fit existing movable footprint', name)

    if name == 'Scavenger' or name in characters:
        rigs = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE']
        assert len(rigs) == 1 and len(rigs[0].data.bones) == 11, 'Invalid skeleton'
        rig = rigs[0]
        assert rig.animation_data and rig.animation_data.action, 'Missing walk animation'
        for obj in meshes:
            for vertex in obj.data.vertices:
                assert vertex.groups and abs(sum(g.weight for g in vertex.groups) - 1) < .001, 'Unnormalized skin weights'
        assert any(len(v.groups) > 1 for o in meshes for v in o.data.vertices), 'Missing blended joints'
        def pose(frame):
            bpy.context.scene.frame_set(int(frame))
            deps = bpy.context.evaluated_depsgraph_get()
            obj = meshes[0].evaluated_get(deps)
            mesh = obj.to_mesh()
            vertices = [v.co.copy() for v in mesh.vertices]
            obj.to_mesh_clear()
            return vertices
        if name in characters:
            assert bpy.data.objects.get('FaceAimOrigin') and bpy.data.objects.get('FaceAimForward'), 'Missing facing markers'
            modes = ('Walk', 'Idle')
        else:
            modes = ('Walk',)
        for mode in modes:
            if name in characters:
                actions = [a for a in bpy.data.actions if mode in a.name]
                assert len(actions) == 1, f'{name}: expected one {mode} action, got {[a.name for a in bpy.data.actions]}'
                rig.animation_data.action = actions[0]
            action = rig.animation_data.action
            start, end = action.frame_range
            assert end - start >= 30, f'Truncated {mode} cycle'
            first = pose(start)
            mid = pose(start + 8)
            last = pose(end)
            minimum = .05 if mode == 'Walk' else .001
            assert max((a-b).length for a,b in zip(first, mid)) > minimum, f'{mode} does not deform mesh'
            assert max((a-b).length for a,b in zip(first, last)) < .005, f'{mode} loop has a pose discontinuity'
        assert 1.5 < dims[2] < 2.1, f'Invalid human height {dims[2]}'
        print('PASS skeletal deformation, blended normalized weights, motion lengths and loop closure')
    print('PASS FBX round trip', name, 'triangles', triangles, 'dimensions', [round(x, 3) for x in dims])

# A walk action can export correctly yet disappear from a source .blend if it has no user.
# Reopen the editable files too, so later rebuilds preserve both animation modes.
for name in characters:
    bpy.ops.wm.open_mainfile(filepath=os.path.join(root, 'Tools/Art/Characters', name + '.blend'))
    assert {a.name for a in bpy.data.actions} == {'Idle', 'Walk'}, f'{name}: editable source lost an animation action'
    assert all(a.use_fake_user for a in bpy.data.actions), f'{name}: source actions are not retained'
    assert bpy.data.objects.get('FaceAimOrigin') and bpy.data.objects.get('FaceAimForward'), f'{name}: source facing markers missing'
    print('PASS editable character source reload', name, 'Idle + Walk retained', flush=True)
