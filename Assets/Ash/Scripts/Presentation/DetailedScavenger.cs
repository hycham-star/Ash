using System;
using UnityEngine;

namespace Ash
{
    /// <summary>Bone-animated visual NPC. Its route does not simulate assigned gathering expeditions.</summary>
    public sealed class DetailedScavenger : MonoBehaviour
    {
        private Vector3[] route;
        private int waypoint;
        private Animation animationPlayer;
        public void Configure(GameObject model,Vector3[] patrol)
        {
            route=patrol;
            animationPlayer=model.GetComponentInChildren<Animation>(true);
            if(animationPlayer==null)animationPlayer=model.AddComponent<Animation>();
            AnimationClip walk=null;
            foreach(var clip in Resources.LoadAll<AnimationClip>("Art/Scavenger"))
                if(!clip.name.StartsWith("__preview__",StringComparison.Ordinal) && clip.legacy) { walk=clip; break; }
            if(walk==null)throw new InvalidOperationException("Scavenger walk clip missing. Reimport the FBX with RealisticArtImporter enabled.");
            animationPlayer.AddClip(walk,"Walk"); animationPlayer.wrapMode=WrapMode.Loop;
            animationPlayer.Play("Walk");
        }
        private void Update()
        {
            if(route==null || route.Length==0)return;
            var delta=route[waypoint]-transform.localPosition;
            if(delta.magnitude<.08f) { waypoint=(waypoint+1)%route.Length; return; }
            transform.localPosition=Vector3.MoveTowards(transform.localPosition,route[waypoint],Time.deltaTime*.5f);
            var rotation=Quaternion.LookRotation(delta);
            transform.localRotation=Quaternion.Slerp(transform.localRotation,rotation,Time.deltaTime*6);
        }
    }
}
