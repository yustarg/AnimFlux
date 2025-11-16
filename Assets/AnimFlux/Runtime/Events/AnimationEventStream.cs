using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimFlux.Runtime
{
    [CreateAssetMenu(menuName = "AnimFlux/Animation Event Stream", fileName = "AnimationEventStream")]
    public sealed class AnimationEventStream : ScriptableObject
    {
        [Serializable]
        private sealed class ClipEvents
        {
            public AnimationClip clip;
            public List<AnimationEventMarker> events = new();
        }

        [SerializeField] private List<ClipEvents> _clips = new();

        public IReadOnlyList<AnimationEventMarker> GetEvents(AnimationClip clip)
        {
            if (!clip) return Array.Empty<AnimationEventMarker>();
            for (var i = 0; i < _clips.Count; i++)
            {
                if (_clips[i].clip == clip)
                {
                    _clips[i].events.Sort((a, b) => a.NormalizedTime.CompareTo(b.NormalizedTime));
                    return _clips[i].events;
                }
            }
            return Array.Empty<AnimationEventMarker>();
        }
    }

    [Serializable]
    public struct AnimationEventMarker
    {
        public string EventId;
        [Range(0f, 1f)] public float NormalizedTime;
    }
}

