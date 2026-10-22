using System;
using UnityEngine;

namespace Ash
{
    /// <summary>Original imported visual slice; independent of resources, armies, saves and gathering.</summary>
    public static class RealisticAssets
    {
        public const float MiniatureScale=.60f;
        public static GameObject Headquarters(Transform parent)
        {
            var model=Create("Headquarters","HeadquartersMaterial",parent);
            model.name="Patched timber command hall — comic low-poly mesh";
            // The detailed mesh owns its roof equipment and yard clutter.
            Grounding(model.transform,new Vector3(0,.17f,0),new Vector2(4.3f,4.1f));
            return model;
        }
        public static GameObject Building(Transform parent,string assetName,Vector2 footprint)
        {
            var model=Create(assetName,assetName+"Material",parent);
            model.name=assetName+" — original textured building";
            // The comic kit owns its roof repairs and supplies; no duplicate procedural surrounds.
            Grounding(model.transform,new Vector3(0,.065f,0),footprint+new Vector2(.25f,.25f));
            return model;
        }
        public static GameObject Scenery(Transform parent,string assetName,Vector3 position,float scale=1,float heading=0)
        {
            var model=Create(assetName,assetName+"Material",parent);model.name=assetName+" — district scenery";
            model.transform.localPosition=position;model.transform.localScale=Vector3.one*scale;model.transform.localRotation=Quaternion.Euler(0,heading,0);
            return model;
        }
        public static GameObject SalvageTruck(Transform parent,Vector3 position,float heading=0)
        {
            var root=new GameObject("Parked salvage truck — environment only");root.transform.SetParent(parent,false);root.transform.localPosition=position;
            root.transform.localRotation=Quaternion.Euler(0,heading,0);root.transform.localScale=Vector3.one*.75f;
            Create("SalvageTruck","SalvageTruckMaterial",root.transform);
            Grounding(root.transform,new Vector3(0,.075f,0),new Vector2(1.8f,3.4f));return root;
        }
        public static GameObject Scavenger(Transform parent,Vector3 position,Vector3[] patrol)
        {
            var root=new GameObject("Detailed scavenger — visual patrol"); root.transform.SetParent(parent,false); root.transform.localPosition=position;
            var model=Create("Scavenger","ScavengerMaterial",root.transform);
            model.transform.localPosition=Vector3.up*.10f;
            model.transform.localScale=Vector3.one*MiniatureScale;
            var motion=root.AddComponent<DetailedScavenger>(); motion.Configure(model,patrol);
            Grounding(root.transform,new Vector3(0,.105f,0),new Vector2(.7f,.55f)*MiniatureScale);
            return root;
        }
        public static GameObject Hero(Transform parent,string heroId,string displayName,Vector3 position,bool portrait=false)
        {
            return Character(parent,HeroVisualCatalog.ModelFor(heroId),displayName,position,null,portrait,portrait?0:.10f);
        }
        public static GameObject Character(Transform parent,string assetName,string displayName,Vector3 position,Vector3[] patrol=null,bool portrait=false,float groundHeight=.065f)
        {
            var root=new GameObject(displayName);root.transform.SetParent(parent,false);root.transform.localPosition=position;
            var alignment=new GameObject("Character visual alignment").transform;
            alignment.SetParent(root.transform,false);alignment.localPosition=Vector3.up*groundHeight;
            var model=Create(assetName,assetName+"Material",alignment);
            Align(model.transform,alignment,root.transform,patrol!=null && patrol.Length>1?Vector3.forward:Vector3.back,assetName);
            if(!portrait)alignment.localScale=Vector3.one*MiniatureScale;
            root.AddComponent<DetailedScavenger>().Configure(model,patrol,assetName);
            if(!portrait)Grounding(root.transform,new Vector3(0,groundHeight+.005f,0),new Vector2(.7f,.55f)*MiniatureScale);
            return root;
        }
        public static GameObject MountedFighter(Transform parent,Vector3 position)
        {
            var root=new GameObject("Mounted cavalry group");root.transform.SetParent(parent,false);root.transform.localPosition=position;
            var alignment=new GameObject("Mounted unit alignment").transform;alignment.SetParent(root.transform,false);
            var model=Create("CavalryMount","CavalryMountMaterial",alignment);
            Align(model.transform,alignment,root.transform,Vector3.back,"CavalryMount");
            alignment.localScale=Vector3.one*MiniatureScale;
            Grounding(root.transform,new Vector3(0,.015f,0),new Vector2(1,2)*MiniatureScale);return root;
        }
        private static void Align(Transform model,Transform alignment,Transform root,Vector3 desired,string assetName)
        {
            Transform origin=null,front=null;
            foreach(var child in model.GetComponentsInChildren<Transform>(true))
            {
                if(child.name=="FaceAimOrigin")origin=child;
                if(child.name=="FaceAimForward")front=child;
            }
            if(origin==null || front==null)throw new InvalidOperationException("Missing facing markers: "+assetName);
            var facing=root.InverseTransformDirection(front.position-origin.position);facing.y=0;
            if(facing.sqrMagnitude<.01f)throw new InvalidOperationException("Invalid facing markers: "+assetName);
            alignment.localRotation=Quaternion.AngleAxis(Vector3.SignedAngle(facing.normalized,desired,Vector3.up),Vector3.up);
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
