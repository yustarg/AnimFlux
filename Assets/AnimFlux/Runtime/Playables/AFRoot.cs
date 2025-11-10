using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AnimFlux
{
    public sealed class AFRoot : IDisposable
    {
        public AnimationLayerMixerPlayable LayerMixer { get; private set; }
        public AnimationPlayableOutput Output { get; private set; }
        public Animator Animator { get; private set; }
        public bool IsInitialized { get; private set; }

        public AFRoot Initialize(PlayableGraph graph, Animator animator)
        {
            if (IsInitialized) return this;
            if (!graph.IsValid()) throw new InvalidOperationException("AFRoot.Initialize: invalid graph");
            if (!animator) throw new ArgumentNullException(nameof(animator));

            Animator = animator;
            LayerMixer = AnimationLayerMixerPlayable.Create(graph, 0);
            Output = AnimationPlayableOutput.Create(graph, "AnimFluxOutput", animator);
            Output.SetSourcePlayable(LayerMixer);
            IsInitialized = true;

            return this;
        }

        public void SetLayerWeight(int layerIndex, float weight)
        {
            if (!IsInitialized) return;
            LayerMixer.SetInputWeight(layerIndex, Mathf.Clamp01(weight));
        }

        public float GetLayerWeight(int layerIndex)
        {
            if (!IsInitialized) return 0f;
            return LayerMixer.GetInputWeight(layerIndex);
        }

        public void SetLayerAdditive(int layerIndex, bool additive)
        {
            if (!IsInitialized) return;
            LayerMixer.SetLayerAdditive((uint)layerIndex, additive);
        }

        public void SetLayerMask(int layerIndex, AvatarMask mask)
        {
            if (!IsInitialized || mask == null) return;
            LayerMixer.SetLayerMaskFromAvatarMask((uint)layerIndex, mask);
        }

        public void SetSpeed(float speed)
        {
            if (!IsInitialized) return;
            LayerMixer.SetSpeed(speed);
        }

        public void Dispose()
        {
            if (!IsInitialized) return;
            if (Output.IsOutputValid()) Output.SetSourcePlayable(Playable.Null);
            if (LayerMixer.IsValid()) LayerMixer.Destroy();
            
            Animator = null;
            IsInitialized = false;
        }
    }
}