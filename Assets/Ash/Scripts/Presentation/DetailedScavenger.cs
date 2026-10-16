using System;
using UnityEngine;

namespace Ash
{
    /// <summary>Shared bone animation for detailed heroes and NPCs. Routes are visual patrols, not gathering simulation.</summary>
    public sealed class DetailedScavenger : MonoBehaviour
    {
        private Vector3[] route;
        private int waypoint;
        private Animation animationPlayer;
        public void Configure(GameObject model,Vector3[] patrol,string assetName="Scavenger")
        {
            route=patrol;
            animationPlayer=model.GetComponentInChildren<Animation>(true);
            if(animationPlayer==null)animationPlayer=model.AddComponent<Animation>();
            string mode=patrol!=null && patrol.Length>1 ? "Walk" : "Idle";
            AnimationClip selected=null;
            foreach(var clip in Resources.LoadAll<AnimationClip>("Art/"+assetName))
                if(!clip.name.StartsWith("__preview__",StringComparison.Ordinal) && clip.legacy && clip.name.IndexOf(mode,StringComparison.OrdinalIgnoreCase)>=0)
                { selected=clip; break; }
            // The original approved scavenger FBX has a single walk take.
            if(selected==null && assetName=="Scavenger" && mode=="Walk")
                foreach(var clip in Resources.LoadAll<AnimationClip>("Art/Scavenger"))
                    if(clip.legacy && !clip.name.StartsWith("__preview__",StringComparison.Ordinal)) { selected=clip; break; }
            if(selected==null)throw new InvalidOperationException(assetName+" "+mode+" clip missing. Reimport the FBX with RealisticArtImporter enabled.");
            animationPlayer.AddClip(selected,mode); animationPlayer.wrapMode=WrapMode.Loop;
            animationPlayer.Play(mode);
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
