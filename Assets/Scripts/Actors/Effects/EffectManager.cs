using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEffect
{
}

public interface ITargetedEffect : IEffect
{
    void Setup(Transform source, Transform target);
}

public interface IAttachedEffect : IEffect
{
    void Setup(Transform transform);
}

public class EffectManager : MonoBehaviour
{
    void Start()
    {
    }
    
    void Update()
    {
    }

    public void ApplyEffect(GameObject effectPrefab)
    {
        var effectObject = GameObject.Instantiate(effectPrefab, transform.position, Quaternion.identity);
    }
}
