using UnityEngine;

namespace AnimFlux.Runtime
{
    [CreateAssetMenu(menuName = "AnimFlux/Locomotion Config", fileName = "LocomotionConfig")]
    public sealed class LocomotionConfig : ScriptableObject
    {
        [Header("Clips")]
        public AnimationClip idleClip;
        public AnimationClip walkClip;
        public AnimationClip runClip;
        public AnimationClip fallClip;

        [Header("Speeds")]
        public float walkSpeed = 1.5f;
        public float runSpeed = 4f;

        [Header("Smoothing")]
        public float speedDampTime = 0.2f;
        public float directionDampTime = 0.2f;
        public float crossFadeDuration = 0.2f;

        [Header("Root Motion")]
        public bool enableRootMotion = true;
    }
}

