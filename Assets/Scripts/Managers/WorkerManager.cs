using System.Collections.Generic;
using RTS.Core;
using RTS.Units.Worker;
using RTS.Common.Enums;
using UnityEngine;
using RTS.Buildings.Data;
using RTS.Buildings.Common;
using RTS.Units.Common;
using RTS.Managers.Research;
using RTS.AI.Resources;

namespace RTS.Managers
{
    public class WorkerManager
    {
        [Header("References")]
        private BuildingDatabase buildingDatabase;
        private ResearchManager researchManager;
        private ResourceManagementAIManager resourceManagementAIManager;

        private readonly PlayerInfo playerInfo;

        private readonly List<WorkerUnitController> workers = new List<WorkerUnitController>();

        // Worker Unit Train Needs
        private float checkWorkerTrainNeedsTimer;
        private const float checkWorkerTrainNeedsInterval = 5f;
        private const float idealWorkerCountPerResource = 0.06f;
        private int idealWorkerCountScore;

        // Worker Gathering Resource Allocation
        private Dictionary<ResourceType, float> currentResourceNeedsRatio;
        private Dictionary<ResourceType, int> currentWorkerAllocation;
        private float updateWorkerAllocationTimer;
        private const float updateWorkerAllocationInterval = 12f;

        private const float workerBuildConstructionRatio = 0.2f; // Ratio of workers to construction goal

        #region Initialization

        public WorkerManager(PlayerInfo playerInfo)
        {
            this.playerInfo = playerInfo;
        }

        #endregion

        #region Worker Registration

        public void RegisterWorker(WorkerUnitController worker)
        {
            if (!workers.Contains(worker))
            {
                workers.Add(worker);
                playerInfo.ResourceManager.AddPopulation(1);
            }
        }

        public void UnregisterWorker(WorkerUnitController worker)
        {
            if (workers.Contains(worker))
            {
                workers.Remove(worker);
            }
        }

        #endregion

        #region Update Loop

        public void Tick()
        {
            updateWorkerAllocationTimer += Time.deltaTime;
            checkWorkerTrainNeedsTimer += Time.deltaTime;

            if (updateWorkerAllocationTimer > updateWorkerAllocationInterval)
            {
                updateWorkerAllocationTimer = 0;

                // Update resource needs ratio
                UpdateResourceNeedsRatio();

                // Update worker allocation
                UpdateWorkerAllocation();

                // Update worker action
                UpdateWorkerGatheringAction();
            }

            if (checkWorkerTrainNeedsTimer > checkWorkerTrainNeedsInterval)
            {
                checkWorkerTrainNeedsTimer = 0;

                CheckWorkerNeeds();
            }
        }

        #endregion

        #region Resource Gathering Priority

        private void UpdateResourceNeedsRatio()
        {
            if (resourceManagementAIManager == null)
            {
                resourceManagementAIManager = playerInfo.AIManager.GetResourceManagementAIManager();

                if (resourceManagementAIManager == null)
                    return;

                currentResourceNeedsRatio = resourceManagementAIManager.GetCurrentResourceNeedsRatio();
            }
            else
            {
                currentResourceNeedsRatio = resourceManagementAIManager.GetCurrentResourceNeedsRatio();
            }
        }

        private void UpdateWorkerAllocation()
        {
            if (currentResourceNeedsRatio == null
                || currentResourceNeedsRatio.Count <= 0
                || GetAllUnits().Count <= 0)
                return;

            Dictionary<ResourceType, int> newWorkerAllocation = new Dictionary<ResourceType, int>();
            int allWorkerCount = GetAllUnits().Count;
            int resourceGatheringWorkerCount = Mathf.CeilToInt(allWorkerCount * (1f - workerBuildConstructionRatio));

            int remainingWorkers = resourceGatheringWorkerCount;
            ResourceType highestRatioResource = default;
            float maxRatio = float.MinValue;

            foreach (var resource in currentResourceNeedsRatio)
            {
                int allocated = Mathf.FloorToInt(resource.Value * resourceGatheringWorkerCount);

                newWorkerAllocation[resource.Key] = allocated;
                remainingWorkers -= allocated;

                if (resource.Value > maxRatio)
                {
                    maxRatio = resource.Value;
                    highestRatioResource = resource.Key;
                }
            }

            if (remainingWorkers > 0 && maxRatio > 0)
            {
                newWorkerAllocation[highestRatioResource] += remainingWorkers;
            }

            currentWorkerAllocation = newWorkerAllocation;
        }

