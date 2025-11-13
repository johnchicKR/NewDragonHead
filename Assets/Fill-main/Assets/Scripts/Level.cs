using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level_New", menuName = "FillGame/Level", order = 0)]
public class Level : ScriptableObject
{
    public int Row;
    public int Col;
    public List<int> Data;
}
