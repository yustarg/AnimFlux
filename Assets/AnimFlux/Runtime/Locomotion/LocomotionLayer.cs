using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace AnimFlux.Runtime
{
    /// <summary>
    /// Drives the base locomotion playable graph using a single root blend tree.
    /// </summary>
    public sealed class LocomotionLayer : IDisposable
    {
        private readonly Animator _animator;
        private readonly LocomotionConfig _config;
        private readonly PlayableGraph _graph;
        private readonly AnimationMixerPlayable _baseLayerMixer;

        private AnimationBlendTreeInstance _rootTreeInstance;
        private AnimationClipPlayable _fallbackClipPlayable;
        private Playable _boundPlayable;
        private bool _isBoundToBase;

        private bool _rootMotionEnabled = true;
        private bool _isGrounded = true;

        private float _desiredSpeed;
        private float _currentSpeed;
        private float _speedVelocity;

        private bool _isStrafing;
        private float _forwardStrafeInput;
        private float _strafeDirectionInput;
        private float _inclineInput;

        private float _currentForwardStrafe;
        private float _currentStrafeDirection;
        private float _currentIncline;
        private float _forwardStrafeVelocity;
        private float _strafeDirVelocity;
        private float _inclineVelocity;
        private LocomotionContext _blendContext;
        private readonly bool _debugLog;
        private readonly float _debugInterval;
        private float _debugTimer;
        private bool _warnedNoPlayable;
        private string _debugSource;

        public LocomotionLayer(Animator animator, LocomotionConfig config, PlayableGraph graph, AnimationMixerPlayable baseLayerMixer)
        {
            _animator = animator;
            _config = config ? config : throw new ArgumentNullException(nameof(config));
            _graph = graph;
            _baseLayerMixer = baseLayerMixer;
            _debugLog = config.debugLog;
            _debugInterval = Mathf.Max(0.05f, config.debugLogInterval);

            if (!_graph.IsValid() || !_baseLayerMixer.IsValid())
            {
                throw new InvalidOperationException("[AnimFlux] LocomotionLayer requires a valid playable graph and base layer mixer.");
            }

            _rootMotionEnabled = config.enableRootMotion;
            BuildRootPlayable();
            ApplyRootMotion();
        }

        public void SetMoveSpeed(float speed)
        {
            _desiredSpeed = Mathf.Max(0f, speed);
        }

        public void SetMoveDirection(Vector3 direction)
        {
            var planar = new Vector2(direction.x, direction.z);
            _forwardStrafeInput = planar.y;
            _strafeDirectionInput = planar.x;
        }

        public void SetIsStrafing(bool isStrafing) => _isStrafing = isStrafing;
        public void SetForwardStrafe(float value) => _forwardStrafeInput = value;
        public void SetStrafeDirection(float value) => _strafeDirectionInput = value;
        public void SetInclineAngle(float value) => _inclineInput = value;

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
            if (!_boundPlayable.IsValid()) return;

            UpdateSpeed(deltaTime);
            UpdateParameters(deltaTime);

            _rootTreeInstance?.Evaluate(_blendContext);
            MaybeDebugLog(deltaTime);
        }

        public void Dispose()
        {
            if (_isBoundToBase && _graph.IsValid() && _baseLayerMixer.IsValid())
            {
                var currentInput = _baseLayerMixer.GetInput(0);
                if (currentInput.IsValid() && currentInput.Equals(_boundPlayable))
                {
                    _graph.Disconnect(_baseLayerMixer, 0);
                }
            }

            _isBoundToBase = false;

            _rootTreeInstance?.Dispose();
            _rootTreeInstance = null;

            if (_fallbackClipPlayable.IsValid())
            {
                _fallbackClipPlayable.Destroy();
            }

            _boundPlayable = default;
        }

        private void BuildRootPlayable()
        {
            if (_config.rootTree)
            {
                _rootTreeInstance = new AnimationBlendTreeInstance(_graph, _config.rootTree);
                if (_rootTreeInstance.IsValid)
                {
                    _boundPlayable = _rootTreeInstance.Playable;
                    _debugSource = $"BlendTree:{_config.rootTree.name}";
                }
                else
                {
                    _rootTreeInstance.Dispose();
                    _rootTreeInstance = null;
                }
            }

            if (!_boundPlayable.IsValid() && _config.fallbackClip)
            {
                _fallbackClipPlayable = AnimationClipPlayable.Create(_graph, _config.fallbackClip);
                _fallbackClipPlayable.SetApplyFootIK(true);
                _fallbackClipPlayable.SetApplyPlayableIK(true);
                _fallbackClipPlayable.SetDuration(_config.fallbackClip.isLooping ? double.PositiveInfinity : _config.fallbackClip.length);
                _fallbackClipPlayable.SetSpeed(1f);
                _boundPlayable = _fallbackClipPlayable;
                _debugSource = $"FallbackClip:{_config.fallbackClip.name}";
            }

            if (!_boundPlayable.IsValid())
            {
                Debug.LogWarning("[AnimFlux] LocomotionLayer requires a root blend tree or fallback clip.");
                _warnedNoPlayable = true;
                return;
            }

            BindToBaseLayer(_boundPlayable);
            if (_debugLog)
            {
                Debug.Log($"[AnimFlux][Locomotion] Bound playable: {_debugSource}");
            }
        }

        private void BindToBaseLayer(Playable source)
        {
            if (!source.IsValid() || !_baseLayerMixer.IsValid()) return;

            var inputCount = Mathf.Max(_baseLayerMixer.GetInputCount(), 1);
            _baseLayerMixer.SetInputCount(inputCount);

            var existing = _baseLayerMixer.GetInput(0);
            if (existing.IsValid())
            {
                _graph.Disconnect(_baseLayerMixer, 0);
            }

            _graph.Connect(source, 0, _baseLayerMixer, 0);
            _baseLayerMixer.SetInputWeight(0, 1f);
            _isBoundToBase = true;
        }

        private void UpdateSpeed(float deltaTime)
        {
            var damp = Mathf.Max(0.0001f, _config.speedDampTime);
            _currentSpeed = Mathf.SmoothDamp(_currentSpeed, _desiredSpeed, ref _speedVelocity, damp, Mathf.Infinity, deltaTime);
        }

        private void UpdateParameters(float deltaTime)
        {
            var paramDamp = Mathf.Max(0.0001f, _config.parameterDampTime);
            _currentForwardStrafe = Mathf.SmoothDamp(_currentForwardStrafe, _forwardStrafeInput, ref _forwardStrafeVelocity, paramDamp, Mathf.Infinity, deltaTime);
            _currentStrafeDirection = Mathf.SmoothDamp(_currentStrafeDirection, _strafeDirectionInput, ref _strafeDirVelocity, paramDamp, Mathf.Infinity, deltaTime);
            _currentIncline = Mathf.SmoothDamp(_currentIncline, _inclineInput, ref _inclineVelocity, paramDamp, Mathf.Infinity, deltaTime);

            var normalizedSpeed = NormalizeParameter(_isGrounded ? _currentSpeed : 0f, _config.maxMoveSpeed);
            var normalizedForwardStrafe = NormalizeParameter(_currentForwardStrafe, _config.maxForwardStrafe);
            var normalizedStrafeDirection = NormalizeParameter(_currentStrafeDirection, _config.maxStrafeDirection);
            var normalizedIncline = NormalizeParameter(_currentIncline, _config.maxInclineAngle);

            var directional = _isStrafing
                ? new Vector2(normalizedStrafeDirection, normalizedForwardStrafe)
                : new Vector2(0f, normalizedSpeed);

            directional.y += normalizedIncline;

            _blendContext = new LocomotionContext
            {
                DirectionalBlend = directional,
                SpeedNormalized = normalizedSpeed,
                ForwardStrafeNormalized = normalizedForwardStrafe,
                StrafeDirectionNormalized = normalizedStrafeDirection,
                InclineNormalized = normalizedIncline,
                IsStrafing = _isStrafing,
                FloatBlend = normalizedSpeed // default float channel; blend spaces可通过参数名访问其他值
            };

            _blendContext.ClearParameters();
            // Populate defaults for easy reuse in BlendSpaces; users can reference by name.
            _blendContext.SetFloat("SpeedNormalized", normalizedSpeed);
            _blendContext.SetFloat("SpeedRaw", _currentSpeed);
            _blendContext.SetFloat("ForwardStrafe", normalizedForwardStrafe);
            _blendContext.SetFloat("StrafeDirection", normalizedStrafeDirection);
            _blendContext.SetFloat("Incline", normalizedIncline);
            _blendContext.SetFloat("IsStrafing", _isStrafing ? 1f : 0f);
            _blendContext.SetFloat("FloatBlend", normalizedSpeed); // default float channel
            _blendContext.SetVector2("Directional", directional);
            _blendContext.SetVector2("Move", directional);
            _blendContext.SetVector2("LocomotionDir", directional);
        }

        private static float NormalizeParameter(float value, float max)
        {
            var denom = Mathf.Max(0.0001f, Mathf.Abs(max));
            return Mathf.Clamp(value / denom, -1f, 1f);
        }

        private void ApplyRootMotion()
        {
            if (!_animator) return;
            _animator.applyRootMotion = _config.enableRootMotion && _rootMotionEnabled;
        }

        private void MaybeDebugLog(float deltaTime)
        {
            if (!_debugLog) return;
            _debugTimer += deltaTime;
            if (_debugTimer < _debugInterval) return;
            _debugTimer = 0f;
            var source = string.IsNullOrEmpty(_debugSource) ? "None" : _debugSource;
            Debug.Log($"[AnimFlux][Locomotion] valid={_boundPlayable.IsValid()}, source={source}, speed={_currentSpeed:F2}, desired={_desiredSpeed:F2}, normSpeed={_blendContext.SpeedNormalized:F2}, dir=({_blendContext.DirectionalBlend.x:F2},{_blendContext.DirectionalBlend.y:F2}), grounded={_isGrounded}, strafing={_isStrafing}");
        }
    }
}
