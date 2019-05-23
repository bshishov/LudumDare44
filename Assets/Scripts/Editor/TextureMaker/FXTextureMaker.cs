using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Editor.TextureMaker
{
    public class FXTextureMaker : EditorWindow
    {
        public enum Source
        {
            TextureRGB,
            TextureAlpha,
        }

        public enum CMapMode
        {
            None,
            ReplaceColor = 1,
            MultiplyColor = 2
        }

        [MenuItem("Window/FX Texture Maker")]
        public static void ShowWindow()
        {
            var wnd = EditorWindow.GetWindow(typeof(FXTextureMaker));
            wnd.titleContent = new GUIContent("FX Texture Maker");
        }

        public const string CombineShaderName = "TextureMaker/Combine";

        private Texture2D _textureA;
        private Texture2D _textureB;
        private Texture2D _resTexture;
        private Texture2D _colorMapRender;
        private Color _color = Color.white;
        private Gradient _colorMap = new Gradient();
        private GUIStyle _textureFieldStyle;
        private Source _rgbSource;
        private Source _aSource;
        private CMapMode _cMapMode;
        private bool _settingsToggle;
        private int _resWidth = 512;
        private int _resHeight = 512;
        private TextureFormat _resFormat = TextureFormat.ARGB32;
        private bool _resMipChain = true;
        private Material _material;

        void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            GUILayout.Label("RGB");
            _rgbSource = (Source)EditorGUILayout.EnumPopup(_rgbSource);
            if(_rgbSource == Source.TextureRGB || _rgbSource == Source.TextureAlpha)
                _textureA = TextureField("A", _textureA);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            GUILayout.Label("Alpha");
            _aSource = (Source)EditorGUILayout.EnumPopup(_aSource);
            if (_aSource == Source.TextureRGB || _aSource == Source.TextureAlpha)
                _textureB = TextureField("B", _textureB);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            _color = EditorGUILayout.ColorField(new GUIContent("Color"), _color, true, true, true);

            _cMapMode = (CMapMode) EditorGUILayout.EnumPopup("Color map", _cMapMode);

            if (_cMapMode != CMapMode.None)
            {
                EditorGUI.BeginChangeCheck();
                _colorMap = EditorGUILayout.GradientField("Color Map", _colorMap);
                if (EditorGUI.EndChangeCheck())
                    _colorMapRender = RenderGradient(_colorMap);
            }

            // Settings
            _settingsToggle = EditorGUILayout.BeginToggleGroup("Result settings", _settingsToggle);
            _resWidth = EditorGUILayout.IntField("Width", _resWidth);
            _resHeight = EditorGUILayout.IntField("Height", _resHeight);
            _resFormat = (TextureFormat)EditorGUILayout.EnumPopup("Format", _resFormat);
            _resMipChain = EditorGUILayout.Toggle("Mip chain", _resMipChain);
            EditorGUILayout.EndToggleGroup();
            

            if (GUILayout.Button("Combine"))
                _resTexture = Render();
            
            if (GUILayout.Button("Save"))
                Save(_resTexture);

            DrawPreview(_colorMapRender);
            DrawPreview(_resTexture);

            EditorGUILayout.EndVertical();
        }

        private void DrawPreview(Texture texture)
        {
            if (texture != null)
            {
                EditorGUILayout.BeginHorizontal();
                var r1 = EditorGUILayout.GetControlRect(false, 200);
                r1.width = Mathf.Min(r1.height, r1.width);
                EditorGUI.DrawPreviewTexture(r1, texture);

                var r2 = EditorGUILayout.GetControlRect(false, 200);
                r2.width = Mathf.Min(r2.height, r2.width);
                EditorGUI.DrawTextureAlpha(r2, texture);
                EditorGUILayout.EndHorizontal();
            }
        }

        private Texture2D Render()
        {
            var shader = Shader.Find(CombineShaderName);
            if (shader == null)
            {
                Debug.LogWarningFormat("Can't find shader: {0}", CombineShaderName);
                return null;
            }

            if(_material == null)
                _material = new Material(shader);

            // Setup material
            _material.SetTexture("_TexA", _textureA);
            _material.SetTexture("_TexB", _textureB);
            _material.SetTexture("_ColorMap", _colorMapRender);
            _material.color = _color;
            _material.SetInt("_rgbMode", (int)_rgbSource);
            _material.SetInt("_alphaSource", (int)_aSource);
            _material.SetInt("_cMapMode", (int)_cMapMode); 

            // Render
            var rt = RenderTexture.GetTemporary(_resWidth, _resHeight, 0, RenderTextureFormat.ARGBFloat);
            Graphics.Blit(null, rt, _material);

            // Read
            var result = new Texture2D(rt.width, rt.height, _resFormat, _resMipChain);
            RenderTexture.active = rt;
            result.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, true);
            result.Apply();
            RenderTexture.active = null;

            // Dispose
            rt.Release();

            return result;
        }

        private Texture2D TextureField(string label, Texture2D texture)
        {
            GUILayout.BeginVertical();
            if (_textureFieldStyle == null)
            {
                _textureFieldStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperCenter,
                    fixedWidth = 100
                };
            }

            GUILayout.Label(label, _textureFieldStyle);
            var result = (Texture2D)EditorGUILayout.ObjectField(
                texture, 
                typeof(Texture2D), 
                false, 
                GUILayout.Width(100), 
                GUILayout.Height(100));
            GUILayout.EndVertical();
            return result;
        }

        void Save(Texture2D tex)
        {
            if (tex == null)
                return;

            var path = EditorUtility.SaveFilePanel("Save generated texture", "", "gradient", "png");
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                AssetDatabase.Refresh();
                //AssetDatabase.ImportAsset(path);
            }
        }

        public static Texture2D RenderGradient(Gradient gradient, int width = 256, int height = 256, TextureFormat format = TextureFormat.ARGB32, bool mipChain=true)
        {
            if (gradient == null)
                return null;

            var result = new Texture2D(width, height, format, mipChain)
            {
                wrapMode = TextureWrapMode.Clamp,
                alphaIsTransparency = true,
                filterMode = FilterMode.Bilinear
            };
            var colors = new Color[width * height];
            for (var j = 0; j < result.width; j++)
            {
                var u = j / (result.width - 0f);
                for (var i = 0; i < result.height; i++)
                {
                    var v = i / (result.height - 0f);
                    colors[i * width + j] = gradient.Evaluate(u);
                }
            }

            result.SetPixels(colors);
            result.Apply(mipChain);
            return result;
        }
    }
}
