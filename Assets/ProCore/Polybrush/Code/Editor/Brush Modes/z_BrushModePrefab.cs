using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Polybrush
{
	/**
	 *	Prefab painter brush mode.
	 */
	public class z_BrushModePrefab : z_BrushMode
	{
		const string PREFAB_PALETTE_PATH = "Prefab Palettes/Default.asset";
		private double lastBrushApplication = 0.0;

		/// preferences
		public bool hitTransformIsParent = true;
		public bool placeWithPivot = false;

		/// The current prefab palette
		[SerializeField] z_PrefabPalette _prefabPalette = null;

		private z_PrefabPalette prefabPalette
		{
			get
			{
				if(_prefabPalette == null)
					prefabPalette = z_EditorUtility.GetDefaultAsset<z_PrefabPalette>(PREFAB_PALETTE_PATH);

				return _prefabPalette;
			}
			set
			{
				if(prefabPaletteEditor != null)
					GameObject.DestroyImmediate(prefabPaletteEditor);

				_prefabPalette = value;
				prefabPaletteEditor = (z_PrefabPaletteEditor) Editor.CreateEditor(_prefabPalette);
				prefabPaletteEditor.onSelectIndex = SelectPrefab;
			}
		}

		// / An Editor for the prefabPalette.
		[SerializeField] z_PrefabPaletteEditor prefabPaletteEditor = null;

		/// the current gameobject to paint
		GameObject gameObject = null;

		public override string UndoMessage { get { return "Paint Prefabs"; } }

		private GUIStyle paletteStyle;

		public override void OnEnable()
		{
			base.OnEnable();

			/// unity won't serialize delegates, so even if prefabPalette isn't null and the editor remains valid
			/// the delegate could still be null after a script reload.
			if(_prefabPalette == null)
				prefabPalette = z_EditorUtility.GetDefaultAsset<z_PrefabPalette>(PREFAB_PALETTE_PATH);
			prefabPaletteEditor.onSelectIndex = SelectPrefab;			

			if(gameObject == null && prefabPalette.prefabs != null && prefabPalette.prefabs.Count() > 0)
				SelectPrefab(prefabPalette.prefabs[0]);

			paletteStyle = new GUIStyle();
			paletteStyle.padding = new RectOffset(8, 8, 8, 8);
		}

		/// Inspector GUI shown in the Editor window.  Base class shows z_BrushSettings by default
		public override void DrawGUI(z_BrushSettings brushSettings)
		{
			base.DrawGUI(brushSettings);

			GUILayout.Label("Placement Settings", z_GUI.headerTextStyle);

			placeWithPivot = EditorGUILayout.Toggle("Use Pivot", placeWithPivot);

			GUILayout.Space(4);

			gameObject = (GameObject) EditorGUILayout.ObjectField("GameObject", gameObject, typeof(GameObject), true);

			GUILayout.BeginVertical( paletteStyle );
				prefabPaletteEditor.OnInspectorGUI();
			GUILayout.EndHorizontal();
		}

		private void SelectPrefab(GameObject go)
		{
			if(go != null)
				gameObject = go;
		}

		public override void OnBrushSettingsChanged(z_BrushTarget target, z_BrushSettings settings)
		{
			base.OnBrushSettingsChanged(target, settings);
		}

		/// Called when the mouse begins hovering an editable object.
		public override void OnBrushEnter(z_EditableObject target, z_BrushSettings settings)
		{
			base.OnBrushEnter(target, settings);
		}

		/// Called whenever the brush is moved.  Note that @target may have a null editableObject. 
		public override void OnBrushMove(z_BrushTarget target, z_BrushSettings settings)
		{
			base.OnBrushMove(target, settings);

			if(!z_Util.IsValid(target))
				return;
		}

		/// Called when the mouse exits hovering an editable object.
		public override void OnBrushExit(z_EditableObject target)
		{
			base.OnBrushExit(target);
		}

		/// Called every time the brush should apply itself to a valid target.  Default is on mouse move.
		public override void OnBrushApply(z_BrushTarget target, z_BrushSettings settings)
		{
			if( (EditorApplication.timeSinceStartup - lastBrushApplication) > Mathf.Max(.06f, (1f - settings.strength)) )
			{
				lastBrushApplication = EditorApplication.timeSinceStartup;

				foreach(z_RaycastHit hit in target.raycastHits)
					PlaceGameObject(hit, gameObject, target, settings);
			}
		}

		/// Handle Undo locally since it doesn't follow the same pattern as mesh modifications.
		public override void RegisterUndo(z_BrushTarget brushTarget) {}

		private void PlaceGameObject(z_RaycastHit hit, GameObject prefab, z_BrushTarget target, z_BrushSettings settings)
		{
			if(prefab == null)
				return;

			Ray ray = RandomRay(hit.position, hit.normal, settings.radius, settings.falloff, settings.falloffCurve);

			Debug.DrawRay(
				target.transform.TransformPoint(ray.origin), 
				target.transform.TransformDirection(ray.direction),
				Color.red);

			z_RaycastHit rand_hit;

			if( z_SceneUtility.MeshRaycast(ray, target.editableObject.meshFilter, out rand_hit) )
			{
				float pivotOffset = placeWithPivot ? 0f : GetPivotOffset(prefab);

				Quaternion rotation = Quaternion.FromToRotation(Vector3.up, target.transform.TransformDirection(rand_hit.normal));
				Quaternion random = Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.up);
 
				GameObject inst = (GameObject) GameObject.Instantiate(
					prefab, 
					target.transform.TransformPoint(rand_hit.position),
					rotation * random);

				inst.transform.position = inst.transform.position + inst.transform.up * pivotOffset;

				if(hitTransformIsParent)
					inst.transform.SetParent(target.transform);

				Undo.RegisterCreatedObjectUndo(inst, UndoMessage);
			}
		}

		private Ray RandomRay(Vector3 position, Vector3 normal, float radius, float falloff, AnimationCurve curve)
		{
			Vector3 a = Vector3.zero;
			Quaternion rotation = Quaternion.LookRotation(normal, Vector3.up);

			a.x = Mathf.Cos(Random.Range(0f, 360f));
			a.y = Mathf.Sin(Random.Range(0f, 360f));

			float r = Mathf.Sqrt(Random.Range(0f, 1f));

			while(true)
			{
				/// this isn't great
				if(r < falloff || Random.Range(0f, 1f) > Mathf.Clamp(curve.Evaluate( 1f - ((r - falloff) / (1f - falloff))), 0f, 1f))
				{
					a = position + (rotation * (a.normalized * r * radius));
					return new Ray(a + normal * 10f, -normal);
				}
				else
				{
					r = Mathf.Sqrt(Random.Range(0f, 1f));
				}
			}
		}

		private float GetPivotOffset(GameObject go)
		{
			MeshFilter mf = go.GetComponent<MeshFilter>();

			// probuilder meshes that are prefabs might not have a mesh
			// associated with them, so make sure they do before querying 
			// for bounds
			object pb = go.GetComponent("pb_Object");

			if(pb != null)
				z_ReflectionUtil.Invoke(pb, "Awake");

			if(mf == null || mf.sharedMesh == null)
				return 0f;

			Bounds bounds = mf.sharedMesh.bounds;
		
			return (-bounds.center.y + bounds.extents.y) * go.transform.localScale.y;
		}
	}
}
