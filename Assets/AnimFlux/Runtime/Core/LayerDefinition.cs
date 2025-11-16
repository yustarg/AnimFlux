using System;
using UnityEngine;

namespace AnimFlux.Runtime
{
    [Serializable]
    public struct LayerDefinition
    {
        public AnimationLayerType LayerType;
        public string Name;
        public AvatarMask Mask;
        [Range(0f, 1f)] public float DefaultWeight;
        public AFBlendMode BlendMode;

        public LayerDefinition(AnimationLayerType type, string name, AvatarMask mask, float weight, AFBlendMode blendMode)
        {
            LayerType = type;
            Name = string.IsNullOrWhiteSpace(name) ? type.ToString() : name;
            Mask = mask;
            DefaultWeight = Mathf.Clamp01(weight);
            BlendMode = blendMode;
        }
    }
}

