using System;
using UnityEngine.Playables;

namespace AnimFlux
{
    public sealed class AFGraphHost : IDisposable
    {
        public PlayableGraph Graph { get; private set; }
        public bool IsInitialized { get; private set; }
        public string GraphName { get; private set; }

        public AFGraphHost Initialize(string graphName = "AnimFluxGraph")
        {
            if (IsInitialized) return this;

            GraphName = graphName;
            Graph = PlayableGraph.Create(GraphName);
            Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            Graph.Play();
            IsInitialized = true;

            return this;
        }
        
        public void Play() { if (IsInitialized && !Graph.IsPlaying()) Graph.Play(); }
        public void Stop() { if (IsInitialized && Graph.IsPlaying()) Graph.Stop(); }
        public void Evaluate(float dt) { if (IsInitialized) Graph.Evaluate(dt); }
        
        public void Dispose()
        {
            if (!IsInitialized) return;
            if (Graph.IsValid()) Graph.Destroy();
            IsInitialized = false;
        }
    }
}