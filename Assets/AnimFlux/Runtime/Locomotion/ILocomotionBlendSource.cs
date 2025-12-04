using System;
using UnityEngine;
using UnityEngine.Playables;

namespace AnimFlux.Runtime
{
    /// <summary>
    /// Asset-level abstraction that knows how to create a playable source for locomotion blend trees.
    /// </summary>
    public interface ILocomotionBlendSource
    {
        /// <summary>
        /// Creates a runtime instance for this source. Returns null if instantiation fails.
        /// </summary>
        ILocomotionBlendRuntime CreateRuntime(PlayableGraph graph);
    }

    /// <summary>
    /// Runtime companion returned by <see cref="ILocomotionBlendSource"/>.
    /// </summary>
    public interface ILocomotionBlendRuntime : IDisposable
    {
        Playable Playable { get; }
        void Evaluate(in LocomotionBlendParameters parameters);
    }
}

