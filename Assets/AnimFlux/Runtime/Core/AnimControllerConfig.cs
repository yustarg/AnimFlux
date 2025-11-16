using System.Collections.Generic;
using UnityEngine;

namespace AnimFlux.Runtime
{
    [CreateAssetMenu(menuName = "AnimFlux/Anim Controller Config", fileName = "AnimControllerConfig")]
    public sealed class AnimControllerConfig : ScriptableObject
    {
        [SerializeField] private List<LayerDefinition> _layers = new();
        [SerializeField] private LocomotionConfig _locomotionConfig;
        [SerializeField] private AnimationEventStream _eventStream;

        public IReadOnlyList<LayerDefinition> Layers => _layers;
        public int LayerCount => _layers?.Count ?? 0;
        public LocomotionConfig LocomotionConfig => _locomotionConfig;
        public AnimationEventStream EventStream => _eventStream;
    }
}

