using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AnimFlux.Runtime
{
    public sealed class AFLayer : IDisposable
    {
        public string Name { get; }
        public int Index { get; }
        private readonly AnimationMixerPlayable _mixer;
        private PlayableGraph _graph;
        
        private AnimationClipPlayable _current;
        private AnimationClipPlayable _next;
        private float _fadeTime;
        private float _fadeElapsed;
        private bool _isFading;
        private ScriptPlayable<FadeBehaviour> _fadePlayable;
        private ScriptPlayableOutput _fadeOutput;
        private FadeBehaviour _fadeBehaviour;
        
        public AFLayer(string name, int index, AnimationMixerPlayable mixer, PlayableGraph graph)
        {
            Name = name;
            Index = index;
            _mixer = mixer;
            _graph = graph;
            mixer.SetInputWeight(0, 1f);
            mixer.SetInputWeight(1, 0f);
            InitializeFadeDriver();
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
                _mixer.SetInputWeight(0, 1f);
                _mixer.SetInputWeight(1, 0f);
                _current = nextPlayable;
                _isFading = false;
                _fadeElapsed = 0;
                _fadeTime = 0;
                return;
            }

            StopFade();

            if (fadeDuration > 0.0001f)
            {
                PlugIntoSlot(1, nextPlayable);
                _next = nextPlayable;
                _fadeTime = fadeDuration;
                _fadeElapsed = 0;
                _isFading = true;
                _fadeBehaviour?.Enable();
            }
            else
            {
                if (_current.IsValid())
                {
                    UnplugSlot(0);
                    _current.Destroy();
                }
                PlugIntoSlot(0, nextPlayable);
                _mixer.SetInputWeight(0, 1f);
                _mixer.SetInputWeight(1, 0f);
                _current = nextPlayable;
                _next = default;
                _isFading = false;
            }
        }

        private void PlugIntoSlot(int slot, Playable playable)
        {
            _mixer.SetInputCount(Math.Max(_mixer.GetInputCount(), slot + 1));
            var existing = _mixer.GetInput(slot);
            if (existing.IsValid())
            {
                _graph.Disconnect(_mixer, slot);
                if (existing.IsPlayableOfType<AnimationClipPlayable>())
                {
                    var a = (AnimationClipPlayable)existing;
                    if(a.IsValid()) a.Destroy();
                }
            }
            _graph.Connect(playable, 0, _mixer, slot);
        }

        private void UnplugSlot(int slot)
        {
            var existing = _mixer.GetInput(slot);
            if (existing.IsValid()) _graph.Disconnect(_mixer, slot);
        }

        private void AdvanceFade(float dt)
        {
            if (!_isFading) return;
            _fadeElapsed += dt;
            float t = Mathf.Clamp01(_fadeElapsed / Mathf.Max(_fadeTime, 0.0001f));
            _mixer.SetInputWeight(0, 1f - t);
            _mixer.SetInputWeight(1, t);
            if (t >= 1f)
            {
                if (_current.IsValid())
                {
                    UnplugSlot(0);
                    _current.Destroy();
                }
                var promoted = _next;
                UnplugSlot(1);
                PlugIntoSlot(0, promoted);
                _mixer.SetInputWeight(0, 1f);
                _mixer.SetInputWeight(1, 0f);
                _current = promoted;
                _next = default;
                _isFading = false;
                _fadeBehaviour?.Disable();
            }
        }

        public void Dispose()
        {
            StopFade();
            if (_mixer.IsValid())
            {
                for (int i = 0; i < _mixer.GetInputCount(); i++)
                {
                    var input = _mixer.GetInput(i);
                    if (input.IsValid())
                    {
                        if (input.IsPlayableOfType<AnimationClipPlayable>())
                        {
                            var a = (AnimationClipPlayable)input;
                            if (a.IsValid()) a.Destroy();
                        }
                        _graph.Disconnect(_mixer, i);
                    }
                }
                _mixer.Destroy();
            }
            if (_fadeOutput.IsOutputValid())
            {
                _fadeOutput.SetSourcePlayable(Playable.Null);
            }
            if (_fadePlayable.IsValid())
            {
                _fadePlayable.Destroy();
            }
        }

        private void StopFade()
        {
            _isFading = false;
            _fadeElapsed = 0f;
            _fadeTime = 0f;
            _next = default;
            _fadeBehaviour?.Disable();
        }

        private void InitializeFadeDriver()
        {
            _fadePlayable = ScriptPlayable<FadeBehaviour>.Create(_graph);
            _fadeBehaviour = _fadePlayable.GetBehaviour();
            _fadeBehaviour.Initialize(this);
            _fadeOutput = ScriptPlayableOutput.Create(_graph, $"{Name}_FadeOutput");
            _fadeOutput.SetSourcePlayable(_fadePlayable);
        }

        private sealed class FadeBehaviour : PlayableBehaviour
        {
            private AFLayer _owner;
            private bool _enabled;

            public void Initialize(AFLayer owner) => _owner = owner;

            public void Enable() => _enabled = true;
            public void Disable() => _enabled = false;

            public override void PrepareFrame(Playable playable, FrameData info)
            {
                if (!_enabled || _owner == null) return;
                _owner.AdvanceFade(info.deltaTime);
            }
        }
    }
}