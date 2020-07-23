using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[Serializable, VolumeComponentMenu("Post-processing/Custom/Bloom")]
public sealed class Bloom : CustomPostProcessVolumeComponent, IPostProcessComponent
{
    [Tooltip("Controls the intensity of the effect.")]
	[Header("Scattering")]
    public ClampedIntParameter steps = new ClampedIntParameter(1, 1, 128); // the noise hash starts to show the pattern if set heigher than around 128
    public ClampedFloatParameter scatter = new ClampedFloatParameter(0f, 0f, 800f);
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
	[Header("Blurring low")]
	public ClampedIntParameter blurStepsLow = new ClampedIntParameter(1, 1, 16);
	public ClampedFloatParameter blurStepSizeLow = new ClampedFloatParameter(1f, 1f, 4f);
	[Header("Blurring high")]
	public ClampedIntParameter blurStepsHigh = new ClampedIntParameter(1, 1, 16);
	public ClampedFloatParameter blurStepSizeHigh = new ClampedFloatParameter(1f, 1f, 4f);

	private int downsample = 8;
	private Vector4 texelSize;

	Material m_Material;
	ComputeShader ScatterCompute;
	ComputeShader BlurCompute;
	int scatterKernel, clearKernel, upsampleKernel, blurKernel, hBlurKernel, vBlurKernel;
	TargetPool m_Pool;

	RTHandle scatterBuffer;
	RTHandle bloomBuffer;
	RTHandle upsampleBuffer;
	RTHandle blurBuffer;

	public bool IsActive() => intensity.value > 0f;

    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.BeforePostProcess;

    const string kShaderName = "Hidden/Shader/Bloom";

	public override void Setup()
    {
		m_Pool = new TargetPool();

		texelSize = new Vector4(downsample, downsample, 1f / downsample, 1f / downsample);

		// Post processing default shader
		if (Shader.Find(kShaderName) != null)
            m_Material = new Material(Shader.Find(kShaderName));
        else
            Debug.LogError($"Unable to find shader '{kShaderName}'. Post Process Volume Bloom is unable to load.");

		// My workaround to get more passes
		ScatterCompute = Resources.Load<ComputeShader>("ScatterCompute");
		BlurCompute = Resources.Load<ComputeShader>("BlurCompute");

		//clearKernel = ScatterCompute.FindKernel("Clear");
		scatterKernel = ScatterCompute.FindKernel("OldScatter");
		upsampleKernel = BlurCompute.FindKernel("UpsampleUnity");
		//blurKernel = BlurCompute.FindKernel("BlurUnity");
		hBlurKernel = BlurCompute.FindKernel("HBlur");
		vBlurKernel = BlurCompute.FindKernel("VBlur");

		scatterBuffer = m_Pool.Get(new Vector2(texelSize.z, texelSize.w), GraphicsFormat.B10G11R11_UFloatPack32);
		bloomBuffer = m_Pool.Get(Vector2.one, GraphicsFormat.B10G11R11_UFloatPack32);
		upsampleBuffer = m_Pool.Get(Vector2.one, GraphicsFormat.B10G11R11_UFloatPack32);
		//blurBuffer = m_Pool.Get(Vector2.one, GraphicsFormat.B10G11R11_UFloatPack32);

		if (ScatterCompute == null)
		{
			Debug.LogError("Could not load the compute shader");
		}
	}

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        if (m_Material == null)
            return;

		m_Pool.SetHWDynamicResolutionState(camera);

		// Clear the scatter buffer
		// Only necessary if the weird inverse scatter is used
		//cmd.SetComputeVectorParam(ScatterCompute, Shader.PropertyToID("_TexelSize"), texelSize);
		//cmd.SetComputeTextureParam(ScatterCompute, clearKernel, Shader.PropertyToID("_InputTexture"), source);
		//cmd.SetComputeTextureParam(ScatterCompute, clearKernel, Shader.PropertyToID("_OutputTexture"), scatterBuffer);
		//cmd.DispatchCompute(ScatterCompute, clearKernel, (int)Math.Floor(camera.actualWidth * texelSize.z + 7) / 8, (int)(camera.actualHeight * texelSize.w + 7) / 8, camera.viewCount);

		// Scatter and save to scatter buffer
		cmd.SetComputeFloatParam(ScatterCompute, Shader.PropertyToID("_Time"), Time.time);
		cmd.SetComputeIntParam(ScatterCompute, Shader.PropertyToID("_Steps"), steps.value);
		cmd.SetComputeFloatParam(ScatterCompute, Shader.PropertyToID("_Radius"), scatter.value * texelSize.z);
		cmd.SetComputeVectorParam(ScatterCompute, Shader.PropertyToID("_TexelSize"), texelSize);
		cmd.SetComputeTextureParam(ScatterCompute, scatterKernel, Shader.PropertyToID("_InputTexture"), source);
		cmd.SetComputeTextureParam(ScatterCompute, scatterKernel, Shader.PropertyToID("_OutputTexture"), scatterBuffer);
		cmd.DispatchCompute(ScatterCompute, scatterKernel, (int) Math.Floor(camera.actualWidth * texelSize.z + 7) / 8, (int) (camera.actualHeight * texelSize.w + 7) / 8, camera.viewCount);

		// Blur the scatter buffer before upsampling
		cmd.SetComputeVectorParam(BlurCompute, Shader.PropertyToID("_TexelSize"), texelSize);
		cmd.SetComputeIntParam(BlurCompute, Shader.PropertyToID("_BlurSteps"), blurStepsLow.value);
		cmd.SetComputeFloatParam(BlurCompute, Shader.PropertyToID("_BlurStepSize"), blurStepSizeLow.value);

