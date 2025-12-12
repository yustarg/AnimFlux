using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimFlux.Runtime
{
    [Serializable]
    public sealed class Directional2DBlendSpace : BlendSpaceDefinition
    {
        public override Type ContextType => typeof(IDirectionalBlendProvider);
        public override Type MetadataType => typeof(Directional2DNodeMetadata);
        private string _directionalParameter = "Directional"; // hidden; overridden by tree

        public override BlendNodeMetadata CreateDefaultMetadata()
        {
            return new Directional2DNodeMetadata();
        }

        internal override IBlendSpaceRuntime CreateRuntime(IReadOnlyList<AnimationBlendNode> nodes)
            => new Runtime(nodes, _directionalParameter);

        internal void SetParameterOverride(string vectorParamName)
        {
            if (!string.IsNullOrWhiteSpace(vectorParamName))
            {
                _directionalParameter = vectorParamName;
            }
        }

        public override void EnsureNodeMetadata(AnimationBlendNode node)
        {
            if (node == null) return;
            if (node.Metadata is Directional2DNodeMetadata) return;
            node.SetMetadata(CreateDefaultMetadata());
        }

        private sealed class Runtime : IBlendSpaceRuntime
        {
            private readonly Vector2[] _positions;
            private readonly string _parameterName;

            public Runtime(IReadOnlyList<AnimationBlendNode> nodes, string parameterName = null)
            {
                _parameterName = parameterName;
                _positions = new Vector2[nodes.Count];
                for (int i = 0; i < nodes.Count; i++)
                {
                    var meta = nodes[i].Metadata as Directional2DNodeMetadata;
                    _positions[i] = meta != null && meta.position.sqrMagnitude > 0.0001f
                        ? meta.position.normalized
                        : Vector2.zero;
                }
            }

            public Type ContextType => typeof(IDirectionalBlendProvider);

            public void Evaluate(object context, float[] weights)
            {
                if (weights == null || weights.Length == 0) return;

                Vector2 parameter = Vector2.zero;
                bool gotParam = false;

                if (context is IBlendParameterProvider paramProvider)
                {
                    if (!string.IsNullOrWhiteSpace(_parameterName) && paramProvider.TryGetVector2(_parameterName, out parameter))
                    {
                        gotParam = true;
                    }
                }

                if (!gotParam && context is IDirectionalBlendProvider provider)
                {
                    parameter = provider.DirectionalBlend;
                    gotParam = true;
                }

                if (!gotParam) return;

                Array.Clear(weights, 0, weights.Length);

                var magnitude = parameter.magnitude;
                var dir = magnitude > 0.0001f ? parameter / magnitude : Vector2.zero;
                float total = 0f;
                float bestDot = -1f;
                int bestIndex = -1;
                int centerIndex = -1;

                for (int i = 0; i < _positions.Length && i < weights.Length; i++)
                {
                    var slotDir = _positions[i];
                    if (slotDir.sqrMagnitude < 0.0001f)
                    {
                        centerIndex = i;
                        continue;
                    }

                    var dot = dir == Vector2.zero ? 0f : Vector2.Dot(dir, slotDir);
                    if (dot > bestDot)
                    {
                        bestDot = dot;
                        bestIndex = i;
                    }

                    var weight = Mathf.Max(0f, dot);
                    weights[i] = weight;
                    total += weight;
                }

                if (total <= 0.0001f)
                {
                    if (bestIndex >= 0)
                    {
                        Array.Clear(weights, 0, weights.Length);
                        weights[bestIndex] = 1f;
                    }
                    else if (centerIndex >= 0)
                    {
                        weights[centerIndex] = 1f;
                    }
                    else
                    {
                        weights[0] = 1f;
                    }
                    return;
                }

                for (int i = 0; i < weights.Length; i++)
                {
                    weights[i] /= total;
                }

                if (centerIndex >= 0)
                {
                    float occupied = 0f;
                    for (int i = 0; i < weights.Length; i++)
                    {
                        if (i == centerIndex) continue;
                        occupied += weights[i];
                    }
                    weights[centerIndex] = Mathf.Clamp01(1f - occupied);
                }
            }

            public void Dispose()
            {
            }
        }
    }

    [Serializable]
    public sealed class Directional2DNodeMetadata : BlendNodeMetadata
    {
        public Vector2 position = Vector2.up;
    }

    public readonly struct DirectionalBlendContext
    {
        public DirectionalBlendContext(Vector2 blendVector)
        {
            BlendVector = blendVector;
        }

        public Vector2 BlendVector { get; }
    }
}

