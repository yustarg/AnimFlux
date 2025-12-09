using System;
using UnityEngine;
using UnityEngine.Playables;

namespace AnimFlux.Runtime
{
    public interface IAnimationBlendSource
    {
        IAnimationBlendRuntime CreateRuntime(PlayableGraph graph);
    }

    public interface IAnimationBlendRuntime : IDisposable
    {
        Playable Playable { get; }
        Type ContextType { get; }
        void Evaluate(object context);
    }

    public interface IDirectionalBlendProvider
    {
        Vector2 DirectionalBlend { get; }
    }

    public interface IFloatBlendProvider
    {
        float FloatBlend { get; }
    }

    public interface IBlendParameterProvider
    {
        bool TryGetFloat(string name, out float value);
        bool TryGetVector2(string name, out Vector2 value);
    }
}

