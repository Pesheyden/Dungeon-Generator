using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DungeonGeneration.Graph;
using NaughtyAttributes;
using UnityEngine;

public enum WallDebugMode {EachTile,Line,None }
public class DungeonFiller : MonoBehaviour
{
    [SerializeField] private GameObject[] _debugPrefabs;
    [SerializeField] private int _floodFillAwaitTime = 1; 
    [SerializeField] private int _wallCreationAwaitTime = 1;
    [SerializeField] private WallDebugMode _wallDebugMode;
    
    [SerializeField] private GameObject[] _tilesPrefabs;
    private Graph _dungeonGraph;
    private List<Transform> _dungeonRoomsTransforms = new List<Transform>();

    private int[,] _dungeonTileMap;
    private int[,] _dungeonRoomsMap;

    [Button]
    public void CreteTileMap()
    {
        _dungeonGraph = DungeonGenerator.Instance.Graph;
        _dungeonTileMap = new int[DungeonGenerator.Instance.Settings.DungeonParameters.height,
            DungeonGenerator.Instance.Settings.DungeonParameters.width];
        _dungeonRoomsMap = new int[DungeonGenerator.Instance.Settings.DungeonParameters.height,
            DungeonGenerator.Instance.Settings.DungeonParameters.width];
        var dungeonRooms = _dungeonGraph.GetRooms();

        //Fill 
        for (int y = 0; y < _dungeonTileMap.GetLength(0); y++)
        {
            for (int x = 0; x < _dungeonTileMap.GetLength(1); x++)
            {
                _dungeonTileMap[y, x] = -1;
            }
        }

        for (int i = 0; i < dungeonRooms.Count; i++)
        {
            AlgorithmsUtils.FillRectangle(_dungeonTileMap, dungeonRooms[i].Dimensions, 0);
            AlgorithmsUtils.FillRectangle(_dungeonRoomsMap, dungeonRooms[i].Dimensions, i);
            AlgorithmsUtils.FillRectangleOutline(_dungeonTileMap, dungeonRooms[i].Dimensions, 1, DungeonGenerator.Instance.Settings.WallWidth);
            
        }

        foreach (var door in _dungeonGraph.GetDoors())
        {
            AlgorithmsUtils.FillRectangle(_dungeonTileMap, door.Dimensions, 0);
        }

        DebugTileMap(true,false);
    }
    [Button]
    public async void FillTheDungeon()
    {
        CreateRoomsTransforms();
        await FloodFillFloor();
        await SpawnWalls();
    }

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
    
    private async Task FloodFillFloor()
    {
        var startPoint = _dungeonGraph.GetRooms()[0].GetCenter();
        startPoint = new Vector3(Mathf.Round(startPoint.x), startPoint.y, Mathf.Round(startPoint.z));
        List<Vector3> discovered =new List<Vector3>();
        await Fill(startPoint,discovered);
        
    }

    private async Task Fill(Vector3 point,List<Vector3> discovered)
    {
        Debug.Log(point);
        if (point.z > _dungeonTileMap.GetLength(0) ||
            point.x > _dungeonTileMap.GetLength(1) || discovered.Contains(point))
            return;

        
        var tile = _dungeonTileMap[(int)point.z, (int)point.x];
        discovered.Add(point);

        if (tile != 0)
            return;

        var floor = Instantiate(_tilesPrefabs[0], point, Quaternion.identity);
        floor.transform.parent = _dungeonRoomsTransforms[_dungeonRoomsMap[(int)point.z, (int)point.x]];
        floor.name = $"Floor_{point.x},{point.z}";

        await Task.Delay(_floodFillAwaitTime);
        List<Task> tasks = new List<Task>();
        tasks.Add(Fill(new Vector3(point.x + 1,point.y,point.z),discovered));
        tasks.Add(Fill(new Vector3(point.x - 1,point.y,point.z),discovered));
        tasks.Add(Fill(new Vector3(point.x,point.y,point.z + 1),discovered));
        tasks.Add(Fill(new Vector3(point.x,point.y,point.z - 1),discovered));

        while (!LoadingIsDone(tasks))
        {
            await Task.Delay(1);
        }
        
        
        
        bool LoadingIsDone(List<Task> tasks)
        {
            bool isDone = false;
            for (int i = 0; i < tasks.Count; i++)
            {
                isDone = tasks[i].IsCompleted;
                if (!isDone)
                {
                    break;
                }
            }
            return isDone;
        }
    }
    
    private async Task SpawnWalls()
    {
        for (int y = 1; y < _dungeonTileMap.GetLength(0) - 1; y++)
        {
            for (int x = 1; x < _dungeonTileMap.GetLength(1) - 1; x++)
            {
                if(_dungeonTileMap[y, x] < 0 || _dungeonTileMap[y - 1, x] < 0 || _dungeonTileMap[y, x - 1] < 0 || _dungeonTileMap[y - 1, x - 1] < 0)
                    continue;
                
                int index = Mathf.Clamp(_dungeonTileMap[y, x], 0, 1) * 2 +
                            Mathf.Clamp(_dungeonTileMap[y - 1, x], 0, 1) * 1 +
                            Mathf.Clamp(_dungeonTileMap[y, x - 1], 0, 1) * 4 +
                            Mathf.Clamp(_dungeonTileMap[y - 1, x - 1], 0, 1) * 8;
                Vector3 position = new Vector3(x - .5f, _dungeonRoomsTransforms[_dungeonRoomsMap[y, x]].position.y, y - .5f);
                
                var tr = Instantiate(_tilesPrefabs[index], position, Quaternion.identity).transform;
                tr.parent = _dungeonRoomsTransforms[_dungeonRoomsMap[y, x]];
                tr.name = $"{_tilesPrefabs[index].name}_{x},{y}_{_dungeonTileMap[y, x]} 2,{_dungeonTileMap[y - 1, x]} 1,{_dungeonTileMap[y, x - 1]} 4,{_dungeonTileMap[y - 1, x - 1]} 8";
                if(_wallDebugMode == WallDebugMode.EachTile)
                    await Task.Delay(_wallCreationAwaitTime);
            }
            if(_wallDebugMode == WallDebugMode.Line)
                await Task.Delay(_wallCreationAwaitTime);
        }
    }
    
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
                string o;
                switch (_dungeonTileMap[i, j])
                {
                    case -1:
                        o = "@";
                        break;
                    case 1:
                        o = "#";
                        break;
                    default:
                        o = _dungeonTileMap[i, j].ToString();
                        break;
                }
                
                sb.Append(o);
            }
            sb.AppendLine();
        }
    
        Debug.Log(sb.ToString());
        
        if(!spawnAssets)
            return;

        for (int y = 0; y < _dungeonTileMap.GetLength(0); y++)
        {
            for (int x = 0; x < _dungeonTileMap.GetLength(1); x++)
            {
                int index = _dungeonTileMap[y, x] < 0 ? 0 : _dungeonTileMap[y, x];
                Vector3 position = new Vector3(x, 0, y);
                var tr = Instantiate(_debugPrefabs[index], position, Quaternion.identity).transform;
            }
        }
    }
}
