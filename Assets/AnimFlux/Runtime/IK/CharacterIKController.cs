using UnityEngine;

namespace AnimFlux.Runtime
{
    public sealed class CharacterIKController
    {
        private Transform _lookAtTarget;
        private float _lookWeight;

        private readonly LimbTarget[] _limbTargets =
        {
            new LimbTarget(), new LimbTarget(), new LimbTarget(), new LimbTarget()
        };

        public void SetLookAtTarget(Transform target, float weight)
        {
            _lookAtTarget = target;
            _lookWeight = Mathf.Clamp01(weight);
        }

        public void ClearLookAt()
        {
            _lookAtTarget = null;
            _lookWeight = 0f;
        }

        public void SetIKTarget(AvatarIKGoal goal, Transform target, float positionWeight, float rotationWeight)
        {
            var index = (int)goal;
            if (index < 0 || index >= _limbTargets.Length) return;

            _limbTargets[index] = new LimbTarget
            {
                Target = target,
                PositionWeight = Mathf.Clamp01(positionWeight),
                RotationWeight = Mathf.Clamp01(rotationWeight)
            };
        }

        public void ClearIKTarget(AvatarIKGoal goal)
        {
            var index = (int)goal;
            if (index < 0 || index >= _limbTargets.Length) return;
            _limbTargets[index] = default;
        }

        public void Apply(Animator animator)
        {
            if (!animator) return;

            if (_lookAtTarget && _lookWeight > 0f)
            {
                animator.SetLookAtWeight(_lookWeight);
                animator.SetLookAtPosition(_lookAtTarget.position);
            }
            else
            {
                animator.SetLookAtWeight(0f);
            }

            for (var i = 0; i < _limbTargets.Length; i++)
            {
                var data = _limbTargets[i];
                var goal = (AvatarIKGoal)i;
                if (data.Target && data.PositionWeight > 0f)
                {
                    animator.SetIKPositionWeight(goal, data.PositionWeight);
                    animator.SetIKPosition(goal, data.Target.position);
                    animator.SetIKRotationWeight(goal, data.RotationWeight);
                    animator.SetIKRotation(goal, data.Target.rotation);
                }
                else
                {
                    animator.SetIKPositionWeight(goal, 0f);
                    animator.SetIKRotationWeight(goal, 0f);
                }
            }
        }

        private struct LimbTarget
        {
            public Transform Target;
            public float PositionWeight;
            public float RotationWeight;
        }
    }
}

