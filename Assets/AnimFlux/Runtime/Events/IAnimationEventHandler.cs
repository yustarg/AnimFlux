namespace AnimFlux.Runtime
{
    public interface IAnimationEventHandler
    {
        void OnAnimationEvent(string eventId, float normalizedTime);
    }
}

