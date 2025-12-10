using System.Collections.Generic;
using UnityEngine;

namespace AnimFlux.Runtime
{
    /// <summary>
    /// Shared parameter library for blend trees; designers can define reusable float parameter names
    /// and pick from them in trees.
    /// </summary>
    [CreateAssetMenu(menuName = "AnimFlux/Animation/Parameter Library", fileName = "AnimationParameterLibrary")]
    public sealed class AnimationParameterLibrary : ScriptableObject
    {
        [Header("Float Parameters")]
        public List<string> floatParameters = new();
    }
}

