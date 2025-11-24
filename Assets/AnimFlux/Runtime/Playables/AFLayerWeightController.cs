using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace AnimFlux.Runtime
{
    internal sealed class AFLayerWeightController : IDisposable
    {
        private readonly AFRoot _root;
        private readonly Dictionary<int, WeightState> _states = new();
        private readonly List<int> _keys = new(8);
        private readonly ScriptPlayable<WeightDriverBehaviour> _driverPlayable;
        private readonly ScriptPlayableOutput _driverOutput;

        private struct WeightState
        {
            public float Current;
            public float Target;
            public float Speed;
            public bool Active;
        }

        private sealed class WeightDriverBehaviour : PlayableBehaviour
        {
            public AFLayerWeightController Owner;
            public override void PrepareFrame(Playable playable, FrameData info)
            {
                Owner?.Tick(info.deltaTime);
            }
        }

        public AFLayerWeightController(PlayableGraph graph, AFRoot root)
        {
            _root = root;
            _driverPlayable = ScriptPlayable<WeightDriverBehaviour>.Create(graph);
            var behaviour = _driverPlayable.GetBehaviour();
            behaviour.Owner = this;
            _driverOutput = ScriptPlayableOutput.Create(graph, "AnimFlux_LayerWeightDriver");
            _driverOutput.SetSourcePlayable(_driverPlayable);
        }

        public void RegisterLayer(int layerIndex, float initialWeight)
        {
            var weight = Mathf.Clamp01(initialWeight);
            _states[layerIndex] = new WeightState
            {
                Current = weight,
                Target = weight,
                Speed = 0f,
                Active = false
            };
            _root.SetLayerWeight(layerIndex, weight);
        }

        public void SetWeight(int layerIndex, float weight)
        {
            if (!_states.TryGetValue(layerIndex, out var state)) return;
            weight = Mathf.Clamp01(weight);
            state.Current = weight;
            state.Target = weight;
            state.Speed = 0f;
            state.Active = false;
            _states[layerIndex] = state;
            _root.SetLayerWeight(layerIndex, weight);
        }

        public void SetWeightSmooth(int layerIndex, float targetWeight, float duration)
        {
            if (!_states.TryGetValue(layerIndex, out var state)) return;
            targetWeight = Mathf.Clamp01(targetWeight);
            if (duration <= 0.0001f)
            {
                SetWeight(layerIndex, targetWeight);
                return;
            }

            state.Target = targetWeight;
            state.Active = true;
            state.Speed = Mathf.Abs(state.Current - targetWeight) / duration;
            if (state.Speed <= 0.0001f)
            {
                SetWeight(layerIndex, targetWeight);
                return;
            }
            _states[layerIndex] = state;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            _keys.Clear();
            foreach (var key in _states.Keys)
            {
                _keys.Add(key);
            }

            for (var i = 0; i < _keys.Count; i++)
            {
                var key = _keys[i];
                var state = _states[key];
                if (!state.Active) continue;

                var step = state.Speed * deltaTime;
                var next = Mathf.MoveTowards(state.Current, state.Target, step);
                state.Current = next;
                _root.SetLayerWeight(key, next);

                if (Mathf.Approximately(next, state.Target))
                {
                    state.Active = false;
                    state.Speed = 0f;
                }
                _states[key] = state;
            }
        }

        public void Dispose()
        {
            _states.Clear();
            if (_driverOutput.IsOutputValid())
            {
                _driverOutput.SetSourcePlayable(Playable.Null);
            }
            if (_driverPlayable.IsValid())
            {
                _driverPlayable.Destroy();
            }
        }
    }
}

