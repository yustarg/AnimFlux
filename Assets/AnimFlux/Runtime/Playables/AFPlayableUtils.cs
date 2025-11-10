using System;
using System.Collections.Generic;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AnimFlux
{
    public static class AFPlayableUtils
    {
        private static readonly Dictionary<int, AnimationMixerPlayable> _graphMixers = new();
        
        private class PerFrameBehaviour : PlayableBehaviour
        {
            public Action<float> Tick;
            public override void PrepareFrame(Playable playable, FrameData info) => Tick?.Invoke(info.deltaTime);
        }

        public static void BindPerFrame(PlayableGraph graph, Action<float> tick)
        {
            var playable = ScriptPlayable<PerFrameBehaviour>.Create(graph);
            playable.GetBehaviour().Tick = tick;

            if (!_graphMixers.TryGetValue(graph.GetHashCode(), out var mixer))
            {
                mixer = AnimationMixerPlayable.Create(graph, 1);
                _graphMixers.Add(graph.GetHashCode(), mixer);
            }
            
            graph.Connect(playable, 0, mixer, 0);
            mixer.SetInputWeight(0, 0f);
        }
    }
}