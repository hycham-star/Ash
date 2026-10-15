using System;

namespace Ash
{
    /// <summary>Decode the declared historical schema, independent of serializer defaults for new fields.</summary>
    public static class ProgressJsonDecoder
    {
        [Serializable]
        private sealed class Header
        {
            public Header() { }
            public int Version=0;
        }
        [Serializable]
        private sealed class LegacyV1
        {
            public LegacyV1() { }
            public string Format=null;
            public HeroArmyState Armies=null;
            public ResourceAmount[] Resources=null;
            public FragmentState[] Fragments=null;
        }
        [Serializable]
        private sealed class LegacyV2
        {
            public LegacyV2() { }
            public string Format=null;
            public HeroArmyState Armies=null;
            public ResourceAmount[] Resources=null;
            public FragmentState[] Fragments=null;
            public BuildingState[] Buildings=null;
        }
        public static ProgressState Decode(string json,Func<string,Type,object> deserialize)
        {
            if(deserialize==null)throw new ArgumentNullException(nameof(deserialize));
            var header=(Header)deserialize(json,typeof(Header));
            if(header==null)throw new ArgumentException("Missing progress document.");
            // Unity can instantiate missing inline classes. Never infer an old document's
            // schema by testing whether a field introduced later deserialized as null.
            if(header.Version==1) {
                var old=(LegacyV1)deserialize(json,typeof(LegacyV1));
                return new ProgressState { Version=1,Format=old.Format,Armies=old.Armies,
                    Resources=old.Resources,Fragments=old.Fragments,Buildings=null,Scavenging=null };
            }
            if(header.Version==2) {
                var old=(LegacyV2)deserialize(json,typeof(LegacyV2));
                return new ProgressState { Version=2,Format=old.Format,Armies=old.Armies,
                    Resources=old.Resources,Fragments=old.Fragments,Buildings=old.Buildings,Scavenging=null };
            }
            // Current/malformed/future documents retain their fields for strict validation.
            return (ProgressState)deserialize(json,typeof(ProgressState));
        }
    }
}
