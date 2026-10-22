"""Bake a seamless painted town-earth tile; intentionally does not change overworld terrain."""
import bpy,sys,os,math,random
root=sys.argv[sys.argv.index('--')+1]
path=os.path.join(root,'Tools/Art/render_reference_desert_ground.py');source=open(path).read()
exec(compile(source[:source.index('scene.render.filepath=')],path,'exec'),globals())
# Periodic coordinates retain seamless edges. Sparse branching cracks and small stone facets
# replace plain noise without runtime tessellation, texture layers or extra draw calls.
cracks=n.new('ShaderNodeTexVoronoi');cracks.feature='DISTANCE_TO_EDGE';cracks.inputs['Scale'].default_value=8;l.new(coord.outputs[0],cracks.inputs['Vector'])
cramp=n.new('ShaderNodeValToRGB');cramp.color_ramp.elements[0].position=.001;cramp.color_ramp.elements[0].color=(.35,.35,.35,1);cramp.color_ramp.elements[1].position=.018;cramp.color_ramp.elements[1].color=(1,1,1,1);l.new(cracks.outputs['Distance'],cramp.inputs[0])
crease=n.new('ShaderNodeMixRGB');crease.blend_type='MULTIPLY';crease.inputs[0].default_value=.18;l.new(painted.outputs[0],crease.inputs[1]);l.new(cramp.outputs[0],crease.inputs[2])
stone=n.new('ShaderNodeTexVoronoi');stone.inputs['Scale'].default_value=48;stone.distance='EUCLIDEAN';l.new(coord.outputs[0],stone.inputs['Vector'])
sr=n.new('ShaderNodeValToRGB');sr.color_ramp.elements[0].position=.05;sr.color_ramp.elements[0].color=(.5,.48,.43,1);sr.color_ramp.elements[1].position=.14;sr.color_ramp.elements[1].color=(1,1,1,1);l.new(stone.outputs['Distance'],sr.inputs[0])
finished=n.new('ShaderNodeMixRGB');finished.blend_type='MULTIPLY';finished.inputs[0].default_value=.55;l.new(crease.outputs[0],finished.inputs[1]);l.new(sr.outputs[0],finished.inputs[2]);l.new(finished.outputs[0],emit.inputs['Color'])
scene.render.filepath=os.path.join(out,'Art/OutpostGroundAlbedo.png');bpy.ops.render.render(write_still=True)
print('PASS seamless town-earth paint; world ground unchanged',flush=True)
