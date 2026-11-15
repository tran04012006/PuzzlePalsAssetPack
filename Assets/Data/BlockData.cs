using UnityEngine;

[CreateAssetMenu(fileName = "BlockData",
    menuName = "Scriptable Objects/BlockData")]

public class BlockData : ScriptableObject
{
    public string name;
    public Sprite s;
}
