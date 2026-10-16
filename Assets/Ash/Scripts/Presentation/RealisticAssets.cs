using System;
using UnityEngine;

namespace Ash
{
    /// <summary>Original imported visual slice; independent of resources, armies, saves and gathering.</summary>
    public static class RealisticAssets
    {
        public static GameObject Headquarters(Transform parent)
        {
            var model=Create("Headquarters","HeadquartersMaterial",parent);
            model.name="Weathered Headquarters — original detailed mesh";
            Create("HeadquartersSurroundings","HeadquartersSurroundingsMaterial",parent);
            Grounding(model.transform,new Vector3(0,.17f,0),new Vector2(4.3f,4.1f));
            return model;
        }
        public static GameObject Building(Transform parent,string assetName,Vector2 footprint)
        {
            var model=Create(assetName,assetName+"Material",parent);
            model.name=assetName+" — original textured building";
            Grounding(model.transform,new Vector3(0,.065f,0),footprint+new Vector2(.25f,.25f));
            return model;
        }
        public static GameObject Scavenger(Transform parent,Vector3 position,Vector3[] patrol)
        {
            var root=new GameObject("Detailed scavenger — visual patrol"); root.transform.SetParent(parent,false); root.transform.localPosition=position;
            var model=Create("Scavenger","ScavengerMaterial",root.transform);
            model.transform.localPosition=Vector3.up*.10f;
            var motion=root.AddComponent<DetailedScavenger>(); motion.Configure(model,patrol);
            Grounding(root.transform,new Vector3(0,.105f,0),new Vector2(.7f,.55f));
            return root;
        }
        private static GameObject Create(string modelName,string materialName,Transform parent)
        {
            var source=UnityEngine.Resources.Load<GameObject>("Art/"+modelName);
            var material=UnityEngine.Resources.Load<Material>("Art/"+materialName);
            if(source==null || material==null || material.shader==null || !material.shader.isSupported)
                throw new InvalidOperationException("Detailed art missing/unsupported: "+modelName+". Keep Resources/Art and the supplied shaders/importer.");
            var model=UnityEngine.Object.Instantiate(source,parent,false);
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true)) {
                renderer.sharedMaterial=material; renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On; renderer.receiveShadows=true;
            }
            return model;
        }
        private static void Grounding(Transform parent,Vector3 center,Vector2 size)
        {
            var material=UnityEngine.Resources.Load<Material>("Art/ContactMaterial");
            if(material==null)throw new InvalidOperationException("Missing bundled contact material.");
            var shadow=GameObject.CreatePrimitive(PrimitiveType.Quad); shadow.name="Soft contact grounding"; shadow.transform.SetParent(parent,false);
            shadow.transform.localPosition=center; shadow.transform.localRotation=Quaternion.Euler(90,0,0); shadow.transform.localScale=new Vector3(size.x,size.y,1);
            SettlementArt.Release(shadow.GetComponent<Collider>());
            var renderer=shadow.GetComponent<Renderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
    }
}
