using UnityEngine;


public class SpellEmitter : MonoBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {

    }

    // Update is called once per frame
    private void Update()
    {

    }

    internal SpellEmitterData GetData(GameObject owner, Ray ray, Vector3 hitPoint) => 
        new SpellEmitterData {
            owner = owner,
            emitter = this,
            ray = ray,
            floorIntercection = hitPoint
        };
}
