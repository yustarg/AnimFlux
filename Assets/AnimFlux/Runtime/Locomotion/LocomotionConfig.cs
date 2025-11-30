using UnityEngine;
using UnityEngine.Serialization;

namespace AnimFlux.Runtime
{
    [CreateAssetMenu(menuName = "AnimFlux/Locomotion Config", fileName = "LocomotionConfig")]
    public sealed class LocomotionConfig : ScriptableObject
    {
        [Header("Base Clips")]
        public AnimationClip idleClip;
        public AnimationClip walkClip;
        [FormerlySerializedAs("runClip")] public AnimationClip sprintClip;
        public AnimationClip fallClip;

        [Header("Blend Trees")]
        public LocomotionBlendTreeAsset walkTree;
        public LocomotionBlendTreeAsset sprintTree;

        [Header("Speeds")]
        public float walkSpeed = 1.5f;
        [FormerlySerializedAs("runSpeed")] public float sprintSpeed = 4f;
        [Tooltip("Range above SprintSpeed used to fully blend into sprint content.")]
        public float sprintBlendRange = 1.5f;

        [Header("Smoothing")]
        public float speedDampTime = 0.2f;
        [Tooltip("Damp time used when blending between Idle/Walk/Sprint/Fall slots.")]
        public float stateBlendDampTime = 0.1f;
        public float crossFadeDuration = 0.2f;

        [Header("Root Motion")]
        public bool enableRootMotion = true;
    }
}
