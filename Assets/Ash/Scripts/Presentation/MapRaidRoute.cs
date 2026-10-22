using UnityEngine;
using UnityEngine.UI;

namespace Ash
{
    /// <summary>Clipped UI route in map coordinates. Decorative geometry never receives clicks.</summary>
    public sealed class MapRaidRoute : MaskableGraphic
    {
        private Vector2 destination;
        protected override void Awake(){base.Awake();raycastTarget=false;color=new Color32(244,204,121,210);}
        public void SetDestination(Vector2 point)
        {
            if(destination==point)return;
            destination=point;rectTransform.sizeDelta=new Vector2(Mathf.Abs(point.x)+20,Mathf.Abs(point.y)+20);
            rectTransform.anchoredPosition=point*.5f;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();float length=destination.magnitude;if(length<1)return;
            Vector2 direction=destination/length,normal=new Vector2(-direction.y,direction.x);
            // Fixed screen thickness and bounded geometry even at maximum map zoom.
            int segments=Mathf.Clamp(Mathf.CeilToInt(length/22),1,256);
            float step=length/segments;
            for(int i=0;i<segments;i++) {
                Vector2 start=direction*(i*step),end=direction*Mathf.Min(length,i*step+step*.6f);
                int at=mesh.currentVertCount;
                Vertex(mesh,start-normal);Vertex(mesh,start+normal);Vertex(mesh,end+normal);Vertex(mesh,end-normal);
                mesh.AddTriangle(at,at+1,at+2);mesh.AddTriangle(at,at+2,at+3);
            }
            for(int i=1;i<=3;i++) {
                Vector2 tip=destination*(i/3f),tail=tip-direction*9;
                int at=mesh.currentVertCount;
                Vertex(mesh,tip);Vertex(mesh,tail+normal*4);Vertex(mesh,tail-normal*4);mesh.AddTriangle(at,at+1,at+2);
            }
        }
        private void Vertex(VertexHelper mesh,Vector2 position)
        {
            var vertex=UIVertex.simpleVert;vertex.position=position-destination*.5f;vertex.color=color;mesh.AddVert(vertex);
        }
    }
}
