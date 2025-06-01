using UnityEngine;
public enum WallDebugMode
{
    EachTile,
    Line,
    None
}

[CreateAssetMenu(fileName = "DungeonFillerSo", menuName = "ScriptableObjects/Data/DungeonFillerSo")]
public class DungeonFillingSettingsSo : ScriptableObject
{
    public int WallCreationAwaitTime = 1;
    public int FloodFillAwaitTime = 1;
    public WallDebugMode WallDebugMode;
    public int MinimalHeightForColumn;
    public int MinimalWidthForColumn;
    public int ColumnsSize;
    public GameObject[] TilesPrefabs;
    public GameObject[] DebugPrefabs;
}
