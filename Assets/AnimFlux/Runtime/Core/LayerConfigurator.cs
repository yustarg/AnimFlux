using System.Collections.Generic;

namespace AnimFlux.Runtime
{
    public static class LayerConfigurator
    {
        private static readonly LayerDefinition[] _default =
        {
            new LayerDefinition(AnimationLayerType.Base, "BaseLayer", null, 1f, AFBlendMode.Override),
            new LayerDefinition(AnimationLayerType.UpperBody, "UpperBodyLayer", null, 0f, AFBlendMode.Override),
            new LayerDefinition(AnimationLayerType.Additive, "AdditiveLayer", null, 0f, AFBlendMode.Additive),
            new LayerDefinition(AnimationLayerType.IK, "IKLayer", null, 0f, AFBlendMode.Override)
        };

        public static IReadOnlyList<LayerDefinition> CreateDefault() => _default;
    }
}

