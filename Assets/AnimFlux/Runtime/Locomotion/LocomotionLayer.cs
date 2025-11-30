using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AnimFlux.Runtime
{
    /// <summary>
    /// Base locomotion layer that drives AnimationLayerType.Base through Playables.
    /// </summary>
    public sealed class LocomotionLayer : IDisposable
    {
        private enum SourceType { Idle, Walk, Sprint, Fall }

        private readonly Animator _animator;
        private readonly LocomotionConfig _config;
        private readonly PlayableGraph _graph;
        private readonly AnimationMixerPlayable _baseLayerMixer;
        private readonly List<SourceSlot> _orderedSlots = new(4);
        private readonly Dictionary<SourceType, SourceSlot> _slotLookup = new();

        private AnimationMixerPlayable _stateMixer;
        private float _desiredSpeed;
        private float _currentSpeed;
        private float _speedVelocity;
        private Vector3 _moveDirection;
        private bool _isGrounded = true;
        private bool _rootMotionEnabled = true;
        private bool _isBoundToBase;

        private float _idleWeight;
        private float _walkWeight;
        private float _sprintWeight;
        private float _fallWeight;
        private float _idleVelocity;
        private float _walkVelocity;
        private float _sprintVelocity;
        private float _fallVelocity;

        public LocomotionLayer(Animator animator, LocomotionConfig config, PlayableGraph graph, AnimationMixerPlayable baseLayerMixer)
        {
            _animator = animator;
            _config = config ? config : throw new ArgumentNullException(nameof(config));
            _graph = graph;
            _baseLayerMixer = baseLayerMixer;

            if (!_graph.IsValid() || !_baseLayerMixer.IsValid())
            {
                throw new InvalidOperationException("[AnimFlux] LocomotionLayer requires a valid playable graph and base layer mixer.");
            }

            _rootMotionEnabled = config.enableRootMotion;
            BuildMixerGraph();
            ApplyRootMotion();
        }

        public void SetMoveSpeed(float speed)
        {
            _desiredSpeed = Mathf.Max(0f, speed);
        }

        public void SetMoveDirection(Vector3 direction)
        {
            _moveDirection = direction;
        }

        public void SetIsGrounded(bool grounded)
        {
            _isGrounded = grounded;
        }

        public void SetRootMotionEnabled(bool enabled)
        {
            _rootMotionEnabled = enabled;
            ApplyRootMotion();
        }

        public void Update(float deltaTime)
        {
            if (!_stateMixer.IsValid()) return;
            UpdateSpeed(deltaTime);
            EvaluateBlendTrees();
            UpdateMixerWeights(deltaTime);
        }

        public void Dispose()
        {
            if (_isBoundToBase && _graph.IsValid() && _baseLayerMixer.IsValid())
            {
                var currentInput = _baseLayerMixer.GetInput(0);
                if (currentInput.IsValid() && currentInput.Equals((Playable)_stateMixer))
                {
                    _graph.Disconnect(_baseLayerMixer, 0);
                }
            }

            if (_stateMixer.IsValid())
            {
                _stateMixer.Destroy();
            }

            for (int i = 0; i < _orderedSlots.Count; i++)
            {
                _orderedSlots[i].Dispose();
            }

            _orderedSlots.Clear();
            _slotLookup.Clear();
        }

        private void BuildMixerGraph()
        {
            var slots = new List<SourceSlot>(4);

            var idleSlot = CreateClipSlot(SourceType.Idle, _config.idleClip);
            if (idleSlot != null) slots.Add(idleSlot);

            var walkSlot = CreateTreeSlot(SourceType.Walk, _config.walkTree, _config.walkClip);
            if (walkSlot != null) slots.Add(walkSlot);

            var sprintSlot = CreateTreeSlot(SourceType.Sprint, _config.sprintTree, _config.sprintClip);
            if (sprintSlot != null) slots.Add(sprintSlot);

            var fallSlot = CreateClipSlot(SourceType.Fall, _config.fallClip);
            if (fallSlot != null) slots.Add(fallSlot);

            if (slots.Count == 0)
            {
                Debug.LogWarning("[AnimFlux] LocomotionLayer has no valid clips or blend trees assigned.");
                return;
            }

            _stateMixer = AnimationMixerPlayable.Create(_graph, slots.Count);
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                slot.Index = i;
                _orderedSlots.Add(slot);
                _slotLookup[slot.Type] = slot;
                _graph.Connect(slot.Playable, 0, _stateMixer, i);
                _stateMixer.SetInputWeight(i, i == 0 ? 1f : 0f);
            }

            BindToBaseLayer();
        }

        private void BindToBaseLayer()
        {
            if (!_stateMixer.IsValid()) return;

            var inputCount = Mathf.Max(_baseLayerMixer.GetInputCount(), 1);
            _baseLayerMixer.SetInputCount(inputCount);

            var existing = _baseLayerMixer.GetInput(0);
            if (existing.IsValid())
            {
                _graph.Disconnect(_baseLayerMixer, 0);
            }

            _graph.Connect(_stateMixer, 0, _baseLayerMixer, 0);
            _baseLayerMixer.SetInputWeight(0, 1f);
            _isBoundToBase = true;
        }

        private SourceSlot CreateClipSlot(SourceType type, AnimationClip clip)
        {
            if (!clip) return null;
            var playable = AnimationClipPlayable.Create(_graph, clip);
            playable.SetApplyFootIK(true);
            playable.SetApplyPlayableIK(true);
            playable.SetDuration(clip.isLooping ? double.PositiveInfinity : clip.length);
            playable.SetSpeed(1f);

            return new SourceSlot(type, playable, default, playable);
        }

        private SourceSlot CreateTreeSlot(SourceType type, LocomotionBlendTreeAsset tree, AnimationClip fallbackClip)
        {
            if (tree)
            {
                var instance = new LocomotionBlendTreeInstance(_graph, tree);
                if (instance.IsValid)
                {
                    return new SourceSlot(type, instance.Output, instance, default);
                }
                instance.Dispose();
            }

            return CreateClipSlot(type, fallbackClip);
        }

        private void UpdateSpeed(float deltaTime)
        {
            var damp = Mathf.Max(0.0001f, _config.speedDampTime);
            _currentSpeed = Mathf.SmoothDamp(_currentSpeed, _desiredSpeed, ref _speedVelocity, damp, Mathf.Infinity, deltaTime);
        }

        private void EvaluateBlendTrees()
        {
            var planarDir = new Vector2(_moveDirection.x, _moveDirection.z);
            if (planarDir.sqrMagnitude > 0.0001f)
            {
                planarDir = planarDir.normalized * _currentSpeed;
            }
            else
            {
                planarDir = new Vector2(0f, _currentSpeed);
            }

            for (int i = 0; i < _orderedSlots.Count; i++)
            {
                _orderedSlots[i].Evaluate(planarDir);
            }
        }

        private void UpdateMixerWeights(float deltaTime)
        {
            float idleTarget;
            float walkTarget;
            float sprintTarget;
            float fallTarget;

            if (!_isGrounded && _slotLookup.ContainsKey(SourceType.Fall))
            {
                idleTarget = 0f;
                walkTarget = 0f;
                sprintTarget = 0f;
                fallTarget = 1f;
            }
            else
            {
                fallTarget = 0f;

                var walkSpeed = Mathf.Max(0.0001f, _config.walkSpeed);
                var sprintSpeed = Mathf.Max(_config.sprintSpeed, walkSpeed + 0.0001f);
                var sprintRange = Mathf.Max(0.0001f, _config.sprintBlendRange);

                var walkBlend = Mathf.Clamp01(_currentSpeed / walkSpeed);
                var sprintBlend = Mathf.Clamp01((_currentSpeed - sprintSpeed) / sprintRange);
                walkBlend = Mathf.Clamp01(walkBlend - sprintBlend);

                walkTarget = walkBlend;
                sprintTarget = sprintBlend;
                idleTarget = 1f - Mathf.Clamp01(walkTarget + sprintTarget);
            }

            if (!_slotLookup.ContainsKey(SourceType.Sprint))
            {
                walkTarget += sprintTarget;
                sprintTarget = 0f;
            }

            if (!_slotLookup.ContainsKey(SourceType.Walk))
            {
                idleTarget += walkTarget;
                walkTarget = 0f;
            }

            if (!_slotLookup.ContainsKey(SourceType.Idle))
            {
                walkTarget += idleTarget;
                idleTarget = 0f;
            }

            if (!_slotLookup.ContainsKey(SourceType.Fall))
            {
                idleTarget += fallTarget;
                fallTarget = 0f;
            }

            var blendDamp = Mathf.Max(0.0001f, _config.stateBlendDampTime);
            _idleWeight = Mathf.SmoothDamp(_idleWeight, idleTarget, ref _idleVelocity, blendDamp, Mathf.Infinity, deltaTime);
            _walkWeight = Mathf.SmoothDamp(_walkWeight, walkTarget, ref _walkVelocity, blendDamp, Mathf.Infinity, deltaTime);
            _sprintWeight = Mathf.SmoothDamp(_sprintWeight, sprintTarget, ref _sprintVelocity, blendDamp, Mathf.Infinity, deltaTime);
            _fallWeight = Mathf.SmoothDamp(_fallWeight, fallTarget, ref _fallVelocity, blendDamp, Mathf.Infinity, deltaTime);

            SetSlotWeight(SourceType.Idle, _idleWeight);
            SetSlotWeight(SourceType.Walk, _walkWeight);
            SetSlotWeight(SourceType.Sprint, _sprintWeight);
            SetSlotWeight(SourceType.Fall, _fallWeight);
        }

        private void SetSlotWeight(SourceType type, float weight)
        {
            if (!_slotLookup.TryGetValue(type, out var slot)) return;
            if (!_stateMixer.IsValid()) return;
            _stateMixer.SetInputWeight(slot.Index, Mathf.Clamp01(weight));
        }

        private void ApplyRootMotion()
        {
            if (!_animator) return;
            _animator.applyRootMotion = _config.enableRootMotion && _rootMotionEnabled;
        }

        private sealed class SourceSlot : IDisposable
        {
            public SourceType Type { get; }
            public Playable Playable { get; }
            public int Index { get; set; }
            public LocomotionBlendTreeInstance TreeInstance { get; }
            public AnimationClipPlayable ClipPlayable { get; }

            public SourceSlot(SourceType type, Playable playable, LocomotionBlendTreeInstance treeInstance, AnimationClipPlayable clipPlayable)
            {
                Type = type;
                Playable = playable;
                TreeInstance = treeInstance;
                ClipPlayable = clipPlayable;
            }

            public void Evaluate(Vector2 parameter) => TreeInstance?.Evaluate(parameter);

            public void Dispose()
            {
                TreeInstance?.Dispose();
                if (ClipPlayable.IsValid())
                {
                    ClipPlayable.Destroy();
                }
            }
        }
    }
}
