using UnityEngine;
using UnityEngine.EventSystems;

namespace Ash
{
    public sealed class MapZoomInput : MonoBehaviour,IScrollHandler,IPointerClickHandler
    {
        public ScavengingMapUI Owner;
        public void OnPointerClick(PointerEventData data){if(Owner!=null && Owner.CanInteract && !data.dragging)Owner.DismissSelection();}
        public void OnScroll(PointerEventData data)
        {
            if(Owner!=null)Owner.ZoomAt(Mathf.Pow(1.2f,data.scrollDelta.y),data.position);
        }
    }
}
