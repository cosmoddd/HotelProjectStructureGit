
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Voxeland 
{
	public class LayoutTools
	{
		#region Layout Instruments

			static public int margin;
			static public int lineHeight = 18;
			
			static public Vector2 lastPos;
			static public int lastHeight;
			
			static public void NewLine (int height=0) 
			{ 
				//replacing height with default
				if (height==0) height = lineHeight;
				
				lastPos = new Vector2(margin, lastPos.y+lastHeight);
				lastHeight = height;
			}
		
			/*static public void MoveLine (int offset) { MoveLine(offset, lineHeight); }
			static public void MoveLine (int offset, int height)
			{
				lastRect = new Rect (margin, lastRect.y + offset, inspectorWidth - margin, height);
			}*/
	
			static public Rect AutoRect (float width=1f) 
			{ 
				//replacing width to relative if it is < 1
				if (width < 1.1f) width = (EditorGUIUtility.currentViewWidth-margin-15)*width - 3; //15 pixels on scrollbar

				lastPos.x += width;
				return new Rect (lastPos.x-width, lastPos.y, width-3, lastHeight-2);
			}
		
			/*static public void MoveTo (float offset) { MoveTo((int)((inspectorWidth-margin)*offset - 3)); }
			static public void MoveTo (int offset)
			{
				lastRect = new Rect (margin+offset, lastRect.y, 0, lastRect.height);
			}
		
			static public void Offset (int left, int top, int right=0, int bottom=0)
			{
				lastRect = new Rect(lastRect.x-left, lastRect.y-top, lastRect.width+left+right, lastRect.height+top+bottom);
			}*/

			static public void Start (int m=15)
			{
				Rect rect = GUILayoutUtility.GetRect(1, 0);
				lastPos = new Vector2(rect.x, rect.y-EditorGUIUtility.singleLineHeight);
				margin = m;
			}

			static public void Finish ()
			{
				if (Event.current.type == EventType.Layout) GUILayoutUtility.GetRect(1, lastPos.y+lastHeight, "TextField");
			}

		#endregion


		#region Quick Types

			static public void QuickInt (ref int param, string name=null, string tooltip=null, int min=0, int max=0, float width=1f, int height=0, float fieldSize=0.6f)
			{
				NewLine(height);
				
				//creating gui content
				GUIContent guiContent = null;
				if (name!=null || tooltip!=null) guiContent = new GUIContent(name, tooltip);
				
				//setting field width
				Rect rect = AutoRect(width);
				EditorGUIUtility.labelWidth = rect.width * (1-fieldSize);

				if (guiContent==null)
				{
					if (min==0 && max==0) param = EditorGUI.IntField(rect, param);
					else param = EditorGUI.IntSlider(rect, param, min, max);
				}
				else
				{
					if (min==0 && max==0) param = EditorGUI.IntField(rect, guiContent, param);
					else param = EditorGUI.IntSlider(rect, guiContent, param, min, max);
				}
			}

			static public void QuickFloat (ref float param, string name=null, string tooltip=null, int min=0, int max=0, float width=1f, int height=0, float fieldSize=0.6f)
			{
				NewLine(height);
				
				//creating gui content
				GUIContent guiContent = null;
				if (name!=null || tooltip!=null) guiContent = new GUIContent(name, tooltip);
				
				//setting field width
				Rect rect = AutoRect(width);
				EditorGUIUtility.labelWidth = rect.width * (1-fieldSize);

				if (guiContent==null)
				{
					if (min==0 && max==0) param = EditorGUI.FloatField(rect, param);
					else param = EditorGUI.Slider(rect, param, min, max);
				}
				else
				{
					if (min==0 && max==0) param = EditorGUI.FloatField(rect, guiContent, param);
					else param = EditorGUI.Slider(rect, guiContent, param, min, max);
				}
			}

			static public bool QuickBool (ref bool param, string name=null, string tooltip=null, float width=1f, int height=0, bool isLeft=false, float fieldSize=0.6f)
			{
				NewLine(height);

				//setting field width
				Rect rect = AutoRect(width);
				EditorGUIUtility.labelWidth = rect.width * (1-fieldSize);

				if (name==null) 
					param = EditorGUI.Toggle(rect, param); //no left toggle without a name
				else
				{
					GUIContent guiContent = new GUIContent(name, tooltip);
					if (isLeft) param = EditorGUI.ToggleLeft(rect, guiContent, param);
					else param = EditorGUI.Toggle(rect, guiContent, param);
				}
				return param;
			}

			static public bool QuickButton (string name=null, string tooltip=null, float width=1f, int height=0)
			{
				NewLine(height);
				return GUI.Button(AutoRect(width), new GUIContent(name, tooltip));
			}

			static GUIStyle foldoutStyle = null;
			static public bool QuickFoldout (ref bool show, string name=null, string tooltip=null)
			{
				if (foldoutStyle == null) foldoutStyle = new GUIStyle(EditorStyles.foldout);
				foldoutStyle.fontStyle = FontStyle.Bold;
				foldoutStyle.onNormal.textColor = Color.black;
				foldoutStyle.onFocused.textColor = Color.black;
				NewLine(5); 
				NewLine(); 
				show = EditorGUI.Foldout(AutoRect(), show, new GUIContent(name, tooltip), true, foldoutStyle);
				return show;
			}

			static public Texture QuickTextureSingleLine(ref Texture tex, string name=null, string tooltip=null, float width=1f, float fieldSize=0.6f)
			{
				LayoutTools.NewLine(20);

				EditorGUI.LabelField(LayoutTools.AutoRect(1-fieldSize), new GUIContent(name, tooltip));
				tex = (Texture)EditorGUI.ObjectField(AutoRect(20), tex, typeof(Texture), false);

				/*tex = (Texture)EditorGUI.ObjectField(LayoutTools.AutoRect(20), tex, typeof(Texture));
				LayoutTools.AutoRect(10);
				LayoutTools.lastPos.y+=2;
				EditorGUI.LabelField(LayoutTools.AutoRect(200), new GUIContent(name, tooltip));
				LayoutTools.lastPos.y-=2;*/
				return tex;
			}

			static public Color QuickColor(ref Color color, string name=null, string tooltip=null, float width=1f, float fieldSize=0.6f)
			{
				LayoutTools.NewLine();

				//setting field width
				Rect rect = AutoRect(width);
				EditorGUIUtility.labelWidth = rect.width * (1-fieldSize);

				color = EditorGUI.ColorField(rect, new GUIContent(name, tooltip), color);
				return color;
			}

			/*static public void QuickObject<T> (ref T param, string name=null, string tooltip=null, float width=1f, int height=0, bool isLeft=false)
			{
				//creating gui content
				GUIContent guiContent = null;
				if (name!=null || tooltip!=null) guiContent = new GUIContent(name, tooltip);
				
				NewLine(height);
				if (guiContent==null) param = EditorGUI.ObjectField(AutoRect(width), (object)param, typeof(T)); //no left toggle without a name
				else
				{
					GUIContent guiContent = new GUIContent(name, tooltip);
					if (isLeft) param = EditorGUI.ToggleLeft(AutoRect(width), guiContent, param);
					else param = EditorGUI.Toggle(AutoRect(width), guiContent, param);
				}
				return param;
			}*/

			/*static public void QuickField<T> (ref T param, string name=null, string tooltip=null, int min=0, int max=0, float width=1f, int height=0, float fieldSize=0.5f)
			{
				//creating gui content
				GUIContent guiContent = null;
				if (name!=null || tooltip!=null) guiContent = new GUIContent(name, tooltip);

				System.Type type = typeof(T);

				if (type==typeof(int))
				{
					if (min==0 && max==0)
					{
					
					}
					else
					{
						if (guiContent==null) param = EditorGUI.IntField(AutoRect(width), (int)(object)param);
					}
					
					if (guiContent!=null)
					{
						 
						else param = EditorGUI.IntSlider(AutoRect(width), param, min, max);
					}
					else
					{
						GUIContent guiContent = new GUIContent(name, tooltip);
						if (min==0 && max==0) param = EditorGUI.IntField(AutoRect(width), guiContent, param);
						else param = EditorGUI.IntSlider(AutoRect(width), guiContent, param, min, max);
					}
				}
			}*/
		#endregion
		

		#region Array Instruments

			static public void ArrayButtons<T> (ref T[] array, ref int selected, bool drawUpDown=true)
			{
				NewLine();
				AutoRect(0.4f);
				if (drawUpDown)
				{
					if (GUI.Button(AutoRect(0.15f), new GUIContent("⤴", "Move selected up")) && selected != 1)
					{
						T tmp = array[selected];
						array[selected] = array[selected-1];
						array[selected-1] = tmp;
						selected--;
					}
				
					if (GUI.Button(AutoRect(0.15f), new GUIContent("⤵", "Move selected down")) && selected < array.Length-1)
					{
						T tmp = array[selected];
						array[selected] = array[selected+1];
						array[selected+1] = tmp;
						selected++;
					}
				}
				else AutoRect(0.3f);
			
				if (GUI.Button(AutoRect(0.15f), new GUIContent("+", "Add new array element")))
				{
					//System.Reflection.MethodInfo memberwiseCloneMethod = typeof(T).GetMethod ("MemberwiseClone", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); 
					T[] newArray = new T[array.Length+1];
					for (int i=0; i<array.Length; i++) 
					{
						if (i<=selected) newArray[i] = array[i];
						//else if (i==selected) newArray[i] = new T();
						else newArray[i+1] = array[i];
					}
					
					//array.Add(memberwiseCloneMethod.Invoke (array[0], null));

					array = newArray;
					selected++;
				}
			
				if (GUI.Button(AutoRect(0.15f), new GUIContent("✕", "Remove element")))
				{
					T[] newArray = new T[array.Length-1];
					for (int i=0; i<array.Length-1; i++) 
					{
						if (i<selected) newArray[i] = array[i];
						else newArray[i] = array[i+1];
					}
					array = newArray;
				}
			}
		#endregion
	}

}