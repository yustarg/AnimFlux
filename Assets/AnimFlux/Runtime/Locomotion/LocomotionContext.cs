using System.Collections.Generic;
using UnityEngine;

namespace AnimFlux.Runtime
{
    /// <summary>
    /// Context passed into blend spaces for locomotion; can be extended as needed.
    /// </summary>
    public struct LocomotionContext : IDirectionalBlendProvider, IFloatBlendProvider, IBlendParameterProvider
    {
        public Vector2 DirectionalBlend;
        public float SpeedNormalized;
        public float ForwardStrafeNormalized;
        public float StrafeDirectionNormalized;
        public float InclineNormalized;
        public bool IsStrafing;
        public float FloatBlend;

        private Dictionary<string, float> _floatParams;
        private Dictionary<string, Vector2> _vectorParams;

        Vector2 IDirectionalBlendProvider.DirectionalBlend => DirectionalBlend;
        float IFloatBlendProvider.FloatBlend => FloatBlend;

        public bool TryGetFloat(string name, out float value)
        {
            value = 0f;
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (_floatParams != null && _floatParams.TryGetValue(name, out value)) return true;
            return false;
        }

        public bool TryGetVector2(string name, out Vector2 value)
        {
            value = Vector2.zero;
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (_vectorParams != null && _vectorParams.TryGetValue(name, out value)) return true;
            return false;
        }

        public void ClearParameters()
        {
            _floatParams ??= new Dictionary<string, float>(8);
            _vectorParams ??= new Dictionary<string, Vector2>(4);
            _floatParams.Clear();
            _vectorParams.Clear();
        }

        public void SetFloat(string name, float value)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            _floatParams ??= new Dictionary<string, float>(8);
            _floatParams[name] = value;
        }

        public void SetVector2(string name, Vector2 value)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            _vectorParams ??= new Dictionary<string, Vector2>(4);
            _vectorParams[name] = value;
        }
    }
}

