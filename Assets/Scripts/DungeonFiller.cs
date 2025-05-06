using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using DungeonGeneration.Graph;
using NaughtyAttributes;
using UnityEngine;

public class DungeonFiller : MonoBehaviour
{
    [SerializeField]
    private GameObject[] _tilesPrefabs;
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

        DebugTileMap(true);
    }
    [Button]
    public async void FillTheDungeon()
    {
        CreateRoomsTransforms();
        await FloodFillFloor();
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

        await Task.Delay(1);
        Fill(new Vector3(point.x + 1,point.y,point.z),discovered);
        Fill(new Vector3(point.x - 1,point.y,point.z),discovered);
        Fill(new Vector3(point.x,point.y,point.z + 1),discovered);
        Fill(new Vector3(point.x,point.y,point.z - 1),discovered);
        
    }
    
    public void DebugTileMap(bool flip)
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
    }
}
