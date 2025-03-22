using System;
using UnityEditor;
using UnityEngine;

namespace DungeonGeneration.Data
{
    [CreateAssetMenu(menuName = "ScriptableObjects/Data/DungeonGenerationSettings", fileName = "_DGSettings")]
    [Serializable]
    public class DungeonGenerationSettingsSo : ScriptableObject
    {
        [Header("Properties")]
        public RectInt DungeonParameters;
        public Vector2Int MinimalRoomSize;
        public int WallWidth;
        public Vector2Int DoorSize;
        public float RoomsRemovePercentage;
        
        [Header("Randomness")]
        public int Seed;
        public Vector2 RandomnessBoundaries;

        [Header("Visual debugging")] 
        public int RoomGenerationAwait = 10;
        public int GraphGenerationAwait = 10;
        public int DoorsGenerationAwait = 10;
        public int GraphFilteringAwait = 10;
    }
}