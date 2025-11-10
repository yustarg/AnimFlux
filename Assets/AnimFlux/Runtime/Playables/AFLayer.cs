using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AnimFlux
{
    public sealed class AFLayer : IDisposable
    {
        public string Name { get; }
        public int Index { get; }
        public AnimationMixerPlayable Mixer { get; }
        private PlayableGraph _graph;
        
        private AnimationClipPlayable _current;
        private AnimationClipPlayable _next;
        private float _fadeTime;
        private float _fadeElapsed;
        private bool _isFading;
        
        public AFLayer(string name, int index, AnimationMixerPlayable mixer, PlayableGraph graph)
        {
            Name = name;
            Index = index;
            Mixer = mixer;
            _graph = graph;
            mixer.SetInputWeight(0, 1f);
            mixer.SetInputWeight(1, 0f);
        }

        public void PlayClip(AnimationClip clip, float normalizedTime, float fadeDuration)
        {
            if (!clip) return;
            
            var nextPlayable = AnimationClipPlayable.Create(_graph, clip);
            nextPlayable.SetApplyFootIK(true);
            nextPlayable.SetApplyPlayableIK(true);
            nextPlayable.SetTime(Mathf.Clamp01(normalizedTime) * Math.Max(clip.length, 0.0001f));
            nextPlayable.SetSpeed(1f);
            nextPlayable.SetDuration(clip.length);

            if (!_current.IsValid())
            {
                PlugIntoSlot(0, nextPlayable);
                Mixer.SetInputWeight(0, 1f);
                Mixer.SetInputWeight(1, 0f);
                _current = nextPlayable;
                _isFading = false;
                _fadeElapsed = 0;
                _fadeTime = 0;
                return;
            }

            if (fadeDuration > 0.0001f)
            {
                PlugIntoSlot(1, nextPlayable);
                _next = nextPlayable;
                _fadeTime = fadeDuration;
                _fadeElapsed = 0;
                _isFading = true;
                AFPlayableUtils.BindPerFrame(_graph, UpdateFade);
            }
            else
            {
                if (_current.IsValid())
                {
                    UnplugSlot(0);
                    _current.Destroy();
                }
                PlugIntoSlot(0, nextPlayable);
                Mixer.SetInputWeight(0, 1f);
                Mixer.SetInputWeight(1, 0f);
                _current = nextPlayable;
                _next = default;
                _isFading = false;
            }
        }

        private void PlugIntoSlot(int slot, Playable playable)
        {
            Mixer.SetInputCount(Math.Max(Mixer.GetInputCount(), slot + 1));
            var existing = Mixer.GetInput(slot);
            if (existing.IsValid())
            {
                _graph.Disconnect(Mixer, slot);
                if (existing.IsPlayableOfType<AnimationClipPlayable>())
                {
                    var a = (AnimationClipPlayable)existing;
                    if(a.IsValid()) a.Destroy();
                }
            }
            _graph.Connect(playable, 0, Mixer, slot);
        }

        private void UnplugSlot(int slot)
        {
            var existing = Mixer.GetInput(slot);
            if (existing.IsValid()) _graph.Disconnect(Mixer, slot);
        }

        private void UpdateFade(float dt)
        {
            if (!_isFading) return;
            _fadeElapsed += dt;
            float t = Mathf.Clamp01(_fadeElapsed / Mathf.Max(_fadeTime, 0.0001f));
            Mixer.SetInputWeight(0, 1f - t);
            Mixer.SetInputWeight(1, t);
            if (t >= 1f)
            {
                if (_current.IsValid())
                {
                    UnplugSlot(0);
                    _current.Destroy();
                }
                PlugIntoSlot(0, _next);
                Mixer.SetInputWeight(0, 1f);
                Mixer.SetInputWeight(1, 0f);
                _current = _next;
                _next = default;
                _isFading = false;
            }
        }

        public void Dispose()
        {
            if (Mixer.IsValid())
            {
                for (int i = 0; i < Mixer.GetInputCount(); i++)
                {
                    var input = Mixer.GetInput(i);
                    if (input.IsValid())
                    {
                        if (input.IsPlayableOfType<AnimationClipPlayable>())
                        {
                            var a = (AnimationClipPlayable)input;
                            if (a.IsValid()) a.Destroy();
                        }
                        _graph.Disconnect(Mixer, i);
                    }
                }
                Mixer.Destroy();
            }
        }
    }
}