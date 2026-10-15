using System;

namespace Ash
{
    [Serializable]
    public sealed class ProgressState
    {
        // No defaults: missing version/format fields must not become valid automatically.
        public int Version;
        public string Format;
        public HeroArmyState Armies;
        public ResourceAmount[] Resources;
        public FragmentState[] Fragments;
        public BuildingState[] Buildings;
    }
    [Serializable]
    public sealed class FragmentState
    {
        public string DefinitionId;
        public int Quantity;
    }
    public sealed class UnsupportedSaveVersionException : Exception
    {
        public UnsupportedSaveVersionException(int version) : base("Unsupported progress version: " + version) { }
    }
    public interface IProgressCodec
    {
        string Encode(ProgressState state);
        ProgressState Decode(string json);
    }
}
