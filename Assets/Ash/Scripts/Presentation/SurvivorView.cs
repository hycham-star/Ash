using UnityEngine;

namespace Ash
{
    /// <summary>Original assembled character with visual-only limb animation and patrol.</summary>
    public sealed class SurvivorView : MonoBehaviour
    {
        private Transform leftLeg, rightLeg, leftArm, rightArm;
        private Vector3[] route;
        private int waypoint;
        private float phase;
        public static SurvivorView Create(SettlementArt art, Transform parent, string name, int appearance, Vector3 position, Vector3[] patrol = null)
        {
            var root = new GameObject(name).transform; root.SetParent(parent, false); root.localPosition = position;
            var view = root.gameObject.AddComponent<SurvivorView>(); view.route = patrol; view.phase = appearance * .71f;
            Color[] coats = { new Color32(73,105,111,255), new Color32(130,104,67,255), new Color32(99,117,77,255), new Color32(130,76,56,255), new Color32(97,93,127,255), new Color32(77,102,118,255), new Color32(182,145,72,255), new Color32(71,75,84,255) };
            Color[] skins = { new Color32(181,126,88,255), new Color32(219,166,126,255), new Color32(118,77,58,255), new Color32(200,147,108,255) };
            var coat = coats[appearance % coats.Length]; var skin = skins[appearance % skins.Length];
            art.Box(root, "Jacket", new Vector3(0,.92f,0), new Vector3(.48f,.55f,.3f), coat, false);
            art.Box(root, "Belt", new Vector3(0,.67f,0), new Vector3(.5f,.08f,.32f), SettlementArt.Timber, false);
            art.Box(root, "Neck", new Vector3(0,1.25f,0), new Vector3(.14f,.18f,.14f), skin, false);
            art.Box(root, "Face", new Vector3(0,1.47f,0), new Vector3(.32f,.38f,.28f), skin, false);
            art.Box(root, "Hair", new Vector3(0,1.67f,.01f), new Vector3(.35f,.13f,.31f), new Color32(47,42,35,255), false);
            art.Box(root, "Bandana", new Vector3(0,1.51f,-.149f), new Vector3(.34f,.065f,.025f), SettlementArt.Amber, false);
            art.Box(root, "Left eye", new Vector3(-.075f,1.43f,-.145f), new Vector3(.045f,.035f,.02f), SettlementArt.Metal, false);
            art.Box(root, "Right eye", new Vector3(.075f,1.43f,-.145f), new Vector3(.045f,.035f,.02f), SettlementArt.Metal, false);
            art.Box(root, "Backpack", new Vector3(0,.99f,.24f), new Vector3(.32f,.4f,.17f), SettlementArt.Timber, false);
            art.Box(root, "Chest strap", new Vector3(.09f,.95f,-.17f), new Vector3(.07f,.5f,.03f), SettlementArt.Timber, false).localRotation = Quaternion.Euler(0,0,-22);
            view.leftLeg = Limb(art, root, "Left leg", new Vector3(-.13f,.64f,0), new Vector3(.19f,.48f,.22f), SettlementArt.Metal, true);
            view.rightLeg = Limb(art, root, "Right leg", new Vector3(.13f,.64f,0), new Vector3(.19f,.48f,.22f), SettlementArt.Metal, true);
            view.leftArm = Limb(art, root, "Left arm", new Vector3(-.31f,1.14f,0), new Vector3(.16f,.45f,.19f), coat, false);
            view.rightArm = Limb(art, root, "Right arm", new Vector3(.31f,1.14f,0), new Vector3(.16f,.45f,.19f), coat, false);
            art.Box(view.rightArm, "Hand", new Vector3(0,-.49f,0), new Vector3(.14f,.14f,.14f), skin, false);
            art.Box(view.leftArm, "Hand", new Vector3(0,-.49f,0), new Vector3(.14f,.14f,.14f), skin, false);
            art.Box(root, "Radio", new Vector3(-.14f,.8f,-.19f), new Vector3(.12f,.19f,.08f), SettlementArt.Metal, false);
            return view;
        }
        private static Transform Limb(SettlementArt art, Transform parent, string name, Vector3 position, Vector3 size, Color color, bool boot)
        {
            var pivot = new GameObject(name).transform; pivot.SetParent(parent, false); pivot.localPosition = position;
            art.Box(pivot, name, new Vector3(0,-size.y/2,0), size, color, false);
            if (boot) art.Box(pivot, "Boot", new Vector3(0,-size.y-.05f,-.035f), new Vector3(.22f,.13f,.31f), new Color32(44,42,38,255), false);
            return pivot;
        }
        private void Update()
        {
            bool walking = route != null && route.Length > 0;
            if (walking)
            {
                var destination = route[waypoint]; var delta = destination - transform.localPosition;
                if (delta.magnitude < .12f) waypoint = (waypoint + 1) % route.Length;
                else { transform.localPosition = Vector3.MoveTowards(transform.localPosition, destination, Time.deltaTime * .8f); transform.localRotation = Quaternion.LookRotation(-delta); }
            }
            float swing = Mathf.Sin(Time.time * (walking ? 6 : 1.5f) + phase) * (walking ? 24 : 3);
            leftLeg.localRotation = Quaternion.Euler(swing,0,0); rightLeg.localRotation = Quaternion.Euler(-swing,0,0);
            leftArm.localRotation = Quaternion.Euler(-swing,0,-7); rightArm.localRotation = Quaternion.Euler(swing,0,7);
        }
    }
}
