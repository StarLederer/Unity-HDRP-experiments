using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;

[Serializable, VolumeComponentMenu("Post-processing/Human eye/Pupillary Scattering")]
public sealed class PupillaryScattering1 : CustomPostProcessVolumeComponent, IPostProcessComponent
{
	// Effect settings
	const string kShaderName = "Hidden/Shader/PupillaryScattering";
	public override CustomPostProcessInjectionPoint injectionPoint => CustomPostProcessInjectionPoint.BeforePostProcess;

	// Constants
	const GraphicsFormat k_ColorFormat = GraphicsFormat.B10G11R11_UFloatPack32;
	const GraphicsFormat k_CoCFormat = GraphicsFormat.R16_SFloat;
	const GraphicsFormat k_ExposureFormat = GraphicsFormat.R32G32_SFloat;
	const int k_RTGuardBandSize = 4; // max guard band size is assumed to be 8 pixels

	// RT pool
	//TargetPool m_Pool;

	// Unity bloom data
	const int k_MaxBloomMipCount = 16;
	readonly RTHandle[] m_BloomMipsDown = new RTHandle[k_MaxBloomMipCount + 1];
	readonly RTHandle[] m_BloomMipsUp = new RTHandle[k_MaxBloomMipCount + 1];
	//RTHandle m_BloomTexture;
	//RTHandle m_HaloTexture;

	// Garbage
	// TODO: Get rid of the material
	Material m_Material;

	#region Parameters

	/// <summary>
	/// Set the level of brightness to filter out pixels under this level. This value is expressed in gamma-space. A value above 0 will disregard energy conservation rules.
	/// </summary>
	[Tooltip("Set the level of brightness to filter out pixels under this level. This value is expressed in gamma-space. A value above 0 will disregard energy conservation rules.")]
	public MinFloatParameter threshold = new MinFloatParameter(0f, 0f);

	/// <summary>
	/// Controls the strength of the bloom filter.
	/// </summary>
	[Tooltip("Controls the strength of the bloom filter.")]
	public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

	/// <summary>
	/// Controls the strength of the bloom filter.
	/// </summary>
	[Tooltip("Controls the strength of the lenticualar halo.")]
	public ClampedFloatParameter lenticularHaloIntensity = new ClampedFloatParameter(0f, 0f, 1f);

	/// <summary>
	/// Controls the extent of the veiling effect.
	/// </summary>
	[Tooltip("Controls the extent of the veiling effect.")]
	public ClampedFloatParameter scatter = new ClampedFloatParameter(0.7f, 0f, 1f);

	/// <summary>
	/// Specifies the tint of the bloom filter.
	/// </summary>
	[Tooltip("Specifies the tint of the bloom filter.")]
	public ColorParameter tint = new ColorParameter(Color.white, false, false, true);

	/// <summary>
	/// Specifies a Texture to add smudges or dust to the bloom effect.
	/// </summary>
	[Tooltip("Specifies a Texture to add smudges or dust to the bloom effect.")]
	public TextureParameter dirtTexture = new TextureParameter(null);

	/// <summary>
	/// Controls the strength of the lens dirt.
	/// </summary>
	[Tooltip("Controls the strength of the lens dirt.")]
	public MinFloatParameter dirtIntensity = new MinFloatParameter(0f, 0f);

	/// <summary>
	/// When enabled, bloom stretches horizontally depending on the current physical Camera's Anamorphism property value.
	/// </summary>
	[Tooltip("When enabled, bloom stretches horizontally depending on the current physical Camera's Anamorphism property value.")]
	public BoolParameter anamorphic = new BoolParameter(true);

	/// <summary>
	/// Specifies the resolution at which HDRP processes the effect.
	/// </summary>
	/// <seealso cref="BloomResolution"/>
	public BloomResolution resolution
	{
		get
		{
			//if (!UsesQualitySettings())
			//{
			return m_Resolution.value;
			//}
			//else
			//{
			//	int qualityLevel = (int)quality.levelAndOverride.level;
			//	return GetPostProcessingQualitySettings().BloomRes[qualityLevel];
			//}
		}
		set { m_Resolution.value = value; }
	}

