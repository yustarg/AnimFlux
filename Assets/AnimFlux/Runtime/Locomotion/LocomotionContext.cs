using UnityEngine;

namespace AnimFlux.Runtime
{
    /// <summary>
    /// Context passed into blend spaces for locomotion; can be extended as needed.
    /// </summary>
    public struct LocomotionContext : IDirectionalBlendProvider
    {
        public Vector2 DirectionalBlend;
        public float SpeedNormalized;
        public float ForwardStrafeNormalized;
        public float StrafeDirectionNormalized;
        public float InclineNormalized;
        public bool IsStrafing;

        Vector2 IDirectionalBlendProvider.DirectionalBlend => DirectionalBlend;
    }
}

