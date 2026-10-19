"""Create a PDF of actual Blender hero renders after rebuilding the eight heroes.
Requires reportlab. Run from the repository root with python3.
"""
from pathlib import Path
from reportlab.pdfgen import canvas
from reportlab.lib.colors import HexColor

root=Path(__file__).resolve().parents[2]
groups=[('ORDINARY - GREY','#BEC3C9',[('MaraFlint','Mara Flint'),('JonasReed','Jonas Reed')]),
        ('RARE - BLUE','#57B0FF',[('SeraVale','Sera Vale'),('BramHolt','Bram Holt')]),
        ('EPIC - PURPLE','#C385FF',[('IlyraVoss','Ilyra Voss'),('KadeRook','Kade Rook')]),
        ('LEGENDARY - GOLD','#FFC74B',[('AshaSol','Asha Sol'),('OrinAshcroft','Orin Ashcroft')])]
output=root/'Docs/HeroRarityUniforms.pdf'
c=canvas.Canvas(str(output),pagesize=(595,842));c.setTitle('Ashlands - Rarity-colored hero uniforms')
for page in range(2):
    c.setFillColorRGB(.07,.10,.11);c.rect(0,0,595,842,fill=1,stroke=0)
    c.setFillColorRGB(1,1,1);c.setFont('Helvetica-Bold',21);c.drawString(35,800,'ASHLANDS - HERO UNIFORMS')
    c.setFont('Helvetica',10);c.drawString(35,778,'Actual Blender renders of exported art; Unity lighting may differ.')
    for row,(title,color,heroes) in enumerate(groups[page*2:page*2+2]):
        y=735-row*345;c.setFillColor(HexColor(color));c.setFont('Helvetica-Bold',17);c.drawString(35,y,title)
        for col,(model,name) in enumerate(heroes):
            path=root/'Tools/Art/Characters'/f'{model}_Preview.png'
            c.drawImage(str(path),65+col*265,y-289,width=195,height=260,preserveAspectRatio=True)
            c.setFillColor(HexColor(color));c.setFont('Helvetica-Bold',12);c.drawCentredString(162+col*265,y-309,name)
    c.setFillColorRGB(.8,.83,.84);c.setFont('Helvetica',9);c.drawString(35,33,'Matching coats, sleeves and trousers; natural skin, hair and equipment details.')
    c.showPage()
c.save();print('Created',output)
