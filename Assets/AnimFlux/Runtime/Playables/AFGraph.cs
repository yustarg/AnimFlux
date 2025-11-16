using System;
using UnityEngine;
using UnityEngine.Playables;

namespace AnimFlux.Runtime
{
    public enum AFBlendMode { Override = 0, Additive = 1 }
    
    public sealed class AFGraph : IDisposable
    {
        public AFGraphHost Host { get; private set; }
        public AFRoot Root { get; private set; }
        public AFLayerManager Layers { get; private set; }
        public Animator Animator => Root?.Animator;
        public PlayableGraph Graph => Host?.Graph?? default;
        public bool IsInitialized => Host is { IsInitialized: true };

        public static AFGraph Create(Animator animator, string graphName = "AnimFluxGraph")
        {
            var af = new AFGraph();
            af.Host = new AFGraphHost().Initialize(graphName);
            af.Root = new AFRoot().Initialize(af.Host.Graph, animator);
            af.Layers = new AFLayerManager(af.Host.Graph, af.Root);
            return af;
        }

        public int AddLayer(string name, AvatarMask mask = null, float weight = 1f, AFBlendMode mode = AFBlendMode.Override)
            => Layers.AddLayer(name, mask, weight, mode);

        public void PlayClip(int layerIndex, AnimationClip clip, float normalizedTime = 0f, float crossFadeDuration = 0.15f)
            => Layers.PlayClip(layerIndex, clip, normalizedTime, crossFadeDuration);

        public void SetLayerWeight(int layerIndex, float weight) => Layers.SetLayerWeight(layerIndex, weight);
        public float GetLayerWeight(int layerIndex) => Layers.GetLayerWeight(layerIndex);
        public void SetLayerAdditive(int layerIndex, bool additive) => Layers.SetLayerAdditive(layerIndex, additive);
        public void SetLayerMask(int layerIndex, AvatarMask mask) => Layers.SetLayerMask(layerIndex, mask);

        public void SetSpeed(float speed) => Root.SetSpeed(speed);
        public void Pause() => Host.Stop();
        public void Resume() => Host.Play();
        public void Evaluate(float dt) => Host.Evaluate(dt);
        
        public void Dispose()
        {
            Layers?.Dispose();
            Root?.Dispose();
            Host?.Dispose();
        }
    }
}