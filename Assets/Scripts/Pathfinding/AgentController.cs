using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using Utilities;

/// <summary>
/// Controls the agent's movement along a calculated path using a pathfinding system.
/// </summary>
public class AgentController : MonoBehaviour
{
    /// <summary>
    /// The list of waypoints that define the path for the agent.
    /// </summary>
    public List<Vector3> Path;

    [SerializeField] private float _moveSpeed;
    [SerializeField] private PathFindingType _pathFindingType;
    [SerializeField] private NavMeshSurface _navMeshSurface;


    private bool _isMoving;
    private NavMeshAgent _agent;

    /// <summary>
    /// Toggles debug visualization for the agent's current path using the debug batcher.
    /// </summary>
    [Button(enabledMode: EButtonEnableMode.Playmode)]
    public void DebugPath() => DebugDrawingBatcher.ReversePauseGroup("Path");

    /// <summary>
    /// Toggles debug visualization for the pathfinding graph structure.
    /// </summary>
    [Button(enabledMode: EButtonEnableMode.Playmode)]
    public void DebugPathFindingGraph() => DebugDrawingBatcher.ReversePauseGroup("PathFindingGraph");

    /// <summary>
    /// Toggles debug visualization for discovered points during pathfinding.
    /// </summary>
    [Button(enabledMode: EButtonEnableMode.Playmode)]
    public void DebugPathFindingDiscoveredPoint() => DebugDrawingBatcher.ReversePauseGroup("PathFindingDiscoveredPoints");

    /// <summary>
    /// Initializes debug drawing for the path and pathfinding graph at startup.
    /// </summary>
    private void Start()
    {
        // Pauses pathfinding graph visualization
        DebugDrawingBatcher.ReversePauseGroup("PathFindingGraph");

        // Draws debug spheres along the path if defined
        DebugDrawingBatcher.BatchCall("Path", () =>
        {
            foreach (var point in Path)
            {
                DebugExtension.DebugWireSphere(point, Color.red, 0.5f);
            }
        });
    }

    /// <summary>
    /// Teleports the agent to the player spawn point defined in the dungeon.
    /// </summary>
    public void InitializePlayer()
    {
        _navMeshSurface.BuildNavMesh();
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = _moveSpeed;
        transform.position = DungeonFiller.Instance.PlayerSpawnPoint;
    }

    /// <summary>
    /// Commands the agent to move toward the given destination using a pathfinding algorithm.
    /// </summary>
    /// <param name="destination">The target position the agent should navigate to.</param>
    public void GotoDestination(Vector3 destination)
    {
        // Ignore command if already moving
        if (_isMoving)
            return;

        // Attempt to generate a path to the destination
        if (_pathFindingType != PathFindingType.NavMesh)
        {
            _agent.enabled = false;
            var createdPath = PathFinder.FindPath(transform.position, destination, _pathFindingType);
            if (createdPath == null)
                return;
            Path = createdPath;
            
            // Start the coroutine that follows the path
            StartCoroutine(FollowPathCoroutine(Path));
            return;
        }

        _agent.enabled = true;
        _agent.SetDestination(destination);
    }

    /// <summary>
    /// Coroutine that moves the agent step-by-step along a path of waypoints.
    /// </summary>
    /// <param name="path">List of positions the agent should follow.</param>
    /// <returns>IEnumerator for coroutine execution.</returns>
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

            // Move toward the current target point until close enough
            while (Vector3.Distance(transform.position, target) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, Time.deltaTime * _moveSpeed);
                yield return null;
            }
        }

        _isMoving = false;
    }
}
