using UnityEngine;
using UnityEngine.EventSystems;

namespace Ash
{
    public sealed class MapZoomInput : MonoBehaviour,IScrollHandler
    {
        public ScavengingMapUI Owner;
        public void OnScroll(PointerEventData data)
        {
            if(Owner!=null)Owner.ZoomAt(Mathf.Pow(1.2f,data.scrollDelta.y),data.position);
        }
    }
}
