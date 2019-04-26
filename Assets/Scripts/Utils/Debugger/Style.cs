using UnityEngine;

namespace Assets.Scripts.Utils.Debugger
{
    public class Style
    {
        public float Padding = 10f;
        public float LineHeight = 20f;
        public float HeaderColumn = 200f;
        public float Opacity = 0.9f;
        public string DefaultFontName = "Consolas";

        private GUIStyle _boxStyle;
        private GUIStyle _propertyHeaderStyle;
        private GUIStyle _selectedBoxStyle;
        private GUIStyle _contentStyle;
        private Font _font;

        public Font DefaultFont
        {
            get
            {
                if (_font != null)
                    return _font;

                _font = Font.CreateDynamicFontFromOSFont(DefaultFontName, 14);
                return _font;
            }
        }

        public GUIStyle HeaderStyle
        {
            get
            {
                if (_boxStyle != null)
                    return _boxStyle;

                var style = new GUIStyle(GUI.skin.label)
                {
                    normal =
                    {
                        background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, Opacity))
                    },
                    contentOffset = new Vector2(5f, 0f),
                    font = DefaultFont
                };
                _boxStyle = style;
                return style;
            }
        }

        public GUIStyle PropertyHeaderStyle
        {
            get
            {
                if (_propertyHeaderStyle != null)
                    return _propertyHeaderStyle;

                var style = new GUIStyle(GUI.skin.label)
                {
                    normal =
                    {
                        background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.3f, Opacity))
                    },
                    contentOffset = new Vector2(5f, 0f),
                    font = DefaultFont
                };
                _propertyHeaderStyle = style;
                return style;
            }
        }

        public GUIStyle SelectedHeaderStyle
        {
            get
            {
                if (_selectedBoxStyle != null)
                    return _selectedBoxStyle;

                var style = new GUIStyle(GUI.skin.label)
                {
                    normal =
                    {
                        background = MakeTex(2, 2, new Color(0.4f, 0.0f, 0.0f, Opacity))
                    },
                    contentOffset = new Vector2(5f, 0f),
                    font = DefaultFont
                };
                _selectedBoxStyle = style;
                return style;
            }
        }

        public GUIStyle ContentStyle
        {
            get
            {
                if (_contentStyle != null)
                    return _contentStyle;

                var style = new GUIStyle(GUI.skin.label)
                {
                    normal =
                    {
                        background = MakeTex(2, 2, new Color(0f, 0f, 0f, Opacity))
                    },
                    font = DefaultFont
                };
                _contentStyle = style;
                return style;
            }
        }

        private static Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
