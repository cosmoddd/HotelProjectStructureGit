using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

[AddComponentMenu("UBER/Deferred Translucency Params")]
[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class UBER_DeferredTranslucencyParams : MonoBehaviour {
	public Color TranslucencyColor1=new Color(1,1,1,1);
	public Color TranslucencyColor2=new Color(1,1,1,1);
	public Color TranslucencyColor3=new Color(1,1,1,1);
	public Color TranslucencyColor4=new Color(1,1,1,1);
	[Tooltip("You can control strength per light using its color alpha")]
	public float Strength=4;
	[Range(0.0f, 1.0f)] public float PointLightsDirectionality=0.7f;
	[Range(0.0f, 0.5f)] public float Constant=0.1f;
	[Range(0.0f, 0.3f)] public float Scattering=0.05f;
	[Range(2.0f, 100f)] public float SpotExponent=30f;
	[Range(0.0f, 20f)] public float SuppressShadows=0.5f;

	private Camera mycam;
	private CommandBuffer[] combufsPreLight;
	private CommandBuffer combufPreLight;
	private CommandBuffer[] combufsPostLight;
	private CommandBuffer combufPostLight;
	public Material DeferredTranslucencyBlit;

#if UNITY_EDITOR
	void Update() {
		SetupTranslucencyValues();
		if (combufPreLight == null) {
			Initialize();
			RefreshComBufs();
		}
	}
#endif

	void Start() {
		SetupTranslucencyValues();
		Initialize();
		RefreshComBufs();
	}

    public void SetupTranslucencyValues() {
		Shader.SetGlobalColor("_TranslucencyColor", TranslucencyColor1);
		Shader.SetGlobalColor("_TranslucencyColor2", TranslucencyColor2);
		Shader.SetGlobalColor("_TranslucencyColor3", TranslucencyColor3);
		Shader.SetGlobalColor("_TranslucencyColor4", TranslucencyColor4);
		Shader.SetGlobalFloat("_TranslucencyStrength", Strength);
		Shader.SetGlobalFloat("_TranslucencyPointLightDirectionality", PointLightsDirectionality);
		Shader.SetGlobalFloat("_TranslucencyConstant", Constant);
		Shader.SetGlobalFloat("_TranslucencyNormalOffset", Scattering);
		Shader.SetGlobalFloat("_TranslucencyExponent", SpotExponent);
		Shader.SetGlobalFloat("_TranslucencySuppressRealtimeShadows", SuppressShadows);
	}

	public void RefreshComBufs() {
		if (mycam && combufPreLight!=null && combufPostLight!=null) {
            {
				combufsPreLight=mycam.GetCommandBuffers(CameraEvent.BeforeLighting);
				bool found=false;
				foreach(CommandBuffer cbuf in combufsPreLight) {
					if (cbuf.name=="UBERTranslucencyPrelight") {
						// got it already in command buffers
						found=true;
						break;
					}
				}
				if (!found) {
					mycam.AddCommandBuffer(CameraEvent.BeforeLighting, combufPreLight);
				}
			}
			{
				combufsPostLight=mycam.GetCommandBuffers(CameraEvent.AfterLighting);
				bool found=false;
				foreach(CommandBuffer cbuf in combufsPostLight) {
					if (cbuf.name=="UBERTranslucencyPostlight") {
						// got it already in command buffers
						found=true;
						break;
					}
				}
				if (!found) { 
					mycam.AddCommandBuffer(CameraEvent.AfterLighting, combufPostLight);
				}
			}
		}
	}

	public void Initialize() {
		if (mycam == null) {
			mycam = GetComponent<Camera>();
		}
		if (DeferredTranslucencyBlit==null) return;
		if (combufPreLight == null) {
			int translucencyBufferID = Shader.PropertyToID("_UBERTranslucencyBuffer");

			// take a copy of emission buffer.a
			combufPreLight = new CommandBuffer();
			combufPreLight.name="UBERTranslucencyPrelight";
			combufPreLight.GetTemporaryRT (translucencyBufferID, -1, -1, 0, FilterMode.Point, RenderTextureFormat.RHalf); // would be RenderTextureFormat.R8 for LDR, but it didn't worked anyway
			combufPreLight.Blit (BuiltinRenderTextureType.CameraTarget, translucencyBufferID, DeferredTranslucencyBlit);
            
			// release temp buffer
			combufPostLight = new CommandBuffer();
			combufPostLight.name="UBERTranslucencyPostlight";
            combufPostLight.ReleaseTemporaryRT (translucencyBufferID);
		}
	}
}
