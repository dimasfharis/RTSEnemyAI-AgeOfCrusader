using RTS.Core;
using RTS.Managers;
using RTS.Common.Enums;
using RTS.World.ResourceNodeManagement;
using UnityEngine;
using RTS.Data.StrategicData;
using RTS.Units.Data;
using RTS.Buildings.Data;
using RTS.Managers.Research;
using RTS.Research.Data;
using RTS.AI.Decision;
using RTS.Managers.Map;

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
        private PlayerStrategicData playerStrategicData;

        public UnitDatabase unitDatabase;
        public BuildingDatabase buildingDatabase;
        public ResearchDatabase researchDatabase;
        public AIProfileDatabase aiProfileDatabase;

        private Vector3 basePosition;

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

            unitDatabase = GameObject.FindObjectOfType<UnitDatabase>();
            buildingDatabase = GameObject.FindObjectOfType<BuildingDatabase>();
            researchDatabase = GameObject.FindObjectOfType<ResearchDatabase>();
            aiProfileDatabase = GameObject.FindAnyObjectByType<AIProfileDatabase>();

            playerStrategicData = new PlayerStrategicData(playerInfo, this);

            // dont search here
            //basePosition = buildingManager.GetBuilding(BuildingType.TownCenter).transform.position;
        }

        #endregion

        #region Data Processing & Getter

        #region Worker Data

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
            return playerStrategicData.GetEstimatedEnemyMilitaryPower();
        }

        #endregion

        #region Economy

        public float GetResourceSaturation()
        {
            float worker = GetWorkerCount();
            float ideal = GetIdealWorkerCount();

            if (ideal == 0) return 1f;

            return Mathf.Clamp01(worker / ideal);
        }

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

        #endregion

        #region Building

        public int GetMilitaryBuildingCount()
        {
            return buildingManager.GetBuildingCountByCategory(BuildingCategory.Military);
        }

        public int GetIdealMilitaryBuildingCount()
        {
            return 3;
        }

        #endregion

        #region Defense

        public float GetEnemyThreatLevel()
        {
            float enemyPower = GetEstimatedEnemyMilitaryPower();
            float ourPower = GetOurMilitaryPower();

            if (ourPower == 0) return 1f;

            return Mathf.Clamp01(enemyPower / ourPower);
        }

        public float GetBaseDefenseLevel()
        {
            int towerCount = buildingManager.CountBuilding(BuildingType.GuardTower);
            int canonCount = buildingManager.CountBuilding(BuildingType.CannonTower);

            int unitsNearBase = GetOurUnitsNearBase();

            int maxBaseHealth = buildingDatabase.GetBuildingTemplate(BuildingType.TownCenter).maxHitPoint;
            int baseHealth = buildingManager.GetBuilding(BuildingType.TownCenter)?
                buildingManager.GetBuilding(BuildingType.TownCenter).GetBuildingInfo().currentHitPoint
                : maxBaseHealth;

            float defenseScore = (towerCount * 0.3f) + (canonCount * 0.5f) + (unitsNearBase * 0.2f) + (baseHealth / maxBaseHealth);

            return defenseScore;
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

        public int GetEnemyUnitsNearBase()
        {
            return playerStrategicData.GetEnemyUnitsInRadius(basePosition, 20f);
        }

        public int GetOurUnitsNearBase()
        {
            return militaryUnitManager.GetUnitsInRadius(basePosition, 20f).Count;
        }

        #endregion

        #region Time & Tech

        public float GetGameTimeNormalized()
        {
            float maxTime = 1200f;
            return Mathf.Clamp01(Time.time / maxTime);
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
            int ourNodes = mapManager.GetKnownResourceNodes().Count;
            int totalNodes = resourceNodeManager.GetTotalActiveNodes();

            if (totalNodes == 0) return 1f;

            return Mathf.Clamp01((float) ourNodes / totalNodes);
        }

        #endregion

        #region Harassment

        public float GetEnemyEcoExposureNormalized()
        {
            int exposedWorkers = playerStrategicData.GetExposedEnemyWorkerCount();
            int totalEnemyWorkers = playerStrategicData.GetEstimatedEnemyWorkerCount();

            if (totalEnemyWorkers == 0) return 1f;

            return Mathf.Clamp01((float) exposedWorkers / totalEnemyWorkers);
        }

        #endregion

        #endregion

        #region Public API

        public PlayerStrategicData GetPlayerStrategicData()
        {
            return playerStrategicData;
        }

        #endregion
    }
}