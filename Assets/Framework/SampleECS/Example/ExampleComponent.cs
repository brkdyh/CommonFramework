using UnityEngine;
using SampleECS;

[System.Obsolete]
[Component]
public struct TestComp
{

}

[Component]
public struct IDComp
{
    public int id;
}

[Component]
public struct PositionComp
{
    public Vector3 position;
}

[Component]
public struct TransformComp
{
    public Transform transform;
}

[Component]
public struct CreateComp
{
    public int create_count;
}
