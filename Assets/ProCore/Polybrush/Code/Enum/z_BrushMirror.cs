using UnityEngine;

namespace Polybrush
{
	public enum z_BrushMirror
	{
		None,
		X,
		Y,
		Z
	}

	/**
	 *	Helper functions for working with Mirror enum.
	 */
	public static class z_BrushMirrorUtility
	{
		static readonly Vector3 HorizontalReflection = new Vector3(-1f,  1f,  1f);
		static readonly Vector3 VerticalReflection = new Vector3( 1f, -1f,  1f);
		static readonly Vector3 ForwardReflection = new Vector3( 1f,  1f, -1f);

		/**
		 *	Convert a mirror enum to it's corresponding vector value.
		 */
		public static Vector3 ToVector3(this z_BrushMirror mirror)
		{
			switch(mirror)
			{
				case z_BrushMirror.X:
					return HorizontalReflection;
				case z_BrushMirror.Y:
					return VerticalReflection;
				case z_BrushMirror.Z:
					return ForwardReflection;
				default:
					return Vector3.one;
			}
		}
	}
}
