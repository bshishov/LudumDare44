using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct InputComponent : IComponentData
{
    public float Horisontal;
    public float Vertical;
}
