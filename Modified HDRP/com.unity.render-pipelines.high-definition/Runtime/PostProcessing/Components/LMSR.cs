using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the Tonemapping effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/LMSR")]
    public sealed class LMSR : VolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter edge1 = new ClampedFloatParameter(0f, 0f, 1f);
        public ClampedFloatParameter edge2 = new ClampedFloatParameter(0f, 0f, 1f);
        public ClampedFloatParameter hueShift = new ClampedFloatParameter(0.5f, 0f, 1f);
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        /// <summary>
        /// Tells if the effect needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            return edge1.value != edge2.value;
        }
    }
}
