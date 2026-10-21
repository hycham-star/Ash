using UnityEngine;
using UnityEngine.UI;

namespace Ash
{
    /// <summary>Nine-slice UI mesh preserves painted corner rivets without runtime Sprite creation.</summary>
    public sealed class WastelandMetalFrame : MaskableGraphic
    {
        public float Border=10;
        public override Texture mainTexture=>WastelandHudSkin.Atlas;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();var r=GetPixelAdjustedRect();if(r.width<=0 || r.height<=0)return;
            float bx=Mathf.Min(Border,r.width*.4f),by=Mathf.Min(Border,r.height*.4f);
            var x=new[]{r.xMin,r.xMin+bx,r.xMax-bx,r.xMax};var y=new[]{r.yMin,r.yMin+by,r.yMax-by,r.yMax};
            const float edge=WastelandHudSkin.AtlasSize;
            // Generated first-cell frame, excluding its transparent outer gutter.
            var u=new[]{16/edge,80/edge,240/edge,304/edge};
            var v=new[]{(edge-304)/edge,(edge-240)/edge,(edge-80)/edge,(edge-16)/edge};
            for(int row=0;row<3;row++)for(int column=0;column<3;column++)
            {
                int start=vh.currentVertCount;
                vh.AddVert(new Vector3(x[column],y[row],0),color,new Vector2(u[column],v[row]));
                vh.AddVert(new Vector3(x[column],y[row+1],0),color,new Vector2(u[column],v[row+1]));
                vh.AddVert(new Vector3(x[column+1],y[row+1],0),color,new Vector2(u[column+1],v[row+1]));
                vh.AddVert(new Vector3(x[column+1],y[row],0),color,new Vector2(u[column+1],v[row]));
                vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
