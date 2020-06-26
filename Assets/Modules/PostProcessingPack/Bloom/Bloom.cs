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
    public ClampedFloatParameter curve = new ClampedFloatParameter(0f, 1f, 32f);
    public ClampedIntParameter steps = new ClampedIntParameter(1, 1, 1000);
    public ClampedFloatParameter scatter = new ClampedFloatParameter(0f, 0f, 800f);

    Material m_Material;
	ComputeShader cs;
	int kernel, kernelt;
	TargetPool m_Pool;

	public bool IsActive() => m_Material != null && cs != null && scatter.value > 0f;

    public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.BeforePostProcess;

    const string kShaderName = "Hidden/Shader/Bloom";

	public override void Setup()
    {
		m_Pool = new TargetPool();

		// Post processing default shader
		if (Shader.Find(kShaderName) != null)
            m_Material = new Material(Shader.Find(kShaderName));
        else
            Debug.LogError($"Unable to find shader '{kShaderName}'. Post Process Volume Bloom is unable to load.");

		// My workaround to get another pass
		cs = Resources.Load<ComputeShader>("BlurCompute");
		kernel = cs.FindKernel("MyBlurComputeShader");
		kernelt = cs.FindKernel("FooBar");

		if (cs == null)
		{
			Debug.LogError("Could not load the compute shader");
		}
	}

    public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
    {
        if (m_Material == null)
            return;

		RTHandle buf = m_Pool.Get(Vector2.one, GraphicsFormat.B10G11R11_UFloatPack32);

		// theirs
		//m_Material.SetFloat("_Curve", curve.value);
		//m_Material.SetInt("_Steps", steps.value);
		//m_Material.SetFloat("_Radius", scatter.value);
		//m_Material.SetTexture("_InputTexture", source);
		//HDUtils.DrawFullScreen(cmd, m_Material, buf);

		// mine
		//var buff = m_Pool.Get(Vector2.one, GraphicsFormat.B10G11R11_UFloatPack32);

		m_Pool.SetHWDynamicResolutionState(camera);
		cmd.SetComputeTextureParam(cs, kernel, Shader.PropertyToID("_InputTexture"), source);
		cmd.SetComputeTextureParam(cs, kernel, Shader.PropertyToID("_OutputTexture"), buf);
		cmd.DispatchCompute(cs, kernel, (camera.actualWidth + 7) / 8, (camera.actualHeight + 7) / 8, camera.viewCount);

		cmd.SetComputeTextureParam(cs, kernelt, Shader.PropertyToID("_InputTexture"), buf);
		cmd.SetComputeTextureParam(cs, kernelt, Shader.PropertyToID("_OutputTexture"), destination);
		cmd.DispatchCompute(cs, kernelt, (camera.actualWidth + 7) / 8, (camera.actualHeight + 7) / 8, camera.viewCount);
	}

    public override void Cleanup()
    {
		m_Pool.Cleanup();
		CoreUtils.Destroy(m_Material);
    }

	#region Render Target Management Utilities

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
