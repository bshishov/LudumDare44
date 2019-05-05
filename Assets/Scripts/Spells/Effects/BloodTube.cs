using UnityEngine;

namespace Spells.Effects {
[RequireComponent(typeof(LineRenderer))]
public class BloodTube : MonoBehaviour
{
    public  int          Points   = 10;
    private LineRenderer _renderer;

    public void Start()
    {
        _renderer = GetComponent<LineRenderer>();
    }

    public void SetupLine(Vector3 from, Vector3 to)
    {
        var positions = new Vector3[Points];
        for (var i = 0; i < Points; i++)
        {
            positions[i] = Vector3.Lerp(from, to, i / (Points - 1f));
        }

        _renderer.positionCount = Points;
        _renderer.SetPositions(positions);
    }

    public void SetupLine(Transform from, Transform to) { SetupLine(from.position, to.position); }

    public void SetupDefaultDebug() { SetupLine(transform.position, transform.position + Vector3.forward * 5f); }
}
}