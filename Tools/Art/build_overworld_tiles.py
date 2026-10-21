"""Original flat top-down map symbols; procedural source, no external artwork."""
from PIL import Image, ImageDraw
from pathlib import Path
import random
ROOT=Path(__file__).resolve().parents[2]
S=3
atlas=Image.new('RGBA',(768*S,512*S))
for cell,name in enumerate(('Food','Water','Scrap','Lumber','Camp','Home')):
 im=Image.new('RGBA',(256*S,256*S));d=ImageDraw.Draw(im)
 def rect(box,fill,outline=None,width=2):d.rectangle(tuple(int(v*S) for v in box),fill,outline,width*S)
 def oval(box,fill,outline=None,width=2):d.ellipse(tuple(int(v*S) for v in box),fill,outline,width*S)
 def line(points,fill,width=2):d.line([(int(x*S),int(y*S)) for x,y in points],fill,width*S)
 def poly(points,fill):d.polygon([(int(x*S),int(y*S)) for x,y in points],fill)
 # Compact tile footprint with broken, dusty boundary; deliberately no isometric faces.
 poly([(32,54),(82,28),(192,38),(230,94),(218,198),(174,225),(60,213),(25,164)],'#4c4838')
 poly([(28,49),(79,23),(188,33),(225,89),(213,193),(169,220),(55,208),(20,159)],'#a28e65')
 rng=random.Random(cell+144)
 for _ in range(38):
  x,y=rng.randrange(45,204),rng.randrange(48,199);oval((x,y,x+2,y+2),'#807654')
 if name=='Food':
  rect((45,50,176,191),'#4e4532','#786244',4)
  for y in range(59,185,25):
   rect((52,y,166,y+16),'#6a5435')
   for x in range(60,165,20):oval((x,y-1,x+14,y+13),'#648142','#9ba05d',1)
  rect((184,65,211,122),'#526e71','#344f52');line([(196,66),(196,120)],'#9ca8a1',3)
  rect((182,145,216,183),'#847150','#534735',3)
 elif name=='Water':
  oval((60,59,186,185),'#696351','#474d46',5);oval((72,71,174,173),'#b4a98b','#827c64',5)
  oval((88,87,158,157),'#315861','#213d46',4);oval((98,96,150,148),'#4b7a82')
  for i in range(8):
   import math
   a=i*math.pi/4;line([(123+52*math.cos(a),122+52*math.sin(a)),(123+63*math.cos(a),122+63*math.sin(a))],'#6b6656',3)
  rect((111,49,132,193),'#75573b','#443e31',3);rect((76,107,172,126),'#96754c','#554936',3)
  rect((182,175,210,202),'#587a7a','#354d52',3)
 elif name=='Scrap':
  for box in [(44,62,104,93),(140,134,207,165)]:
   rect(box,'#793f2d','#4a3b30',3);x,y,x2,y2=box;rect((x+16,y+4,x2-15,y2-4),'#434e4b')
   for yy in (y-4,y2-2):rect((x+8,yy,x+21,yy+6),'#343b35');rect((x2-21,yy,x2-8,yy+6),'#343b35')
  for i in range(7):
   x=48+i*10;rect((x,130+i%2*5,x+7,183),'#9a6840','#4e4d40',1)
  for x,y in [(157,66),(190,82),(150,105)]:oval((x,y,x+23,y+23),'#3c4540','#72776a',3);oval((x+7,y+7,x+16,y+16),'#95875f')
 elif name=='Lumber':
  for x,y,r in [(80,76,34),(152,64,31),(119,128,41),(176,147,34),(66,165,29)]:
   oval((x-r+3,y-r+4,x+r+3,y+r+4),'#585c3c');poly([(x-r,y),(x-r//2,y-r),(x+r//2,y-r+4),(x+r,y),(x+r//2,y+r),(x-r//2,y+r-3)],'#415f3d')
   poly([(x-r//2,y),(x,y-r+5),(x+r//2,y+2),(x,y+r//2)],'#718449');rect((x-3,y-4,x+3,y+5),'#9f8c5f')
  for y in (188,201):rect((137,y,192,y+8),'#8f7043','#564932',2);oval((185,y,195,y+8),'#b49b66')
 else:
  rect((37,44,215,207),'#6a6552','#353f39',5)
  rect((45,52,207,199),'#938263','#b79d6c',3)
  for x,y in [(39,46),(196,46),(39,185),(196,185)]:rect((x,y,x+17,y+17),'#5d5550','#bd8c50',3)
  for x,y,w,h in [(65,70,61,42),(145,72,42,58),(65,144,58,39)]:
   rect((x+3,y+3,x+w+3,y+h+3),'#534d3d');rect((x,y,x+w,y+h),'#8b4d34' if name=='Camp' else '#b3975b','#503f30',3)
   for yy in range(y+6,y+h-2,7):line([(x+3,yy),(x+w-3,yy)],'#b77743' if name=='Camp' else '#d0b67c',2)
  rect((145,151,188,181),'#536e70','#35494b',3)
  rect((112,195,141,211),'#9a895f');line([(127,193),(127,215)],'#d4b96f',3)
  if name=='Home':oval((103,112,147,156),'#dec084','#725b36',3);poly([(114,134),(126,121),(138,134),(138,146),(114,146)],'#4c5c4a')
 atlas.alpha_composite(im,((cell%3)*256*S,(cell//3)*256*S))
atlas=atlas.resize((768,512),Image.Resampling.LANCZOS)
path=ROOT/'Assets/Ash/Resources/Map/OverworldAtlas.png';atlas.save(path)
path.with_name('OverworldAtlasPixels.bytes').write_bytes(atlas.transpose(Image.Transpose.FLIP_TOP_BOTTOM).tobytes())
print(path)
