using RTS.AI.Behavior;
using RTS.AI.Decision;
using RTS.AI.GoalManagement;
using RTS.AI.Resources;
using RTS.Buildings.Common;
using RTS.Buildings.Data;
using RTS.Common.Enums;
using RTS.Common.Structs;
using RTS.Core;
using RTS.Managers;
using RTS.Managers.Map;
using RTS.Managers.Research;
using RTS.Research.Data;
using RTS.Units.Data;
using RTS.World.ResourceNodeManagement;
using RTS.World.WorldManagement;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace RTS.Data
{
    public class DataManager
    {
        private PlayerInfo playerInfo;
        private WorkerManager workerManager;
        private ResourceManager resourceManager;
        private BuildingManager buildingManager;
        private MilitaryUnitManager militaryUnitManager;
        private ResearchManager researchManager;
        private MapManager mapManager;
        private ResourceNodeManager resourceNodeManager;
        private WorldManager worldManager;

        private ResourceManagementAIManager resourceManagementAIManager;

        private GoalCoordinator goalCoordinator;

        public UnitDatabase unitDatabase;
        public BuildingDatabase buildingDatabase;
        public ResearchDatabase researchDatabase;
        public AIProfileDatabase aiProfileDatabase;

        #region Initialization

        public DataManager(PlayerInfo owner)
        {
            playerInfo = owner;
            workerManager = owner.WorkerManager;
            resourceManager = owner.ResourceManager;
            buildingManager = owner.BuildingManager;
            militaryUnitManager = owner.MilitaryUnitManager;
            researchManager = owner.ResearchManager;
            mapManager = owner.MapManager;
            resourceNodeManager = owner.ResourceNodeManager;
            worldManager = playerInfo.GameManager.WorldManager;

            unitDatabase = GameObject.FindObjectOfType<UnitDatabase>();
            buildingDatabase = GameObject.FindObjectOfType<BuildingDatabase>();
            researchDatabase = GameObject.FindObjectOfType<ResearchDatabase>();
            aiProfileDatabase = GameObject.FindAnyObjectByType<AIProfileDatabase>();
        }

        #endregion

        #region Data Processing & Getter

        #region Worker

        public int GetWorkerCount()
        {
            return workerManager.GetAllUnits().Count;
        }

        public int GetIdealWorkerCount()
        {
            return workerManager.GetIdealWorkerCount();
        }

        #endregion

        #region Military Data

        public float GetOurMilitaryPower()
        {
            return militaryUnitManager.CalculateTotalPower();
        }

        public float GetEstimatedEnemyMilitaryPower()
        {
            int totalPower = 0;
            var knownEnemyUnits = mapManager.GetKnownEnemyUnits();

            foreach (var unit in knownEnemyUnits)
            {
                int thisUnitPower = unitDatabase.GetUnitAttackPower(unit.Value.UnitType);

                totalPower += thisUnitPower;
            }

            return totalPower;
        }

        #endregion

        #region Economy & Resource

        public float GetTotalResourceStockpile()
        {
            int food = resourceManager.GetAmount(ResourceType.Food);
            int wood = resourceManager.GetAmount(ResourceType.Wood);
            int gold = resourceManager.GetAmount(ResourceType.Gold);
            int stone = resourceManager.GetAmount(ResourceType.Stone);

            return food + wood + gold + stone;
        }

        public float GetResourceIncomeRate()
        {
            float food = resourceManager.GetIncomeRate(ResourceType.Food);
            float wood = resourceManager.GetIncomeRate(ResourceType.Wood);
            float gold = resourceManager.GetIncomeRate(ResourceType.Gold);
            float stone = resourceManager.GetIncomeRate(ResourceType.Stone);

            return (food + wood + gold + stone) / 4f;
        }

        public int GetResourceNeedsAmount()
        {
            if (resourceManagementAIManager == null)
                resourceManagementAIManager = playerInfo.AIManager.GetResourceManagementAIManager();

            return resourceManagementAIManager.GetAllResourceNeedsAmount();
        }

        public int GetKnownResourceNodeCount()
        {
            return mapManager.GetKnownResourceNodes().Count;
        }

        public float CalculateRatioResourceAmountNeeds(BuildingType buildingType, float idealResourceRatioForBuild)
        {
            int count = 0;
            float finalRatio = 0f;

            List<ResourceAmount> resourceNeeds = resourceManager.GetStructureResourceAmount(new Dictionary<BuildingType, int>(){ {buildingType, 1 } });

            if (resourceNeeds == null || resourceNeeds.Count == 0)
                return 0f;

            foreach (var resource in resourceNeeds)
            {
                if (resource.amount <= 0)
                    continue;

                count++;

                int ourResourceAmount = resourceManager.GetAmount(resource.resourceType);

                finalRatio += (float)ourResourceAmount / (resource.amount * idealResourceRatioForBuild);
            }

            if (count == 0)
                return 0f;

            return finalRatio / count;
        }

        public int GetPopulationCapacityAmountNeeds()
        {
            if (resourceManagementAIManager == null)
                resourceManagementAIManager = playerInfo.AIManager.GetResourceManagementAIManager();

            return resourceManagementAIManager.GetPopulationCapacityAmountNeeds();
        }

        #endregion

        #region Building

        public int GetNonDefensiveBuildingCount()
        {
            int economyBuildingCount = buildingManager.GetBuildingCountByCategory(BuildingCategory.Economic);
            int militaryBuildingCount = buildingManager.GetBuildingCountByCategory(BuildingCategory.Military);

            return economyBuildingCount + militaryBuildingCount;
        }

        public int GetBuildingCount(BuildingType buildingType)
        {
            return buildingManager.CountBuilding(buildingType);
        }

        #endregion

        #region Unit Training

        public int GetTrainUnitNeedsCount(BuildingType buildingType)
        {
            if (goalCoordinator == null)
                goalCoordinator = playerInfo.AIManager.GetEnemyBehaviorAIManager().GetGoalCoordinator();

            var trainableUnits = buildingDatabase.GetTrainableUnits(buildingType);
            var unitRequests = goalCoordinator.GetUnitRequest();

            if (trainableUnits == null || unitRequests == null)
                return 0;

            return unitRequests.Values
                .SelectMany(u => u.unitRequests)
                .Where(kvp => trainableUnits.Contains(kvp.Key))
                .Sum(kvp => kvp.Value);
        }

        #endregion

        #region Defense

        public float GetEstimatedMilitaryPowerRatio()
        {
            float ourPower = GetOurMilitaryPower();
            float enemyPower = GetEstimatedEnemyMilitaryPower();

            if (enemyPower == 0) return 1f;

            return Mathf.Clamp01(ourPower / enemyPower);
        }

        public float GetEnemyThreatLevel()
        {
            float enemyPower = GetEstimatedEnemyMilitaryPower();
            float ourPower = GetOurMilitaryPower();

            if (ourPower == 0) return 1f;

            return Mathf.Clamp01(enemyPower / ourPower);
        }

        public float GetBaseDamageLevel()
        {
            float maxBaseHealth = buildingDatabase.GetBuildingTemplate(BuildingType.TownCenter).maxHitPoint;
            float baseCurrentHealth = buildingManager.GetBuilding(BuildingType.TownCenter) ?
                buildingManager.GetBuilding(BuildingType.TownCenter).GetBuildingInfo().currentHitPoint
                : maxBaseHealth;

            float baseDamaged = maxBaseHealth - baseCurrentHealth;

            return Mathf.Clamp01(baseDamaged / maxBaseHealth);
        }

        public int GetActualEnemyUnitsNearBaseInRadius(float radius)
        {
            Vector3 basePosition = buildingManager.GetBuilding(BuildingType.TownCenter) ?
                buildingManager.GetBuilding(BuildingType.TownCenter).transform.position
                : default;

            return GetActualEnemyMilitaryUnitsInRadius(basePosition, radius);
        }

        public int GetEstimatedEnemyUnitsNearBaseInRadius(float radius)
        {
            Vector3 basePosition = buildingManager.GetBuilding(BuildingType.TownCenter) ?
                buildingManager.GetBuilding(BuildingType.TownCenter).transform.position
                : default;

            return GetEstimatedEnemyUnitsInRadius(basePosition, radius);
        }

        public int GetOurUnitsNearBaseInRadius(float radius)
        {
            Vector3 basePosition = buildingManager.GetBuilding(BuildingType.TownCenter) ?
                buildingManager.GetBuilding(BuildingType.TownCenter).transform.position
                : default;

            return militaryUnitManager.GetUnitsInRadius(basePosition, radius).Count;
        }

        public Vector3 GetBaseDefensePosition()
        {
            Vector3 townhallPosition = buildingManager.GetBuilding(BuildingType.TownCenter).transform.position;
            float radius = buildingDatabase.GetBuildingTemplate(BuildingType.TownCenter).lineOfSightRange * 4;
            List<Vector3> directions = GetEnemyAttackDirectionWithinRadius(townhallPosition, radius);

            Vector3 defenseDirection = Vector3.zero;

            foreach (var direction in directions)
            {
                defenseDirection += (Vector3)direction;
            }

            defenseDirection.Normalize();

            BaseBuildingController outerBuildingInDirection = mapManager.GetOuterBuildingInDirection(townhallPosition, defenseDirection);

            return outerBuildingInDirection.transform.position;
        }

        public List<Vector3> GetEnemyAttackDirectionWithinRadius(Vector3 towardPosition, float radius)
        {
            List<Vector3> directions = new List<Vector3>();
            var knownEnemyUnits = mapManager.GetKnownEnemyUnits();

            // if not seen the enemy yet, get enemy base direction
            if (knownEnemyUnits == null || knownEnemyUnits.Count <= 0)
            {
                List<PlayerInfo> enemyPlayerInfo = playerInfo.GameManager.GetOpponentPlayerInfo(playerInfo.PlayerNumber);

                if (enemyPlayerInfo != null && enemyPlayerInfo.Count > 0)
                {
                    Vector3 enemyBasePosition = enemyPlayerInfo[0].BuildingManager.GetBuilding(BuildingType.TownCenter).transform.position;
                    Vector3 dirToBase = (enemyBasePosition - towardPosition).normalized;
                    directions.Add(dirToBase);
                }

                return directions;
            }

            // Take directions towards towardPosition for each enemy unit in radius
            foreach (var enemyUnit in knownEnemyUnits)
            {
                float distance = Vector3.Distance(towardPosition, enemyUnit.Key);
                if (distance <= radius && distance > 0.01f)
                {
                    Vector3 dirToEnemy = (enemyUnit.Key - towardPosition).normalized;
                    directions.Add(dirToEnemy);
                }
            }

            return directions;
        }

        #endregion

        #region Time & Tech

        public float GetGameTimeNormalized()
        {
            float maxTime = 1200f;
            return Mathf.Clamp01(Time.time / maxTime);
        }

        public float GetElapsedTimeSinceLastScout()
        {
            float recentTime = Time.time;

            if (goalCoordinator == null)
                goalCoordinator = playerInfo.AIManager.GetEnemyBehaviorAIManager().GetGoalCoordinator();

            AIGoal latestScoutGoal = goalCoordinator.GetLatestGoalExecuted(AIGoalType.AssignScout);

            float latestScoutExecutedTime = latestScoutGoal != null ? latestScoutGoal.timeExecuted : 0f;

            return recentTime - latestScoutExecutedTime;
        }

        public float GetTechProgressNormalized()
        {
            int completed = researchManager.GetResearchedType().Count;
            int total = researchManager.GetTotalResearch().Count;

            if (total == 0) return 0f;

            return Mathf.Clamp01((float) completed / total);
        }

        #endregion

        #region Map Control

        public float GetMapControlValue()
        {
            int knownTiles = mapManager.GetExploredTiles().Count(x => x.Value > 0f);
            int totalTiles = mapManager.GetExploredTiles().Count;

            if (totalTiles == 0) return 1f;

            return Mathf.Clamp01((float) knownTiles / totalTiles);
        }

        public Vector3 GetUnexploredTilesRandomly()
        {
            var unexploredTiles = mapManager.GetExploredTiles().Where(x => x.Value == 0f);
            
            List<PlayerInfo> enemyPlayerInfo = playerInfo.GameManager.GetOpponentPlayerInfo(playerInfo.PlayerNumber);
            Vector3 enemyBasePosition = enemyPlayerInfo[0].BuildingManager.GetBuilding(BuildingType.TownCenter).transform.position;
            List<Vector2Int> tilesAround = mapManager.GetTilesAround(enemyBasePosition, 10f);

            // avoid tiles around enemy base
            unexploredTiles = unexploredTiles.Where(x => !tilesAround.Contains(x.Key));

            var validTilesList = unexploredTiles.ToList();

            if (validTilesList.Count == 0)
                return Vector3.zero;

            int randomIndex = Random.Range(0, validTilesList.Count);
            Vector2Int chosenTile = validTilesList[randomIndex].Key;
            
            return new Vector3(chosenTile.x, chosenTile.y, 0);
        }

        public Vector3 GetTilesAroundEnemyBaseRandomly()
        {
            List<PlayerInfo> enemyPlayerInfo = playerInfo.GameManager.GetOpponentPlayerInfo(playerInfo.PlayerNumber);
            Vector3 enemyBasePosition = enemyPlayerInfo[0].BuildingManager.GetBuilding(BuildingType.TownCenter).transform.position;
            List<Vector2Int> tilesAround = mapManager.GetTilesAround(enemyBasePosition, 10f);

            // Make sure the tile is within the map
            Tilemap groundTilemap = worldManager.tileDatabase.GetGroundTilemap();
            List<Vector2Int> validTilesList = tilesAround
                .Where(x => groundTilemap.HasTile(new Vector3Int(x.x, x.y, 0)))
                .ToList();

            int randomIndex = Random.Range(0, validTilesList.Count);
            Vector2Int chosenTile = validTilesList[randomIndex];

            return new Vector3(chosenTile.x, chosenTile.y, 0);
        }

        #endregion

        #region Harassment

        public float GetEnemyEcoExposureNormalized()
        {
            int exposedWorkers = GetEstimatedExposedEnemyWorkerCount();
            int totalEnemyWorkers = GetEstimatedEnemyWorkerCount();

            if (totalEnemyWorkers == 0) return 1f;

            return Mathf.Clamp01((float) exposedWorkers / totalEnemyWorkers);
        }

        #endregion

        #region Enemy Info Acknowledge

        public float GetKnownEnemyInfoPercentage()
        {
            int knownEnemyMilitaryUnitCount = GetEstimatedEnemyMilitaryUnitCount();
            int actualEnemyMilitaryUnitCount = GetActualEnemyMilitaryUnitCount();

            int knownEnemyWorkerUnitCount = GetEstimatedEnemyWorkerCount();
            int actualEnemyWorkerUnitCount = GetActualEnemyWorkerCount();

            int knownEnemyBuildingCount = GetEstimatedEnemyBuildingCount();
            int actualEnemyBuildingCount = GetActualEnemyBuildingCount();

            float knownEnemyMilitaryPercentage = actualEnemyMilitaryUnitCount == 0 ? 1f :
                (float) knownEnemyMilitaryUnitCount / actualEnemyMilitaryUnitCount;

            float knownEnemyWorkerPercentage = actualEnemyWorkerUnitCount == 0 ? 1f :
                (float) knownEnemyWorkerUnitCount / actualEnemyWorkerUnitCount;

            float knownEnemyBuildingPercentage = actualEnemyBuildingCount == 0 ? 1f :
                (float) knownEnemyBuildingCount / actualEnemyBuildingCount;

            float totalKnownEnemyInfoPercentage = (knownEnemyMilitaryPercentage + knownEnemyWorkerPercentage + knownEnemyBuildingPercentage) / 3f;

            return totalKnownEnemyInfoPercentage;
        }

        #endregion

        #region Enemy Military Unit

        private int GetActualEnemyMilitaryUnitCount()
        {
            int totalUnits = 0;
            var opponentsUnit = worldManager.GetAllOpponentUnits(playerInfo);

            foreach (var unit in opponentsUnit)
            {
                if (unit.GetUnitInfo().unitType != UnitType.Worker)
                {
                    totalUnits++;
                }
            }

            return totalUnits;
        }

        public int GetEstimatedEnemyMilitaryUnitCount()
        {
            int totalUnits = 0;
            var knownEnemyUnits = mapManager.GetKnownEnemyUnits();

            foreach (var unit in knownEnemyUnits)
            {
                if (unit.Value.UnitType != UnitType.Worker)
                {
                    totalUnits++;
                }
            }

            return totalUnits;
        }

        private int GetActualEnemyMilitaryUnitsInRadius(Vector3 position, float radius)
        {
            int totalUnits = 0;
            var opponentsUnit = worldManager.GetAllOpponentUnits(playerInfo);

            foreach (var unit in opponentsUnit)
            {
                if (Vector3.Distance(position, unit.transform.position) <= radius)
                {
                    if (unit.GetUnitInfo().unitType != UnitType.Worker)
                    {
                        totalUnits++;
                    }
                }
            }

            return totalUnits;
        }

        public int GetEstimatedEnemyUnitsInRadius(Vector3 position, float radius)
        {
            int totalCount = 0;
            var knownEnemyUnits = mapManager.GetKnownEnemyUnits();

            foreach (var unit in knownEnemyUnits)
            {
                if (Vector3.Distance(position, unit.Key) <= radius)
                {
                    totalCount++;
                }
            }

            return totalCount;
        }

        #endregion

        #region Enemy Worker Unit

        private int GetActualEnemyWorkerCount()
        {
            int totalUnits = 0;
            var opponentsUnit = worldManager.GetAllOpponentUnits(playerInfo);

            foreach (var unit in opponentsUnit)
            {
                if (unit.GetUnitInfo().unitType == UnitType.Worker)
                {
                    totalUnits++;
                }
            }

            return totalUnits;
        }

        public int GetEstimatedEnemyWorkerCount()
        {
            int totalWorkers = 0;
            var knownEnemyUnits = mapManager.GetKnownEnemyUnits();

            foreach (var unit in knownEnemyUnits)
            {
                if (unit.Value.UnitType == UnitType.Worker)
                {
                    totalWorkers++;
                }
            }

            return totalWorkers;
        }

        public int GetEstimatedExposedEnemyWorkerCount()
        {
            int exposedEnemyWorkers = 0;
            var knownEnemyUnits = mapManager.GetKnownEnemyUnits();
            Vector3 knownEnemyBasePosition = mapManager.GetKnownEnemyBasePosition();

            foreach (var enemyUnit in knownEnemyUnits)
            {
                if (enemyUnit.Value.UnitType == UnitType.Worker)
                {
                    if (Vector3.Distance(enemyUnit.Key, knownEnemyBasePosition) > 20f)
                    {
                        exposedEnemyWorkers++;
                    }
                }
            }

            return exposedEnemyWorkers;
        }

        public Vector3 GetEstimatedExposedEnemyWorkerPosition()
        {
            Vector3 ecoPosition = Vector3.zero;
            var knownEnemyUnits = mapManager.GetKnownEnemyUnits();
            Vector3 knownEnemyBasePosition = mapManager.GetKnownEnemyBasePosition();

            foreach (var enemyUnit in knownEnemyUnits)
            {
                if (enemyUnit.Value.UnitType == UnitType.Worker)
                {
                    if (Vector3.Distance(enemyUnit.Key, knownEnemyBasePosition) > 20f)
                    {
                        ecoPosition = enemyUnit.Key;
                    }
                }
            }

            return ecoPosition;
        }

        #endregion

        #region Enemy Building

        private int GetActualEnemyBuildingCount()
        {
            int totalBuildings = 0;
            var opponentsBuilding = worldManager.GetAllOpponentBuildings(playerInfo);

            totalBuildings += opponentsBuilding.Count;

            return totalBuildings;
        }

        public int GetEstimatedEnemyBuildingCount()
        {
            int totalBuildings = 0;
            var knownEnemyBuildings = mapManager.GetKnownEnemyBuildings();

            totalBuildings += knownEnemyBuildings.Count;

            return totalBuildings;
        }

        #endregion

        #endregion
    }
}