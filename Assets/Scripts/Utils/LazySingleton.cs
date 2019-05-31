using UnityEngine;

namespace Utils
{
    public class LazySingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        protected static T _instance;

        // Returns the instance of this singleton
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = (T)FindObjectOfType(typeof(T));

                    if (_instance == null)
                    {
                        var obj = new GameObject($"[LazySingleton] {typeof(T).Name}");
                        _instance = obj.AddComponent<T>();
                        return _instance;
                    }
                }

                return _instance;
            }
        }
    }
}
