using AnimFlux.Runtime;
using UnityEngine;

namespace AnimFlux.Samples
{
    public class AFGraphSample : MonoBehaviour
    {
        public Animator animator;
        public AnimationClip clipA;
        public AnimationClip clipB;
        private AFGraph _graph;
        private int _layer;
        private float _timer;

        private void Start()
        {
            if (!animator) animator = GetComponent<Animator>();
            _graph = AFGraph.Create(animator);
            _layer = _graph.AddLayer("BaseLayer");
            if (clipA)
                _graph.PlayClip(_layer, clipA, 0f, 0f);
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            // Alternate between clipA and clipB every 3 seconds.
            if (_timer > 3f)
            {
                _timer = 0f;
                if (clipA && clipB)
                {
                    if (UnityEngine.Random.value > 0.5f)
                        _graph.PlayClip(_layer, clipA, 0f, 0.2f);
                    else
                        _graph.PlayClip(_layer, clipB, 0f, 0.2f);
                }
            }
        }

        private void OnDestroy()
        {
            _graph?.Dispose();
        }
    }
}