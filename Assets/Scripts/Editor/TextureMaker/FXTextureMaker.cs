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

        [MenuItem("Window/FX Texture Maker")]
        public static void ShowWindow()
        {
            var wnd = EditorWindow.GetWindow(typeof(FXTextureMaker));
            wnd.titleContent = new GUIContent("FX Texture Maker");
        }

        public const string CombineShaderName = "TextureMaker/Combine";

        private Texture2D _textureA;
        private Texture2D _textureB;
        private RenderTexture _resTexture;
        private Color _color = Color.white;
        private GUIStyle _textureFieldStyle;
        private Source _rgbSource;
        private Source _aSource;

        private bool _settingsToggle;
        private int _resWidth = 512;
        private int _resHeight = 512;
        private TextureFormat _resFormat = TextureFormat.ARGB32;
        private bool _resMipChain = true;

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

            // Settings
            _settingsToggle = EditorGUILayout.BeginToggleGroup("Result settings", _settingsToggle);
            _resWidth = EditorGUILayout.IntField("Width", _resWidth);
            _resHeight = EditorGUILayout.IntField("Height", _resHeight);
            _resFormat = (TextureFormat)EditorGUILayout.EnumPopup("Format", _resFormat);
            _resMipChain = EditorGUILayout.Toggle("Mip chain", _resMipChain);
            EditorGUILayout.EndToggleGroup();
            

            if (GUILayout.Button("Combine"))
                DoCombine();

            if (GUILayout.Button("Save"))
                Save(_resTexture);

            EditorGUILayout.BeginHorizontal();
            var r1 = EditorGUILayout.GetControlRect(false, 200);
            r1.width = Mathf.Min(r1.height, r1.width);
            EditorGUI.DrawPreviewTexture(r1, _resTexture);

            var r2 = EditorGUILayout.GetControlRect(false, 200);
            r2.width = Mathf.Min(r2.height, r2.width);
            EditorGUI.DrawTextureAlpha(r2, _resTexture);
            
            EditorGUILayout.EndHorizontal();
            //GUILayout.Label(_resTexture);

            EditorGUILayout.EndVertical();
        }

        private void DoCombine()
        {
            var shader = Shader.Find(CombineShaderName);
            if (shader == null)
            {
                Debug.LogWarningFormat("Can't find shader: {0}", CombineShaderName);
                return;
            }

            var material = new Material(shader);
            material.SetTexture("_TexA", _textureA);
            material.SetTexture("_TexB", _textureB);
            material.color = _color;
            material.SetInt("_rgbMode", (int)_rgbSource);
            material.SetInt("_alphaSource", (int)_aSource);

            if (_resWidth > 0 && _resHeight > 0)
            {
                _resTexture = new RenderTexture(_resWidth, _resHeight, 0, RenderTextureFormat.ARGBFloat);
                Graphics.Blit(null, _resTexture, material);
            }
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

        void Save(RenderTexture rt)
        {
            if (_resTexture == null)
                return;

            var tex = new Texture2D(rt.width, rt.height, _resFormat, _resMipChain);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, true);
            tex.Apply();
            RenderTexture.active = null;

            var path = EditorUtility.SaveFilePanel("Save generated texture", "", "gradient", "png");
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
                AssetDatabase.Refresh();
                //AssetDatabase.ImportAsset(path);
            }
        }
    }
}