        private void UpdateWorkerGatheringAction()
        {
            if (currentResourceNeedsRatio == null
                || currentResourceNeedsRatio.Count <= 0
                || GetAllUnits().Count <= 0)
                return;

            Dictionary<ResourceType, int> workerAllocationNeedsCount = new Dictionary<ResourceType, int>();
            List <WorkerUnitController> freedWorkers = new List<WorkerUnitController>();

            // Determine worker allocation needs
            // and freed resource that has more worker
            foreach (var resource in currentWorkerAllocation)
            {
                List<WorkerUnitController> gatheringWorkers = GetGatheringWorker(resource.Key);

                if (gatheringWorkers.Count < currentWorkerAllocation[resource.Key])
                {
                    if (workerAllocationNeedsCount.ContainsKey(resource.Key))
                    {
                        workerAllocationNeedsCount[resource.Key] += currentWorkerAllocation[resource.Key] - gatheringWorkers.Count;
                    }else
                    {
                        workerAllocationNeedsCount[resource.Key] = currentWorkerAllocation[resource.Key] - gatheringWorkers.Count;
                    }
                }else if (gatheringWorkers.Count > currentWorkerAllocation[resource.Key])
                {
                    List<WorkerUnitController> unitFreeded = StopWorkerFromGatheringResource(resource.Key, gatheringWorkers.Count - currentWorkerAllocation[resource.Key]);
                    freedWorkers.AddRange(unitFreeded);
                }
            }

            // Getting non gathering and construction worker
            List<WorkerUnitController> nonGatheringWorkers = GetAllUnits().FindAll(
                worker => worker.GetCurrentBuildingTemplate() == null
                && worker.GetCurrentResourceType() == ResourceType.None);
            if (nonGatheringWorkers != null || nonGatheringWorkers.Count > 0)
            {
                freedWorkers.AddRange(nonGatheringWorkers);
            }

            // Fulfilling the worker allocation needs determined
            foreach (var workerNeed in workerAllocationNeedsCount)
            {
                if (workerNeed.Value > 0 && freedWorkers.Count >= workerNeed.Value)
                {
                    List<WorkerUnitController> workerToGather = freedWorkers.GetRange(0, workerNeed.Value);
                    AssignWorkerToGather(workerToGather, workerNeed.Key);
                }
            }
        }

        #endregion

        #region Resource Gathering

        public void AssignWorkerToGather(List<WorkerUnitController> workers, ResourceType resourceType)
        {
            foreach (var worker in workers)
            {
                worker.SetWorkerToGather(resourceType);
            }
        }

        private List<WorkerUnitController> StopWorkerFromGatheringResource(ResourceType resourceType, int amount)
        {
            List<WorkerUnitController> workerInResource = GetGatheringWorker(resourceType);

            if (workerInResource.Count < amount)
                return null;

            List<WorkerUnitController> freedUnit = workerInResource.GetRange(0, amount);
            StopWorkersFromGather(freedUnit);

            return freedUnit;
        }

        public void StopWorkersFromGather(List<WorkerUnitController> workers)
        {
            if (workers.Count <= 0)
                return;

            foreach (var worker in workers)
            {
                worker.StopGathering();
            }
        }

        #endregion

        #region Worker Train Needs

        private void CheckWorkerNeeds()
        {
            if (resourceManagementAIManager == null)
                return;

            int allResourceNeedsAmount = resourceManagementAIManager.GetAllResourceNeedsAmount();

            idealWorkerCountScore = Mathf.CeilToInt(allResourceNeedsAmount * idealWorkerCountPerResource);
        }

        public int GetIdealWorkerCount()
        {
            return idealWorkerCountScore;
        }

        #endregion

        #region Building Construction

