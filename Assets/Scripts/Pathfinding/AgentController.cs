using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

public class AgentController : MonoBehaviour
{
    public List<Vector3> Path;
    [SerializeField] private float _moveSpeed;
    private bool _isMoving;
    
    [Button(enabledMode: EButtonEnableMode.Playmode)]
    public void DebugPath() => DebugDrawingBatcher.ReversePauseGroup("Path");
    
    [Button(enabledMode: EButtonEnableMode.Playmode)]
    public void DebugPathFindingGraph() => DebugDrawingBatcher.ReversePauseGroup("PathFindingGraph");
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        DebugDrawingBatcher.ReversePauseGroup("PathFindingGraph");
        DebugDrawingBatcher.BatchCall("Path", () =>
        {
            foreach (var point in Path)
            {
                DebugExtension.DebugWireSphere(point, Color.red, 0.5f);
            }
        });
    }
    
    public void GotoDestination(Vector3 destination)
    {
        if(_isMoving)
            return;
        
        var createdPath = PathFinder.FindPath(transform.position, destination);
        if(createdPath == null)
            return;
        Path = createdPath;
        
        StartCoroutine(FollowPathCoroutine(Path));
    }
    
    private IEnumerator FollowPathCoroutine(List<Vector3> path)
    {
        if (path == null || path.Count == 0)
        {
            Debug.Log("No path found");
            yield break;
        }
        _isMoving = true;
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 target = path[i];
            // Move towards the target position
            while (Vector3.Distance(transform.position, target) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * _moveSpeed);
                yield return null;
            }
        }
        _isMoving = false;
    }
}
