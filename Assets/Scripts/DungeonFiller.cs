using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DungeonGeneration.Graph;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Events;
using Utilities;

/// <summary>
/// Debug mode options for visualizing wall generation.
/// </summary>


/// <summary>
/// Handles the population of a dungeon graph into a tile-based representation,
/// including room filling, wall spawning, and debug visualization.
/// </summary>
public class DungeonFiller : Singleton<DungeonFiller>
{
    [Popup] [Tooltip("Settings config of the dungeon filler. To edit use ctrl + left click")]
    [SerializeField] private DungeonFillingSettingsSo _settings;

    public UnityEvent OnDungeonFilled;

    private Graph _dungeonGraph;
    private List<Transform> _dungeonRoomsTransforms = new List<Transform>();
    private int[,] _dungeonTileMap;
    private int[,] _dungeonRoomsMap;
    [HideInInspector]
    public Vector3 PlayerSpawnPoint;

    #region Debug

    [Button(enabledMode: EButtonEnableMode.Playmode)]
    public void DebugTileMap() => DebugTileMap(true, false);

    [Button(enabledMode: EButtonEnableMode.Playmode)]
    public void DebugTileMapWithAssets() => DebugTileMap(true, true);

    #endregion



    /// <summary>
    /// Initiates the dungeon filling process:
    /// tile mapping, room transforms, floor and wall spawning.
    /// </summary>
    [Button]
    public async void FillTheDungeon()
    {
        Initialize();
        CreteTileMap();
        PathFinder.Initialize(_dungeonTileMap, _dungeonRoomsMap, _dungeonGraph);
        CreateRoomsTransforms();
        await FloodFillFloor();
        await SpawnWalls();
        OnDungeonFilled?.Invoke();
    }
    private void Initialize()
    {
        _dungeonGraph = DungeonGenerator.Instance.Graph;
        _dungeonTileMap = new int[
            DungeonGenerator.Instance.Settings.DungeonParameters.height,
            DungeonGenerator.Instance.Settings.DungeonParameters.width];

        _dungeonRoomsMap = new int[
            DungeonGenerator.Instance.Settings.DungeonParameters.height,
            DungeonGenerator.Instance.Settings.DungeonParameters.width];

        DungeonGenerator.Instance.OnDungeonCreated.AddListener(FillTheDungeon);
    }

    /// <summary>
    /// Fills the tile map based on the dungeon graph structure.
    /// Marks floors, walls, and doors.
    /// </summary>
    public void CreteTileMap()
    {
        var dungeonRooms = _dungeonGraph.GetRooms();

        // Initialize map to default value
        for (int y = 0; y < _dungeonTileMap.GetLength(0); y++)
            for (int x = 0; x < _dungeonTileMap.GetLength(1); x++)
                _dungeonTileMap[y, x] = -1;

        // Fill the map
        for (int i = 0; i < dungeonRooms.Count; i++)
        {
            AlgorithmsUtils.FillRectangle(_dungeonTileMap, dungeonRooms[i].Dimensions, 0);
            AlgorithmsUtils.FillRectangle(_dungeonRoomsMap, dungeonRooms[i].Dimensions, i);
            AlgorithmsUtils.FillRectangleOutline(_dungeonTileMap, dungeonRooms[i].Dimensions, 1,
                DungeonGenerator.Instance.Settings.WallWidth);

            //Add columns
            if (dungeonRooms[i].Dimensions.height > _settings.MinimalHeightForColumn &&
                dungeonRooms[i].Dimensions.width > _settings.MinimalWidthForColumn)
            {
                var columns = CreateColumnsFromRoom(dungeonRooms[i].Dimensions);
                foreach (var column in columns)
                    AlgorithmsUtils.FillRectangle(_dungeonTileMap, column, 1);
            }
        }

        //Add doors
        foreach (var door in _dungeonGraph.GetDoors())
            AlgorithmsUtils.FillRectangle(_dungeonTileMap, door.Dimensions, 0);
    }

