using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the Tonemapping effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Human Eye/Eyelids")]
    public sealed class Eyelids : VolumeComponent, IPostProcessComponent
    {
        public BoolParameter enable = new BoolParameter(false);

        [Header("Squinting")]
        public ClampedFloatParameter maxSquintMin = new ClampedFloatParameter(0.2f, 0f, 0.5f);
        public ClampedFloatParameter maxSquintMax = new ClampedFloatParameter(0.2f, 0f, 0.5f);
        public ClampedFloatParameter brightnessOpen = new ClampedFloatParameter(13.5f, 0f, 30f);
        public ClampedFloatParameter brightnessClosed = new ClampedFloatParameter(16f, 0f, 30f);

        [Header("Effects")]
        public ClampedFloatParameter maxDarken = new ClampedFloatParameter(0.8f, 0f, 1f);
        public ClampedFloatParameter focus = new ClampedFloatParameter(0.9f, 0.5f, 1f);

        [Header("Overrides")]
        public ClampedFloatParameter forceClose = new ClampedFloatParameter(0f, 0f, 1f);

        /// <summary>
        /// Tells if the effect needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            return enable.value;
        }
    }
}
