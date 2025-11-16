using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimFlux.Runtime
{
    public sealed class AnimationEventRouter : IDisposable
    {
        private readonly AnimationEventStream _stream;
        private readonly Action<string, float> _dispatch;
        private readonly Dictionary<int, ClipTracker> _trackers = new();
        private readonly List<int> _completedLayers = new();

        public AnimationEventRouter(AnimationEventStream stream, Action<string, float> dispatch)
        {
            _stream = stream;
            _dispatch = dispatch;
        }

        public void TrackClip(int layerIndex, AnimationClip clip)
        {
            if (!_stream || clip == null) return;
            var events = _stream.GetEvents(clip);
            if (events == null || events.Count == 0)
            {
                _trackers.Remove(layerIndex);
                return;
            }

            _trackers[layerIndex] = new ClipTracker(clip.length, events);
        }

        public void Update(float deltaTime)
        {
            if (_trackers.Count == 0 || deltaTime <= 0f) return;
            _completedLayers.Clear();

            foreach (var pair in _trackers)
            {
                var tracker = pair.Value;
                tracker.Advance(deltaTime, _dispatch);
                if (tracker.IsComplete)
                {
                    _completedLayers.Add(pair.Key);
                }
            }

            for (var i = 0; i < _completedLayers.Count; i++)
            {
                _trackers.Remove(_completedLayers[i]);
            }
        }

        public void Dispose()
        {
            _trackers.Clear();
            _completedLayers.Clear();
        }

        private sealed class ClipTracker
        {
            private readonly float _duration;
            private readonly IReadOnlyList<AnimationEventMarker> _events;
            private int _nextIndex;
            private float _elapsed;

            public bool IsComplete { get; private set; }

            public ClipTracker(float duration, IReadOnlyList<AnimationEventMarker> events)
            {
                _duration = Mathf.Max(0.0001f, duration);
                _events = events;
                _nextIndex = 0;
            }

            public void Advance(float deltaTime, Action<string, float> dispatch)
            {
                if (IsComplete) return;
                _elapsed += deltaTime;
                var normalized = Mathf.Clamp01(_elapsed / _duration);

                while (_nextIndex < _events.Count && normalized >= _events[_nextIndex].NormalizedTime)
                {
                    var evt = _events[_nextIndex];
                    dispatch?.Invoke(evt.EventId, evt.NormalizedTime);
                    _nextIndex++;
                }

                if (normalized >= 0.999f)
                {
                    IsComplete = true;
                }
            }
        }
    }
}

