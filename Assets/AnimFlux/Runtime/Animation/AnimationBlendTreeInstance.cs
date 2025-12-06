using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AnimFlux.Runtime
{
    internal sealed class AnimationBlendTreeInstance : IDisposable, IAnimationBlendRuntime
    {
        private readonly PlayableGraph _graph;
        private readonly AnimationMixerPlayable _mixer;
        private readonly ChildSlot[] _children;
        private readonly float[] _weights;
        private readonly IBlendSpaceRuntime _blendSpaceRuntime;

        public bool IsValid => _mixer.IsValid() && _children.Length > 0 && _blendSpaceRuntime != null;
        public Playable Playable => _mixer;
        public Type ContextType => _blendSpaceRuntime?.ContextType ?? typeof(object);

        public AnimationBlendTreeInstance(PlayableGraph graph, AnimationBlendTreeAsset asset)
        {
            _graph = graph;

            var blendSpace = asset?.ResolveBlendSpace();
            if (!graph.IsValid() || blendSpace == null)
            {
                _children = Array.Empty<ChildSlot>();
                _weights = Array.Empty<float>();
                _mixer = AnimationMixerPlayable.Create(graph, 1);
                _blendSpaceRuntime = null;
                return;
            }

            _children = BuildChildren(graph, asset);
            _weights = _children.Length > 0 ? new float[_children.Length] : Array.Empty<float>();
            _mixer = _children.Length > 0
                ? AnimationMixerPlayable.Create(graph, _children.Length)
                : AnimationMixerPlayable.Create(graph, 1);

            for (int i = 0; i < _children.Length; i++)
            {
                var slot = _children[i];
                if (!slot.HasPlayable) continue;
                graph.Connect(slot.Playable, 0, _mixer, i);
                _mixer.SetInputWeight(i, i == 0 ? 1f : 0f);
            }

            _blendSpaceRuntime = blendSpace.CreateRuntime(asset.Nodes);
        }

        public void Evaluate(object context)
        {
            if (!IsValid || context == null) return;
            if (!_blendSpaceRuntime.ContextType.IsInstanceOfType(context)) return;

            for (int i = 0; i < _children.Length; i++)
            {
                _children[i].Evaluate(context);
            }

            _blendSpaceRuntime.Evaluate(context, _weights);

            for (int i = 0; i < _weights.Length; i++)
            {
                _mixer.SetInputWeight(i, _weights[i]);
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < _children.Length; i++)
            {
                _children[i].Dispose();
            }

            _blendSpaceRuntime?.Dispose();

            if (_mixer.IsValid())
            {
                _mixer.Destroy();
            }
        }

        private static ChildSlot[] BuildChildren(PlayableGraph graph, AnimationBlendTreeAsset asset)
        {
            if (asset.Nodes == null || asset.Nodes.Count == 0)
            {
                return Array.Empty<ChildSlot>();
            }

            var buffer = new List<ChildSlot>(asset.Nodes.Count);
            foreach (var child in asset.Nodes)
            {
                if (child == null) continue;
                var slot = ChildSlot.Create(graph, child);
                if (slot != null && slot.HasPlayable)
                {
                    buffer.Add(slot);
                }
            }

            return buffer.ToArray();
        }

        private sealed class ChildSlot : IDisposable
        {
            public Playable Playable => _runtime?.Playable ?? Playable.Null;
            public bool HasPlayable => _runtime != null && _runtime.Playable.IsValid();

            private readonly IAnimationBlendRuntime _runtime;

            private ChildSlot(IAnimationBlendRuntime runtime)
            {
                _runtime = runtime;
            }

            public static ChildSlot Create(PlayableGraph graph, AnimationBlendNode data)
            {
                if (!graph.IsValid() || data == null) return null;

                if (!data.motion.TryCreateRuntime(graph, out var runtime) || runtime == null ||
                    !runtime.Playable.IsValid())
                {
                    runtime?.Dispose();
                    return null;
                }

                return new ChildSlot(runtime);
            }

            public void Evaluate(object context)
            {
                if (_runtime == null) return;
                if (!_runtime.ContextType.IsInstanceOfType(context)) return;
                _runtime.Evaluate(context);
            }

            public void Dispose()
            {
                _runtime?.Dispose();
            }
        }
    }
}

