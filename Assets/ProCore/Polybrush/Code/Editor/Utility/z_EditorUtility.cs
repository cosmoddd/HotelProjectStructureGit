using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;
using System.IO;

namespace Polybrush
{
	public static class z_EditorUtility
	{
		/**
		 *	Return the mesh source, and the guid if applicable (scene instances don't get GUIDs).
		 */
		public static z_ModelSource GetMeshGUID(Mesh mesh, ref string guid)
		{
			string path = AssetDatabase.GetAssetPath(mesh);

			if(path != "")
			{
				AssetImporter assetImporter = AssetImporter.GetAtPath(path);

				if( assetImporter != null )
				{
					// Only imported model (e.g. FBX) assets use the ModelImporter,
					// where a saved asset will have an AssetImporter but *not* ModelImporter.
					// A procedural mesh (one only existing in a scene) will not have any.
					if (assetImporter is ModelImporter)
					{
						guid = AssetDatabase.AssetPathToGUID(path);
						return z_ModelSource.Imported;
					}
					else
					{
						guid = AssetDatabase.AssetPathToGUID(path);
						return z_ModelSource.Asset;
					}
				}
				else
				{
					return z_ModelSource.Scene;
				}
			}

			return z_ModelSource.Scene;
		}


		const int DIALOG_OK = 0;
		const int DIALOG_CANCEL = 1;
		const int DIALOG_ALT = 2;
		const string DO_NOT_SAVE = "DO_NOT_SAVE";

		/**
		 *	Save any modifications to the z_EditableObject.  If the mesh is a scene mesh or imported mesh, it
		 *	will be saved to a new asset.  If the mesh was originally an asset mesh, the asset is overwritten.
		 * 	\return true if save was successfull, false if user-cancelled or otherwise failed.
		 */
		public static bool SaveMeshAsset(Mesh mesh, MeshFilter meshFilter = null, SkinnedMeshRenderer skinnedMeshRenderer = null)
		{
			string save_path = DO_NOT_SAVE;

			string guid = null;
			z_ModelSource source = GetMeshGUID(mesh, ref guid);

			switch( source )
			{
				case z_ModelSource.Asset:

					int saveChanges = EditorUtility.DisplayDialogComplex(
						"Save Changes",
						"Save changes to edited mesh?",
						"Save",				// DIALOG_OK
						"Cancel",			// DIALOG_CANCEL
						"Save As");			// DIALOG_ALT

					if( saveChanges == DIALOG_OK )
						save_path = AssetDatabase.GetAssetPath(mesh);
					else if( saveChanges == DIALOG_ALT )
						save_path = EditorUtility.SaveFilePanelInProject("Save Mesh As", mesh.name + ".asset", "asset", "Save edited mesh to");
					else
						return false;

					break;

				case z_ModelSource.Imported:
				case z_ModelSource.Scene:
				default:
					// @todo make sure path is in Assets/
					save_path = EditorUtility.SaveFilePanelInProject("Save Mesh As", mesh.name + ".asset", "asset", "Save edited mesh to");
				break;
			}

			if( !save_path.Equals(DO_NOT_SAVE) && !string.IsNullOrEmpty(save_path) )
			{
				Object existing = AssetDatabase.LoadMainAssetAtPath(save_path);

				if( existing != null && existing is Mesh )
				{
					/// save over an existing mesh asset
					z_MeshUtility.Copy((Mesh)existing, mesh);
					GameObject.DestroyImmediate(mesh);
				}
				else
				{
					AssetDatabase.CreateAsset(mesh, save_path );
				}

				AssetDatabase.Refresh();

				if(meshFilter != null)
					meshFilter.sharedMesh = (Mesh)AssetDatabase.LoadAssetAtPath(save_path, typeof(Mesh));
				else if(skinnedMeshRenderer != null)
					skinnedMeshRenderer.sharedMesh = (Mesh)AssetDatabase.LoadAssetAtPath(save_path, typeof(Mesh));

				return true;
			}

			// Save was canceled
			return false;
		}

		/**
		 *	Load a Unity internal icon.
		 */
		internal static Texture2D LoadIcon(string icon)
		{
			MethodInfo loadIconMethod = typeof(EditorGUIUtility).GetMethod("LoadIcon", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
			Texture2D img = (Texture2D) loadIconMethod.Invoke(null, new object[] { icon } );
			return img;
		}

		public static string RootFolder
		{
			get
			{
				if( Directory.Exists("Assets/ProCore/" + z_Pref.ProductName) )
				{
					return "Assets/ProCore/" + z_Pref.ProductName + "/";
				}
				else
				{
					string[] matches = Directory.GetDirectories("Assets/", z_Pref.ProductName, SearchOption.AllDirectories);

					if(matches != null && matches.Length == 1)
					{
						return matches[0];
					}
					else
					{
						Debug.LogError("Cannot find " + z_Pref.ProductName + " folder!  Please re-install the package.");
						return "";
					}
				}
			}
		}

		public static string FindFolder(string folder)
		{
			string single = folder.Replace("\\", "/").Substring(folder.LastIndexOf('/') + 1);

			string[] matches = Directory.GetDirectories("Assets/", single, SearchOption.AllDirectories);

			foreach(string str in matches)
			{
				if(str.Replace("\\", "/").Contains(folder))
					return str;
			}
			return null;
		}

		/**
		 *	Fetch a default asset from path relative to the product folder.  If not found, a new one is created.
		 */
		public static T GetDefaultAsset<T>(string path) where T : UnityEngine.ScriptableObject, z_IHasDefault
		{
			string full = z_EditorUtility.RootFolder + path;

			T asset = AssetDatabase.LoadAssetAtPath<T>(full);

			if(asset == null)
			{
				asset = ScriptableObject.CreateInstance<T>();
				asset.SetDefaultValues();
				EditorUtility.SetDirty(asset);

				string folder = Path.GetDirectoryName(full);

				if(!Directory.Exists(folder))
					Directory.CreateDirectory(folder);

				AssetDatabase.CreateAsset(asset, full);
			}

			return asset;
		}
	}
}