		cmd.SetComputeTextureParam(BlurCompute, hBlurKernel, Shader.PropertyToID("_InputTexture"), scatterBuffer);
		cmd.SetComputeTextureParam(BlurCompute, hBlurKernel, Shader.PropertyToID("_OutputTexture"), bloomBuffer);
		cmd.DispatchCompute(BlurCompute, hBlurKernel, (int)Math.Floor(camera.actualWidth * texelSize.z + 7) / 8, (int)(camera.actualHeight * texelSize.w + 7) / 8, camera.viewCount);

		cmd.SetComputeTextureParam(BlurCompute, vBlurKernel, Shader.PropertyToID("_InputTexture"), bloomBuffer);
		cmd.SetComputeTextureParam(BlurCompute, vBlurKernel, Shader.PropertyToID("_OutputTexture"), bloomBuffer);
		cmd.DispatchCompute(BlurCompute, vBlurKernel, (int)Math.Floor(camera.actualWidth * texelSize.z + 7) / 8, (int)(camera.actualHeight * texelSize.w + 7) / 8, camera.viewCount);

		// Upsample the scatter buffer
		cmd.SetComputeVectorParam(BlurCompute, Shader.PropertyToID("_TexelSize"), texelSize);
		cmd.SetComputeTextureParam(BlurCompute, upsampleKernel, Shader.PropertyToID("_InputTexture"), bloomBuffer);
		cmd.SetComputeTextureParam(BlurCompute, upsampleKernel, Shader.PropertyToID("_OutputTexture"), upsampleBuffer);
		cmd.SetComputeVectorParam(BlurCompute, Shader.PropertyToID("_BloomBicubicParams"), texelSize);
		cmd.DispatchCompute(BlurCompute, upsampleKernel, (camera.actualWidth + 7) / 8, (camera.actualHeight + 7) / 8, camera.viewCount);

		// Blur the scatter buffer after upsampling
		cmd.SetComputeVectorParam(BlurCompute, Shader.PropertyToID("_TexelSize"), new Vector4(1, 1, 1, 1));
		cmd.SetComputeIntParam(BlurCompute, Shader.PropertyToID("_BlurSteps"), blurStepsHigh.value);
		cmd.SetComputeFloatParam(BlurCompute, Shader.PropertyToID("_BlurStepSize"), blurStepSizeHigh.value);
		
		cmd.SetComputeTextureParam(BlurCompute, hBlurKernel, Shader.PropertyToID("_InputTexture"), upsampleBuffer);
		cmd.SetComputeTextureParam(BlurCompute, hBlurKernel, Shader.PropertyToID("_OutputTexture"), bloomBuffer);
		cmd.DispatchCompute(BlurCompute, hBlurKernel, (camera.actualWidth + 7) / 8, (camera.actualHeight + 7) / 8, camera.viewCount);

		cmd.SetComputeTextureParam(BlurCompute, vBlurKernel, Shader.PropertyToID("_InputTexture"), bloomBuffer);
		cmd.SetComputeTextureParam(BlurCompute, vBlurKernel, Shader.PropertyToID("_OutputTexture"), bloomBuffer);
		cmd.DispatchCompute(BlurCompute, vBlurKernel, (camera.actualWidth + 7) / 8, (camera.actualHeight + 7) / 8, camera.viewCount);

		// Combine
		m_Material.SetTexture("_InputTexture", source);
		m_Material.SetTexture("_BloomTexture", bloomBuffer);
		m_Material.SetFloat("_Intensity", intensity.value);
		HDUtils.DrawFullScreen(cmd, m_Material, destination);
	}

	public override void Cleanup()
    {
		m_Pool.Cleanup();
		CoreUtils.Destroy(m_Material);
    }

	#region Render Target Management Utility

	// Quick utility class to manage temporary render targets for post-processing and keep the
	// code readable.
	class TargetPool
	{
		readonly Dictionary<int, Stack<RTHandle>> m_Targets;
		int m_Tracker;
		bool m_HasHWDynamicResolution;

		public TargetPool()
		{
			m_Targets = new Dictionary<int, Stack<RTHandle>>();
			m_Tracker = 0;
			m_HasHWDynamicResolution = false;
		}

		public void Cleanup()
		{
			foreach (var kvp in m_Targets)
			{
				var stack = kvp.Value;

				if (stack == null)
					continue;

				while (stack.Count > 0)
					RTHandles.Release(stack.Pop());
			}

			m_Targets.Clear();
		}

		public void SetHWDynamicResolutionState(HDCamera camera)
		{
			bool needsHW = DynamicResolutionHandler.instance.HardwareDynamicResIsEnabled();
			if (needsHW && !m_HasHWDynamicResolution)
			{
				// If any target has no dynamic resolution enabled, but we require it, we need to cleanup the pool.
				bool missDynamicScale = false;
				foreach (var kvp in m_Targets)
				{
					var stack = kvp.Value;

					if (stack == null)
						continue;

					// We found a RT with no dynamic scale
					if (stack.Count > 0 && !stack.Peek().rt.useDynamicScale)
					{
						missDynamicScale = true;
						break;
					}
				}

				if (missDynamicScale)
				{
					m_HasHWDynamicResolution = needsHW;
					Cleanup();
				}
			}
		}

		public RTHandle Get(in Vector2 scaleFactor, GraphicsFormat format, bool mipmap = false)
		{
			var rt = RTHandles.Alloc(
				scaleFactor, TextureXR.slices, DepthBits.None, colorFormat: format, dimension: TextureXR.dimension,
				useMipMap: mipmap, enableRandomWrite: true, useDynamicScale: true, name: "Post-processing Target Pool " + m_Tracker
			);

			m_Tracker++;
			return rt;
		}
	}

	#endregion
}
