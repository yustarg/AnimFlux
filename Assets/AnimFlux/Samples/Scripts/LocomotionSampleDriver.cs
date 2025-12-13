using AnimFlux.Runtime;
using UnityEngine;

namespace AnimFlux.Samples
{
    /// <summary>
    /// Minimal sample that drives the AnimController locomotion layer using WASD input so designers can preview blend trees.
    /// </summary>
    [RequireComponent(typeof(AnimController))]
    public sealed class LocomotionSampleDriver : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float maxWalkSpeed = 2f;
        [SerializeField] private float sprintMultiplier = 1.6f;
        [SerializeField] private float acceleration = 8f;
        [SerializeField] private float deceleration = 10f;
        [SerializeField] private float turnSpeed = 540f;
        [SerializeField] private Transform facingRoot;
        [SerializeField] private Transform cameraTransform;

        [Header("Grounding")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Transform rearGroundProbe;
        [SerializeField] private Transform frontGroundProbe;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private bool autoGrounded = true;
        [SerializeField] private KeyCode toggleGroundedKey = KeyCode.Space;
        [SerializeField] private float gravity = 9.81f;
        [SerializeField] private bool useCharacterControllerMove = true;

        [Header("Strafe & Incline")]
        [SerializeField] private bool alwaysStrafe = true;
        [SerializeField] private float forwardStrafeMinAngle = -55f;
        [SerializeField] private float forwardStrafeMaxAngle = 125f;
        [SerializeField] private float strafeDirectionDamp = 20f;
        [SerializeField] private float forwardStrafeDamp = 12f;
        [SerializeField] private bool faceMoveDirection = true;

        [Header("Sample Input Mapping")]
        [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode alternateSprintKey = KeyCode.RightShift;
        [SerializeField] private KeyCode aimKey = KeyCode.Mouse1;
        [SerializeField] private KeyCode lockOnToggleKey = KeyCode.Tab;
        [SerializeField] private KeyCode strafeToggleKey = KeyCode.LeftAlt;
        [SerializeField] private KeyCode resetCameraKey = KeyCode.R;

        private AnimController _controller;
        private Vector3 _currentVelocity;
        private bool _manualGrounded = true;
        private bool _inputSprintActive;
        private bool _inputAimActive;
        private bool _inputLockOnActive;
        private float _currentForwardStrafe;
        private float _currentStrafeDirection;
        private float _currentIncline;
        private float _verticalVelocity;

        private void Awake()
        {
            _controller = GetComponent<AnimController>();
            if (!cameraTransform && Camera.main)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void Update()
        {
            if (_controller == null || !_controller.IsInitialized) return;

            ReadSampleInputToggles();
            UpdateGroundedState();
            var moveInput = ReadMoveInput();
            var desired = CalculateDesiredVelocity(moveInput, out var normalizedPlanarDir);
            var accel = desired.sqrMagnitude > 0.0001f ? acceleration : deceleration;
            _currentVelocity = Vector3.MoveTowards(_currentVelocity, desired, accel * Time.deltaTime);

            var planarSpeed = _currentVelocity.magnitude;
            var planarDir = planarSpeed > 0.0001f ? _currentVelocity.normalized : normalizedPlanarDir;
            var isStrafing = ShouldStrafe();

            _controller.SetMoveSpeed(planarSpeed);
            _controller.SetMoveDirection(planarDir);
            _controller.SetIsGrounded(autoGrounded || _manualGrounded);
            _controller.SetIsStrafing(isStrafing);
            UpdateStrafeParameters(planarDir, isStrafing, Time.deltaTime);
            _controller.SetInclineAngle(ComputeInclineAngle(Time.deltaTime));

            ApplyMovement(planarDir);
            UpdateFacing(planarDir);
        }

        private Vector2 ReadMoveInput()
        {
            var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            return Vector2.ClampMagnitude(input, 1f);
        }

        private Vector3 CalculateDesiredVelocity(Vector2 moveInput, out Vector3 planarDirection)
        {
            var forward = cameraTransform ? Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized : Vector3.forward;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            var right = new Vector3(forward.z, 0f, -forward.x);

            var worldDir = (forward * moveInput.y) + (right * moveInput.x);
            planarDirection = worldDir.sqrMagnitude > 0.0001f ? worldDir.normalized : Vector3.zero;

            var targetSpeed = maxWalkSpeed;
            if (IsSprintingRequested())
            {
                targetSpeed *= sprintMultiplier;
            }

            return planarDirection * targetSpeed;
        }

        private void UpdateStrafeParameters(Vector3 planarDir, bool isStrafing, float deltaTime)
        {
            var characterForward = facingRoot ? facingRoot.forward : transform.forward;
            characterForward = Vector3.ProjectOnPlane(characterForward, Vector3.up).normalized;
            if (characterForward.sqrMagnitude < 0.0001f) characterForward = Vector3.forward;

            var characterRight = Vector3.Cross(Vector3.up, characterForward);
            var directionForward = planarDir.sqrMagnitude > 0.0001f ? planarDir : characterForward;

            var targetStrafeZ = Vector3.Dot(characterForward, directionForward);
            var targetStrafeX = Vector3.Dot(characterRight, directionForward);

            var angle = Vector3.SignedAngle(characterForward, directionForward, Vector3.up);
            var targetForwardStrafe = isStrafing && angle > forwardStrafeMinAngle && angle < forwardStrafeMaxAngle ? 1f : 0f;

            // Match Synty sample behavior: smooth but responsive
            _currentStrafeDirection = Mathf.Lerp(_currentStrafeDirection, targetStrafeX, strafeDirectionDamp * deltaTime);
            float t = Mathf.Clamp01(forwardStrafeDamp * deltaTime);
            _currentForwardStrafe = Mathf.SmoothStep(_currentForwardStrafe, targetForwardStrafe, t);

            _controller.SetForwardStrafe(_currentForwardStrafe);
            _controller.SetStrafeDirection(_currentStrafeDirection);
        }

        private float ComputeInclineAngle(float deltaTime)
        {
            if (!rearGroundProbe || !frontGroundProbe) return 0f;

            bool rearHitFound = Physics.Raycast(rearGroundProbe.position, Vector3.down, out var rearHit, 2f, groundMask, QueryTriggerInteraction.Ignore);
            bool frontHitFound = Physics.Raycast(frontGroundProbe.position, Vector3.down, out var frontHit, 2f, groundMask, QueryTriggerInteraction.Ignore);

            if (!rearHitFound || !frontHitFound) return 0f;

            var difference = frontHit.point - rearHit.point;
            var horizontal = new Vector2(difference.x, difference.z).magnitude;
            var targetAngle = Mathf.Atan2(difference.y, horizontal) * Mathf.Rad2Deg;

            _currentIncline = Mathf.Lerp(_currentIncline, targetAngle, 20f * deltaTime);
            return _currentIncline;
        }

        private void UpdateGroundedState()
        {
            if (autoGrounded) return;
            if (Input.GetKeyDown(toggleGroundedKey))
            {
                _manualGrounded = !_manualGrounded;
            }
            else if (characterController)
            {
                _manualGrounded = characterController.isGrounded;
            }
        }

        private void UpdateFacing(Vector3 planarDir)
        {
            if (!facingRoot || planarDir.sqrMagnitude < 0.0001f) return;
            if (!faceMoveDirection) return;
            var targetRotation = Quaternion.LookRotation(planarDir, Vector3.up);
            facingRoot.rotation = Quaternion.RotateTowards(facingRoot.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        private void ApplyMovement(Vector3 planarDir)
        {
            var dt = Time.deltaTime;
            if (characterController && useCharacterControllerMove)
            {
                if (characterController.isGrounded)
                {
                    // keep a small downward force to stay grounded
                    if (_verticalVelocity < 0f) _verticalVelocity = -2f;
                }
                else
                {
                    _verticalVelocity -= gravity * dt;
                }

                var move = new Vector3(_currentVelocity.x, _verticalVelocity, _currentVelocity.z);
                characterController.Move(move * dt);
            }
            else
            {
                // fall back to direct transform move (no collision)
                transform.position += _currentVelocity * dt;
            }
        }

        private bool ShouldStrafe() => alwaysStrafe || _inputAimActive || _inputLockOnActive;

        private bool IsSprintingRequested() => _inputSprintActive;

        private void ReadSampleInputToggles()
        {
            var sprintPressed = Input.GetKey(sprintKey) || Input.GetKey(alternateSprintKey);
            _inputSprintActive = sprintPressed;

            _inputAimActive = Input.GetKey(aimKey);

            if (Input.GetKeyDown(lockOnToggleKey))
            {
                _inputLockOnActive = !_inputLockOnActive;
            }

            if (Input.GetKeyDown(strafeToggleKey))
            {
                alwaysStrafe = !alwaysStrafe;
            }

            if (cameraTransform && Input.GetKeyDown(resetCameraKey) && facingRoot)
            {
                var forward = Vector3.ProjectOnPlane(facingRoot.forward, Vector3.up);
                if (forward.sqrMagnitude > 0.0001f)
                {
                    cameraTransform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                }
            }
        }
    }
}

