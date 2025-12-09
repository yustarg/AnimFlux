using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimFlux.Runtime
{
    [CreateAssetMenu(menuName = "AnimFlux/BlendSpaces/Float Threshold", fileName = "FloatThresholdBlendSpace")]
    public sealed class FloatThresholdBlendSpace : BlendSpaceDefinition
    {
        public override Type ContextType => typeof(IFloatBlendProvider);
        public override Type MetadataType => typeof(FloatThresholdNodeMetadata);
        
        [SerializeField] private string _floatParameter = "SpeedNormalized";

        public override BlendNodeMetadata CreateDefaultMetadata() => new FloatThresholdNodeMetadata();

        internal override IBlendSpaceRuntime CreateRuntime(IReadOnlyList<AnimationBlendNode> nodes)
        {
            return new Runtime(nodes, _floatParameter);
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
            private readonly bool _hasAny;
            private readonly string _parameterName;

            public Runtime(IReadOnlyList<AnimationBlendNode> nodes, string parameterName)
            {
                _parameterName = parameterName;
                _thresholds = new float[nodes.Count];
                _hasAny = nodes.Count > 0;
                for (int i = 0; i < nodes.Count; i++)
                {
                    var meta = nodes[i].Metadata as FloatThresholdNodeMetadata;
                    if (meta == null)
                    {
                        Debug.LogWarning("[AnimFlux][FloatThreshold] Node metadata type mismatch. Resetting to default threshold=0.");
                        nodes[i].SetMetadata(new FloatThresholdNodeMetadata());
                        meta = nodes[i].Metadata as FloatThresholdNodeMetadata;
                    }
                    _thresholds[i] = meta != null ? meta.threshold : 0f;
                }

                if (AnimFluxDebug.Enabled)
                {
                    var joined = _hasAny ? string.Join(", ", _thresholds) : "none";
                    Debug.Log($"[AnimFlux][FloatThreshold] thresholds: {joined}");
                }
            }

            public Type ContextType => typeof(IFloatBlendProvider);

            public void Evaluate(object context, float[] weights)
            {
                if (context is not IFloatBlendProvider provider || weights == null || weights.Length == 0) return;
                float value;

                if (context is IBlendParameterProvider paramProvider && !string.IsNullOrWhiteSpace(_parameterName))
                {
                    if (!paramProvider.TryGetFloat(_parameterName, out value))
                    {
                        value = provider.FloatBlend;
                    }
                }
                else
                {
                    value = provider.FloatBlend;
                }

                Array.Clear(weights, 0, weights.Length);
                if (weights.Length == 1)
                {
                    weights[0] = 1f;
                    if (AnimFluxDebug.Enabled)
                        Debug.Log("[AnimFlux][FloatThreshold] Single slot -> 1");
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
                    if (AnimFluxDebug.Enabled)
                        Debug.Log("[AnimFlux][FloatThreshold] No thresholds found, fallback weights[0]=1");
                    return;
                }

                if (lower == upper || Mathf.Approximately(lowerVal, upperVal))
                {
                    weights[lower] = 1f;
                    if (AnimFluxDebug.Enabled)
                        Debug.Log($"[AnimFlux][FloatThreshold] Exact match -> slot {lower} = 1 (value={value:F2})");
                    return;
                }

                var tBlend = Mathf.InverseLerp(lowerVal, upperVal, value);
                weights[lower] = 1f - tBlend;
                weights[upper] = tBlend;

                if (AnimFluxDebug.Enabled)
                {
                    Debug.Log($"[AnimFlux][FloatThreshold] value={value:F2}, lower=({lower},{lowerVal:F2}), upper=({upper},{upperVal:F2}), weights[lower]={weights[lower]:F2}, weights[upper]={weights[upper]:F2}");
                }
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


