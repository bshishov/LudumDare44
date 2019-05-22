using UnityEngine;

namespace Utils.Debugger
{
    public class Style
    {
        public float Padding = 10f;
        public float LineHeight = 20f;
        public float HeaderColumn = 200f;
        public string DefaultFontName = "Consolas";
        public Color HeaderColor = new Color(0.1f, 0.1f, 0.1f, .9f);
        public Color SelectedHeaderColor = new Color(0.4f, 0.0f, 0.0f, .9f);
        public Color ContentColor = new Color(0.1f, 0.1f, 0.1f, .9f);
        public Color PropertyHeaderColor = new Color(0.1f, 0.1f, 0.3f, .9f);

        public bool IsInitialized { get; private set; }

        public Font DefaultFont { get; private set; }

        public GUIStyle HeaderStyle { get; private set; }

        public GUIStyle PropertyHeaderStyle { get; private set; }

        public GUIStyle SelectedHeaderStyle { get; private set; }

        public GUIStyle ContentStyle { get; private set; }

        public void Initialize()
        {
            DefaultFont = Font.CreateDynamicFontFromOSFont(DefaultFontName, 14);
            HeaderStyle = CreateLabelStyle(DefaultFont, HeaderColor);
            PropertyHeaderStyle = CreateLabelStyle(DefaultFont, PropertyHeaderColor);
            SelectedHeaderStyle = CreateLabelStyle(DefaultFont, SelectedHeaderColor);
            ContentStyle = CreateLabelStyle(DefaultFont, ContentColor);
            IsInitialized = true;
        }

        private static GUIStyle CreateLabelStyle(Font font, Color background)
        {
            return new GUIStyle(GUI.skin.label)
            {
                normal =
                {
                    background = MakeTex(2, 2, background),
                },
                contentOffset = new Vector2(5f, 0f),
                font = font
            };
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
