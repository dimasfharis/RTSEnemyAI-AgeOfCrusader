using RTS.Core;
using RTS.Data;
using RTS.Monitoring.Log;
using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;

namespace RTS.Managers.Log
{
    public class PlayerLogManager
    {
        PlayerInfo playerInfo;
        DataManager dataManager;

        // Map Exploration Log
        private List<MapExplorationLog> mapExplorationLogs;

        // can add more debugging info such as unit composition, building status, research status, etc.

        // Log Update Time
        private float mapExplorationLogUpdateTime;
        float mapExplorationLogUpdateInterval = 40f;

        // Update Simultaneous Data
        float updateSimultaneousDataTime;
        float updateSimultaneousDataInterval = 3f;

        #region Initialization

        public PlayerLogManager(PlayerInfo owner)
        {
            playerInfo = owner;
            dataManager = playerInfo.DataManager;

            mapExplorationLogs = new List<MapExplorationLog>();
        }

        #endregion

        #region Update Lifecycle

        public void Tick()
        {
            UpdateMapExplorationLog();

            updateSimultaneousDataTime += Time.deltaTime;
            if (updateSimultaneousDataTime > updateSimultaneousDataInterval)
            {
                updateSimultaneousDataTime = 0f;

                UpdateWorldAcknowledgeLog();
            }
        }

        #endregion

        #region Log UI API

        public void LogUnitFeature(string message)
        {
            AIMonitoringUI.Instance.LogUnitFeature(playerInfo.PlayerNumber, message);
        }

        public void LogBuildingFeature(string message)
        {
            AIMonitoringUI.Instance.LogBuildingFeature(playerInfo.PlayerNumber, message);
        }

        #endregion

        #region Simultaneous Data

        public void UpdateWorldAcknowledgeLog()
        {
            // Map explored
            int exploredTiles = dataManager.GetExploredTiles();
            int totalTiles = dataManager.GetTotalTiles();

            // Resource explored
            int knownResource = dataManager.GetKnownResourceNodeCount();
            int totalResource = dataManager.GetTotalResource();

            // Known Enemy Info
            int knownMilitaryUnit = dataManager.GetEstimatedEnemyMilitaryUnitCount();
            int knownWorkerUnit = dataManager.GetEstimatedEnemyWorkerCount();
            int knownBuilding = dataManager.GetEstimatedEnemyBuildingCount();
            int totalKnown = knownMilitaryUnit + knownWorkerUnit + knownBuilding;

            int actualMilitaryUnit = dataManager.GetActualEnemyMilitaryUnitCount();
            int actualWorkerUnit = dataManager.GetActualEnemyWorkerCount();
            int actualBuilding = dataManager.GetActualEnemyBuildingCount();
            int totalActual = actualMilitaryUnit + actualWorkerUnit + actualBuilding;

            AIMonitoringUI.Instance.UpdateWorldAcknowledgeData(playerInfo.PlayerNumber, new Dictionary<string, object>()
            {
                { "Map Explored\n", $"{exploredTiles} : {totalTiles}\n" },
                { "Resource Explored\n", $"{knownResource} : {totalResource}\n" },
                { "Known Enemy Info\n", $"{totalKnown} : {totalActual}\n" },
            });
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