	/// <summary>
	/// When enabled, bloom uses bicubic sampling instead of bilinear sampling for the upsampling passes.
	/// </summary>
	public bool highQualityFiltering
	{
		get
		{
			//if (!UsesQualitySettings())
			//{
			return m_HighQualityFiltering.value;
			//}
			//else
			//{
			//	int qualityLevel = (int)quality.levelAndOverride.level;
			//	return GetPostProcessingQualitySettings().BloomHighQualityFiltering[qualityLevel];
			//}
		}
		set { m_HighQualityFiltering.value = value; }
	}

	[Tooltip("Specifies the resolution at which HDRP processes the effect. Quarter resolution is less resource intensive but can result in aliasing artifacts.")]
	[SerializeField, FormerlySerializedAs("resolution")]
	private BloomResolutionParameter m_Resolution = new BloomResolutionParameter(BloomResolution.Half);

	[Tooltip("When enabled, bloom uses bicubic sampling instead of bilinear sampling for the upsampling passes.")]
	[SerializeField, FormerlySerializedAs("highQualityFiltering")]
	private BoolParameter m_HighQualityFiltering = new BoolParameter(true);

	#endregion

	#region Effect methods

	public bool IsActive() => m_Material != null && intensity.value > 0f;

	// TODO: Try implementing a constructor and see of it works
	// if it does make m_Pool readonly and initialize here

	public override void Setup()
	{
		// Initialize our target pool to ease RT management
		//m_Pool = new TargetPool();

		// TODO: Try to get rid of this, we don't need to use a material
		// we are using compute shaders instead
		if (Shader.Find(kShaderName) != null)
			m_Material = new Material(Shader.Find(kShaderName));
	}

	public override void Render(CommandBuffer cmd, HDCamera camera, RTHandle source, RTHandle destination)
	{
		// Set target pool dynamic resolution
		//m_Pool.SetHWDynamicResolutionState(camera);

		// resetting halo and bloom textures
		switch (resolution)
		{
			case BloomResolution.Half:
				//m_HaloTexture = m_Pool.Get(new Vector2(0.5f, 0.5f), k_ColorFormat);
				break;
			default:
				//m_HaloTexture = m_Pool.Get(new Vector2(0.25f, 0.25f), k_ColorFormat);
				break;
		}
		//m_BloomTexture = m_Pool.Get(Vector2.one, k_ColorFormat);

		DoBloom(cmd, camera, source, m_Material);

		// Draw with material
		//m_Material.SetTexture("_InputTexture", source);
		//HDUtils.DrawFullScreen(cmd, m_Material, destination);

		// Cleanup
		//m_Pool.Recycle(m_BloomTexture);
		//m_BloomTexture = null;
		//m_Pool.Recycle(m_HaloTexture);
		//m_HaloTexture = null;
	}

	public override void Cleanup()
	{
		//m_Pool.Cleanup();
		// TODO: Get rid of the material
		CoreUtils.Destroy(m_Material);
	}

	#endregion

	#region Unity bloom

	// Yoinked straight out of PostProcessSystem

