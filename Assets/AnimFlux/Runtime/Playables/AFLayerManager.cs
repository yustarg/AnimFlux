using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AnimFlux.Runtime
{
    public sealed class AFLayerManager : IDisposable
    {
        private PlayableGraph _graph;
        private readonly AFRoot _root;
        private readonly List<AFLayer> _layers = new(4);
        
        public int Count => _layers.Count;

        public AFLayerManager(PlayableGraph graph, AFRoot root)
        {
            _graph = graph;
            _root = root;
        }

        public int AddLayer(string name, AvatarMask mask = null, float weight = 1f, AFBlendMode mode = AFBlendMode.Override)
        {
            var layerIndex = _layers.Count;
            _root.LayerMixer.SetInputCount(layerIndex + 1);
            
            var mixer = AnimationMixerPlayable.Create(_graph, 2);
            _graph.Connect(mixer, 0, _root.LayerMixer, layerIndex);
            _root.SetLayerWeight(layerIndex, weight);
            if (mask != null) _root.SetLayerMask(layerIndex, mask);
            _root.SetLayerAdditive(layerIndex, mode == AFBlendMode.Additive);
            
            var layer = new AFLayer(name, layerIndex, mixer, _graph);
            _layers.Add(layer);
            return layerIndex;
        }

        public void PlayClip(int layerIndex, AnimationClip clip, float normalizedTime = 0f, float crossFadeDuration = 0.15f)
        {
            ValidateLayer(layerIndex);
            _layers[layerIndex].PlayClip(clip, normalizedTime, crossFadeDuration);
        }
        
        public void SetLayerWeight(int layerIndex, float weight)
        {
            ValidateLayer(layerIndex);
            _root.SetLayerWeight(layerIndex, weight);
        }

        public float GetLayerWeight(int layerIndex)
        {
            ValidateLayer(layerIndex);
            return _root.GetLayerWeight(layerIndex);
        }

        public void SetLayerAdditive(int layerIndex, bool additive)
        {
            ValidateLayer(layerIndex);   
            _root.SetLayerAdditive(layerIndex, additive);
        }

        public void SetLayerMask(int layerIndex, AvatarMask mask)
        {
            ValidateLayer(layerIndex);
            _root.SetLayerMask(layerIndex, mask);
        }
        
        private void ValidateLayer(int idx)
        {
            if (idx < 0 || idx >= _layers.Count)
            {
                throw new IndexOutOfRangeException($"Layer index {idx} is out of range (0..{_layers.Count - 1}).");
            }
        }
        
        public void Dispose()
        {
            for (int i = _layers.Count - 1; i >= 0; --i)
            {
                _layers[i].Dispose();
            }
            _layers.Clear();
        }
    }
}