using UnityEngine;

namespace Polybrush
{
	/**
	 *	A simplified version of UnityEngine.RaycastHit that only contains information we're interested in.
	 */
	public struct z_RaycastHit
	{
		/// Distance from the Raycast origin to the point of impact.
		public float distance;
		/// The position in model space where a raycast intercepted a triangle.
		public Vector3 position;
		/// The normal in model space of the triangle that this raycast hit.
		public Vector3 normal;
		/// The triangle index of the hit face.
		public int triangle;

		/**
		 *	Constructor.
		 *	\notes Tautological comments aren't very helpful.
		 */
		public z_RaycastHit(float InDistance, Vector3 InPosition, Vector3 InNormal, int InTriangle)
		{
			this.distance 	= InDistance;
			this.position 	= InPosition;
			this.normal 	= InNormal;
			this.triangle 	= InTriangle;
		}

		/**
		 *	Prints a summary of this struct.
		 */
		public override string ToString()
		{
			return string.Format("p{0}, n{1}", position, normal);
		}
	}
}
