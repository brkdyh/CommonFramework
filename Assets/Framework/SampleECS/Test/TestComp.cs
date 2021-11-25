using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using SampleECS;

[Component]
public struct TestComp
{
    public int test_field;
    public Vector3 position;
    public Transform go;
}
