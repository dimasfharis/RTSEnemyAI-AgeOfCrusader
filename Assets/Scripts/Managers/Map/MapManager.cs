using RTS.Buildings.Common;
using RTS.Common.Enums;
using RTS.Core;
using RTS.Data.StrategicData;
using RTS.Units.Common;
using RTS.World.ResourceNodeManagement;
using RTS.World.WorldManagement;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

namespace RTS.Managers.Map
{
    public class MapManager
    {
        // References
        public PlayerInfo playerInfo;
        public TileDatabase tileDatabase;
        private WorldManager worldManager;
        private BuildingManager buildingManager;

        // Memory
        private Dictionary<Vector3, ResourceNodeMemory> knownResourceNodes;
        private Dictionary<Vector3, EnemyBuildingMemory> knownEnemyBuildings;
        private Dictionary<Vector3, EnemyUnitMemory> knownEnemyUnits;
        private Vector3 knownEnemyBasePosition;

        // Map Exploration
        private Dictionary<Vector2Int, float> exploredTiles;

        #region Initialization

        public MapManager(PlayerInfo owner)
        {
            this.playerInfo = owner;
            tileDatabase = playerInfo.GameManager.WorldManager.tileDatabase;
            worldManager = playerInfo.GameManager.WorldManager;
            buildingManager = playerInfo.BuildingManager;

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
                knownEnemyBasePosition = opponent.GameManager.GetStarterBaseLocation(opponent.PlayerNumber);
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
            if (knownEnemyBasePosition != position)
            {
                knownEnemyBasePosition = position;
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
                    //DebugCalculateResourceNodeExplored(); // for debugging purposes
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
                    DebugCalculateEnemyBuildingExplored(); // for debugging purposes
                }
            }
        }

        // for debugging purposes, calculate the number of explored enemy buildings
        private void DebugCalculateEnemyBuildingExplored()
        {
            int allBuildings = playerInfo.GameManager.GetOpponentPlayerInfo(playerInfo.PlayerNumber)[0].BuildingManager.GetAllBuildings().Count;
            //Debug.Log($"Enemy Buildings Explored: {knownEnemyBuildings.Count} / {allBuildings}");
        }

        public void UpdateEnemyUnitMemory(List<BaseUnitController> unitsInRadius, PlayerInfo opponentPlayerInfo)
        {
            UnregisterEmptyEnemyUnit(opponentPlayerInfo);

            foreach (var unit in unitsInRadius)
            {
                if (!IsPositionInMemory(unit.transform.position, knownEnemyUnits))
                {
                    RegisterEnemyUnitSeen(unit.GetUnitInfo().unitType, unit.transform.position);
                    DebugCalculateEnemyUnitExplored(); // for debugging purposes
                }
            }
        }

        // for debugging purposes, calculate the number of explored enemy units
        private void DebugCalculateEnemyUnitExplored()
        {
            int allUnits = playerInfo.GameManager.GetOpponentPlayerInfo(playerInfo.PlayerNumber)[0].MilitaryUnitManager.GetAllUnits().Count;
            //Debug.Log($"Enemy Units Explored: {knownEnemyUnits.Count} / {allUnits}");
        }

        #endregion

        #region Map Public API

        public Vector3 FindBuildablePositionNear(BuildingType buildingType, Vector3 baseRef, float scanRadius, Vector3 trendDirection = default)
        {
            // if trend direction is zero, then make it random
            if (trendDirection == Vector3.zero)
            {
                float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                trendDirection = new Vector3(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle), 0f);
            }
            trendDirection.Normalize();

            List<(Vector3 position, float score)> candidates = new List<(Vector3, float)>();

            int minX = Mathf.FloorToInt(baseRef.x - scanRadius);
            int maxX = Mathf.CeilToInt(baseRef.x + scanRadius);
            int minY = Mathf.FloorToInt(baseRef.y - scanRadius);
            int maxY = Mathf.CeilToInt(baseRef.y + scanRadius);

            // radius scan, begin scan from center/baseRef, then moving to outer radius. trend direction is prioritized when it determined
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    Vector3 candidatePos = new Vector3(x, y, 0f);
                    float distance = Vector3.Distance(baseRef, candidatePos);

                    // Tile must be in radius
                    if (distance > scanRadius)
                        continue;

                    Vector3Int tileCoord = new Vector3Int(x, y, 0);

                    // Tile must can be built
                    if (!worldManager.IsTileImpassible(buildingType, tileCoord))
                    {
                        Vector3 dirFromBase = (candidatePos - baseRef).normalized;

                        // Alignment: score 1.0 if perfectly aligned with trendDirection, -1.0 if opposite
                        float alignment = Vector3.Dot(dirFromBase, trendDirection);

                        // Close to Base: score 1.0 (closest to the base), 0.0 (farthest from the base)
                        float proximity = 1.0f - (distance / scanRadius);

                        // Add randomness
                        float randomNoise = Random.Range(-0.2f, 0.2f);
                        float finalScore = alignment + proximity + randomNoise;

                        candidates.Add((candidatePos, finalScore));
                    }
                }
            }

            if (candidates.Count == 0)
                return Vector3.zero;

            // Sort from highest score
            candidates.Sort((a, b) => b.score.CompareTo(a.score));

            // Pick randomly on top 3 position
            int selectIndex = Random.Range(0, Mathf.Min(3, candidates.Count));
            return candidates[selectIndex].position;
        }

        public BaseBuildingController GetOuterBuildingInDirection(Vector3 baseRef, Vector3 direction)
        {
            List<BaseBuildingController> allBuildings = buildingManager.GetAllBuildings();
            BaseBuildingController baseRefBuilding = buildingManager.GetBuildingByTilePos(baseRef);

            BaseBuildingController outerBuilding = null;
            float outerDistance = float.MinValue;
            float sensitivityAngle = 75f;

            foreach (BaseBuildingController building in allBuildings)
            {
                if (building == baseRefBuilding) continue;

                Vector3 buildingDirection = building.transform.position - baseRef;

                float angle = Vector3.Angle(direction, buildingDirection);
                float distance = Vector3.Distance(baseRef, building.transform.position);
                if (angle < sensitivityAngle && distance > outerDistance)
                {
                    outerBuilding = building;
                    outerDistance = distance;
                }
            }

            return outerBuilding;
        }

        public Dictionary<Vector2Int, float> GetExploredTiles()
        {
            return exploredTiles;
        }

        public List<Vector2Int> GetTilesAround(Vector3 baseRef, float radius)
        {
            List<Vector2Int> tilesAround = new List<Vector2Int>();

            int minX = Mathf.FloorToInt(baseRef.x - radius);
            int maxX = Mathf.CeilToInt(baseRef.x + radius);
            int minY = Mathf.FloorToInt(baseRef.y - radius);
            int maxY = Mathf.CeilToInt(baseRef.y + radius);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    tilesAround.Add(new Vector2Int(x, y));
                }
            }

            return tilesAround;
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

        public Vector3 GetKnownEnemyBasePosition()
        {
            return knownEnemyBasePosition;
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