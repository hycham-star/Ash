using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Ash
{
    public sealed partial class SettlementView
    {
        private GameObject districtScenery;
        private SettlementArt districtArt;
        private string districtSignature;
        private static readonly Color DistrictShoulder=new Color32(137,111,80,255);
        private void RefreshDistrictScenery()
        {
            if(world==null)return;
            var placement=gameManager.Buildings.CapturePlacement();var development=gameManager.Buildings.CaptureDevelopment();var signature=new StringBuilder();
            foreach(var p in placement.Positions)signature.Append(p.Id).Append(':').Append(p.X).Append(':').Append(p.Z).Append(';');
            foreach(var p in development.Plots)signature.Append(p.Id).Append(':').Append((int)p.Kind).Append(';');
            if(districtSignature==signature.ToString())return;districtSignature=signature.ToString();
            if(districtScenery!=null){districtScenery.SetActive(false);SettlementArt.Release(districtScenery);}districtArt?.Dispose();districtArt=new SettlementArt();
            districtScenery=new GameObject("Outpost streets and landscaping");districtScenery.transform.SetParent(world,false);var root=districtScenery.transform;
            // Keep road cuts clear of all reserved lots, plus every actual moved building.
            var blocked=new List<Bounds>();var natureObstacles=new List<Bounds>();var roads=new List<Bounds>();
            foreach(var p in SettlementLayout.Plots)blocked.Add(new Bounds(new Vector3(p.X,0,p.Z),new Vector3(6,1,6)));
            foreach(var p in placement.Positions)
            {
                bool exists=true;foreach(var plot in development.Plots)if(plot.Id==p.Id){exists=plot.Kind!=PlotKind.Empty;break;}
                if(!exists)continue;var f=SettlementPlacementCatalog.Find(p.Id);var footprint=new Bounds(new Vector3(p.X,0,p.Z),new Vector3(f.Width,1,f.Depth));blocked.Add(footprint);natureObstacles.Add(footprint);
            }
            void Road(float fixedAxis,bool across,float width,float limit=-1)
            {
                float extent=across?SettlementLayout.HalfWidth-1:SettlementLayout.HalfDepth-1;
                if(limit>0)extent=Mathf.Min(extent,limit);
                var cuts=new List<Vector2>();
                foreach(var b in blocked)
                {
                    float side=across?b.center.z:b.center.x,half=across?b.extents.z:b.extents.x;
                    if(Mathf.Abs(side-fixedAxis)>=half+width*.5f+.03f)continue;
                    float along=across?b.center.x:b.center.z,span=across?b.extents.x:b.extents.z;cuts.Add(new Vector2(along-span-.25f,along+span+.25f));
                }
                cuts.Sort((a,b)=>a.x.CompareTo(b.x));float start=-extent;
                void Segment(float a,float b)
                {
                    if(b-a<.2f)return;float middle=(a+b)*.5f;
                    Vector3 pos=across?new Vector3(middle,.035f,fixedAxis):new Vector3(fixedAxis,.03f,middle);
                    Vector3 size=across?new Vector3(b-a,.018f,width):new Vector3(width,.018f,b-a);
                    roads.Add(new Bounds(pos,size+new Vector3(.7f,1,.7f)));
                    districtArt.Box(root,"Dusty road shoulder",pos-Vector3.up*.012f,size+new Vector3(across?0:.65f,0,across?.65f:0),DistrictShoulder);
                    districtArt.Box(root,"Worn asphalt street",pos,size,SettlementArt.Asphalt);
                }
                foreach(var cut in cuts){if(cut.x>extent)break;Segment(start,Mathf.Min(extent,cut.x));start=Mathf.Max(start,cut.y);}
                Segment(start,extent);
            }
            // Connected spines run between the saved lots; the outer ring joins all districts.
            foreach(float x in new[]{-18f,-8,8,18})Road(x,false,1.8f,20);
            Road(0,false,1.2f,10);
            foreach(float z in new[]{-20f,-10,-1.9f,20})Road(z,true,z==-1.9f?1.2f:1.8f);
            foreach(float x in new[]{-26.8f,26.8f})Road(x,false,1.2f,30);
            foreach(float z in new[]{-30f,30f})Road(z,true,1.2f);
            bool Free(Vector3 p,float radius)
            {
                foreach(var b in blocked)if(Mathf.Abs(p.x-b.center.x)<b.extents.x+radius && Mathf.Abs(p.z-b.center.z)<b.extents.z+radius)return false;
                return true;
            }
            // Vacant lots can grow dry vegetation; constructing there clears it on the next refresh.
            SettlementNature.Build(root,districtArt,natureObstacles,roads);
            int trees=0;
            foreach(var p in new[]{new Vector3(-5.8f,0,8),new Vector3(5.8f,0,8),new Vector3(-5.8f,0,-7),new Vector3(5.8f,0,-7)})
                if(Free(p,.35f))RealisticAssets.Scenery(root,"OutpostTree",p+Vector3.up*.025f,.52f,trees++*137.5f);
            // Small, noninteractive street clutter stays away from every building and vacant lot.
            for(int i=0;i<12;i++)
            {
                var p=new Vector3(i%2==0?-5.9f:5.9f,.2f,-18+(i/2)*6.5f);if(!Free(p,.45f))continue;
                districtArt.Box(root,"Street supply crate",p,new Vector3(.32f,.34f,.35f),SettlementArt.Timber);
                districtArt.Box(root,"Crate binding",p+Vector3.up*.18f,new Vector3(.04f,.02f,.36f),SettlementArt.Metal);
            }
            districtArt.Combine(root);
        }
        private void ResidentialShelters(Transform root,int level)
        {
            var points=new[]{new Vector3(-1.25f,.03f,-1.1f),new Vector3(1.25f,.03f,-1.1f),new Vector3(0,.03f,1.15f)};
            for(int i=0;i<points.Length;i++)RealisticAssets.Scenery(root,"OutpostHut",points[i],.8f+Mathf.Min(level,5)*.008f,i==2?180:0);
        }
    }
}
