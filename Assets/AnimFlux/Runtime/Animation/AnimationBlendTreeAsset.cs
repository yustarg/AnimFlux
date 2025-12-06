using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Serialization;

namespace AnimFlux.Runtime
{
    /// <summary>
    /// Generic blend tree asset that can drive any animation graph via a configured blend space.
    /// </summary>
    [CreateAssetMenu(menuName = "AnimFlux/Animation/Blend Tree", fileName = "AnimationBlendTree")]
    public class AnimationBlendTreeAsset : ScriptableObject, IAnimationBlendSource
    {
        [SerializeField] private BlendSpaceDefinition _blendSpace;
        [NonSerialized] private BlendSpaceDefinition _runtimeFallback;
        [SerializeField] private List<AnimationBlendNode> _nodes = new();

        public BlendSpaceDefinition BlendSpace => _blendSpace;
        public IReadOnlyList<AnimationBlendNode> Nodes => _nodes;

        public IAnimationBlendRuntime CreateRuntime(PlayableGraph graph)
        {
            var blendSpace = ResolveBlendSpace();
            if (blendSpace == null)
            {
                Debug.LogWarning($"[AnimFlux] BlendTree '{name}' could not resolve a BlendSpace.", this);
                return null;
            }

            var instance = new AnimationBlendTreeInstance(graph, this);
            return instance.IsValid ? instance : null;
        }

        private void OnDisable()
        {
            if (_runtimeFallback != null)
            {
                DestroyImmediate(_runtimeFallback);
                _runtimeFallback = null;
            }
        }

        private void OnValidate()
        {
            if (_nodes == null) _nodes = new List<AnimationBlendNode>();
            var blendSpace = ResolveBlendSpace();
            if (blendSpace == null) return;

            for (int i = _nodes.Count - 1; i >= 0; i--)
            {
                var node = _nodes[i];
                if (node == null)
                {
                    _nodes.RemoveAt(i);
                    continue;
                }

                blendSpace.EnsureNodeMetadata(node);

                if (!node.motion.HasValidReference)
                {
                    Debug.LogWarning($"[AnimFlux] BlendTree child '{node.name}' is missing a Motion reference.", this);
                }
            }
        }

        internal BlendSpaceDefinition ResolveBlendSpace()
        {
            if (_blendSpace != null) return _blendSpace;
            if (_runtimeFallback == null)
            {
                _runtimeFallback = ScriptableObject.CreateInstance<Directional2DBlendSpace>();
                _runtimeFallback.hideFlags = HideFlags.HideAndDontSave;
            }

            return _runtimeFallback;
        }
    }

    [Serializable]
    public sealed class AnimationBlendNode
    {
        [Tooltip("Optional name for debugging in inspectors.")]
        public string name;

        [SerializeReference] private BlendNodeMetadata _metadata;
        public AnimationBlendMotion motion;

        internal BlendNodeMetadata Metadata => _metadata;

        internal void SetMetadata(BlendNodeMetadata metadata)
        {
            _metadata = metadata;
        }
    }

    [Serializable]
    public abstract class BlendNodeMetadata
    {
    }

    public abstract class BlendSpaceDefinition : ScriptableObject
    {
        public abstract Type ContextType { get; }
        public abstract Type MetadataType { get; }
        internal abstract IBlendSpaceRuntime CreateRuntime(IReadOnlyList<AnimationBlendNode> nodes);
        public abstract BlendNodeMetadata CreateDefaultMetadata();

        public virtual void EnsureNodeMetadata(AnimationBlendNode node)
        {
            if (node == null) return;
            if (node.Metadata != null && MetadataType.IsInstanceOfType(node.Metadata)) return;
            node.SetMetadata(CreateDefaultMetadata());
        }
    }

    internal interface IBlendSpaceRuntime : IDisposable
    {
        Type ContextType { get; }
        void Evaluate(object context, float[] weights);
    }

    [Serializable]
    public struct AnimationBlendMotion
    {
        [SerializeField] private UnityEngine.Object _asset;

        public UnityEngine.Object Asset => _asset;
        public bool HasValidReference => Clip != null || Tree != null || Source != null;
        public AnimationClip Clip => _asset as AnimationClip;
        public AnimationBlendTreeAsset Tree => _asset as AnimationBlendTreeAsset;
        public IAnimationBlendSource Source => _asset as IAnimationBlendSource;

        public void Set(UnityEngine.Object asset)
        {
            _asset = asset;
        }

        public bool TryCreateRuntime(PlayableGraph graph, out IAnimationBlendRuntime runtime)
        {
            runtime = null;
            if (!graph.IsValid() || !HasValidReference)
            {
                return false;
            }

            if (_asset is IAnimationBlendSource source)
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

        private sealed class ClipBlendRuntime : IAnimationBlendRuntime
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
            public Type ContextType => typeof(object);

            public void Evaluate(object context)
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

