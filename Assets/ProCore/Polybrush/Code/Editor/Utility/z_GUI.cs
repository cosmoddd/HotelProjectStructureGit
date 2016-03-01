using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Polybrush
{
	/**
	 *	GUI field extensions.
	 */
	internal static class z_GUI
	{
		/// Used as a container to pass text to GUI functions requiring a GUIContent without allocating
		/// a new GUIContent isntance.
		static GUIContent tmp_content = new GUIContent("", "");

		/// Maintain GUI.backgroundColor history.
		private static Stack<Color> backgroundColor = new Stack<Color>();

		public static void PushBackgroundColor(Color bg)
		{
			backgroundColor.Push(GUI.backgroundColor);
			GUI.backgroundColor = bg;
		}

		public static void PopBackgroundColor()
		{
			GUI.backgroundColor = backgroundColor.Pop();
		}

		private static GUIStyle _headerTextStyle = null;

		/**
		 *	Large bold slightly transparent font.
		 */
		public static GUIStyle headerTextStyle
		{
			get
			{
				const int PAD = 2, MARGIN_HORIZONTAL = 4, MARGIN_VERTICAL = 0;

				if(_headerTextStyle == null)
				{
					_headerTextStyle = new GUIStyle();
					_headerTextStyle.margin = new RectOffset(MARGIN_HORIZONTAL, MARGIN_HORIZONTAL, MARGIN_VERTICAL, MARGIN_VERTICAL);
					_headerTextStyle.padding = new RectOffset(PAD, PAD, 4, PAD);
					_headerTextStyle.fontSize = 14;
					_headerTextStyle.fontStyle = FontStyle.Bold;
					_headerTextStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.gray : Color.gray;
				}

				return _headerTextStyle;
			}
		}

		public static int CycleButton(int index, GUIContent[] content, GUIStyle style = null)
		{
			if(style != null)
			{
				if( GUILayout.Button(content[index], style) )
					return (index + 1) % content.Length;
				else
					return index;
			}
			else
			{
				if( GUILayout.Button(content[index]) )
					return (index + 1) % content.Length;
				else
					return index;
			}
		}

		/**
		 * Similar to EditorGUILayoutUtility.Slider, except this allows for values outside of the min/max bounds via the float field.
		 */
		public static float FreeSlider(string content, float value, float min, float max)
		{
			tmp_content.text = content;
			return FreeSlider(tmp_content, value, min, max);
		}

		/**
		 * Similar to EditorGUILayoutUtility.Slider, except this allows for values outside of the min/max bounds via the float field.
		 */
		public static float FreeSlider(GUIContent content, float value, float min, float max)
		{
			const float PAD = 4f;
			const float SLIDER_HEIGHT = 16f;
			const float MIN_LABEL_WIDTH = 0f;
			const float MAX_LABEL_WIDTH = 128f;
			const float MIN_FIELD_WIDTH = 48f;

			GUILayoutUtility.GetRect(Screen.width, 18);

			Rect previousRect = GUILayoutUtility.GetLastRect();
			float y = previousRect.y;

			float labelWidth = content != null ? Mathf.Max(MIN_LABEL_WIDTH, Mathf.Min(GUI.skin.label.CalcSize(content).x + PAD, MAX_LABEL_WIDTH)) : 0f;
			float remaining = (Screen.width - (PAD * 2f)) - labelWidth;
			float sliderWidth = remaining - (MIN_FIELD_WIDTH + PAD);
			float floatWidth = MIN_FIELD_WIDTH;

			Rect labelRect = new Rect(PAD, y + 2f, labelWidth, SLIDER_HEIGHT);
			Rect sliderRect = new Rect(labelRect.x + labelWidth, y + 1f, sliderWidth, SLIDER_HEIGHT);
			Rect floatRect = new Rect(sliderRect.x + sliderRect.width + PAD, y + 1f, floatWidth, SLIDER_HEIGHT);

			if(content != null)
				GUI.Label(labelRect, content);

			EditorGUI.BeginChangeCheck();

				int controlID = GUIUtility.GetControlID(FocusType.Native, sliderRect);
				float tmp = value;
				tmp = GUI.Slider(sliderRect, tmp, 0f, min, max, GUI.skin.horizontalSlider, (!EditorGUI.showMixedValue) ? GUI.skin.horizontalSliderThumb : "SliderMixed", true, controlID);

			if(EditorGUI.EndChangeCheck())
				value = Event.current.control ? 1f * Mathf.Round(tmp / 1f) : tmp;

			value = EditorGUI.FloatField(floatRect, value);

			return value;
		}

		public static int ChannelField(int index, Texture2D[] channels, int thumbSize)
		{
			int mIndex = index;

			Rect last = GUILayoutUtility.GetLastRect();

			const int margin = 4; 					// group pad
			const int pad = 2; 						// texture pad
			const int selected_rect_height = 10;	// the little green bar and height padding

			int actual_width = (int) Mathf.Ceil(thumbSize + pad/2);
			int container_width = (int) Mathf.Floor(EditorGUIUtility.currentViewWidth) - margin * 2 - 3;	// subtract 2 because Screen.width is screwy
			int columns = (int) Mathf.Floor(container_width / actual_width);
			int fill = (int) Mathf.Floor(((container_width % actual_width) - 1) / columns);
			int size = thumbSize + fill;
			int rows = channels.Length / columns + (channels.Length % columns == 0 ? 0 : 1);
			int height = rows * (size + selected_rect_height);// + margin * 2;

			Rect r = new Rect(last.x + margin + pad, last.y + last.height + margin, size, size);

			Rect border = new Rect( last.x + margin, last.y + margin, container_width + margin, height + margin );
			GUI.color = texture_button_border;
			EditorGUI.DrawPreviewTexture(border, EditorGUIUtility.whiteTexture);
			border.x += 1;
			border.y += 1;
			border.width -= 2;
			border.height -= 2;
			GUI.color = channel_field_fill;
			EditorGUI.DrawPreviewTexture(border, EditorGUIUtility.whiteTexture);
			GUI.color = Color.white;

			for(int i = 0; i < channels.Length; i++)
			{
				if(i > 0 && i % columns == 0)
				{
					r.x = pad + margin;
					r.y += r.height + selected_rect_height;
				}
					
				if( TextureButton(r, "channel\n" + (i+1), channels[i], i == mIndex) )
				{
					mIndex = i;
					GUI.changed = true;
				}

				r.x += r.width + pad;
			}

			GUILayoutUtility.GetRect(Screen.width - 8, height);

			// channel_field_fill = EditorGUILayout.ColorField(channel_field_fill);
			// GUILayout.Label(channel_field_fill.ToString("F3"));

			return mIndex;
		}

		static GUIStyle _centeredStyle = null;

		static GUIStyle centeredStyle
		{
			get
			{
				if(_centeredStyle == null)
				{
					_centeredStyle = new GUIStyle();
					_centeredStyle.alignment = TextAnchor.MiddleCenter;
					_centeredStyle.normal.textColor = new Color(.85f, .85f, .85f, 1f);
					_centeredStyle.wordWrap = true;
				}
				return _centeredStyle;
			}
		}

		static readonly Color texture_button_border = new Color(.1f, .1f, .1f, 1f);
		static readonly Color texture_button_fill = new Color(.18f, .18f, .18f, 1f);
		static readonly Color channel_field_fill = new Color(.265f, .265f, .265f, 1f);

		static bool TextureButton(Rect rect, string text, Texture2D img, bool selected)
		{
			bool clicked = false;

			Rect r = rect;

			Rect border = new Rect(r.x + 2, r.y + 2, r.width - 4, r.height - 4);

			GUI.color = texture_button_border;
			EditorGUI.DrawPreviewTexture(border, EditorGUIUtility.whiteTexture, null, ScaleMode.ScaleToFit, 0f);
			GUI.color = Color.white;

			border.x += 2;
			border.y += 2;
			border.width -= 4;
			border.height -= 4;

			if(img != null)
			{
				EditorGUI.DrawPreviewTexture(border, img, null, ScaleMode.ScaleToFit, 0f);
			}
			else
			{
				GUI.color = texture_button_fill;
				EditorGUI.DrawPreviewTexture(border, EditorGUIUtility.whiteTexture, null, ScaleMode.ScaleToFit, 0f);
				GUI.color = Color.white;
				GUI.Label(border, text, centeredStyle);
			}

			if(selected)
			{
				r.y += r.height;
				r.x += 2;
				r.width -= 4;
				r.height = 6;
				GUI.color = Color.green;
				EditorGUI.DrawPreviewTexture(r, EditorGUIUtility.whiteTexture, null, ScaleMode.StretchToFill, 0);
				GUI.color = Color.white;
			}

			clicked = GUI.Button(border, "", GUIStyle.none);

			return clicked;
		}
	}
}
