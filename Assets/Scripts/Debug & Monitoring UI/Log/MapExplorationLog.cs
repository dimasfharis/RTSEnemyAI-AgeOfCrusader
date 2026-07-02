using RTS.Core;
using RTS.Managers.Map;
using System.Linq;
using UnityEngine;

namespace RTS.Monitoring.Log
{
    public class MapExplorationLog
    {
        public PlayerInfo playerInfo;
        private MapManager mapManager;

        private float logTimeCreated;

        public int exploredTileCount;
        public int totalTileCount;

        public int exploredResourceNodeCount;
        public int totalResourceNodeCount;

        // can be extended to include enemy buildings and units
        // can be extended to calculate map area possessed by player

        #region Initialization

        public MapExplorationLog(PlayerInfo owner)
        {
            this.playerInfo = owner;
            this.mapManager = playerInfo.MapManager;

            InitExplorationData();
        }

        private void InitExplorationData()
        {
            logTimeCreated = Time.time;

            exploredTileCount = mapManager.GetExploredTiles().Count(x => x.Value > 0f);
            totalTileCount = mapManager.GetExploredTiles().Count;

            exploredResourceNodeCount = mapManager.GetKnownResourceNodes().Count;
            totalResourceNodeCount = playerInfo.GameManager.ResourceNodeManager.GetTotalActiveNodes();
        }

        #endregion

        #region Print Log

        public void PrintLog()
        {
            Debug.Log($"Player {playerInfo.PlayerNumber} - Map Exploration Log:");
            Debug.Log($"Explored Tiles: {exploredTileCount}/{totalTileCount} ({(float)exploredTileCount / totalTileCount * 100f:F2}%)");
            Debug.Log($"Explored Resource Nodes: {exploredResourceNodeCount}/{totalResourceNodeCount} ({(float)exploredResourceNodeCount / totalResourceNodeCount * 100f:F2}%)");
        }

        #endregion
    }
}