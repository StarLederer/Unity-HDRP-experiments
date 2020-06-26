using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;

[Serializable, VolumeComponentMenu("Post-processing/Custom/Bloom")]
public sealed class Bloom : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    [Tooltip("Controls the intensity of the effect.")]
    public ClampedFloatParameter curve = new ClampedFloatParameter(0f, 1f, 32f);
    public ClampedIntParameter steps = new ClampedIntParameter(1, 1, 1000);
    public ClampedFloatParameter scatter = new ClampedFloatParameter(0f, 0f, 800f);

    Material m_Material;
	ComputeShader cs;
	int kernel;
	RenderTexture tex;

	public bool IsActive() => m_Material != null && cs != null && scatter.value > 0f;

    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.BeforePostProcess;

    const string kShaderName = "Hidden/Shader/Bloom";

    public override void Setup()
    {
		// Post processing default shader
        if (Shader.Find(kShaderName) != null)
            m_Material = new Material(Shader.Find(kShaderName));
        else
            Debug.LogError($"Unable to find shader '{kShaderName}'. Post Process Volume Bloom is unable to load.");

		// My workaround to get another pass
		cs = Resources.Load<ComputeShader>("BlurCompute");
		kernel = cs.FindKernel("MyBlurComputeShader");

		if (cs == null)
		{
			Debug.LogError("Could not load the compute shader");
		}

		tex = new RenderTexture(256, 256, 24);
		tex.enableRandomWrite = true;
		tex.Create();
	}

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        if (m_Material == null)
            return;

		// theirs
        m_Material.SetFloat("_Curve", curve.value);
        m_Material.SetInt("_Steps", steps.value);
        m_Material.SetFloat("_Radius", scatter.value);
        m_Material.SetTexture("_InputTexture", source);
        HDUtils.DrawFullScreen(cmd, m_Material, destination);

		// mine
		int w = camera.actualWidth;
		int h = camera.actualHeight;

		cmd.SetComputeTextureParam(cs, kernel, Shader.PropertyToID("_InputTexture"), source);
		cmd.SetComputeTextureParam(cs, kernel, Shader.PropertyToID("_OutputTexture"), destination);

		cmd.DispatchCompute(cs, kernel, (camera.actualWidth + 7) / 8, (camera.actualHeight + 7) / 8, camera.viewCount);
	}

    public override void Cleanup()
    {
        CoreUtils.Destroy(m_Material);
    }
}
