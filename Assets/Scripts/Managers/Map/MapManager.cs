using RTS.Buildings.Common;
using RTS.Common.Enums;
using RTS.Core;
using RTS.Data.StrategicData;
using RTS.Units.Common;
using RTS.World.ResourceNodeManagement;
using RTS.World.WorldManagement;
using System.Collections.Generic;
using UnityEngine;

namespace RTS.Managers.Map
{
    public class MapManager
    {
        // References
        public PlayerInfo playerInfo;
        public TileDatabase tileDatabase;

        // Memory
        private Dictionary<Vector3, ResourceNodeMemory> knownResourceNodes;
        private Dictionary<Vector3, EnemyBuildingMemory> knownEnemyBuildings;
        private Dictionary<Vector3, EnemyUnitMemory> knownEnemyUnits;
        private Vector3 knownEnemyBaseLocation;

        // Map Exploration
        private Dictionary<Vector2Int, float> exploredTiles;

        #region Initialization

        public MapManager(PlayerInfo owner)
        {
            this.playerInfo = owner;
            tileDatabase = playerInfo.GameManager.WorldManager.tileDatabase;

            knownResourceNodes = new Dictionary<Vector3, ResourceNodeMemory>();
            knownEnemyBuildings = new Dictionary<Vector3, EnemyBuildingMemory>();
            knownEnemyUnits = new Dictionary<Vector3, EnemyUnitMemory>();

            GetEnemyStarterBaseLocation();
            InitializeExploredTiles();
        }

        #endregion

        #region Initialization Settings

        private void GetEnemyStarterBaseLocation()
        {
            List<PlayerInfo> opponents = playerInfo.GameManager.GetOpponentPlayerInfo(playerInfo.PlayerNumber);

            foreach (var opponent in opponents)
            {
                knownEnemyBaseLocation = opponent.GameManager.GetStarterBaseLocation(opponent.PlayerNumber);
            }
        }