    /// <summary>
    /// Creates support columns inside a room based on its dimensions.
    /// </summary>
    private List<RectInt> CreateColumnsFromRoom(RectInt d)
    {
        List<RectInt> columns = new List<RectInt>();
        int columnH = Mathf.CeilToInt((float)(d.height - DungeonGenerator.Instance.Settings.WallWidth * 2) / _settings.MinimalHeightForColumn);
        int columnW = Mathf.CeilToInt((float)(d.width - DungeonGenerator.Instance.Settings.WallWidth * 2) / _settings.MinimalWidthForColumn);
        if (columnH <= 0 || columnW <= 0) return columns;

        float distanceH = (float)(d.height - DungeonGenerator.Instance.Settings.WallWidth * 2) / (columnH + 1);
        float distanceW = (float)(d.width - DungeonGenerator.Instance.Settings.WallWidth * 2) / (columnW + 1);

        for (int i = 1; i <= columnH; i++)
        {
            for (int j = 1; j <= columnW; j++)
            {
                RectInt newColumn = new RectInt
                {
                    height = _settings.ColumnsSize,
                    width = _settings.ColumnsSize,
                    x = Mathf.RoundToInt(d.x + distanceW * j - (_settings.ColumnsSize - 1) + DungeonGenerator.Instance.Settings.WallWidth),
                    y = Mathf.RoundToInt(d.y + distanceH * i - (_settings.ColumnsSize - 1) + DungeonGenerator.Instance.Settings.WallWidth)
                };
                columns.Add(newColumn);
            }
        }

        return columns;
    }

    /// <summary>
    /// Instantiates room GameObjects to hold generated tiles.
    /// </summary>
    private void CreateRoomsTransforms()
    {
        foreach (var room in _dungeonGraph.GetRooms())
        {
            var tr = new GameObject($"Room_{room.Dimensions.x},{room.Dimensions.y}").transform;
            tr.parent = transform;
            tr.position = room.GetCenter();
            _dungeonRoomsTransforms.Add(tr);
        }
    }

    /// <summary>
    /// Starts the flood-fill algorithm from the first room to instantiate floor tiles.
    /// </summary>
    private async Task FloodFillFloor()
    {
        var startPoint = _dungeonGraph.GetRooms()[0].GetCenter();
        var tile = _dungeonTileMap[(int)startPoint.z, (int)startPoint.x];

        // Get fill start point
        if (tile != 0)
        {
            var roomDim = _dungeonGraph.GetRooms()[0].Dimensions;
            startPoint = new Vector3(roomDim.x, startPoint.y, roomDim.y);

            while (true)
            {
                tile = _dungeonTileMap[(int)startPoint.z, (int)startPoint.x];
                if (tile == 0) break;
                if (startPoint.x < roomDim.x + roomDim.width - 1)
                {
                    startPoint.x += 1;
                    continue;
                }

                if (startPoint.z < roomDim.y + roomDim.height - 1)
                {
                    startPoint.z += 1;
                }
            }
        }

        startPoint = new Vector3((int)startPoint.x, startPoint.y, (int)startPoint.z);
        List<Vector3> discovered = new List<Vector3>();
        PlayerSpawnPoint = new Vector3(startPoint.x + 0.5f, startPoint.y, startPoint.z +0.5f);
        
        await Fill(startPoint, discovered);
    }

