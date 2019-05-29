using Actors;
using UnityEngine;

public class CheatSpellSpawner : MonoBehaviour
{
    public GameObject Prefab;
    public float Padding = 1f;
    public int ItemsInRow = 10;
    private Cheats _cheats;

    void Start()
    {
        _cheats = FindObjectOfType<Cheats>();
        var idx = 0;
        foreach (var cheatsSpell in _cheats.Spells)
        {
            var col = idx % ItemsInRow;
            var row = idx / ItemsInRow;
            var pos = new Vector3(col, 0, row) * Padding;


            var go = GameObject.Instantiate(Prefab, transform.position + pos, Quaternion.identity);
            if (go != null)
            {
                var dSpell = go.GetComponent<DroppedSpell>();
                if (dSpell != null)
                {
                    dSpell.Setup(cheatsSpell, 1);
                }
                go.transform.SetParent(transform, true);
                idx++;
            }
        }
    }
}
