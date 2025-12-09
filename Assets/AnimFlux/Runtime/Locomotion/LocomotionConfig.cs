using UnityEngine;
using UnityEngine.Serialization;

namespace AnimFlux.Runtime
{
    [CreateAssetMenu(menuName = "AnimFlux/Locomotion Config", fileName = "LocomotionConfig")]
    public sealed class LocomotionConfig : ScriptableObject
    {
        [Header("Root Blend Tree")]
        [FormerlySerializedAs("walkTree")] public AnimationBlendTreeAsset rootTree;
        public AnimationClip fallbackClip;

        [Header("Parameter Normalization")]
        [Min(0.01f)] public float maxMoveSpeed = 4f;
        [Min(0.01f)] public float maxForwardStrafe = 1f;
        [Min(0.01f)] public float maxStrafeDirection = 1f;
        [Min(0.01f)] public float maxInclineAngle = 30f;

        [Header("Smoothing")]
        public float speedDampTime = 0.2f;
        public float parameterDampTime = 0.1f;
        public float crossFadeDuration = 0.2f;

        [Header("Root Motion")]
        public bool enableRootMotion = true;

        [Header("Debug")]
        public bool debugLog;
        [Min(0.05f)] public float debugLogInterval = 0.5f;
    }
}