    /// <summary>
    /// Recursively flood-fills tiles to place floor prefabs.
    /// </summary>
    private async Task Fill(Vector3 point, List<Vector3> discovered)
    {
        await Task.Delay(_settings.FloodFillAwaitTime, Application.exitCancellationToken);
        if (point.z >= _dungeonTileMap.GetLength(0) ||
            point.x >= _dungeonTileMap.GetLength(1) || discovered.Contains(point)) return;

        var tile = _dungeonTileMap[(int)point.z, (int)point.x];
        discovered.Add(point);
        if (tile != 0) return;

        var floor = Instantiate(_settings.TilesPrefabs[0], point, Quaternion.identity);
        floor.transform.parent = _dungeonRoomsTransforms[_dungeonRoomsMap[(int)point.z, (int)point.x]];
        floor.name = $"Floor_{point.x},{point.z}";
        
        List<Task> tasks = new List<Task>
        {
            Fill(new Vector3(point.x + 1, point.y, point.z), discovered),
            Fill(new Vector3(point.x - 1, point.y, point.z), discovered),
            Fill(new Vector3(point.x, point.y, point.z + 1), discovered),
            Fill(new Vector3(point.x, point.y, point.z - 1), discovered)
        };

        while (!LoadingIsDone(tasks))
            await Task.Delay(1, Application.exitCancellationToken);

        bool LoadingIsDone(List<Task> ts)
        {
            foreach (var t in ts)
                if (!t.IsCompleted) return false;
            return true;
        }
    }

    /// <summary>
    /// Spawns wall tiles based on tile map transitions using bitmask index.
    /// </summary>
    private async Task SpawnWalls()
    {
        for (int y = 1; y < _dungeonTileMap.GetLength(0); y++)
        {
            for (int x = 1; x < _dungeonTileMap.GetLength(1); x++)
            {
                if (_dungeonTileMap[y, x] < 0 || _dungeonTileMap[y - 1, x] < 0 ||
                    _dungeonTileMap[y, x - 1] < 0 || _dungeonTileMap[y - 1, x - 1] < 0) continue;

                // Calculate wall index
                int index = _dungeonTileMap[y, x] * 2 +
                            _dungeonTileMap[y - 1, x] * 1 +
                            _dungeonTileMap[y, x - 1] * 4 +
                            _dungeonTileMap[y - 1, x - 1] * 8;
                if (index == 0) continue;

                // Instantiate prefab
                Vector3 position = new Vector3(x - .5f, _dungeonRoomsTransforms[_dungeonRoomsMap[y, x]].position.y, y - .5f);
                var tr = Instantiate(_settings.TilesPrefabs[index], position, Quaternion.identity).transform;
                tr.parent = _dungeonRoomsTransforms[_dungeonRoomsMap[y, x]];
                tr.name = $"{_settings.TilesPrefabs[index].name}_{x},{y}";

                if (_settings.WallDebugMode == WallDebugMode.EachTile)
                    await Task.Delay(_settings.WallCreationAwaitTime, Application.exitCancellationToken);
            }

            if (_settings.WallDebugMode == WallDebugMode.Line)
                await Task.Delay(_settings.WallCreationAwaitTime, Application.exitCancellationToken);
        }
    }

    /// <summary>
    /// Outputs the current dungeon tile map to the console.
    /// Optionally spawns debug prefabs at tile locations.
    /// </summary>
    public void DebugTileMap(bool flip, bool spawnAssets)
    {
        int rows = _dungeonTileMap.GetLength(0);
        int cols = _dungeonTileMap.GetLength(1);
        var sb = new StringBuilder();

        int start = flip ? rows - 1 : 0;
        int end = flip ? -1 : rows;
        int step = flip ? -1 : 1;

        for (int i = start; i != end; i += step)
        {
            for (int j = 0; j < cols; j++)
            {
                string o = _dungeonTileMap[i, j] switch
                {
                    -1 => "@",
                    1 => "#",
                    _ => _dungeonTileMap[i, j].ToString()
                };
                sb.Append(o);
            }

            sb.AppendLine();
        }

        Debug.Log(sb.ToString());

        if (!spawnAssets) return;

        for (int y = 0; y < _dungeonTileMap.GetLength(0); y++)
        {
            for (int x = 0; x < _dungeonTileMap.GetLength(1); x++)
            {
                int index = _dungeonTileMap[y, x] < 0 ? 0 : _dungeonTileMap[y, x];
                Vector3 position = new Vector3(x, 0, y);
                Instantiate(_settings.DebugPrefabs[index], position, Quaternion.identity);
            }
        }
    }
}
