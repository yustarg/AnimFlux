using System;
using UnityEngine;

namespace AnimFlux.Runtime
{
    public sealed class LocomotionLayer
    {
        private readonly Animator _animator;
        private readonly LocomotionConfig _config;
        private readonly Action<AnimationClip, float, float> _playClip;

        private float _desiredSpeed;
        private float _currentSpeed;
        private float _speedVelocity;
        private bool _isGrounded = true;
        private bool _rootMotionEnabled = true;
        private AnimationClip _activeClip;

        public LocomotionLayer(Animator animator, LocomotionConfig config, Action<AnimationClip, float, float> playClip)
        {
            if (!config) throw new ArgumentNullException(nameof(config));
            _animator = animator;
            _config = config;
            _playClip = playClip;
            _rootMotionEnabled = config.enableRootMotion;
            ApplyRootMotion();
            if (_config.idleClip && _playClip != null)
            {
                _playClip.Invoke(_config.idleClip, 0f, 0f);
                _activeClip = _config.idleClip;
            }
        }

        public void SetMoveSpeed(float speed)
        {
            _desiredSpeed = Mathf.Max(0f, speed);
        }

        public void SetMoveDirection(Vector3 direction)
        {
            // Reserved for future directional blends. Currently unused but stored for extension.
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
            UpdateSpeed(deltaTime);
            var nextClip = SelectClip();
            if (nextClip && nextClip != _activeClip)
            {
                _playClip?.Invoke(nextClip, 0f, _config.crossFadeDuration);
                _activeClip = nextClip;
            }
        }

        private void UpdateSpeed(float deltaTime)
        {
            var damp = Mathf.Max(0.0001f, _config.speedDampTime);
            _currentSpeed = Mathf.SmoothDamp(_currentSpeed, _desiredSpeed, ref _speedVelocity, damp, Mathf.Infinity, deltaTime);
        }

        private AnimationClip SelectClip()
        {
            if (!_isGrounded && _config.fallClip) return _config.fallClip;
            if (_currentSpeed < Mathf.Epsilon) return _config.idleClip;
            if (_currentSpeed < _config.walkSpeed) return _config.walkClip ? _config.walkClip : _config.idleClip;
            if (_currentSpeed < _config.runSpeed) return _config.walkClip ? _config.walkClip : _config.idleClip;
            return _config.runClip ? _config.runClip : _config.walkClip ? _config.walkClip : _config.idleClip;
        }

        private void ApplyRootMotion()
        {
            if (!_animator) return;
            _animator.applyRootMotion = _config.enableRootMotion && _rootMotionEnabled;
        }
    }
}

