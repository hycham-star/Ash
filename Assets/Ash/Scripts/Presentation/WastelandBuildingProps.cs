using UnityEngine;

namespace Ash
{
    /// <summary>Original low-poly environment meshes. Visuals only; owned by each movable building.</summary>
    public sealed class WastelandBuildingProps : MonoBehaviour
    {
        private SettlementArt art;
        private Mesh drumMesh,bagMesh;
        public static void Attach(Transform parent,string building,Vector2 footprint)
        {
            var root=new GameObject("Wasteland environment props");root.transform.SetParent(parent,false);
            root.AddComponent<WastelandBuildingProps>().Build(building,footprint);
        }
        private void Build(string building,Vector2 footprint)
        {
            art=new SettlementArt();drumMesh=RoundMesh(false);bagMesh=RoundMesh(true);
            float w=footprint.x,d=footprint.y;
            Color soot=new Color32(51,48,40,255),cloth=new Color32(133,111,70,255),sack=new Color32(136,123,91,255);
            // Uneven reclaimed paving leaves an earth border around the existing foundations.
            for(int i=0;i<4;i++)
            {
                float x=(i%2==0?-.30f:.30f)*w,z=(i<2?-.29f:.29f)*d;
                var slab=Part("Broken paving",new Vector3(x,.105f,z),new Vector3(w*.42f,.045f,d*.39f),i%2==0?SettlementArt.Concrete:SettlementArt.Sand);
                slab.localRotation=Quaternion.Euler(0,i*7-10,0);
            }
            // A clear central doorway remains open between two small defensive stacks.
            for(int side=-1;side<=1;side+=2)for(int row=0;row<2;row++)for(int i=0;i<2;i++)
                Part("Staggered sandbag",new Vector3(side*w*.32f+(i-.5f)*.32f+(row==1?.06f:0),.23f+row*.19f,-d*.44f),new Vector3(.40f,.21f,.26f),sack,PrimitiveType.Capsule).localRotation=Quaternion.Euler(0,0,90);
            for(int i=0;i<2;i++)
            {
                float x=w*.39f,z=d*(.23f+i*.15f);
                Part("Salvaged fuel drum",new Vector3(x,.40f,z),new Vector3(.32f,.29f,.32f),i==0?SettlementArt.Rust:SettlementArt.Metal,PrimitiveType.Cylinder);
                for(int band=0;band<2;band++)Part("Drum reinforcing band",new Vector3(x,.25f+band*.30f,z),new Vector3(.34f,.025f,.34f),soot,PrimitiveType.Cylinder);
            }
            bool garden=building=="FoodGarden",water=building=="WaterStation";
            if(water)
            {
                // Small wind-driven pump: recognizable silhouette above the tanks.
                var mast=new Vector3(-w*.34f,0,d*.30f);
                Rod("Pump mast",mast+Vector3.up*.15f,mast+Vector3.up*3.3f,.055f,SettlementArt.Metal);
                for(int i=0;i<4;i++)
                {
                    var blade=Part("Wind pump sail",mast+new Vector3(0,3.05f,-.08f),new Vector3(.12f,1.05f,.035f),i%2==0?cloth:SettlementArt.Rust);
                    blade.localRotation=Quaternion.Euler(0,0,i*45);
                }
            }
            else
            {
                float height=garden?1.1f:building=="Headquarters"?2.65f:2.30f;
                // Corrugated salvage roof patches, with visible ribs and mismatched repair plates.
                if(!garden)for(int patch=0;patch<3;patch++)
                {
                    float x=(patch-1)*w*.22f,z=d*.13f;
                    float roofHeight=(building=="Headquarters"?3.26f:building=="Workshop"?2.67f:2.30f)-.20f;
                    var plate=Part("Riveted roof repair",new Vector3(x,roofHeight,z),new Vector3(w*.24f,.045f,d*.36f),patch==1?cloth:SettlementArt.Rust);
                    plate.localRotation=Quaternion.Euler(20,patch*6-5,0);
                    for(int rib=0;rib<4;rib++)Rod("Roof corrugation",new Vector3(x-w*.10f+rib*w*.065f,roofHeight+.055f+d*.055f,z-d*.16f),new Vector3(x-w*.10f+rib*w*.065f,roofHeight+.055f-d*.055f,z+d*.16f),.012f,SettlementArt.Metal);
                }
                // Sloped reclaimed shade cloth, exposed poles and crossed tension stays.
                float z0=-d*.22f,z1=-d*.46f;
                for(int side=-1;side<=1;side+=2)
                {
                    float x=side*w*.30f;
                    Rod("Awning post",new Vector3(x,.15f,z1),new Vector3(x,height-.30f,z1),.035f,SettlementArt.Timber);
                    Rod("Awning guy",new Vector3(x,height-.32f,z1),new Vector3(side*w*.43f,.15f,z1),.010f,SettlementArt.Metal);
                }
                var canopy=Part("Patched shade canvas",new Vector3(0,height-.16f,(z0+z1)/2),new Vector3(w*.67f,.035f,z0-z1+.10f),cloth);
                canopy.localRotation=Quaternion.Euler(-16,0,0);
                Part("Canvas repair patch",new Vector3(-w*.15f,height-.12f,(z0+z1)/2),new Vector3(w*.17f,.025f,.18f),SettlementArt.Metal).localRotation=canopy.localRotation;
                if(building=="Workshop")
                {
                    // Scrap sorting rack and angled sheet stock, rather than a clean generic shed.
                    for(int i=0;i<4;i++)Part("Reclaimed sheet stock",new Vector3(w*.40f,.65f,-d*.08f+i*.13f),new Vector3(.065f,.85f,.55f),i%2==0?SettlementArt.Rust:SettlementArt.Metal).localRotation=Quaternion.Euler(0,0,-18+i*3);
                }
            }
            art.Combine(transform);
        }
        private Transform Part(string name,Vector3 position,Vector3 size,Color color,PrimitiveType shape=PrimitiveType.Cube)
        {
            var part=art.Box(transform,name,position,size,color,true,shape);
            if(shape==PrimitiveType.Cylinder || shape==PrimitiveType.Capsule)
                part.GetComponent<MeshFilter>().sharedMesh=shape==PrimitiveType.Cylinder?drumMesh:bagMesh;
            return part;
        }
        private static Mesh RoundMesh(bool bag)
        {
            // Eight-sided drums; four-band sacks. No high-resolution sphere/capsule geometry.
            var vertices=new System.Collections.Generic.List<Vector3>();
            var indices=new System.Collections.Generic.List<int>();
            int rings=bag?4:1;
            for(int row=0;row<rings;row++)for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4,b=(i+1)*Mathf.PI/4;
                float y0=bag?-Mathf.Cos(row*Mathf.PI/rings):-1,y1=bag?-Mathf.Cos((row+1)*Mathf.PI/rings):1;
                float r0=bag?Mathf.Sin(row*Mathf.PI/rings)*.5f:.5f,r1=bag?Mathf.Sin((row+1)*Mathf.PI/rings)*.5f:.5f;
                Vector3 p=new Vector3(Mathf.Cos(a)*r0,y0,Mathf.Sin(a)*r0),q=new Vector3(Mathf.Cos(b)*r0,y0,Mathf.Sin(b)*r0);
                Vector3 r=new Vector3(Mathf.Cos(a)*r1,y1,Mathf.Sin(a)*r1),t=new Vector3(Mathf.Cos(b)*r1,y1,Mathf.Sin(b)*r1);
                Triangle(vertices,indices,p,r,q);Triangle(vertices,indices,q,r,t);
                if(!bag){Triangle(vertices,indices,new Vector3(0,-1,0),p,q);Triangle(vertices,indices,new Vector3(0,1,0),t,r);}
            }
            var mesh=new Mesh{name=bag?"Low-poly sandbag":"Eight-sided salvage drum"};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static void Triangle(System.Collections.Generic.List<Vector3> vertices,System.Collections.Generic.List<int> indices,Vector3 a,Vector3 b,Vector3 c)
        {
            int start=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);indices.Add(start);indices.Add(start+1);indices.Add(start+2);
        }
        private void Rod(string name,Vector3 start,Vector3 end,float radius,Color color)
        {
            var rod=Part(name,(start+end)/2,new Vector3(radius*2,(end-start).magnitude/2,radius*2),color,PrimitiveType.Cylinder);
            rod.localRotation=Quaternion.FromToRotation(Vector3.up,end-start);
        }
        private void OnDestroy(){art?.Dispose();if(drumMesh!=null)SettlementArt.Release(drumMesh);if(bagMesh!=null)SettlementArt.Release(bagMesh);}
    }
}
