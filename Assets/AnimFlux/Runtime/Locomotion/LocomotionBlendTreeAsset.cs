using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Serialization;

namespace AnimFlux.Runtime
{
    [CreateAssetMenu(menuName = "AnimFlux/Locomotion/Blend Tree", fileName = "LocomotionBlendTree")]
    public sealed class LocomotionBlendTreeAsset : ScriptableObject, ILocomotionBlendSource
    {
        [SerializeField] private List<LocomotionBlendNode> _nodes = new();

        public IReadOnlyList<LocomotionBlendNode> Children => _nodes;

        public ILocomotionBlendRuntime CreateRuntime(PlayableGraph graph)
        {
            var instance = new LocomotionBlendTreeInstance(graph, this);
            return instance.IsValid ? instance : null;
        }

        private void OnValidate()
        {
            if (_nodes == null || _nodes.Count == 0) return;

            for (int i = _nodes.Count - 1; i >= 0; i--)
            {
                if (_nodes[i].motion.HasValidReference) continue;
                Debug.LogWarning($"[AnimFlux] BlendTree child '{_nodes[i].name}' is missing a Motion reference.", this);
            }
        }
    }

    [Serializable]
    public sealed class LocomotionBlendNode
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
        public ILocomotionBlendSource Source => _asset as ILocomotionBlendSource;

        public void Set(UnityEngine.Object asset)
        {
            _asset = asset;
        }

        public bool TryCreateRuntime(PlayableGraph graph, out ILocomotionBlendRuntime runtime)
        {
            runtime = null;
            if (!graph.IsValid() || !HasValidReference)
            {
                return false;
            }

            if (_asset is ILocomotionBlendSource source)
            {
                runtime = source.CreateRuntime(graph);
                if (runtime == null || !runtime.Playable.IsValid())
                {
                    runtime?.Dispose();
                    runtime = null;
                    return false;
                }

                return true;
            }

            if (_asset is AnimationClip clip)
            {
                runtime = new ClipBlendRuntime(graph, clip);
                if (!runtime.Playable.IsValid())
                {
                    runtime.Dispose();
                    runtime = null;
                    return false;
                }

                return true;
            }

            return false;
        }

        private sealed class ClipBlendRuntime : ILocomotionBlendRuntime
        {
            private readonly AnimationClipPlayable _playable;

            public ClipBlendRuntime(PlayableGraph graph, AnimationClip clip)
            {
                _playable = graph.IsValid() && clip
                    ? AnimationClipPlayable.Create(graph, clip)
                    : default;

                if (_playable.IsValid())
                {
                    _playable.SetApplyFootIK(true);
                    _playable.SetApplyPlayableIK(true);
                    _playable.SetDuration(clip.isLooping ? double.PositiveInfinity : clip.length);
                    _playable.SetSpeed(1f);
                }
            }

            public Playable Playable => _playable;

            public void Evaluate(in LocomotionBlendParameters parameters)
            {
                // Clips don't need parameter evaluation.
            }

            public void Dispose()
            {
                if (_playable.IsValid())
                {
                    _playable.Destroy();
                }
            }
        }
    }
}
