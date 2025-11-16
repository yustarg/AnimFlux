using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnimFlux.Runtime
{
    [DisallowMultipleComponent]
    public sealed class AnimController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private AnimControllerConfig config;
        [SerializeField] private bool initializeOnAwake = true;

        private AFGraph _graph;
        private readonly Dictionary<AnimationLayerType, int> _layerLookup = new();
        private LocomotionLayer _locomotionLayer;
        private CharacterIKController _ikController;
        private AnimationEventRouter _eventRouter;
        private readonly List<IAnimationEventHandler> _eventHandlers = new();
        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;
        public CharacterIKController IKController => _ikController;

        private void Awake()
        {
            if (!animator) animator = GetComponent<Animator>();
            if (initializeOnAwake)
            {
                Initialize();
            }
        }

        private void OnEnable()
        {
            if (!_isInitialized && initializeOnAwake)
            {
                Initialize();
            }
        }

        private void Update()
        {
            if (!_isInitialized) return;
            var dt = Time.deltaTime;
            _locomotionLayer?.Update(dt);
            _eventRouter?.Update(dt);
        }

        private void OnDisable()
        {
            Dispose();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!_isInitialized) return;
            _ikController?.Apply(animator);
        }

        public void Initialize(Animator overrideAnimator = null)
        {
            if (_isInitialized) return;
            animator = overrideAnimator ? overrideAnimator : animator ? animator : GetComponent<Animator>();
            if (!animator)
            {
                Debug.LogWarning("[AnimFlux] AnimController requires an Animator.");
                return;
            }

            _graph = AFGraph.Create(animator);
            BuildLayers();
            _ikController = new CharacterIKController();

            var eventStream = config ? config.EventStream : null;
            if (eventStream)
            {
                _eventRouter = new AnimationEventRouter(eventStream, DispatchAnimationEvent);
            }

            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized) return;
            _eventRouter?.Dispose();
            _eventRouter = null;
            _locomotionLayer = null;
            _ikController = null;
            _layerLookup.Clear();
            _graph?.Dispose();
            _graph = null;
            _isInitialized = false;
        }

        private void BuildLayers()
        {
            var definitions = ResolveLayerDefinitions();
            foreach (var def in definitions)
            {
                var index = _graph.AddLayer(def.Name, def.Mask, def.DefaultWeight, def.BlendMode);
                _layerLookup[def.LayerType] = index;
            }

            var locomotionConfig = config ? config.LocomotionConfig : null;
            if (locomotionConfig && _layerLookup.TryGetValue(AnimationLayerType.Base, out var baseLayerIndex))
            {
                _locomotionLayer = new LocomotionLayer(animator, locomotionConfig, (clip, time, fade) =>
                {
                    PlayClipInternal(baseLayerIndex, clip, time, fade);
                });
            }
        }

        private IReadOnlyList<LayerDefinition> ResolveLayerDefinitions()
        {
            if (config != null && config.LayerCount > 0) return config.Layers;
            return LayerConfigurator.CreateDefault();
        }

        public void SetLayerWeight(AnimationLayerType layerType, float weight)
        {
            if (!_layerLookup.TryGetValue(layerType, out var index) || _graph == null) return;
            _graph.SetLayerWeight(index, Mathf.Clamp01(weight));
        }

        public float GetLayerWeight(AnimationLayerType layerType)
        {
            if (!_layerLookup.TryGetValue(layerType, out var index) || _graph == null) return 0f;
            return _graph.GetLayerWeight(index);
        }

        public void PlayClip(AnimationLayerType layerType, AnimationClip clip, float normalizedTime = 0f, float crossFadeDuration = 0.15f)
        {
            if (!_layerLookup.TryGetValue(layerType, out var index)) return;
            PlayClipInternal(index, clip, normalizedTime, crossFadeDuration);
        }

        private void PlayClipInternal(int layerIndex, AnimationClip clip, float normalizedTime, float crossFadeDuration)
        {
            if (_graph == null || clip == null) return;
            _graph.PlayClip(layerIndex, clip, normalizedTime, crossFadeDuration);
            _eventRouter?.TrackClip(layerIndex, clip);
        }

        public void SetMoveSpeed(float speed) => _locomotionLayer?.SetMoveSpeed(speed);
        public void SetMoveDirection(Vector3 direction) => _locomotionLayer?.SetMoveDirection(direction);
        public void SetIsGrounded(bool grounded) => _locomotionLayer?.SetIsGrounded(grounded);
        public void SetRootMotionEnabled(bool enabled) => _locomotionLayer?.SetRootMotionEnabled(enabled);

        public void SetLookAtTarget(Transform target, float weight = 1f) => _ikController?.SetLookAtTarget(target, weight);
        public void ClearLookAtTarget() => _ikController?.ClearLookAt();
        public void SetIKTarget(AvatarIKGoal goal, Transform target, float positionWeight = 1f, float rotationWeight = 1f)
            => _ikController?.SetIKTarget(goal, target, positionWeight, rotationWeight);
        public void ClearIKTarget(AvatarIKGoal goal) => _ikController?.ClearIKTarget(goal);

        public void RegisterEventHandler(IAnimationEventHandler handler)
        {
            if (handler == null || _eventHandlers.Contains(handler)) return;
            _eventHandlers.Add(handler);
        }

        public void UnregisterEventHandler(IAnimationEventHandler handler)
        {
            if (handler == null) return;
            _eventHandlers.Remove(handler);
        }

        private void DispatchAnimationEvent(string eventId, float normalizedTime)
        {
            foreach (var handler in _eventHandlers)
            {
                handler?.OnAnimationEvent(eventId, normalizedTime);
            }
        }
    }
}

