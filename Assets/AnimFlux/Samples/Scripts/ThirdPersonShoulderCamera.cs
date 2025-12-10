using UnityEngine;

namespace AnimFlux.Samples
{
    /// <summary>
    /// Simple third-person over-shoulder camera controller.
    /// - Mouse/gamepad right-stick orbit (yaw/pitch).
    /// - Scroll wheel zoom.
    /// - Shoulder swap (left/right) by key.
    /// - Collision avoidance via sphere cast.
    /// </summary>
    public sealed class ThirdPersonShoulderCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 shoulderOffset = new Vector3(0.4f, 1.6f, 0f);
        [SerializeField] private bool startOnLeftShoulder = false;

        [Header("Orbit")]
        [SerializeField] private float yawSpeed = 180f;
        [SerializeField] private float pitchSpeed = 120f;
        [SerializeField] private float minPitch = -30f;
        [SerializeField] private float maxPitch = 70f;
        [SerializeField] private float rotationSmoothTime = 0.05f;

        [Header("Zoom")]
        [SerializeField] private float distance = 3f;
        [SerializeField] private float minDistance = 1.2f;
        [SerializeField] private float maxDistance = 5f;
        [SerializeField] private float zoomSpeed = 2.5f;

        [Header("Smoothing")]
        [SerializeField] private float positionSmoothTime = 0.05f;

        [Header("Collision")]
        [SerializeField] private float collisionRadius = 0.2f;
        [SerializeField] private LayerMask collisionMask = ~0;

        [Header("Input")]
        [SerializeField] private string mouseXInput = "Mouse X";
        [SerializeField] private string mouseYInput = "Mouse Y";
        [SerializeField] private string zoomInput = "Mouse ScrollWheel";
        [SerializeField] private KeyCode shoulderSwapKey = KeyCode.LeftAlt;
        [SerializeField] private bool lockCursor = true;
        [SerializeField] private KeyCode toggleCursorLockKey = KeyCode.Escape;

        private float _yaw;
        private float _pitch;
        private float _targetYaw;
        private float _targetPitch;
        private float _currentDistance;
        private bool _useLeftShoulder;
        private Vector3 _smoothVelocity;
        private float _yawVel;
        private float _pitchVel;
        private bool _cursorLocked;

        private void Awake()
        {
            _useLeftShoulder = startOnLeftShoulder;
            _currentDistance = Mathf.Clamp(distance, minDistance, maxDistance);
            SetCursorLocked(lockCursor);
        }

        private void LateUpdate()
        {
            if (!target) return;

            HandleCursorToggle();
            ReadInput();
            _yaw = Mathf.SmoothDampAngle(_yaw, _targetYaw, ref _yawVel, rotationSmoothTime);
            _pitch = Mathf.SmoothDampAngle(_pitch, _targetPitch, ref _pitchVel, rotationSmoothTime);
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

            var rotation = Quaternion.Euler(_pitch, _yaw, 0f);

            var side = _useLeftShoulder ? -1f : 1f;
            var localOffset = new Vector3(shoulderOffset.x * side, shoulderOffset.y, shoulderOffset.z);

            var desiredPos = target.position + rotation * localOffset - rotation * Vector3.forward * _currentDistance;
            desiredPos = ResolveCollision(target.position + rotation * localOffset, desiredPos);

            transform.rotation = rotation;
            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref _smoothVelocity, positionSmoothTime);
        }

        private void ReadInput()
        {
            float dx = Input.GetAxisRaw(mouseXInput);
            float dy = Input.GetAxisRaw(mouseYInput);
            float scroll = Input.GetAxisRaw(zoomInput);

            _targetYaw += dx * yawSpeed * Time.deltaTime;
            _targetPitch -= dy * pitchSpeed * Time.deltaTime;
            _targetPitch = Mathf.Clamp(_targetPitch, minPitch, maxPitch);

            if (Mathf.Abs(scroll) > Mathf.Epsilon)
            {
                _currentDistance = Mathf.Clamp(_currentDistance - scroll * zoomSpeed, minDistance, maxDistance);
            }

            if (Input.GetKeyDown(shoulderSwapKey))
            {
                _useLeftShoulder = !_useLeftShoulder;
            }
        }

        private Vector3 ResolveCollision(Vector3 origin, Vector3 desired)
        {
            Vector3 dir = desired - origin;
            float dist = dir.magnitude;
            if (dist <= 0.0001f) return desired;

            dir /= dist;
            if (Physics.SphereCast(origin, collisionRadius, dir, out var hit, dist, collisionMask, QueryTriggerInteraction.Ignore))
            {
                return origin + dir * Mathf.Max(hit.distance - 0.05f, minDistance * 0.5f);
            }

            return desired;
        }

        private void HandleCursorToggle()
        {
            if (Input.GetKeyDown(toggleCursorLockKey))
            {
                SetCursorLocked(!_cursorLocked);
            }
        }

        private void SetCursorLocked(bool locked)
        {
            _cursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}

