using UnityEngine;

namespace AnimFlux.Runtime
{
    /// <summary>
    /// Normalized locomotion parameters that drive the root blend tree.
    /// </summary>
    public struct LocomotionBlendParameters
    {
        public float MoveSpeed;
        public bool IsStrafing;
        public float ForwardStrafe;
        public float StrafeDirection;
        public float InclineAngle;

        public readonly Vector2 ToBlendVector()
        {
            Vector2 vector;
            if (IsStrafing)
            {
                vector = new Vector2(StrafeDirection, ForwardStrafe);
            }
            else
            {
                vector = new Vector2(0f, MoveSpeed);
            }

            vector.y += InclineAngle;
            return vector;
        }
    }
}

