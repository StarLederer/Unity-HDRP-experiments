using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[Serializable, VolumeComponentMenu("Post-processing/Human eye/Eye lens scattering")]
public sealed class EyeLensScattering : CustomPostProcessVolumeComponent, IPostProcessComponent
{
	[Header("Scattering")]
	public ClampedFloatParameter radius = new ClampedFloatParameter(64f, 0f, 512f);
	public ClampedFloatParameter thicnkess = new ClampedFloatParameter(30f, 1f, 300f);
	public ClampedIntParameter bladeCount = new ClampedIntParameter(20, 2, 100);
	[Header("Effect")]
	public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

	Material m_Material;
	ComputeShader BloomCompute;
	int clearKernel, bloomKernel;
	RTHandle buffer;
	TargetPool m_Pool;

	public bool IsActive() => m_Material != null && intensity.value > 0f;

	public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.BeforePostProcess;

	const string kShaderName = "Hidden/Shader/EyeLensScattering";

	public override void Setup()
	{
		// Render texture factory
		m_Pool = new TargetPool();

		// Compute shader
		BloomCompute = Resources.Load<ComputeShader>("EyeLensScatteringCompute");
		clearKernel = BloomCompute.FindKernel("Clear");
		bloomKernel = BloomCompute.FindKernel("RainbowBloom");

		if (Shader.Find(kShaderName) != null)
			m_Material = new Material(Shader.Find(kShaderName));
		else
			Debug.LogError($"Unable to find shader '{kShaderName}'. Post Process Volume EyeLensScattering is unable to load.");
	}

	public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
	{
		if (m_Material == null)
			return;

		// Clear the scatter buffer
		buffer = m_Pool.Get(Vector2.one, GraphicsFormat.B10G11R11_UFloatPack32);
		cmd.SetComputeTextureParam(BloomCompute, clearKernel, Shader.PropertyToID("_OutputTexture"), buffer);
		cmd.DispatchCompute(BloomCompute, clearKernel, (camera.actualWidth + 7) / 8, (camera.actualHeight + 7) / 8, camera.viewCount);

		// Haloes
		cmd.SetComputeFloatParam(BloomCompute, Shader.PropertyToID("_Radius"), radius.value);
		cmd.SetComputeFloatParam(BloomCompute, Shader.PropertyToID("_Thickness"), thicnkess.value);
		cmd.SetComputeIntParam(BloomCompute, Shader.PropertyToID("_BladeCount"), bladeCount.value);
		cmd.SetComputeTextureParam(BloomCompute, bloomKernel, Shader.PropertyToID("_InputTexture"), source);
		cmd.SetComputeTextureParam(BloomCompute, bloomKernel, Shader.PropertyToID("_OutputTexture"), buffer);
		cmd.DispatchCompute(BloomCompute, bloomKernel, (camera.actualWidth + 7) / 8, (camera.actualHeight + 7) / 8, camera.viewCount);

		// Combine
		m_Material.SetFloat("_Intensity", intensity.value);
		m_Material.SetInt("_BladeCount", bladeCount.value);
		m_Material.SetTexture("_InputTexture", source);
		m_Material.SetTexture("_BloomTexture", buffer);
		HDUtils.DrawFullScreen(cmd, m_Material, destination);

		// Cleanup
		m_Pool.Recycle(buffer);
		buffer = null;
	}

	public override void Cleanup()
	{
		CoreUtils.Destroy(m_Material);
		m_Pool.Cleanup();
	}

	#region Render Target Management Utilities

	// Yoinked straight out of PostProcessSystem

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
			var hashCode = ComputeHashCode(scaleFactor.x, scaleFactor.y, (int)format, mipmap);

			if (m_Targets.TryGetValue(hashCode, out var stack) && stack.Count > 0)
				return stack.Pop();

			var rt = RTHandles.Alloc(
				scaleFactor, TextureXR.slices, DepthBits.None, colorFormat: format, dimension: TextureXR.dimension,
				useMipMap: mipmap, enableRandomWrite: true, useDynamicScale: true, name: "Post-processing Target Pool " + m_Tracker
			);

			m_Tracker++;
			return rt;
		}

		public void Recycle(RTHandle rt)
		{
			Assert.IsNotNull(rt);
			var hashCode = ComputeHashCode(rt.scaleFactor.x, rt.scaleFactor.y, (int)rt.rt.graphicsFormat, rt.rt.useMipMap);

			if (!m_Targets.TryGetValue(hashCode, out var stack))
			{
				stack = new Stack<RTHandle>();
				m_Targets.Add(hashCode, stack);
			}

			stack.Push(rt);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		int ComputeHashCode(float scaleX, float scaleY, int format, bool mipmap)
		{
			int hashCode = 17;

			unchecked
			{
				unsafe
				{
					hashCode = hashCode * 23 + *((int*)&scaleX);
					hashCode = hashCode * 23 + *((int*)&scaleY);
				}

				hashCode = hashCode * 23 + format;
				hashCode = hashCode * 23 + (mipmap ? 1 : 0);
			}

			return hashCode;
		}
	}

	#endregion
}
