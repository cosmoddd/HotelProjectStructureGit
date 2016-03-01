#define TEXTURE_BLEND_MODE_ENABLED

namespace Polybrush
{
	/**
	 *	Tool enum for brush modes.
	 */
	public enum z_BrushTool
	{
		None,
		RaiseLower,
		Smooth,
		Paint,
#if PREFAB_MODE_ENABLED
		Prefab,
#endif
#if TEXTURE_BLEND_MODE_ENABLED
		Texture,
#endif
		Settings
	}

	public static class z_BrushToolUtility
	{
		public static System.Type GetModeType(this z_BrushTool tool)
		{
			switch(tool)
			{
				case z_BrushTool.RaiseLower:
					return typeof(z_BrushModeRaiseLower);

				case z_BrushTool.Smooth:
					return typeof(z_BrushModeSmooth);

				case z_BrushTool.Paint:
					return typeof(z_BrushModePaint);

#if PREFAB_MODE_ENABLED
				case z_BrushTool.Prefab:
					return typeof(z_BrushModePrefab);
#endif

#if TEXTURE_BLEND_MODE_ENABLED
				case z_BrushTool.Texture:
					return typeof(z_BrushModeTexture);
#endif
			}

			return null;
		}
	}
}
