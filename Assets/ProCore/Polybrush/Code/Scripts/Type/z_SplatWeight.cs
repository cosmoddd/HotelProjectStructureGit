using UnityEngine;
using System.Linq;

namespace Polybrush
{
	[System.Serializable]
	public class z_SplatWeight
	{
		// The maximum number of components possible (assuming every single possible attribute is used)
		public const int MAX_TEXTURE_COMPONENTS = 24;

		public byte[] components;

		public int Length { get { return components.Length; } }

		public static readonly z_SplatWeight Channel0 = new z_SplatWeight(8);

		public z_SplatWeight(int size)
		{
			this.components = z_Util.Fill<byte>( 0x0, size );
			this.components[0] = 0x1;
		}

		public z_SplatWeight(byte[] components)
		{
			this.components = components;
		}

		public void Resize(int newSize)
		{
			if(newSize == Length)
				return;

			components = new byte[newSize];
			components[0] = 0xFF;
		}

		public void MakeNonZero()
		{
			for(int i = 0; i < Length; i++)
				if(components[i] > 0)
					return;
			components[0] = 0xFF;
		}

		public static z_SplatWeight Lerp(z_SplatWeight lhs, z_SplatWeight rhs, float alpha)//, z_SplatWeight target)
		{
			int len = System.Math.Min(lhs.Length, rhs.Length);

			byte[] lerped = new byte[len];

			for(int i = 0; i < len; i++)
				lerped[i] = (byte) (lhs.components[i] * (1f-alpha) + rhs.components[i] * alpha);

			return new z_SplatWeight(lerped);
		}

		/**
		 * .
		 */
		public Color32 GetColor32(int index)
		{
			int l = Length;
			return new Color32( index + 0 < l ? components[index+0] : (byte) 0x0,
								index + 1 < l ? components[index+1] : (byte) 0x0,
								index + 2 < l ? components[index+2] : (byte) 0x0,
								index + 3 < l ? components[index+3] : (byte) 0x0 );
		}

		/**
		 * Fills a Vector4 with the components starting at index + 4.  Values are normalized to 0,1.
		 */
		public Vector4 GetVector4(int index)
		{
			int l = Length;
			return new Vector4( index + 0 < l ? components[index + 0] / 255f : 0f,
								index + 1 < l ? components[index + 1] / 255f : 0f,
								index + 2 < l ? components[index + 2] / 255f : 0f,
								index + 3 < l ? components[index + 3] / 255f : 0f );
		}

		public void Set(int index, Color32 color)
		{
			components[index + 0] = color.r;
			components[index + 1] = color.g;
			components[index + 2] = color.b;
			components[index + 3] = color.a;
		}

		/**
		 * wants normalized vec4
		 */
		public void Set(int index, Vector4 vec)
		{
			components[index + 0] = (byte) (vec.x * 255);
			components[index + 1] = (byte) (vec.y * 255);
			components[index + 2] = (byte) (vec.z * 255);
			components[index + 3] = (byte) (vec.w * 255);
		}

		public override string ToString()
		{
			return string.Join(", ", components.Select(x => string.Format("{0,3}", x)).ToArray());
		}
	}
}
