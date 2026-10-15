using System;
using UnityEngine;

namespace Ash
{
    public sealed class UnityProgressCodec : IProgressCodec
    {
        public string Encode(ProgressState state) => JsonUtility.ToJson(state);
        public ProgressState Decode(string json)
        {
            try { return ProgressJsonDecoder.Decode(json,(text,type)=>JsonUtility.FromJson(text,type)); }
            catch(Exception error) { throw new ArgumentException("Progress JSON could not be decoded.",error); }
        }
    }
}
