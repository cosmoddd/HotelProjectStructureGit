using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Polybrush
{
	/**
	 *	Utility methods for working with shaders.
	 */
	public static class z_ShaderUtil
	{
		/**
		 *	Attempt to read the shader source code to a string.  If source can't be found (built-in shaders are in binary bundles)
		 * 	an empty string is returned.
		 */
		public static string GetSource(Shader shader)
		{
			string path = AssetDatabase.GetAssetPath(shader);

			// built-in shaders don't have a valid path.
			if(File.Exists(path))
				return File.ReadAllText( path );
			else
				return string.Empty;
		}

		/**
		 *	Returns true if shader has a COLOR attribute.
		 */
		public static bool SupportsVertexColors(Shader source)
		{
			return SupportsVertexColors(GetSource(source));
		}

		public static bool SupportsVertexColors(string source)
		{
			return Regex.Match(source, "float4\\s.*\\s:\\sCOLOR;").Success;
		}

		/**
		 *	True if the shader likely supports texture blending.
		 */
		public static bool SupportsTextureBlending(Shader shader)
		{
			string src = GetSource(shader);
			return GetTextureChannelCount(src) > 0;
		}

		public static int GetTextureChannelCount(string src)
		{
			string pattern = "(?<=" + z_PostProcessTextureBlend.TEXTURE_CHANNEL_DEF + ")[0-9]{1,2}";
			
			Match match = Regex.Match(src, pattern);

			int val = -1;

			if(match.Success)
				int.TryParse(match.Value, out val);

			return val;
		}

		/**
		 *	Returns the shader defined mesh attributes used.  Null if no attributes are defined.
		 */
		public static z_MeshChannel[] GetUsedMeshAttributes(Material material)
		{
			return material == null ? null : GetUsedMeshAttributes( GetSource(material.shader) );
		}

		public static z_MeshChannel[] GetUsedMeshAttributes(string src)
		{
			string pattern = "(?<=" + z_PostProcessTextureBlend.MESH_ATTRIBS_DEF + ").*?(?=,|\\r\\n|\\n)";
			// string pattern = "(?<=" + z_PostProcessTextureBlend.MESH_ATTRIBS_DEF + ")(.\\w+.)*(?=(\\r\\n|,|\\n?))";

			Match match = Regex.Match(src, pattern);

			if(match.Success)
			{
				string[] attribs = match.Value.Trim().Split(' ');
				z_MeshChannel[] channels = attribs.Select(x => z_MeshChannelUtility.StringToEnum(x))
													.Where(y => y != z_MeshChannel.NULL).ToArray();
				return channels;
			}

			return null;			
		}

		/**
		 *	Tries to extract texture channels from a blend material.
		 * 	@todo - build normals as well?
		 */
		public static Texture2D[] GetBlendTextures(Material material)
		{
			string src = GetSource(material.shader);

			int expectedTextureCount = GetTextureChannelCount(src);

			MatchCollection non_bump_textures = Regex.Matches(src, "_.*?\\s\\(\".*?\", 2D\\)\\s=\\s\"[^(bump)|(gray)]*?\"");

			Texture2D[] textures = new Texture2D[ expectedTextureCount ];

			int i = 0;

			foreach(Match m in non_bump_textures)
			{
				int space = m.Value.IndexOf(" ");

				if(space < 0)
				{
					textures[i] = null;
				}
				else
				{
					string prop = m.Value.Substring(0, space);

					if( material.HasProperty(prop) )
						textures[i] = material.GetTexture(prop) as Texture2D;
					else
						textures[i] = null;
				}

				if(++i >= expectedTextureCount)
					break;
			}

			return textures;
		}
	}
}
