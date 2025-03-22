using UnityEngine;

namespace DungeonGeneration.Data
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Data/DungeonGenerationSettings", fileName = "_DGSettings")]
    public class DungeonGenerationSettingsSo : ScriptableObject
    {
        [Header("Size")]
        public RectInt DungeonParameters;
        public Vector2Int MinimalRoomSize;
        public int WallWidth;
        public Vector2Int DoorSize;
        
        [Header("Randomness")]
        public int Seed;
        public Vector2 RandomnessBoundaries;
        
        [Header("Visual debugging")]
        public bool DebugRooms;
        public bool DebugWalls;
        public bool DebugConnections;
        public int AwaitTime = 10;
        
        [Header("Other")]
        public float RoomsRemovePercentage;
    }
}