        public bool TryAssignWorkerToBuild(List<WorkerUnitController> workers, BuildingType buildingType, Vector3 buildingPosition)
        {
            if (workers == null)
                return false;

            if (buildingDatabase == null)
            {
                buildingDatabase = playerInfo.DataManager.buildingDatabase;
            }

            BuildingInfoSO template = buildingDatabase.GetBuildingTemplate(buildingType);

            if (template == null)
            {
                Debug.LogError($"Template not found for {buildingType}");
                return false;
            }

            if (!CanBuildBuilding(buildingType))
                return false;

            var cost = buildingDatabase.GetBuildingCost(buildingType);
            bool canBuild = playerInfo.ResourceManager.SpendResource(cost);

            if (canBuild)
            {
                foreach (var worker in workers)
                {
                    worker.AssignWorkerToBuild(template, buildingPosition);
                }

                return true;
            }

            return false;
        }

        private bool CanBuildBuilding(BuildingType buildingType)
        {
            var cost = buildingDatabase.GetBuildingCost(buildingType);

            if (!playerInfo.ResourceManager.CanAfford(cost))
                return false;

            return true;
        }

        #endregion

        #region Unit Spawning

        public WorkerUnitController SpawnUnitAtBuilding(BaseBuildingController building, UnitType unitType)
        {
            // Prefab Instantiation
            GameObject prefab = playerInfo.DataManager.unitDatabase.GetUnitPrefab(unitType);
            Vector3 spawnPosition = building.transform.position + building.GetBuildingInfo().unitSpawnPoint;
            GameObject unitObj = Object.Instantiate(prefab, spawnPosition, Quaternion.identity);

            // Controller & UnitInfo Initialization
            var controller = unitObj.GetComponent<WorkerUnitController>();
            var unitInfoTemplate = playerInfo.DataManager.unitDatabase.GetUnitTemplate(unitType);
            controller.Init(playerInfo, unitInfoTemplate);

            // Unit Stats Upgrade
            if (researchManager == null)
                researchManager = playerInfo.ResearchManager;
            researchManager.UpgradeStats(controller);

            // Unit Registration
            RegisterWorker(controller);

            return controller;
        }

        #endregion

        #region Stats Research Upgrade

        public void LevelUpCarryingCapacity(float amount)
        {
            foreach (var worker in workers)
            {
                float upgradedValue = worker.GetUnitInfo().carryingCapacity * amount;
                worker.GetUnitInfo().carryingCapacity = (int) upgradedValue;
            }
        }

        public void LevelUpWorkerHP(float amount)
        {
            foreach (var worker in workers)
            {
                worker.GetUnitInfo().unitMaxHealth *= amount;
                worker.GetUnitInfo().unitHealth = worker.GetUnitInfo().unitMaxHealth;
            }
        }

        public void LevelUpAttackPoint(float amount)
        {
            foreach (var worker in workers)
            {
                float upgradedValue = worker.GetUnitInfo().attackDamage * amount;
                worker.GetUnitInfo().attackDamage *= (int) upgradedValue;
            }
        }

        #endregion

        #region Public API

        public List<WorkerUnitController> GetAllUnits()
        {
            return workers;
        }

        public List<WorkerUnitController> GetIdleWorkers()
        {
            List<WorkerUnitController> idleWorkers = new List<WorkerUnitController>();

            foreach (var worker in workers)
            {
                if (worker.IsWorkerIdle())
                {
                    idleWorkers.Add(worker);
                }
            }

            return idleWorkers;
        }

        public List<WorkerUnitController> GetWorkersInRadius(Vector3 position, float radius)
        {
            var workersInArea = new List<WorkerUnitController>();

            foreach (var worker in workers)
            {
                if (Vector3.Distance(worker.transform.position, position) <= radius)
                {
                    workersInArea.Add(worker);
                }
            }

            return workersInArea;
        }

        public BaseUnitController GetWorkerAtPosition(Vector3 position)
        {
            foreach (var worker in workers)
            {
                if (Vector3.Distance(worker.transform.position, position) < 0.7f)
                {
                    return worker;
                }
            }

            return null;
        }

        #endregion

        #region Helper

        private List<WorkerUnitController> GetGatheringWorker(ResourceType resourceType)
        {
            var gatheringWorkers = new List<WorkerUnitController>();

            foreach (var worker in workers)
            {
                if (worker.GetCurrentResourceType() == resourceType)
                    gatheringWorkers.Add(worker);
            }

            return gatheringWorkers;
        }

        #endregion
    }
}