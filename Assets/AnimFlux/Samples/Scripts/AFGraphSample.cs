using AnimFlux.Runtime;
using UnityEngine;

namespace AnimFlux.Samples
{
    public class AFGraphSample : MonoBehaviour
    {
        public Animator animator;
        [Header("Base Layer Clips")]
        public AnimationClip clipA;
        public AnimationClip clipB;

        [Header("Upper Body Layer")]
        public AvatarMask upperBodyMask;
        public AnimationClip upperBodyClip;
        public float upperBodyTriggerInterval = 4f;
        public float upperBodyActiveDuration = 1.5f;
        public float upperBodyFadeDuration = 0.25f;
        
        private AFGraph _graph;
        private int _baseLayer;
        private int _upperBodyLayer;
        private float _baseTimer;
        private float _upperBodyTimer;
        private float _upperBodyActiveTime;
        private bool _playingA = true;
        private bool _upperBodyPlaying;

        private void Start()
        {
            if (!animator) animator = GetComponent<Animator>();
            _graph = AFGraph.Create(animator);
            _baseLayer = _graph.AddLayer("BaseLayer");
            _upperBodyLayer = _graph.AddLayer("UpperBodyLayer", upperBodyMask, 0f, AFBlendMode.Override);
            if (clipA)
                _graph.PlayClip(_baseLayer, clipA, 0f, 0f);
        }

        private void Update()
        {
            if (clipA && clipB)
            {
                _baseTimer += Time.deltaTime;
                if (_baseTimer >= 3f)
                {
                    _baseTimer = 0f;
                    _playingA = !_playingA;
                    var target = _playingA ? clipA : clipB;
                    _graph.PlayClip(_baseLayer, target, 0f, 0.2f);
                }
            }

            UpdateUpperBodyLayer();
        }

        private void UpdateUpperBodyLayer()
        {
            if (!upperBodyClip) return;
            _upperBodyTimer += Time.deltaTime;
            if (_upperBodyTimer >= upperBodyTriggerInterval)
            {
                _upperBodyTimer = 0f;
                _upperBodyPlaying = true;
                _upperBodyActiveTime = 0f;
                _graph.SetLayerWeightSmooth(_upperBodyLayer, 1f, upperBodyFadeDuration);
                _graph.PlayClip(_upperBodyLayer, upperBodyClip, 0f, 0.1f);
            }

            if (_upperBodyPlaying)
            {
                _upperBodyActiveTime += Time.deltaTime;
                if (_upperBodyActiveTime >= upperBodyActiveDuration)
                {
                    _graph.SetLayerWeightSmooth(_upperBodyLayer, 0f, upperBodyFadeDuration);
                    _upperBodyPlaying = false;
                }
            }
        }

        private void OnDestroy()
        {
            _graph?.Dispose();
        }
    }
}