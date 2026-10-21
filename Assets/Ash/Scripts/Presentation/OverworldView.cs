using System.Collections.Generic;
using UnityEngine;

namespace Ash
{
    public sealed partial class ScavengingMapUI
    {
        private RectTransform mapFooter;
        private GameObject restoreMapControls;
        private readonly Vector3[] visibleCorners=new Vector3[4];
        private WorldCamp[] atlasCamps;
        private int atlasSeed;
        private WorldCamp[] MapCamps
        {
            get{if(atlasCamps==null || atlasSeed!=service.Seed){atlasCamps=worldService.Camps;atlasSeed=service.Seed;}return atlasCamps;}
        }
        private void BuildMapTravelControls()
        {
            var footer=new GameObject("World map bottom controls",typeof(RectTransform));mapFooter=(RectTransform)footer.transform;mapFooter.SetParent(transform,false);
            mapFooter.anchorMin=Vector2.zero;mapFooter.anchorMax=Vector2.one;mapFooter.offsetMin=mapFooter.offsetMax=Vector2.zero;
            var controls=new List<RectTransform>();
            foreach(Transform child in transform)
            {
                var rect=child as RectTransform;
                if(rect!=null && rect!=mapFooter && rect.anchorMax.y==0 && child.gameObject!=campPanel)controls.Add(rect);
            }
            foreach(var control in controls)control.SetParent(mapFooter,false);
            restoreMapControls=Control(transform,"CONTROLS",Vector2.zero,Vector2.zero,new Vector2(10,10),new Vector2(122,44),()=>SetMapTravelView(false)).gameObject;
            restoreMapControls.SetActive(false);
        }
        private void SetMapTravelView(bool clean)
        {
            if(mapFooter==null)return;
            mapFooter.gameObject.SetActive(!clean);restoreMapControls.SetActive(clean);
            workforce.transform.parent.gameObject.SetActive(!clean);
            viewport.offsetMin=new Vector2(8,clean?8:200);viewport.offsetMax=new Vector2(-8,clean?-52:-86);
            Canvas.ForceUpdateCanvases();ClampMap();CullMapMarkers();
        }
        private void RefreshMapCoordinates()
        {
            if(worldService==null || viewport==null)return;
            var center=markerRoot.InverseTransformPoint(viewport.TransformPoint(viewport.rect.center));
            int x=WorldRegions.Tile(center.x*WorldRegions.TileCount/(side-32)),y=WorldRegions.Tile(center.y*WorldRegions.TileCount/(side-32));
            workforce.text=WorldRegions.Name(WorldRegions.ThreatAt(MapCamps,x,y))+" • Lv "+WorldRegions.ThreatAt(MapCamps,x,y)+" • X "+x+" / Y "+y;
        }
        private void CullMapMarkers()
        {
            if(worldService==null || markerRoot==null || viewport==null)return;
            viewport.GetWorldCorners(visibleCorners);
            var min=markerRoot.InverseTransformPoint(visibleCorners[0]);var max=markerRoot.InverseTransformPoint(visibleCorners[2]);
            var visible=UnityEngine.Rect.MinMaxRect(min.x-160,min.y-160,max.x+160,max.y+160);
            int index=0;
            foreach(var camp in MapCamps)
            {
                if(!campMarkers.TryGetValue(camp.Id,out var marker))continue;
                bool active=(side>=1200 || index<WorldRegions.LegacyCampCount) && visible.Contains(marker.anchoredPosition);index++;
                if(marker.gameObject.activeSelf!=active)marker.gameObject.SetActive(active);
            }
        }
    }
}
