using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace AnimFlux.Runtime
{
    [CreateAssetMenu(menuName = "AnimFlux/Locomotion/Blend Tree", fileName = "LocomotionBlendTree")]
    public sealed class LocomotionBlendTreeAsset : ScriptableObject
    {
        [SerializeField] private List<LocomotionBlendChild> _children = new();

        public IReadOnlyList<LocomotionBlendChild> Children => _children;

        private void OnValidate()
        {
            if (_children == null || _children.Count == 0) return;

            for (int i = 0; i < _children.Count; i++)
            {
                var c = _children[i];
                if (c.direction.sqrMagnitude > 0.0001f)
                {
                    c.direction = c.direction.normalized;
                }
            }

            for (int i = _children.Count - 1; i >= 0; i--)
            {
                if (_children[i].motion.HasValidReference) continue;
                Debug.LogWarning($"[AnimFlux] BlendTree child '{_children[i].name}' is missing a Motion reference.", this);
            }
        }
    }

    [Serializable]
    public sealed class LocomotionBlendChild
    {
        [Tooltip("Optional name for debugging in inspectors.")]
        public string name;

        [Tooltip("Planar velocity direction this child represents. Magnitude can encode speed preference.")]
        public Vector2 direction = Vector2.up;

        [Tooltip("Motion driven by this child. Accepts AnimationClip or another LocomotionBlendTree.")]
        public LocomotionBlendMotion motion;
    }

    [Serializable]
    public struct LocomotionBlendMotion
    {
        [SerializeField] private UnityEngine.Object _asset;

        public UnityEngine.Object Asset => _asset;
        public bool HasValidReference => Clip != null || Tree != null;
        public AnimationClip Clip => _asset as AnimationClip;
        public LocomotionBlendTreeAsset Tree => _asset as LocomotionBlendTreeAsset;

        public bool TryGetClip(out AnimationClip clip)
        {
            clip = Clip;
            return clip;
        }

        public bool TryGetTree(out LocomotionBlendTreeAsset tree)
        {
            tree = Tree;
            return tree;
        }

        public void Set(UnityEngine.Object asset)
        {
            _asset = asset;
        }
    }
}
