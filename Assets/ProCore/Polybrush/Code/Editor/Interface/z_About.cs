using UnityEditor;
using UnityEngine;

namespace Polybrush
{
	public class z_About : EditorWindow
	{
		const string VERSION_NUMBER = "0.9.0b1";

		GUIStyle centeredLargeLabel = null;
		bool initialized = false;

		void BeginHorizontalCenter()
		{
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
		}

		void EndHorizontalCenter()
		{
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}

		void OnGUI()
		{
			if(!initialized)
			{
				centeredLargeLabel = new GUIStyle( EditorStyles.largeLabel );
				centeredLargeLabel.alignment = TextAnchor.MiddleCenter;
			}

			GUILayout.Space(12);

			GUILayout.Label("Polybrush " + VERSION_NUMBER, centeredLargeLabel);

			GUILayout.Space(12);


			BeginHorizontalCenter();
			if(GUILayout.Button(" Documentation "))
				Application.OpenURL(z_Pref.DocumentationLink);
			EndHorizontalCenter();

			BeginHorizontalCenter();
			if(GUILayout.Button(" Website "))
				Application.OpenURL(z_Pref.WebsiteLink);
			EndHorizontalCenter();

			BeginHorizontalCenter();
			if(GUILayout.Button(" Contact "))
				Application.OpenURL(z_Pref.ContactLink);
			EndHorizontalCenter();
		}
	}
}
