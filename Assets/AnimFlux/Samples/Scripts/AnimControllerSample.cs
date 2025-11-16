using AnimFlux.Runtime;
using UnityEngine;

namespace AnimFlux.Samples
{
    [RequireComponent(typeof(AnimController))]
    public sealed class AnimControllerSample : MonoBehaviour, IAnimationEventHandler
    {
        [SerializeField] private bool demoUpperBodyClip;
        [SerializeField] private AnimationClip upperBodyClip;
        [SerializeField] private float upperBodyInterval = 4f;

        private AnimController _controller;
        private float _timer;

        private void Awake()
        {
            _controller = GetComponent<AnimController>();
            _controller?.RegisterEventHandler(this);
        }

        private void OnDestroy()
        {
            _controller?.UnregisterEventHandler(this);
        }

        private void Update()
        {
            if (_controller == null || !_controller.IsInitialized) return;

            var oscillatingSpeed = Mathf.PingPong(Time.time, 4f);
            _controller.SetMoveSpeed(oscillatingSpeed);
            _controller.SetIsGrounded(true);

            if (!demoUpperBodyClip || upperBodyClip == null) return;

            _timer += Time.deltaTime;
            if (_timer >= upperBodyInterval)
            {
                _timer = 0f;
                _controller.PlayClip(AnimationLayerType.UpperBody, upperBodyClip, 0f, 0.2f);
            }
        }

        public void OnAnimationEvent(string eventId, float normalizedTime)
        {
            Debug.Log($"[AnimFlux Sample] Event {eventId} at {normalizedTime:0.00}");
        }
    }
}

