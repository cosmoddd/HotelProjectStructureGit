using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Polybrush
{
	/**
	 *	Base class for brush modes.
	 */
	[System.Serializable]
	public abstract class z_BrushMode : ScriptableObject
	{
		/// The message that will accompany Undo commands for this brush.  Undo/Redo is handled by z_Editor.
		public virtual string UndoMessage { get { return "Apply Brush"; } }

		/// A temporary component attached to the currently editing object.  Use this to (by default) override the 
		/// scene zoom functionality, or optionally extend (see z_OverlayRenderer).
		[SerializeField] protected z_ZoomOverride tempComponent;

		protected Color innerColor, outerColor;

		protected virtual void CreateTempComponent(z_EditableObject target, z_BrushSettings settings)
		{
			if(!z_Util.IsValid(target))
				return;

			tempComponent = target.gameObject.AddComponent<z_ZoomOverride>();
			tempComponent.hideFlags = HideFlags.HideAndDontSave;
			tempComponent.SetWeights(null, 0f);
		}

		protected virtual void UpdateTempComponent(z_BrushTarget target, z_BrushSettings settings)
		{
			if(!z_Util.IsValid(target))
				return;
				
			tempComponent.SetWeights(target.weights, settings.strength);
		}

		protected virtual void DestroyTempComponent()
		{
			if(tempComponent != null)
				GameObject.DestroyImmediate(tempComponent);
		}

		/// Called on instantiation.  Base implementation sets HideFlags.
		public virtual void OnEnable()
		{
			this.hideFlags = HideFlags.HideAndDontSave;

			innerColor = z_Pref.GetColor(z_Pref.brushColor);
			outerColor = z_Pref.GetGradient(z_Pref.brushGradient).Evaluate(1f);

			innerColor.a = .9f;
			outerColor.a = .35f;
		}

		/// Called when mode is disabled.
		public virtual void OnDisable()
		{
			DestroyTempComponent();
		}

		/// Called by z_Editor when brush settings have been modified.
		public virtual void OnBrushSettingsChanged(z_BrushTarget target, z_BrushSettings settings)
		{
			UpdateTempComponent(target, settings);
		}

		/// Inspector GUI shown in the Editor window.
		public virtual void DrawGUI(z_BrushSettings brushSettings) {}

		/// Called when the mouse begins hovering an editable object.
		public virtual void OnBrushEnter(z_EditableObject target, z_BrushSettings settings)
		{
			if(z_Pref.GetBool(z_Pref.hideWireframe) && target.renderer != null)
				EditorUtility.SetSelectedWireframeHidden(target.renderer, true);

			CreateTempComponent(target, settings);
		}

		/// Called whenever the brush is moved.  Note that @target may have a null editableObject.
		public virtual void OnBrushMove(z_BrushTarget target, z_BrushSettings settings)
		{
			UpdateTempComponent(target, settings);
		}

		/// Called when the mouse exits hovering an editable object.
		public virtual void OnBrushExit(z_EditableObject target)
		{
			if(target.renderer != null)
				EditorUtility.SetSelectedWireframeHidden(target.renderer, false);

			DestroyTempComponent();
		}

		/// Called when the mouse begins a drag across a valid target.
		public virtual void OnBrushBeginApply(z_BrushTarget target, z_BrushSettings settings) {}

		/// Called every time the brush should apply itself to a valid target.  Default is on mouse move.
		public abstract void OnBrushApply(z_BrushTarget target, z_BrushSettings settings);

		/// Called when a brush application has finished.  Use this to clean up temporary resources or apply 
		/// deferred actions to a mesh (rebuild UV2, tangents, whatever).
		public virtual void OnBrushFinishApply(z_BrushTarget target, z_BrushSettings settings)
		{
			DestroyTempComponent();
		}

		/// Draw scene gizmos.  Base implementation draws the brush preview.
		public virtual void DrawGizmos(z_BrushTarget target, z_BrushSettings settings)
		{
			foreach(z_RaycastHit hit in target.raycastHits)
				z_Handles.DrawBrush(hit.position, hit.normal, settings, target.localToWorldMatrix, innerColor, outerColor);
		}

		public abstract void RegisterUndo(z_BrushTarget brushTarget);

		public virtual void UndoRedoPerformed(List<GameObject> modified)
		{
			DestroyTempComponent();
		}
	}
}