	// TODO: All of this could be simplified and made faster once we have the ability to bind mips as SRV
	unsafe void DoBloom(CommandBuffer cmd, HDCamera camera, RTHandle source, Material material)
	{
		float scaleW = 1f / ((int)resolution / 2f);
		float scaleH = 1f / ((int)resolution / 2f);

		// If the scene is less than 50% of 900p, then we operate on full res, since it's going to be cheap anyway and this will improve quality in challenging situations.
		// Also we switch to bilinear upsampling as it goes less wide than bicubic and due to our border/RTHandle handling, going wide on small resolution
		// where small mips have a strong influence, might result problematic. 
		if (camera.actualWidth < 800 || camera.actualHeight < 450)
		{
			scaleW = 1.0f;
			scaleH = 1.0f;
			highQualityFiltering = false;
		}

		//if (m_Bloom.anamorphic.value)
		//{
		//	// Positive anamorphic ratio values distort vertically - negative is horizontal
		//	float anamorphism = m_PhysicalCamera.anamorphism * 0.5f;
		//	scaleW *= anamorphism < 0 ? 1f + anamorphism : 1f;
		//	scaleH *= anamorphism > 0 ? 1f - anamorphism : 1f;
		//}

		// Determine the iteration count
		int maxSize = Mathf.Max(camera.actualWidth, camera.actualHeight);
		int iterations = Mathf.FloorToInt(Mathf.Log(maxSize, 2f) - 2 - (resolution == BloomResolution.Half ? 0 : 1));
		int mipCount = Mathf.Clamp(iterations, 1, k_MaxBloomMipCount);
		var mipSizes = stackalloc Vector2Int[mipCount];

		// Thresholding
		// A value of 0 in the UI will keep energy conservation
		const float k_Softness = 0.5f;
		float lthresh = 0;// Mathf.GammaToLinearSpace(m_Bloom.threshold.value);
		float knee = lthresh * k_Softness + 1e-5f;
		var threshold = new Vector4(lthresh, lthresh - knee, knee * 2f, 0.25f / knee);

		// Prepare targets
		// We could have a single texture with mips but because we can't bind individual mips as
		// SRVs right now we have to ping-pong between buffers and make the code more
		// complicated than it should be
		for (int i = 0; i < mipCount; i++)
		{
			float p = 1f / Mathf.Pow(2f, i + 1f);
			float sw = scaleW * p;
			float sh = scaleH * p;
			int pw, ph;
			if (DynamicResolutionHandler.instance.HardwareDynamicResIsEnabled())
			{
				pw = Mathf.Max(1, Mathf.CeilToInt(sw * camera.actualWidth));
				ph = Mathf.Max(1, Mathf.CeilToInt(sh * camera.actualHeight));
			}
			else
			{
				pw = Mathf.Max(1, Mathf.RoundToInt(sw * camera.actualWidth));
				ph = Mathf.Max(1, Mathf.RoundToInt(sh * camera.actualHeight));
			}
			var scale = new Vector2(sw, sh);
			var pixelSize = new Vector2Int(pw, ph);

			mipSizes[i] = pixelSize;
			//m_BloomMipsDown[i] = m_Pool.Get(scale, k_ColorFormat);
			//m_BloomMipsUp[i] = m_Pool.Get(scale, k_ColorFormat);
		}

		// All the computes for this effect use the same group size so let's use a local
		// function to simplify dispatches
		// Make sure the thread group count is sufficient to draw the guard bands
		void DispatchWithGuardBands(ComputeShader shader, int kernelId, in Vector2Int size)
		{
			int w = size.x;
			int h = size.y;

			if (w < source.rt.width && w % 8 < k_RTGuardBandSize)
				w += k_RTGuardBandSize;
			if (h < source.rt.height && h % 8 < k_RTGuardBandSize)
				h += k_RTGuardBandSize;

			cmd.DispatchCompute(shader, kernelId, (w + 7) / 8, (h + 7) / 8, camera.viewCount);
		}

		// Pre-filtering
		ComputeShader cs;
		int kernel;

		{
			var size = mipSizes[0];
			cs = Resources.Load<ComputeShader>("PupillaryScatteringPrefilter");
			kernel = cs.FindKernel("KMain");

			cmd.SetComputeTextureParam(cs, kernel, Shader.PropertyToID("_InputTexture"), source);
			//cmd.SetComputeTextureParam(cs, kernel, Shader.PropertyToID("_OutputTexture"), m_BloomMipsUp[0]); // Use m_BloomMipsUp as temp target
			cmd.SetComputeVectorParam(cs, Shader.PropertyToID("_TexelSize"), new Vector4(size.x, size.y, 1f / size.x, 1f / size.y));
			cmd.SetComputeVectorParam(cs, Shader.PropertyToID("_BloomThreshold"), threshold);
			//DispatchWithGuardBands(cs, kernel, size);

			cs = Resources.Load<ComputeShader>("PupillaryScatteringBlur");
			kernel = cs.FindKernel("KMain"); // Only blur

			//cmd.SetComputeTextureParam(cs, kernel, Shader.PropertyToID("_InputTexture"), m_BloomMipsUp[0]);
			//cmd.SetComputeTextureParam(cs, kernel, Shader.PropertyToID("_OutputTexture"), m_BloomMipsDown[0]);
			cmd.SetComputeVectorParam(cs, Shader.PropertyToID("_TexelSize"), new Vector4(size.x, size.y, 1f / size.x, 1f / size.y));
			//DispatchWithGuardBands(cs, kernel, size);
		}

		// Blur pyramid
		kernel = cs.FindKernel("KMainDownsample");

		for (int i = 0; i < mipCount - 1; i++)
		{
			//var src = m_BloomMipsDown[i];
			//var dst = m_BloomMipsDown[i + 1];
			var size = mipSizes[i + 1];

			//cmd.SetComputeTextureParam(cs, kernel, Shader.PropertyToID("_InputTexture"), src);
			//cmd.SetComputeTextureParam(cs, kernel, Shader.PropertyToID("_OutputTexture"), dst);
			cmd.SetComputeVectorParam(cs, Shader.PropertyToID("_TexelSize"), new Vector4(size.x, size.y, 1f / size.x, 1f / size.y));
			//DispatchWithGuardBands(cs, kernel, size);
		}

		// Upsample & combine
		cs = Resources.Load<ComputeShader>("PupillaryScatteringUpsample");
		kernel = cs.FindKernel(highQualityFiltering ? "KMainHighQ" : "KMainLowQ");

		float transformedScatter = Mathf.Lerp(0.05f, 0.95f, scatter.value); //scatter
		var highSize = Vector2Int.one;
		var lowSize = Vector2Int.one;

		for (int i = mipCount - 2; i >= 0; i--)
		{
			//var low = (i == mipCount - 2) ? m_BloomMipsDown : m_BloomMipsUp;
			//var srcLow = low[i + 1];
			//var srcHigh = m_BloomMipsDown[i];
			//var dst = m_BloomMipsUp[i];
			highSize = mipSizes[i];
			lowSize = mipSizes[i + 1];

			//cmd.SetComputeTextureParam(cs, kernel, Shader.PropertyToID("_InputLowTexture"), srcLow);
			//cmd.SetComputeTextureParam(cs, kernel, Shader.PropertyToID("_InputHighTexture"), srcHigh);
			//cmd.SetComputeTextureParam(cs, kernel, Shader.PropertyToID("_OutputTexture"), dst);
			cmd.SetComputeVectorParam(cs, Shader.PropertyToID("_Params"), new Vector4(transformedScatter, 0f, 0f, 0f));
			cmd.SetComputeVectorParam(cs, Shader.PropertyToID("_BloomBicubicParams"), new Vector4(lowSize.x, lowSize.y, 1f / lowSize.x, 1f / lowSize.y));
			cmd.SetComputeVectorParam(cs, Shader.PropertyToID("_TexelSize"), new Vector4(highSize.x, highSize.y, 1f / highSize.x, 1f / highSize.y));
			DispatchWithGuardBands(cs, kernel, highSize);
		}

		// Lenticular halo
		ComputeShader BloomCompute = Resources.Load<ComputeShader>("EyeLensScatteringCompute");
		int bloomKernel = BloomCompute.FindKernel("RainbowBloom");
		cmd.SetComputeFloatParam(BloomCompute, Shader.PropertyToID("_Radius"), 80f);
		cmd.SetComputeFloatParam(BloomCompute, Shader.PropertyToID("_Thickness"), 50f);
		cmd.SetComputeIntParam(BloomCompute, Shader.PropertyToID("_BladeCount"), 54);
		cmd.SetComputeTextureParam(BloomCompute, bloomKernel, Shader.PropertyToID("_InputTexture"), m_BloomMipsUp[0]);
		//cmd.SetComputeTextureParam(BloomCompute, bloomKernel, Shader.PropertyToID("_OutputTexture"), m_HaloTexture);
		//DispatchWithGuardBands(BloomCompute, bloomKernel, highSize);

		var bloomSize = mipSizes[0];
		//m_BloomTexture = m_BloomMipsUp[0];


		lowSize *= (int)resolution;
		highSize *= (int)resolution;

		// Upscaling
		kernel = cs.FindKernel("Last");
		cmd.SetComputeTextureParam(cs, kernel, Shader.PropertyToID("_InputLowTexture"), m_BloomMipsUp[0]);
		//cmd.SetComputeTextureParam(cs, kernel, Shader.PropertyToID("_InputHaloTexture"), m_HaloTexture);
		//cmd.SetComputeTextureParam(cs, kernel, Shader.PropertyToID("_OutputTexture"), m_BloomTexture);
		cmd.SetComputeFloatParam(cs, Shader.PropertyToID("_LenticularHaloIntensity"), lenticularHaloIntensity.value);
		cmd.SetComputeIntParam(cs, Shader.PropertyToID("_LenticularHaloBladeCount"), 54);
		cmd.SetComputeVectorParam(cs, Shader.PropertyToID("_BloomBicubicParams"), new Vector4(lowSize.x, lowSize.y, 1f / lowSize.x, 1f / lowSize.y));
		cmd.SetComputeVectorParam(cs, Shader.PropertyToID("_TexelSize"), new Vector4(highSize.x, highSize.y, 1f / highSize.x, 1f / highSize.y));
		//DispatchWithGuardBands(cs, kernel, highSize);

		float transformedIntensity = Mathf.Pow(2f, intensity.value) - 1f; // Makes intensity easier to control
		transformedIntensity += 0.02f * ((Mathf.Sin(Time.time * 6) + Mathf.Sin(Time.time * 12f) + Mathf.Sin(Time.time * 23f)) / 3f);
		var transformedTint = tint.value.linear;
		var luma = ColorUtils.Luminance(transformedTint);
		transformedTint = luma > 0f ? transformedTint * (1f / luma) : Color.white;

		// Lens dirtiness
		// Keep the aspect ratio correct & center the dirt texture, we don't want it to be
		// stretched or squashed
		/*
		var dirtTexture = m_Bloom.dirtTexture.value == null ? Texture2D.blackTexture : m_Bloom.dirtTexture.value;
		int dirtEnabled = m_Bloom.dirtTexture.value != null && m_Bloom.dirtIntensity.value > 0f ? 1 : 0;
		float dirtRatio = (float)dirtTexture.width / (float)dirtTexture.height;
		float screenRatio = (float)camera.actualWidth / (float)camera.actualHeight;
		var dirtTileOffset = new Vector4(1f, 1f, 0f, 0f);
		float dirtIntensity = m_Bloom.dirtIntensity.value * transformedIntensity;

		if (dirtRatio > screenRatio)
		{
			dirtTileOffset.x = screenRatio / dirtRatio;
			dirtTileOffset.z = (1f - dirtTileOffset.x) * 0.5f;
		}
		else if (screenRatio > dirtRatio)
		{
			dirtTileOffset.y = dirtRatio / screenRatio;
			dirtTileOffset.w = (1f - dirtTileOffset.y) * 0.5f;
		}*/

		// Set uber data
		//m_Material.SetTexture("_BloomTexture", m_BloomTexture);
		//m_Material.SetTexture("_BloomDirtTexture", dirtTexture);
		m_Material.SetFloat("_Intensity", transformedIntensity);
		//m_Material.SetVector("_BloomParams", new Vector4(transformedIntensity, dirtIntensity, 1f, dirtEnabled));
		//m_Material.SetVector("_BloomTint", (Vector4)tint);
		m_Material.SetVector("_RTHandleScale", new Vector2(2, 2));
		m_Material.SetVector("_BloomBicubicParams", new Vector4(bloomSize.x, bloomSize.y, 1f / bloomSize.x, 1f / bloomSize.y));
		//m_Material.SetVector("_BloomDirtScaleOffset", dirtTileOffset);
		m_Material.SetVector("_BloomThreshold", threshold);

		//cmd.SetComputeTextureParam(uberCS, uberKernel, Shader.PropertyToID("_BloomTexture"), m_BloomTexture);
		//cmd.SetComputeTextureParam(uberCS, uberKernel, Shader.PropertyToID("_BloomDirtTexture"), dirtTexture);
		//cmd.SetComputeVectorParam(uberCS, Shader.PropertyToID("_BloomParams"), new Vector4(transformedIntensity, dirtIntensity, 1f, dirtEnabled));
		//cmd.SetComputeVectorParam(uberCS, Shader.PropertyToID("_BloomTint"), (Vector4)tint);
		//cmd.SetComputeVectorParam(uberCS, Shader.PropertyToID("_BloomBicubicParams"), new Vector4(bloomSize.x, bloomSize.y, 1f / bloomSize.x, 1f / bloomSize.y));
		//cmd.SetComputeVectorParam(uberCS, Shader.PropertyToID("_BloomDirtScaleOffset"), dirtTileOffset);
		//cmd.SetComputeVectorParam(uberCS, Shader.PropertyToID("_BloomThreshold"), threshold);

		// Cleanup
		for (int i = 0; i < mipCount; i++)
		{
			//m_Pool.Recycle(m_BloomMipsDown[i]);
			//if (i > 0) m_Pool.Recycle(m_BloomMipsUp[i]);
		}
	}

	#endregion

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

			Debug.Log("Could not get an RT, creating a new one witht scale " + scaleFactor.x);
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

			Debug.Log("Recycled an RT with scale " + rt.scaleFactor.x);
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
