using System.Collections.Generic;
using UnityEngine;

namespace Ash
{
    public sealed partial class ScavengingMapUI
    {
        private RectTransform mapFooter;
        private readonly Vector3[] visibleCorners=new Vector3[4];
        private WorldCamp[] atlasCamps;
        private int atlasSeed;
        private WorldCamp[] MapCamps
        {
            get{if(atlasCamps==null || atlasSeed!=service.Seed){atlasCamps=worldService.Camps;atlasSeed=service.Seed;}return atlasCamps;}
        }
        private void SetMapTravelView(bool clean)
        {
            if(!clean)return;
            siteSelectionOpen=false;selectedCampId=null;
            if(mapFooter!=null)mapFooter.gameObject.SetActive(false);
            if(campPanel!=null)campPanel.SetActive(false);
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
                bool active=(ReferenceMapSize>=2200 || index<WorldRegions.LegacyCampCount) && visible.Contains(marker.anchoredPosition);index++;
                if(marker.gameObject.activeSelf!=active)marker.gameObject.SetActive(active);
            }
        }
    }
}