        private void InitializeExploredTiles()
        {
            exploredTiles = new Dictionary<Vector2Int, float>();

            int mapWidth = tileDatabase.GetGroundTilemap().size.x;
            int mapHeight = tileDatabase.GetGroundTilemap().size.y;
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    exploredTiles[new Vector2Int(x, y)] = 0f;
                }
            }
        }

        #endregion

        #region Map Exploration

        public List<Vector2Int> UpdateExploredTiles(Vector3 position, float radius)
        {
            List<Vector2Int> tilesInRadius = tileDatabase.GetGroundTilemapInRadius(position, radius);

            List<Vector2Int> newlyExploredTiles = new List<Vector2Int>();

            foreach (var tile in tilesInRadius)
            {
                if (exploredTiles.ContainsKey(tile) && exploredTiles[tile] == 0f)
                {
                    newlyExploredTiles.Add(tile);
                }

                exploredTiles[tile] = Time.time;
            }

            return newlyExploredTiles;
        }

        #endregion

        #region Registration & Unregistration

        private void RegisterResourceNodeSeen(ResourceType type, Vector3 knownPosition)
        {
            var memory = new ResourceNodeMemory(type);
            knownResourceNodes.Add(knownPosition, memory);
        }

        private void RegisterEnemyBuildingSeen(BuildingType type, Vector3 knownPosition)
        {
            var memory = new EnemyBuildingMemory(type);
            knownEnemyBuildings.Add(knownPosition, memory);
        }

        private void RegisterEnemyUnitSeen(UnitType type, Vector3 knownPosition)
        {
            var memory = new EnemyUnitMemory(type);
            knownEnemyUnits.Add(knownPosition, memory);
        }

        private void RegisterEnemyBaseLocation(Vector3 position)
        {
            if (knownEnemyBaseLocation != position)
            {
                knownEnemyBaseLocation = position;
            }
        }

        private void UnregisterEmptyResourceNode()
        {
            List<Vector3> emptyNodes = new List<Vector3>();

            foreach (var node in knownResourceNodes)
            {
                ResourceNode worldNode = playerInfo.ResourceNodeManager.GetResourceNodeAtPosition(node.Key);

                if (worldNode == null)
                {
                    emptyNodes.Add(node.Key);
                }
            }

            foreach (var emptyNode in emptyNodes)
            {
                knownResourceNodes.Remove(emptyNode);
            }
        }

        private void UnregisterEmptyBuilding(PlayerInfo opponentPlayerInfo)
        {
            List<Vector3> emptyBuildings = new List<Vector3>();

            foreach (var building in knownEnemyBuildings)
            {
                BaseBuildingController worldBuilding = opponentPlayerInfo.BuildingManager.GetBuildingAtPosition(building.Key);

                if (worldBuilding == null)
                {
                    emptyBuildings.Add(building.Key);
                }
            }

            foreach (var emptyBuilding in emptyBuildings)
            {
                knownEnemyBuildings.Remove(emptyBuilding);
            }
        }

        private void UnregisterEmptyEnemyUnit(PlayerInfo opponentPlayerInfo)
        {
            List<Vector3> emptyUnits = new List<Vector3>();

            foreach (var unit in knownEnemyUnits)
            {
                BaseUnitController worldUnit = opponentPlayerInfo.MilitaryUnitManager.GetUnitAtPosition(unit.Key);

                if (worldUnit == null)
                {
                    emptyUnits.Add(unit.Key);
                }
            }

            foreach (var emptyUnit in emptyUnits)
            {
                knownEnemyUnits.Remove(emptyUnit);
            }
        }

        #endregion

        #region Memory Updates

        public void UpdateResourceNodeMemory(List<ResourceNode> nodesInRadius)
        {
            UnregisterEmptyResourceNode();

            foreach (var node in nodesInRadius)
            {
                if (!knownResourceNodes.ContainsKey(node.GetPosition()))
                {
                    RegisterResourceNodeSeen(node.GetResourceType(), node.GetPosition());
                    DebugCalculateResourceNodeExplored(); // for debugging purposes
                }
            }
        }

        // for debugging purposes, calculate the number of explored resource nodes
        private void DebugCalculateResourceNodeExplored()
        {
            int allNodes = ResourceNodeManager.Instance.GetTotalActiveNodes();

            Debug.Log($"Resource Nodes Explored: {knownResourceNodes.Count} / {allNodes}");
        }

        public void UpdateBuildingMemory(List<BaseBuildingController> buildingsInRadius, PlayerInfo opponentPlayerInfo)
        {
            UnregisterEmptyBuilding(opponentPlayerInfo);

            foreach (var building in buildingsInRadius)
            {
                if (!IsPositionInMemory(building.transform.position, knownEnemyBuildings))
                {
                    RegisterEnemyBuildingSeen(building.GetBuildingInfo().buildingType, building.transform.position);
                }
            }
        }

        public void UpdateEnemyUnitMemory(List<BaseUnitController> unitsInRadius, PlayerInfo opponentPlayerInfo)
        {
            UnregisterEmptyEnemyUnit(opponentPlayerInfo);

            foreach (var unit in unitsInRadius)
            {
                if (!IsPositionInMemory(unit.transform.position, knownEnemyUnits))
                {
                    RegisterEnemyUnitSeen(unit.GetUnitInfo().unitType, unit.transform.position);
                }
            }
        }

        #endregion

        #region Building Placement

        public Vector3 FindBuildablePositionNear(Vector3 baseRef, float buildRadius)
        {
            return Vector3.zero; // placeholder
        }

        #endregion

        #region Public API

        public Vector3 GetFrontierPosition()
        {
            return Vector3.zero; // placeholder
        }

        public Dictionary<Vector2Int, float> GetExploredTiles()
        {
            return exploredTiles;
        }

        public Dictionary<Vector3, ResourceNodeMemory> GetKnownResourceNodes()
        {
            return knownResourceNodes;
        }

        public Dictionary<Vector3, EnemyBuildingMemory> GetKnownEnemyBuildings()
        {
            return knownEnemyBuildings;
        }

        public Dictionary<Vector3, EnemyUnitMemory> GetKnownEnemyUnits()
        {
            return knownEnemyUnits;
        }

        public Vector3 GetKnownEnemyBaseLocations()
        {
            return knownEnemyBaseLocation;
        }

        #endregion

        #region Resource Node Public API

        public Vector3 GetNearestResourceNodeFromPosition(Vector3 fromPosition, ResourceType resourceType)
        {
            Vector3 nodePosition = Vector3.zero;

            float closestDistance = Mathf.Infinity;

            foreach (var node in knownResourceNodes)
            {
                if (node.Value.Type == resourceType)
                {
                    float distance = Vector3.Distance(fromPosition, node.Key);

                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        nodePosition = node.Key;
                    }
                }
            }

            return nodePosition;
        }

        #endregion

        #region Helpers

        private bool IsPositionInMemory(Vector3 position, Dictionary<Vector3, EnemyUnitMemory> memory)
        {
            foreach (var entry in memory)
            {
                if (Vector3.Distance(position, entry.Key) < 0.5f)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsPositionInMemory(Vector3 position, Dictionary<Vector3, EnemyBuildingMemory> memory)
        {
            foreach (var entry in memory)
            {
                if (Vector3.Distance(position, entry.Key) < 0.5f)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}