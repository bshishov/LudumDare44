using UnityEngine;
using System.Collections;
using Assets.Scripts.Data;
using TMPro;

[RequireComponent(typeof(Cheats))]
[ExecuteInEditMode]
public class SpellsPlayground : MonoBehaviour
{
    private Cheats _cheats;

    public SpellsPlaygroundCharacter Caster;
    public CharacterState Enemy;
    public TextMeshPro Text;

    public float Distance = 5;
    public float Spacing = 5;
    public float HalfWidth = 25;



    void Start()
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
            SpawnCasterAndDummy(x, y, root.transform, spell);
            x += Spacing;
            if (x >= HalfWidth)
            {
                y += Distance * 2.5f;
                x = -HalfWidth;
            }
        }
    }

    private void SpawnCasterAndDummy(float x, float y, Transform root, Spell spell)
    {
        var caster = Instantiate(Caster, new Vector3(x, 0, y), Quaternion.identity, root);
        var text = Instantiate(Text, new Vector3(x, 0, y + 1), Quaternion.Euler(80,0,0), root);
        var enemy = Instantiate(Enemy, new Vector3(x , 0, y + Distance), Quaternion.identity, root);

        caster.SpellToCast = spell;
        caster.Target = enemy;
        text.text += spell.Name;
    }
    
    // Update is called once per frame
    void Update()
    {

    }
}