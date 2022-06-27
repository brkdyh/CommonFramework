using UnityEngine;
using SampleECS;

[System.Obsolete]
[Component("Game")]
public struct TestComp
{

}

[Component("Game")]
public struct IDComp
{
    public int id;
}

[Component("Game")]
public struct PositionComp
{
    public Vector3 position;
}

[Component("Game")]
public struct TransformComp
{
    public Transform transform;
}

[Component("Game")]
public struct CreateComp
{
    public int create_count;
}
