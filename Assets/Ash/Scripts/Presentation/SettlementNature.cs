using System.Collections.Generic;
using UnityEngine;

namespace Ash
{
    /// <summary>Deterministic noninteractive decoration, combined by the district's existing art owner.</summary>
    public sealed class SettlementNature : MonoBehaviour
    {
        private Mesh grass,wood;
        public int GrassCount {get;private set;}
        public int DeadTreeCount {get;private set;}
        public static SettlementNature Build(Transform root,SettlementArt art,List<Bounds> occupied,List<Bounds> roads)
        {
            var nature=root.gameObject.AddComponent<SettlementNature>();nature.grass=GrassMesh();nature.wood=WoodMesh();
            bool Clear(Vector3 p,float radius)
            {
                foreach(var b in occupied)if(Mathf.Abs(p.x-b.center.x)<b.extents.x+radius && Mathf.Abs(p.z-b.center.z)<b.extents.z+radius)return false;
                foreach(var b in roads)if(Mathf.Abs(p.x-b.center.x)<b.extents.x+radius && Mathf.Abs(p.z-b.center.z)<b.extents.z+radius)return false;
                return true;
            }
            float Random(int i,int salt)=>(WorldRegions.TileHash(4761,i,salt)&65535)/65535f;
            for(int i=0;i<2400 && nature.GrassCount<420;i++)
            {
                var p=new Vector3(Mathf.Lerp(-26,26,Random(i,1)),.04f,Mathf.Lerp(-30,30,Random(i,2)));
                if(!Clear(p,.20f))continue;
                float scale=Mathf.Lerp(.42f,1.05f,Random(i,3));
                var tuft=art.MeshPiece(root,"Dry grass tuft",p,new Vector3(scale,scale,scale),nature.grass,i%3==0?new Color32(151,126,72,255):i%3==1?new Color32(127,111,70,255):new Color32(168,142,86,255));
                tuft.localRotation=Quaternion.Euler(0,Random(i,4)*360,0);nature.GrassCount++;
            }
            for(int i=0;i<600 && nature.DeadTreeCount<20;i++)
            {
                var p=new Vector3(Mathf.Lerp(-25,25,Random(i,11)),.025f,Mathf.Lerp(-29,29,Random(i,12)));
                if((Mathf.Abs(p.x)<7 && Mathf.Abs(p.z)<10) || !Clear(p,1.1f))continue;
                float scale=Mathf.Lerp(.7f,1.15f,Random(i,13));var rotation=Quaternion.Euler(0,Random(i,14)*360,0);
                void Branch(Vector3 a,Vector3 b,float radius)
                {
                    a=p+rotation*a*scale;b=p+rotation*b*scale;
                    var part=art.MeshPiece(root,"Dead tree branch",(a+b)*.5f,new Vector3(radius*scale,(b-a).magnitude,radius*scale),nature.wood,SettlementArt.Timber);
                    part.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
                }
                Branch(Vector3.zero,new Vector3(.12f,1.85f,.04f),.085f);
                Branch(new Vector3(.04f,.68f,0),new Vector3(-.62f,1.27f,.18f),.045f);
                Branch(new Vector3(-.34f,1.05f,.1f),new Vector3(-.50f,1.60f,.08f),.020f);
                Branch(new Vector3(.08f,1.0f,0),new Vector3(.58f,1.50f,-.22f),.040f);
                Branch(new Vector3(.38f,1.34f,-.16f),new Vector3(.76f,1.36f,-.18f),.017f);
                Branch(new Vector3(.11f,1.42f,.03f),new Vector3(-.2f,1.88f,-.2f),.028f);
                nature.DeadTreeCount++;
            }
            return nature;
        }
        private static Mesh GrassMesh()
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            void Face(Vector3 a,Vector3 b,Vector3 c)
            {
                int start=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);triangles.Add(start);triangles.Add(start+1);triangles.Add(start+2);
                start=vertices.Count;vertices.Add(c);vertices.Add(b);vertices.Add(a);triangles.Add(start);triangles.Add(start+1);triangles.Add(start+2);
            }
            for(int i=0;i<6;i++)
            {
                var r=Quaternion.Euler(0,i*67,0);float height=.25f+(i%3)*.05f;
                var a=r*new Vector3(-.017f,0,0);var b=r*new Vector3(.017f,0,0);var mid=r*new Vector3(.055f,height*.6f,.012f);var tip=r*new Vector3(.11f,height,.025f);
                Face(a,b,mid);Face(a,mid,tip);
            }
            var mesh=new Mesh{name="Shared dry grass blades"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static Mesh WoodMesh()
        {
            var vertices=new List<Vector3>();var faces=new List<int>();
            for(int i=0;i<6;i++){float a=i*Mathf.PI/3;vertices.Add(new Vector3(Mathf.Cos(a),-.5f,Mathf.Sin(a)));vertices.Add(new Vector3(Mathf.Cos(a),.5f,Mathf.Sin(a)));}
            for(int i=0;i<6;i++){int a=i*2,b=(i+1)%6*2;faces.AddRange(new[]{a,a+1,b,a+1,b+1,b});}
            for(int i=1;i<5;i++)faces.AddRange(new[]{0,i*2,i*2+2,1,i*2+3,i*2+1});
            var mesh=new Mesh{name="Shared bare wood branch"};mesh.SetVertices(vertices);mesh.SetTriangles(faces,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private void OnDestroy(){if(grass!=null)SettlementArt.Release(grass);if(wood!=null)SettlementArt.Release(wood);}
    }
}
