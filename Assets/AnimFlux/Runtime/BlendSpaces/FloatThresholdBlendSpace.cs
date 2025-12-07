using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimFlux.Runtime
{
    [CreateAssetMenu(menuName = "AnimFlux/BlendSpaces/Float Threshold", fileName = "FloatThresholdBlendSpace")]
    public sealed class FloatThresholdBlendSpace : BlendSpaceDefinition
    {
        public override Type ContextType => typeof(LocomotionContext);
        public override Type MetadataType => typeof(FloatThresholdNodeMetadata);

        public override BlendNodeMetadata CreateDefaultMetadata() => new FloatThresholdNodeMetadata();

        internal override IBlendSpaceRuntime CreateRuntime(IReadOnlyList<AnimationBlendNode> nodes)
        {
            return new Runtime(nodes);
        }

        public override void EnsureNodeMetadata(AnimationBlendNode node)
        {
            if (node == null) return;
            if (node.Metadata is FloatThresholdNodeMetadata) return;
            node.SetMetadata(CreateDefaultMetadata());
        }

        private sealed class Runtime : IBlendSpaceRuntime
        {
            private readonly float[] _thresholds;

            public Runtime(IReadOnlyList<AnimationBlendNode> nodes)
            {
                _thresholds = new float[nodes.Count];
                for (int i = 0; i < nodes.Count; i++)
                {
                    var meta = nodes[i].Metadata as FloatThresholdNodeMetadata;
                    _thresholds[i] = meta != null ? meta.threshold : 0f;
                }
            }

            public Type ContextType => typeof(LocomotionContext);

            public void Evaluate(object context, float[] weights)
            {
                if (context is not LocomotionContext lc || weights == null || weights.Length == 0) return;
                var value = lc.SpeedNormalized; // default to speed; other usages can reuse the same field

                Array.Clear(weights, 0, weights.Length);
                if (weights.Length == 1)
                {
                    weights[0] = 1f;
                    return;
                }

                // Find two nearest thresholds
                int lower = -1, upper = -1;
                float lowerVal = float.NegativeInfinity, upperVal = float.PositiveInfinity;
                for (int i = 0; i < _thresholds.Length; i++)
                {
                    var t = _thresholds[i];
                    if (t <= value && t > lowerVal)
                    {
                        lowerVal = t;
                        lower = i;
                    }
                    if (t >= value && t < upperVal)
                    {
                        upperVal = t;
                        upper = i;
                    }
                }

                if (lower == -1) lower = upper;
                if (upper == -1) upper = lower;

                if (lower == -1 && upper == -1)
                {
                    weights[0] = 1f;
                    return;
                }

                if (lower == upper || Mathf.Approximately(lowerVal, upperVal))
                {
                    weights[lower] = 1f;
                    return;
                }

                var tBlend = Mathf.InverseLerp(lowerVal, upperVal, value);
                weights[lower] = 1f - tBlend;
                weights[upper] = tBlend;
            }

            public void Dispose()
            {
            }
        }
    }

    [Serializable]
    public sealed class FloatThresholdNodeMetadata : BlendNodeMetadata
    {
        public float threshold;
    }
}


