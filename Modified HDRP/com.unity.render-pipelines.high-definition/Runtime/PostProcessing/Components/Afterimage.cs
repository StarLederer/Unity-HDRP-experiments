using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the Tonemapping effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Afterimage")]
    public sealed class Afterimage : VolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
        public ClampedFloatParameter tiringPower = new ClampedFloatParameter(0f, 0f, 1f);
        public ClampedFloatParameter healingPower = new ClampedFloatParameter(0f, 0f, 1f);

        /// <summary>
        /// Tells if the effect needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            return intensity.value > 0f;
        }
    }
}
