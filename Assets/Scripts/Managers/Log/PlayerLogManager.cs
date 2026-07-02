using RTS.Core;
using RTS.Monitoring.Log;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Managers.Log
{
    public class PlayerLogManager
    {
        PlayerInfo playerInfo;

        // Map Exploration Log
        private List<MapExplorationLog> mapExplorationLogs;

        // can add more debugging info such as unit composition, building status, research status, etc.

        // Log Update Time
        private float mapExplorationLogUpdateTime;
        float mapExplorationLogUpdateInterval = 40f;

        #region Initialization

        public PlayerLogManager(PlayerInfo owner)
        {
            playerInfo = owner;

            mapExplorationLogs = new List<MapExplorationLog>();
        }

        #endregion

        #region Update Lifecycle

        public void Tick()
        {
            UpdateMapExplorationLog();
        }

        #endregion

        #region Map Exploration Log

        private void UpdateMapExplorationLog()
        {
            mapExplorationLogUpdateTime += Time.deltaTime;

            if (mapExplorationLogUpdateTime >= mapExplorationLogUpdateInterval)
            {
                mapExplorationLogUpdateTime = 0f;

                GenerateMapExplorationLog();
            }
        }

        public MapExplorationLog GenerateMapExplorationLog()
        {
            MapExplorationLog log = new MapExplorationLog(playerInfo);

            mapExplorationLogs.Add(log);

            return log;
        }

        public MapExplorationLog PrintMapExplorationLog()
        {
            MapExplorationLog log = mapExplorationLogs[mapExplorationLogs.Count - 1];

            log.PrintLog();

            return log;
        }

        #endregion
    }
}