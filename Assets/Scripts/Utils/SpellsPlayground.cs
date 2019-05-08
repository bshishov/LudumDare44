using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Cheats))]
[ExecuteInEditMode]
public class SpellsPlayground : MonoBehaviour
{
    private Cheats _cheats;

    public GameObject Caster;
    public GameObject Enemy;

    public float Distance = 5;
    public float Spacing = 5;
    public float HalfWidth = 25;



#if UNITY_EDITOR

    [ContextMenu("Spawn")]
    void Spawn()
    {
        var cheats = GetComponent<Cheats>();

        {
            var oldRoot = gameObject.transform.Find("SpawnedMobs");
            if (oldRoot != null)
                DestroyImmediate(oldRoot.gameObject);
        }

        var root = new GameObject("SpawnedMobs");
        root.transform.SetParent(gameObject.transform, false);

        var x = -HalfWidth;
        float y = 0;
        foreach (var spell in cheats.Spells)
        {
            SpawnCasterAndDummy(root.transform, x, y);
            x += Spacing;
            if (x >= HalfWidth)
            {
                y += Distance * 2.5f;
                x = -HalfWidth;
            }
        }
    }

    private void SpawnCasterAndDummy(Transform root, float x, float y)
    {
        Instantiate(Caster, new Vector3(y, 0, x), Quaternion.identity, root);
        Instantiate(Enemy, new Vector3(y + Distance, 0, x), Quaternion.identity, root);
    }

#endif

    // Update is called once per frame
    void Update()
    {

    }
}
