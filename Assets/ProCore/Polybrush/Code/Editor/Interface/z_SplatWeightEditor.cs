using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Linq;

namespace Polybrush
{
	/**
	 *	The default editor for z_SplatWeight.
	 */
	[CustomEditor(typeof(z_SplatWeight), isFallback = true)]
	public class z_SplatWeightEditor : Editor
	{
		static int thumbSize = 64;

		public static z_SplatWeight OnInspectorGUI(z_SplatWeight blend, Texture2D[] textures)
		{
			byte[] bytes = blend.components;

			if(bytes == null)
			{
				bytes = new byte[z_SplatWeight.MAX_TEXTURE_COMPONENTS];
				bytes[0] = 255;
				return new z_SplatWeight(bytes);
			}

			Texture2D[] channel_textures;

			// if textures array is longer than the number of channels available, truncate it.
			// if the opposite is true, do not care.
			if(textures == null)
			{
				channel_textures = z_Util.Fill<Texture2D>((Texture2D)null, bytes.Length);
			}
			else if(textures.Length > bytes.Length)
			{
				channel_textures = new Texture2D[bytes.Length];
				System.Array.Copy(textures, 0, channel_textures, 0, bytes.Length);
			}
			else
			{
				channel_textures = textures;
			}

			int index = System.Array.FindIndex(bytes, x => x > 0);

			EditorGUI.BeginChangeCheck();

			index = z_GUI.ChannelField(index, channel_textures, thumbSize);

			if(EditorGUI.EndChangeCheck())
			{
				for(int i = 0; i < bytes.Length; i++)
					bytes[i] = 0;
				bytes[index] = 255;
				return new z_SplatWeight(bytes);
			}
			else
			{
				return blend;
			}
		}


	}
}
