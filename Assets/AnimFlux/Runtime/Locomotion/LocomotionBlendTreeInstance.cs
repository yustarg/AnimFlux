using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AnimFlux.Runtime
{
    /// <summary>
    /// Runtime representation of a 2D directional blend tree that can be nested.
    /// </summary>
    internal sealed class LocomotionBlendTreeInstance : IDisposable, ILocomotionBlendRuntime
    {
        private readonly PlayableGraph _graph;
        private readonly AnimationMixerPlayable _mixer;
        private readonly ChildSlot[] _children;
        private readonly float[] _weights;

        public bool IsValid => _mixer.IsValid() && _children.Length > 0;
        public Playable Playable => _mixer;
        public Playable Output => _mixer;

        public LocomotionBlendTreeInstance(PlayableGraph graph, LocomotionBlendTreeAsset asset)
        {
            _graph = graph;

            if (!graph.IsValid() || asset == null)
            {
                _children = Array.Empty<ChildSlot>();
                _weights = Array.Empty<float>();
                _mixer = AnimationMixerPlayable.Create(graph, 1);
                return;
            }

            _children = BuildChildren(graph, asset);
            _weights = _children.Length > 0 ? new float[_children.Length] : Array.Empty<float>();
            _mixer = _children.Length > 0 ? AnimationMixerPlayable.Create(graph, _children.Length) : AnimationMixerPlayable.Create(graph, 1);

            for (int i = 0; i < _children.Length; i++)
            {
                var slot = _children[i];
                if (!slot.HasPlayable) continue;
                graph.Connect(slot.Playable, 0, _mixer, i);
                _mixer.SetInputWeight(i, i == 0 ? 1f : 0f);
            }
        }

        public void Evaluate(Vector2 parameter)
        {
            if (!IsValid) return;

            for (int i = 0; i < _children.Length; i++)
            {
                _children[i].Evaluate(parameter);
            }

            ComputeWeights(parameter);

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

            if (_mixer.IsValid())
            {
                _mixer.Destroy();
            }
        }

        private void ComputeWeights(Vector2 parameter)
        {
            Array.Clear(_weights, 0, _weights.Length);
            if (_weights.Length == 0) return;

            var magnitude = parameter.magnitude;
            var dir = magnitude > 0.0001f ? parameter / magnitude : Vector2.zero;
            float total = 0f;
            float bestDot = -1f;
            int bestIndex = -1;
            int centerIndex = -1;

            for (int i = 0; i < _children.Length; i++)
            {
                var slotDir = _children[i].Direction;
                if (slotDir.sqrMagnitude < 0.0001f)
                {
                    centerIndex = i;
                    continue;
                }

                var dot = dir == Vector2.zero ? 0f : Vector2.Dot(dir, slotDir);
                bestIndex = dot > bestDot ? i : bestIndex;
                bestDot = Mathf.Max(bestDot, dot);
                var weight = Mathf.Max(0f, dot);
                _weights[i] = weight;
                total += weight;
            }

            if (total <= 0.0001f)
            {
                if (bestIndex >= 0)
                {
                    Array.Clear(_weights, 0, _weights.Length);
                    _weights[bestIndex] = 1f;
                }
                else if (centerIndex >= 0)
                {
                    _weights[centerIndex] = 1f;
                }
                else
                {
                    _weights[0] = 1f;
                }
                return;
            }

            for (int i = 0; i < _weights.Length; i++)
            {
                _weights[i] /= total;
            }

            if (centerIndex >= 0)
            {
                float occupied = 0f;
                for (int i = 0; i < _weights.Length; i++)
                {
                    if (i == centerIndex) continue;
                    occupied += _weights[i];
                }
                _weights[centerIndex] = Mathf.Clamp01(1f - occupied);
            }
        }

            private static ChildSlot[] BuildChildren(PlayableGraph graph, LocomotionBlendTreeAsset asset)
        {
            if (asset.Children == null || asset.Children.Count == 0)
            {
                return Array.Empty<ChildSlot>();
            }

            var buffer = new List<ChildSlot>(asset.Children.Count);
            foreach (var child in asset.Children)
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
            public readonly Vector2 Direction;
            public Playable Playable => _runtime?.Playable ?? Playable.Null;
            public bool HasPlayable => _runtime != null && _runtime.Playable.IsValid();

            private readonly ILocomotionBlendRuntime _runtime;

            private ChildSlot(Vector2 direction, ILocomotionBlendRuntime runtime)
            {
                Direction = direction;
                _runtime = runtime;
            }

            public static ChildSlot Create(PlayableGraph graph, LocomotionBlendNode data)
            {
                if (!graph.IsValid() || data == null) return null;

                var direction = data.direction.sqrMagnitude > 0.0001f ? data.direction.normalized : Vector2.zero;
                if (!data.motion.TryCreateRuntime(graph, out var runtime) || runtime == null || !runtime.Playable.IsValid())
                {
                    runtime?.Dispose();
                    return null;
                }

                return new ChildSlot(direction, runtime);
            }

            public void Evaluate(Vector2 parameter)
            {
                _runtime?.Evaluate(parameter);
            }

            public void Dispose()
            {
                _runtime?.Dispose();
            }
        }
    }
}

