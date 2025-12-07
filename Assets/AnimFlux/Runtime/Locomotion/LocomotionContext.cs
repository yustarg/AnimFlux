using UnityEngine;

namespace AnimFlux.Runtime
{
    /// <summary>
    /// Context passed into blend spaces for locomotion; can be extended as needed.
    /// </summary>
    public struct LocomotionContext : IDirectionalBlendProvider, IFloatBlendProvider
    {
        public Vector2 DirectionalBlend;
        public float SpeedNormalized;
        public float ForwardStrafeNormalized;
        public float StrafeDirectionNormalized;
        public float InclineNormalized;
        public bool IsStrafing;
        public float FloatBlend;

        Vector2 IDirectionalBlendProvider.DirectionalBlend => DirectionalBlend;
        float IFloatBlendProvider.FloatBlend => FloatBlend;
    }
}

